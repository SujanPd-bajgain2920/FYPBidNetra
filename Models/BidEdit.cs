namespace FYPBidNetra.Models
{
    public class BidEdit
    {
        public short BiddingId { get; set; }

        public short AucBidId { get; set; }

        public decimal BiddingAmount { get; set; }

        public string EncId { get; set; } = null!;

        public decimal BidAmount { get; set; }

        public string Biddername { get; set; }

    }
}
