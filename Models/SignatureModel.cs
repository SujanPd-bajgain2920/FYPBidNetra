namespace FYPBidNetra.Models
{
    public class SignatureModel
    {
        public string SignatureData { get; set; } // Base64 image data
        public string Role { get; set; } // Seller or Buyer
        public int ContractId { get; set; }
    }
}
