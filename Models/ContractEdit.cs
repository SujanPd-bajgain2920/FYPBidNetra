namespace FYPBidNetra.Models
{
    public class ContractEdit
    {
        public short ContractId { get; set; }

        public short ConTenderId { get; set; }

        public short ConCompanyId { get; set; }

        public short ConAuctionId { get; set; }

        public short SellerId { get; set; }

        public short BuyerId { get; set; }

        public DateOnly ContractCreateDate { get; set; }

        public string ContractStatus { get; set; }

        public bool SignedBySeller { get; set; }

        public bool SignedByBuyer { get; set; }

        public string SellerSignature { get; set; }

        public string BuyerSignature { get; set; }

        public short CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string EncId { get; set; } = null!;
    }
}
