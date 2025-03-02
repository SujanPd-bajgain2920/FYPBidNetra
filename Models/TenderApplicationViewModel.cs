namespace FYPBidNetra.Models
{
    public class TenderApplicationViewModel
    {
        public TenderEdit Tender { get; set; }

        public CompanyEdit Company { get; set; }
        public TenderApplicationEdit Application { get; set; }

        public UserListEdit User { get; set; }

        public List<UserListEdit> Users { get; set; }

        public UserListEdit Publisher { get; set; }

        public UserListEdit Bidder { get; set; }

        public BankEdit Bank { get; set; }

        public ContractEdit Contract { get; set; }

        public TermEdit Term { get; set; }

        public List<TermEdit> Terms { get; set; } = new List<TermEdit>();

        public UserListEdit SelectedUser { get; set; }
    }
}
