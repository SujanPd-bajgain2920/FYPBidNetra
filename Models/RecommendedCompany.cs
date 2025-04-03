namespace FYPBidNetra.Models
{
    public class RecommendedCompany
    {
        public short CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string CompanyType { get; set; } = null!;
        public decimal Rating { get; set; }
    }
}