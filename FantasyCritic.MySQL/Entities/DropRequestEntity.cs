﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain;
using NodaTime;

namespace FantasyCritic.MySQL.Entities
{
    public class DropRequestEntity
    {
        public DropRequestEntity()
        {
            
        }

        public DropRequestEntity(DropRequest domain)
        {
            DropRequestID = domain.DropRequestID;
            PublisherID = domain.Publisher.PublisherID;
            MasterGameID = domain.MasterGame.MasterGameID;
            Timestamp = domain.Timestamp.ToDateTimeUtc();
            Successful = domain.Successful;
        }

        public DropRequestEntity(DropRequest domain, bool successful)
        {
            DropRequestID = domain.DropRequestID;
            PublisherID = domain.Publisher.PublisherID;
            MasterGameID = domain.MasterGame.MasterGameID;
            Timestamp = domain.Timestamp.ToDateTimeUtc();
            Successful = successful;
        }

        public Guid DropRequestID { get; set; }
        public Guid PublisherID { get; set; }
        public Guid MasterGameID { get; set; }
        public DateTime Timestamp { get; set; }
        public bool? Successful { get; set; }

        public DropRequest ToDomain(Publisher publisher, MasterGame masterGame, LeagueYear leagueYear)
        {
            Instant instant = LocalDateTime.FromDateTime(Timestamp).InZoneStrictly(DateTimeZone.Utc).ToInstant();
            return new DropRequest(DropRequestID, publisher, leagueYear, masterGame, instant, Successful);
        }
    }
}
