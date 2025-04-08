namespace FYPBidNetra.Models
{
    public class AuctionContractViewModel
    {
        public AuctionEdit Auction { get; set; }
        public AuctionBidEdit Bid { get; set; }
        public UserListEdit Publisher { get; set; }
        public UserListEdit Bidder { get; set; }
        public ContractEdit Contract { get; set; }
        public List<TermEdit> Terms { get; set; }
    }
}
