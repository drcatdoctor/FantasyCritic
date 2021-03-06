using System;
using System.ComponentModel.DataAnnotations;

namespace FantasyCritic.Web.Models.Requests.League
{
    public class PickupBidRequest
    {
        [Required]
        public Guid PublisherID { get; set; }
        [Required]
        public Guid MasterGameID { get; set; }
        [Required]
        public uint BidAmount { get; set; }
    }
}
