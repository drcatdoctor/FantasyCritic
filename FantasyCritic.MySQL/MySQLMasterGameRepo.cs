﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dapper;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.Lib.OpenCritic;
using FantasyCritic.MySQL.Entities;
using MySql.Data.MySqlClient;

namespace FantasyCritic.MySQL
{
    public class MySQLMasterGameRepo : IMasterGameRepo
    {
        private readonly string _connectionString;
        private IReadOnlyList<EligibilityLevel> _eligibilityLevels;
        private Dictionary<Guid, MasterGame> _masterGamesCache;
        private Dictionary<int, Dictionary<Guid, MasterGameYear>> _masterGameYearsCache;

        public MySQLMasterGameRepo(string connectionString)
        {
            _connectionString = connectionString;
            _masterGamesCache = new Dictionary<Guid, MasterGame>();
            _masterGameYearsCache = new Dictionary<int, Dictionary<Guid, MasterGameYear>>();
        }

        public async Task<IReadOnlyList<MasterGame>> GetMasterGames()
        {
            if (_masterGamesCache.Any())
            {
                return _masterGamesCache.Values.ToList();
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                var masterGameResults = await connection.QueryAsync<MasterGameEntity>("select * from tblmastergame;");
                var masterSubGameResults = await connection.QueryAsync<MasterSubGameEntity>("select * from tblmastersubgame;");

                var masterSubGames = masterSubGameResults.Select(x => x.ToDomain()).ToList();
                List<MasterGame> masterGames = new List<MasterGame>();
                foreach (var entity in masterGameResults)
                {
                    EligibilityLevel eligibilityLevel = await GetEligibilityLevel(entity.EligibilityLevel);
                    MasterGame domain = entity.ToDomain(masterSubGames.Where(sub => sub.MasterGameID == entity.MasterGameID),
                            eligibilityLevel);
                    masterGames.Add(domain);
                }

                _masterGamesCache = masterGames.ToDictionary(x => x.MasterGameID, y => y);
                return masterGames;
            }
        }

        public async Task<IReadOnlyList<MasterGameYear>> GetMasterGameYears(int year)
        {
            if (_masterGameYearsCache.ContainsKey(year))
            {
                return _masterGameYearsCache[year].Values.ToList();
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                var masterGameResults = await connection.QueryAsync<MasterGameYearEntity>("select * from vwmastergame where Year = @year;", new { year });
                var masterSubGameResults = await connection.QueryAsync<MasterSubGameEntity>("select * from tblmastersubgame;");

                var masterSubGames = masterSubGameResults.Select(x => x.ToDomain()).ToList();
                List<MasterGameYear> masterGames = new List<MasterGameYear>();
                foreach (var entity in masterGameResults)
                {
                    EligibilityLevel eligibilityLevel = await GetEligibilityLevel(entity.EligibilityLevel);
                    MasterGameYear domain = entity.ToDomain(masterSubGames.Where(sub => sub.MasterGameID == entity.MasterGameID),
                            eligibilityLevel, year);
                    masterGames.Add(domain);
                }

                _masterGameYearsCache[year] = masterGames.ToDictionary(x => x.MasterGame.MasterGameID, y => y);

                return masterGames;
            }
        }

        public async Task<Maybe<MasterGame>> GetMasterGame(Guid masterGameID)
        {
            if (!_masterGamesCache.Any())
            {
                await GetMasterGames();
            }

            _masterGamesCache.TryGetValue(masterGameID, out MasterGame foundMasterGame);
            if (foundMasterGame is null)
            {
                return Maybe<MasterGame>.None;
            }

            return foundMasterGame;
        }

        public async Task<Maybe<MasterGameYear>> GetMasterGameYear(Guid masterGameID, int year)
        {
            if (!_masterGameYearsCache.ContainsKey(year))
            {
                await GetMasterGameYears(year);
            }

            var yearCache = _masterGameYearsCache[year];
            yearCache.TryGetValue(masterGameID, out MasterGameYear foundMasterGame);
            if (foundMasterGame is null)
            {
                return Maybe<MasterGameYear>.None;
            }

            return foundMasterGame;
        }

        public async Task UpdateCriticStats(MasterGame masterGame, OpenCriticGame openCriticGame)
        {
            DateTime? releaseDate = null;
            if (openCriticGame.ReleaseDate.HasValue)
            {
                releaseDate = openCriticGame.ReleaseDate.Value.ToDateTimeUnspecified();
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.ExecuteAsync("update tblmastergame set ReleaseDate = @releaseDate, CriticScore = @criticScore where MasterGameID = @masterGameID",
                    new
                    {
                        masterGameID = masterGame.MasterGameID,
                        releaseDate = releaseDate,
                        criticScore = openCriticGame.Score
                    });
            }
        }

        public async Task UpdateCriticStats(MasterSubGame masterSubGame, OpenCriticGame openCriticGame)
        {
            DateTime? releaseDate = null;
            if (openCriticGame.ReleaseDate.HasValue)
            {
                releaseDate = openCriticGame.ReleaseDate.Value.ToDateTimeUnspecified();
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.ExecuteAsync("update tblmastersubgame set ReleaseDate = @releaseDate, CriticScore = @criticScore where MasterSubGameID = @masterSubGameID",
                    new
                    {
                        masterSubGameID = masterSubGame.MasterSubGameID,
                        releaseDate = releaseDate,
                        criticScore = openCriticGame.Score
                    });
            }
        }

        public async Task CreateMasterGame(MasterGame masterGame)
        {
            var entity = new MasterGameEntity(masterGame);
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(
                    "insert into tblmastergame(MasterGameID,GameName,EstimatedReleaseDate,ReleaseDate,OpenCriticID,CriticScore,MinimumReleaseYear,EligibilityLevel,YearlyInstallment,EarlyAccess,BoxartFileName) VALUES " +
                    "(@MasterGameID,@GameName,@EstimatedReleaseDate,@ReleaseDate,@OpenCriticID,@CriticScore,@MinimumReleaseYear,@EligibilityLevel,@YearlyInstallment,@EarlyAccess,@BoxartFileName);",
                    entity);
            }
        }

        public async Task<EligibilityLevel> GetEligibilityLevel(int eligibilityLevel)
        {
            var eligbilityLevel = await GetEligibilityLevels();
            return eligbilityLevel.Single(x => x.Level == eligibilityLevel);
        }

        public async Task<IReadOnlyList<EligibilityLevel>> GetEligibilityLevels()
        {
            if (_eligibilityLevels != null)
            {
                return _eligibilityLevels;
            }
            using (var connection = new MySqlConnection(_connectionString))
            {
                var entities = await connection.QueryAsync<EligibilityLevelEntity>("select * from tbleligibilitylevel;");
                _eligibilityLevels = entities.Select(x => x.ToDomain()).ToList();
                return _eligibilityLevels;
            }
        }

        public async Task<IReadOnlyList<Guid>> GetAllSelectedMasterGameIDsForYear(int year)
        {
            var sql = "select distinct MasterGameID from tblpublishergame " +
                      "join tblpublisher on(tblpublisher.PublisherID = tblpublishergame.PublisherID) " +
                      "join tblleague on (tblleague.LeagueID = tblpublisher.LeagueID) " +
                      "where Year = @year and tblleague.TestLeague = 0";

            using (var connection = new MySqlConnection(_connectionString))
            {
                IEnumerable<Guid> guids = await connection.QueryAsync<Guid>(sql, new { year });
                return guids.ToList();
            }
        }
    }
}