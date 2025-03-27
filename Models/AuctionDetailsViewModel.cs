namespace FYPBidNetra.Models
{
    public class AuctionDetailsViewModel
    {
        public AuctionEdit Auction { get; set; }

        public AuctionDetail AuctionDetails { get; set; }

        public List<AuctionBidEdit> AuctionBid { get; set; }
        public List<AuctionBidEdit> BidHistoryy { get; set; }

        public List<BidEdit> Bid { get; set; }

        public AuctionBidEdit AuctionBidEdit { get; set; }

        public BidHistory BidHistory { get; set; }

        public UserListEdit User { get; set; }

        public UserListEdit Publisher { get; set; }
        public List <UserListEdit> Bidders { get; set; }

        public decimal HighestBidAmount { get; set; }

        public decimal? HighestBidsAmount { get; set; }

        public decimal MinBidAmount { get; set; }

        public List<AuctionBidEdit>? UserBidHistory { get; set; }
    }
}
