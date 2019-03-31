using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Utilities;
using NodaTime;

namespace FantasyCritic.Web.Models.Requests
{
    public class MasterGameRequestRequest
    {
        [Required]
        public string GameName { get; set; }

        public string SteamLink { get; set; }
        public string OpenCriticLink { get; set; }
        public string EstimatedReleaseDate { get; set; }
        public int? EligibilityLevel { get; set; }
        public bool? YearlyInstallment { get; set; }
        public bool? EarlyAccess { get; set; }

        public string RequestNote { get; set; }

        public MasterGameRequest ToDomain(FantasyCriticUser user, Instant requestTimestamp, Maybe<EligibilityLevel> eligibilityLevel)
        {
            int? steamID = null;
            var steamGameIDString = SubstringSearching.GetBetween(SteamLink, "/app/", "/");
            if (steamGameIDString.IsSuccess)
            {
                bool parseResult = int.TryParse(steamGameIDString.Value, out int steamIDResult);
                if (parseResult)
                {
                    steamID = steamIDResult;
                }
            }

            int? openCriticID = null;
            var openCriticGameIDString = SubstringSearching.GetBetween(OpenCriticLink, "/game/", "/");
            if (openCriticGameIDString.IsSuccess)
            {
                bool parseResult = int.TryParse(openCriticGameIDString.Value, out int openCriticIDResult);
                if (parseResult)
                {
                    openCriticID = openCriticIDResult;
                }
            }

            return new MasterGameRequest(Guid.NewGuid(), user, requestTimestamp, RequestNote, GameName, steamID, openCriticID, EstimatedReleaseDate, eligibilityLevel,
                YearlyInstallment, EarlyAccess, false, null, null, Maybe<MasterGame>.None);
        }
    }
}