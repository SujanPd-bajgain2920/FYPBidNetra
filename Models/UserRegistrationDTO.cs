namespace FYPBidNetra.Models
{
    public class UserRegistrationDTO
    {
        public short UserId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string EmailAddress { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Gender { get; set; }
        public string? UserPhoto { get; set; }
        public string UserPassword { get; set; }
        public string UserRole { get; set; }
        public short? CompanyId { get; set; }
        public short? BankId { get; set; }
        public string? CompanyName { get; set; }
        public string? FullAddress { get; set; }
        public string? OfficeEmail { get; set; }
        public string? CompanyWebsiteUrl { get; set; }
        public string? CompanyType { get; set; }
        public string? RegistrationDocument { get; set; }
        public string? PanDocument { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? PanNumber { get; set; }
        public decimal? Rating { get; set; }
        public string? Position { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountType { get; set; }
    }
}
