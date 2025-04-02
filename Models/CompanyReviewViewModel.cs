namespace FYPBidNetra.Models
{
    public class CompanyReviewViewModel
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public decimal? CompanyRating { get; set; }
        public List<RatingEdit> Reviews { get; set; }

        public RatingEdit NewRating { get; set; }

        public string EncId { get; set; }
    }
}
