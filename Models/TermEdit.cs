namespace FYPBidNetra.Models
{
    public class TermEdit
    {
        public short TermId { get; set; }

        public short ContractId { get; set; }

        public string TermDescription { get; set; }

        public DateTime CreatedDate { get; set; }

        public short CreatedBy { get; set; }

        public string EncId { get; set; } = null!;
    }
}
