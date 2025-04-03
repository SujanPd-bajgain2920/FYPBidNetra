namespace FYPBidNetra.Models
{
    public class RecommendationResponse
    {
        public string tender_id { get; set; }
        public string tender_type { get; set; }
        public List<RecommendedCompany> recommended_companies { get; set; }
    }
}