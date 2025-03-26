using System.ComponentModel.DataAnnotations;

namespace FYPBidNetra.Models
{
    public class AuctionEdit
    {
        public short AuctionId { get; set; }

        public string Title { get; set; } = null!;

        public string? AuctionDescription { get; set; }

        public string? ItemImage { get; set; }

        public decimal StartingPrice { get; set; }

        public DateOnly StartDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public DateOnly EndDate { get; set; }

        public string AuctionType { get; set; } = null!;

        public string AuctionStatus { get; set; } = null!;

        public short? BuyerId { get; set; }

        public decimal? WinningBidAmount { get; set; }

        public short PublishedByUserId { get; set; }

        public string IsVerified { get; set; } = null!;

        public string AwardStatus { get; set; } = null!;

        public string EncId { get; set; } = null!;

        [DataType(DataType.Upload)]
        public IFormFile? AuctionFile { get; set; } = null!;

        public AuctionBidEdit Bid { get; set; }

        public UserListEdit? WinnerDetails { get; set; }

       

        public string? PaymentStatus { get; set; }

        public short? PaymentId { get; set; }

    }
}
