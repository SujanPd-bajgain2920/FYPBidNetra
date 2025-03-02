using System.ComponentModel.DataAnnotations;

namespace FYPBidNetra.Models
{
    public class AuctionBidEdit
    {
        public short BidId { get; set; }

        public short AuctionBidId { get; set; }

        public short BidderId { get; set; }

        public decimal BidAmount { get; set; }

        public DateOnly BidDate { get; set; }

        public TimeOnly BidTime { get; set; }

        public string BidStatus { get; set; }

        public List<BidEdit> Bids { get; set; }

        public List<UserListEdit> BidderName { get; set; }

        public string? Biddername { get; set; }

        public string EncId { get; set; } = null!;


    }
}
