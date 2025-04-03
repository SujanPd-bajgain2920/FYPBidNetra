using FYPBidNetra.Services;

namespace FYPBidNetra.Models
{
    public class MonitorTenderViewModel
    {
        public TenderEdit Tender { get; set; }
        public List<TenderApplicationViewModel> Applications { get; set; }
        public List<RecommendedCompany> RecommendedCompanies { get; set; }
    }
}
