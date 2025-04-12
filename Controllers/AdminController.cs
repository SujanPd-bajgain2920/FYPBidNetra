using FYPBidNetra.Models;
using FYPBidNetra.Security;
using FYPBidNetra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BidNetra.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly FypContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AdminController(FypContext context, IWebHostEnvironment env, 
            DataSecurityProvider key, IDataProtectionProvider provider,
            EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _emailService = emailService;
            _configuration = configuration;
        }

        private void UpdateTenderStatuses()
        {
            var currentDate = DateTime.UtcNow.AddMinutes(345); // Current date-time in Nepal time
            var tenders = _context.TenderDetails.ToList();

            foreach (var tender in tenders)
            {
                var openingDate = tender.OpeningDate.ToDateTime(TimeOnly.MinValue);
                var closingDate = tender.ClosingDate.ToDateTime(TimeOnly.MaxValue);

                if (currentDate >= openingDate && currentDate < closingDate)
                {
                    tender.TenderStatus = "Open";
                }
                else if (currentDate >= closingDate)
                {
                    tender.TenderStatus = "Closed";
                }
                else
                {
                    tender.TenderStatus = "Pending";
                }
            }

            _context.SaveChanges();
        }

        // home page
        public IActionResult Index()
        {
            // Fetch statistics for the dashboard
            var totalUsers = _context.UserLists.Count();
            var totalTenders = _context.TenderDetails.Count();
            var totalAuctions = _context.AuctionDetails.Count();
            var totalPending = _context.Companies.Count(k => !k.IsVerified);
            


            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTenders = totalTenders;
            ViewBag.TotalAuctions = totalAuctions;
            ViewBag.TotalCompanies = totalPending;

            return View();
        }

        public IActionResult UserList()
        {
            //var users = _context.UserLists.ToList();
            var users = _context.UserLists
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    UserEncId = _protector.Protect(u.UserId.ToString()),
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    Gender = u.Gender,
                    Phone = u.Phone,
                    EmailAddress = u.EmailAddress,
                    UserPhoto = u.UserPhoto,
                    UserRole = u.UserRole
                })
                .ToList();
            var totalAdmins = _context.UserLists.Count(u => u.UserRole == "Admin");
            var totalPublishers = _context.UserLists.Count(u => u.UserRole == "Publisher");
            var totalBidders = _context.UserLists.Count(u => u.UserRole == "Bidders");

            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalBidders = totalBidders;
            ViewBag.TotalPublishers = totalPublishers;

            return View(users);
        }


        public IActionResult UserDetails(string id)
        {
            int userId = Convert.ToInt32(_protector.Unprotect(id));

            var user = _context.UserLists
                .Where(u => u.UserId == userId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    Gender = u.Gender,
                    Phone = u.Phone,
                    EmailAddress = u.EmailAddress,
                    UserPhoto = u.UserPhoto,
                    UserRole = u.UserRole
                })
                .FirstOrDefault();

            if (user == null)
            {
                return NotFound();
            }

            // Get company details if they exist
            var company = _context.Companies
                .Where(c => c.UserbidId == userId)
                .Select(c => new CompanyEdit
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName,
                    FullAddress = c.FullAddress,
                    OfficeEmail = c.OfficeEmail,
                    CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                    RegistrationNumber = c.RegistrationNumber,
                    RegistrationDocument = c.RegistrationDocument,
                    PanNumber = c.PanNumber,
                    PanDocument = c.PanDocument,
                    CompanyType = c.CompanyType,
                    Position = c.Position,
                    Rating = (decimal)c.Rating
                })
                .FirstOrDefault();

            // Get bank details if they exist
            var bank = _context.Banks
                .Where(b => b.UserbankId == userId)
                .Select(b => new BankEdit
                {
                    BankId = b.BankId,
                    BankName = b.BankName,
                    AccountNumber = b.AccountNumber,
                    AccountType = b.AccountType,
                    AccountHolderName = b.AccountHolderName
                })
                .FirstOrDefault();

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                Company = company,
                Bank = bank
            };

            return View(viewModel);
        }

        // ####################################### Tender ################################
        public IActionResult TenderList()
        {
            UpdateTenderStatuses();
            var tenders = _context.TenderDetails
               .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
               {
                   TenderId = t.TenderId,
                   Title = t.Title,
                   IssuedBy = t.IssuedBy,
                   IssuedDate = t.IssuedDate,
                   TenderType = t.TenderType,
                   TenderStatus = t.TenderStatus,
                   IsVerified = t.IsVerified,
                   OpeningDate = t.OpeningDate,
                   ClosingDate = t.ClosingDate,
                   BudgetEstimation = t.BudgetEstimation,
                   EncId = _protector.Protect(t.TenderId.ToString())
               })
                 .ToList();
            var totalTenders = _context.TenderDetails.Count();
            var totalPendingTenders = _context.TenderDetails.Count(u => u.IsVerified == "Pending");
            var totalVerifiedTenders = _context.TenderDetails.Count(u => u.IsVerified == "Verified");
            var totalNotVerifiedTenders = _context.TenderDetails.Count(u => u.IsVerified == "Not Verified");

            ViewBag.TotalTenders = totalTenders;
            ViewBag.PendingTenders = totalPendingTenders;
            ViewBag.VerifiedTenders = totalVerifiedTenders;
            ViewBag.NotVerifiedTenders = totalNotVerifiedTenders;

            return View(tenders);
        }

   

        [HttpPost]
        public async Task<IActionResult> UpdateVerifiedStatus(long TenderId, string IsVerified)
        {
            try
            {
                var tender = await _context.TenderDetails
                    .Include(t => t.PublishedByUser) // Include publisher details
                    .FirstOrDefaultAsync(d => d.TenderId == TenderId);

                if (tender != null)
                {
                    tender.IsVerified = IsVerified;
                    await _context.SaveChangesAsync();

                    try
                    {
                        if (IsVerified == "Verified")
                        {
                            // Get publisher details
                            var publisher = await _context.UserLists
                                .FirstOrDefaultAsync(u => u.UserId == tender.PublishedByUserId);

                            if (publisher != null)
                            {
                                // Send email to publisher
                                await _emailService.SendEmailAsync(
                                    publisher.EmailAddress,
                                    "Your Tender Has Been Verified",
                                    GeneratePublisherEmailBody(tender));

                                // Get all bidders
                                var bidders = await _context.UserLists
                                    .Where(u => u.UserRole == "Bidder")
                                    .ToListAsync();

                                // Send emails to bidders
                                foreach (var bidder in bidders)
                                {
                                    await _emailService.SendEmailAsync(
                                        bidder.EmailAddress,
                                        "New Tender Available",
                                        GenerateBidderEmailBody(tender));
                                }
                            }
                        }
                        else if (IsVerified == "Not Verified")
                        {
                            var publisher = await _context.UserLists
                                .FirstOrDefaultAsync(u => u.UserId == tender.PublishedByUserId);

                            if (publisher != null)
                            {
                                await _emailService.SendEmailAsync(
                                    publisher.EmailAddress,
                                    "Tender Verification Status Update",
                                    GenerateRejectionEmailBody(tender));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the email error but continue with the response
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                        return Json(new { success = true, warning = "Status updated but email notification failed." });
                    }

                    return Json(new { success = true });
                }

                return Json(new { success = false, error = "Tender not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Helper methods to generate email bodies
        private string GeneratePublisherEmailBody(TenderDetail tender)
        {
            return $@"
        <h2>Tender Verification Update</h2>
        <p>Your tender has been verified by the admin:</p>
        <ul>
            <li><strong>Tender ID:</strong> {tender.TenderId}</li>
            <li><strong>Title:</strong> {tender.Title}</li>
            <li><strong>Status:</strong> Verified</li>
        </ul>
        <p>Your tender is now visible to bidders.</p>";
        }

        private string GenerateBidderEmailBody(TenderDetail tender)
        {
            return $@"
        <h2>New Tender Published</h2>
        <p>A new tender has been published:</p>
        <ul>
            <li><strong>Title:</strong> {tender.Title}</li>
            <li><strong>Type:</strong> {tender.TenderType}</li>
            <li><strong>Budget:</strong> {tender.BudgetEstimation:C}</li>
            <li><strong>Opening Date:</strong> {tender.OpeningDate:d}</li>
            <li><strong>Closing Date:</strong> {tender.ClosingDate:d}</li>
        </ul>
        <p>Login to your account to view more details and submit your bid.</p>";
        }

        private string GenerateRejectionEmailBody(TenderDetail tender)
        {
            return $@"
        <h2>Tender Not Verified</h2>
        <p>Your tender has not been verified by the admin:</p>
        <ul>
            <li><strong>Tender ID:</strong> {tender.TenderId}</li>
            <li><strong>Title:</strong> {tender.Title}</li>
            <li><strong>Status:</strong> Not Verified</li>
        </ul>
        <p>Please contact the admin for more information.</p>";
        }


        public IActionResult AdminTenderDetails(string id)
        {
            UpdateTenderStatuses();
            int tenderid = Convert.ToInt32(_protector.Unprotect(id));

            var tender = _context.TenderDetails
                .Where(t => t.TenderId == tenderid)
                .Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    TenderType = t.TenderType,
                    ProjectDuration = t.ProjectDuration,
                    BudgetEstimation = t.BudgetEstimation,
                    TenderStatus = t.TenderStatus,
                    IsVerified = t.IsVerified,
                    IssuedDate = t.IssuedDate,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    AwardDate = t.AwardDate,
                    AwardCompanyId = t.AwardCompanyId,
                    TenderDescription = t.TenderDescription,
                    PublishedByUserId = t.PublishedByUserId,
                    TenderDocument = t.TenderDocument
                })
                .FirstOrDefault();

            if (tender == null)
            {
                return NotFound();
            }

            var user = _context.UserLists
                .Where(u => u.UserId == tender.PublishedByUserId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone,
                    UserRole = u.UserRole,
                    UserPhoto = u.UserPhoto,
                })
                .FirstOrDefault();

            var viewModel = new TenderDetailsViewModel
            {
                Tender = tender,
                User = user
            };

            return View(viewModel);
        }




        // ########################## Auction List ########################################
        private void UpdateAuctionStatuses()
        {
            var currentDate = DateTime.UtcNow.AddMinutes(345); // Current date-time in Nepal time
            var auctions = _context.AuctionDetails.ToList();

            foreach (var auction in auctions)
            {
                // Combine the date and time
                var openingDateTime = new DateTime(auction.StartDate.Year, auction.StartDate.Month, auction.StartDate.Day,
                                                   auction.StartTime.Hour, auction.StartTime.Minute, auction.StartTime.Second);
                var closingDateTime = new DateTime(auction.EndDate.Year, auction.EndDate.Month, auction.EndDate.Day,
                                                   auction.EndTime.Hour, auction.EndTime.Minute, auction.EndTime.Second);

                // Compare the current date with the auction start and end times
                if (currentDate >= openingDateTime && currentDate < closingDateTime)
                {
                    auction.AuctionStatus = "Active";
                }
                else if (currentDate >= closingDateTime)
                {
                    auction.AuctionStatus = "Completed";
                }
                else
                {
                    auction.AuctionStatus = "Pending";
                }
            }

            _context.SaveChanges();
        }

        public IActionResult AuctionList()
        {
            UpdateAuctionStatuses();
            var auctions = _context.AuctionDetails
               .Select(t => new AuctionEdit
               {
                   AuctionId = t.AuctionId,
                   Title = t.Title,
                   AuctionDescription = t.AuctionDescription,
                   StartingPrice = t.StartingPrice,
                   StartDate = t.StartDate,
                   StartTime = t.StartTime,
                   EndTime = t.EndTime,
                   EndDate = t.EndDate,
                   AuctionType = t.AuctionType,
                   AuctionStatus = t.AuctionStatus,
                   IsVerified = t.IsVerified,
                   EncId = _protector.Protect(t.AuctionId.ToString())
               })
                 .ToList();

            var totalAuctions = _context.AuctionDetails.Count();
            var totalPendingAuctions = _context.AuctionDetails.Count(u => u.IsVerified == "Pending");
            var totalVerifiedAuctions = _context.AuctionDetails.Count(u => u.IsVerified == "Verified");
            var totalNotVerifiedAuctions = _context.AuctionDetails.Count(u => u.IsVerified == "Not Verified");

            ViewBag.TotalAuctions = totalAuctions;
            ViewBag.PendingAuctions = totalPendingAuctions;
            ViewBag.VerifiedAuctions = totalVerifiedAuctions;
            ViewBag.NotVerifiedAuctions = totalNotVerifiedAuctions;

            //return Json(auctions);
            return View(auctions);
        }


        /*[HttpPost]
        public IActionResult UpdateAVerifiedStatus(long AuctionId, string IsVerified)
        {
            var auction = _context.AuctionDetails.FirstOrDefault(d => d.AuctionId == AuctionId);
            if (auction != null)
            {
                auction.IsVerified = IsVerified;
                _context.SaveChanges();

                // Return a JSON response indicating success
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }*/


        [HttpPost]
        public async Task<IActionResult> UpdateAVerifiedStatus(long AuctionId, string IsVerified)
        {
            try
            {
                var auction = await _context.AuctionDetails
                    .Include(t => t.PublishedByUser)
                    .FirstOrDefaultAsync(d => d.AuctionId == AuctionId);

                if (auction != null)
                {
                    auction.IsVerified = IsVerified;
                    await _context.SaveChangesAsync();

                    try
                    {
                        if (IsVerified == "Verified")
                        {
                            // Get publisher details
                            var publisher = await _context.UserLists
                                .FirstOrDefaultAsync(u => u.UserId == auction.PublishedByUserId);

                            if (publisher != null)
                            {
                                // Send email to publisher
                                await _emailService.SendEmailAsync(
                                    publisher.EmailAddress,
                                    "Your Auction Has Been Verified",
                                    GenerateAuctionPublisherEmailBody(auction));

                                // Get all bidders
                                var bidders = await _context.UserLists
                                    .Where(u => u.UserRole == "Bidder")
                                    .ToListAsync();

                                // Send emails to bidders
                                foreach (var bidder in bidders)
                                {
                                    await _emailService.SendEmailAsync(
                                        bidder.EmailAddress,
                                        "New Auction Available",
                                        GenerateAuctionBidderEmailBody(auction));
                                }
                            }
                        }
                        else if (IsVerified == "Not Verified")
                        {
                            var publisher = await _context.UserLists
                                .FirstOrDefaultAsync(u => u.UserId == auction.PublishedByUserId);

                            if (publisher != null)
                            {
                                await _emailService.SendEmailAsync(
                                    publisher.EmailAddress,
                                    "Auction Verification Status Update",
                                    GenerateAuctionRejectionEmailBody(auction));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                        return Json(new { success = true, warning = "Status updated but email notification failed." });
                    }

                    return Json(new { success = true });
                }

                return Json(new { success = false, error = "Auction not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Add these helper methods for email templates
        private string GenerateAuctionPublisherEmailBody(AuctionDetail auction)
        {
            return $@"
        <h2>Auction Verification Update</h2>
        <p>Your auction has been verified by the admin:</p>
        <ul>
            <li><strong>Auction ID:</strong> {auction.AuctionId}</li>
            <li><strong>Title:</strong> {auction.Title}</li>
            <li><strong>Status:</strong> Verified</li>
        </ul>
        <p>Your auction is now visible to bidders.</p>";
        }

        private string GenerateAuctionBidderEmailBody(AuctionDetail auction)
        {
            return $@"
        <h2>New Auction Available</h2>
        <p>A new auction has been published:</p>
        <ul>
            <li><strong>Title:</strong> {auction.Title}</li>
            <li><strong>Type:</strong> {auction.AuctionType}</li>
            <li><strong>Starting Price:</strong> {auction.StartingPrice:C}</li>
            <li><strong>Start Date:</strong> {auction.StartDate:d}</li>
            <li><strong>Start Time:</strong> {auction.StartTime:t}</li>
            <li><strong>End Date:</strong> {auction.EndDate:d}</li>
            <li><strong>End Time:</strong> {auction.EndTime:t}</li>
        </ul>
        <p>Login to your account to view more details and participate in the auction.</p>";
        }

        private string GenerateAuctionRejectionEmailBody(AuctionDetail auction)
        {
            return $@"
        <h2>Auction Not Verified</h2>
        <p>Your auction has not been verified by the admin:</p>
        <ul>
            <li><strong>Auction ID:</strong> {auction.AuctionId}</li>
            <li><strong>Title:</strong> {auction.Title}</li>
            <li><strong>Status:</strong> Not Verified</li>
        </ul>
        <p>Please contact the admin for more information.</p>";
        }

        public IActionResult AdminAuctionDetails(string id)
        {
            UpdateAuctionStatuses();
            int auctionid = Convert.ToInt32(_protector.Unprotect(id));

            var auction = _context.AuctionDetails
                .Where(t => t.AuctionId == auctionid)
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionDescription = t.AuctionDescription,
                    StartingPrice = t.StartingPrice,
                    StartDate = t.StartDate,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    EndDate = t.EndDate,
                    AuctionType = t.AuctionType,
                    AuctionStatus = t.AuctionStatus,
                    IsVerified = t.IsVerified,
                    PublishedByUserId = t.PublishedByUserId,
                    ItemImage = t.ItemImage,
                })
                .FirstOrDefault();

            if (auction == null)
            {
                return NotFound();
            }

            var user = _context.UserLists
                .Where(u => u.UserId == auction.PublishedByUserId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone,
                    UserRole = u.UserRole,
                    UserPhoto = u.UserPhoto,
                })
                .FirstOrDefault();

            var viewModel = new AuctionDetailsViewModel
            {
                Auction = auction,
                User = user
            };

            return View(viewModel);
        }

        /// ########################### Kyc ##############################

        public IActionResult KycList()
        {
            var kycList = _context.UserLists
                .Include(u => u.Companies)
             .Where(u => u.UserRole == "Bidder")
             .Select(u => new KycViewModel
             {
                 UserId = u.UserId,
                 UserEncId = _protector.Protect(u.UserId.ToString()),
                 FirstName = u.FirstName,
                 LastName = u.LastName,
                 EmailAddress = u.EmailAddress,
                 Phone = u.Phone,
                 
                 CompanyId = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.CompanyId)
                     .FirstOrDefault(),
                 BankId = _context.Banks
                     .Where(b => b.UserbankId == u.UserId)
                     .Select(b => b.BankId)
                     .FirstOrDefault(),
                 CompanyName = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.CompanyName)
                     .FirstOrDefault(),
                 RegistrationNumber = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.RegistrationNumber)
                     .FirstOrDefault(),
                 PanNumber = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.PanNumber)
                     .FirstOrDefault(),
                 IsVerified = _context.Companies
                     .Where(c => c.UserbidId == u.UserId)
                     .Select(c => c.IsVerified)
                     .FirstOrDefault(),
                 HasCompany = _context.Companies.Any(c => c.UserbidId == u.UserId),
                 HasBank = _context.Banks.Any(b => b.UserbankId == u.UserId)
             })
             .Where(k => k.HasCompany && k.HasBank && !k.IsVerified)
             .ToList();

            var totalPending = kycList.Count(k => !k.IsVerified);
            var totalVerified = kycList.Count(k => k.IsVerified);

            ViewBag.TotalPending = totalPending;
            ViewBag.TotalVerified = totalVerified;

            return View(kycList);
        }

        public IActionResult KycDetails(string id)
        {
            try
            {
                // Decrypt the user ID
                int userId = Convert.ToInt32(_protector.Unprotect(id));

                // Get user details
                var user = _context.UserLists
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserListEdit
                    {
                        UserId = u.UserId,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        EmailAddress = u.EmailAddress,
                        Phone = u.Phone,
                        Province = u.Province,
                        District = u.District,
                        City = u.City,
                        UserPhoto = u.UserPhoto
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    return NotFound();
                }

                // Get company details
                var company = _context.Companies
                    .Where(c => c.UserbidId == userId)
                    .Select(c => new CompanyEdit
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        FullAddress = c.FullAddress,
                        OfficeEmail = c.OfficeEmail,
                        RegistrationNumber = c.RegistrationNumber,
                        RegistrationDocument = c.RegistrationDocument,
                        PanNumber = c.PanNumber,
                        PanDocument = c.PanDocument,
                        CompanyType = c.CompanyType,
                        IsVerified = c.IsVerified
                    })
                    .FirstOrDefault();

                // Get bank details
                var bank = _context.Banks
                    .Where(b => b.UserbankId == userId)
                    .Select(b => new BankEdit
                    {
                        BankId = b.BankId,
                        BankName = b.BankName,
                        AccountNumber = b.AccountNumber,
                        AccountType = b.AccountType,
                        AccountHolderName = b.AccountHolderName,
                        IsVerified = b.IsVerified
                    })
                    .FirstOrDefault();

                // Create view model
                var viewModel = new KycDetailsViewModel
                {
                    User = user,
                    Company = company,
                    Bank = bank
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error and return error view
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKycStatus(int userId, int companyId, int bankId, bool isVerified)
        {
            try
            {
                // Verify company
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.UserbidId == userId);
                if (company == null)
                {
                    return Json(new { success = false, error = "Company not found" });
                }

                // Verify bank
                var bank = await _context.Banks
                    .FirstOrDefaultAsync(b => b.BankId == bankId && b.UserbankId == userId);
                if (bank == null)
                {
                    return Json(new { success = false, error = "Bank details not found" });
                }

                // Update both records
                company.IsVerified = isVerified;
                bank.IsVerified = isVerified;
                await _context.SaveChangesAsync();

                // Get user details for email
                var user = await _context.UserLists
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null)
                {
                    try
                    {
                        var emailSubject = isVerified ?
                            "Your KYC Has Been Verified" :
                            "KYC Verification Update";

                        var emailBody = isVerified ?
                            GenerateKycApprovedEmailBody(company, user) :
                            GenerateKycRejectedEmailBody(company, user);

                        await _emailService.SendEmailAsync(
                            user.EmailAddress,
                            emailSubject,
                            emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                        return Json(new { success = true, warning = "Status updated but email notification failed." });
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

     

        private string GenerateKycApprovedEmailBody(Company company, UserList user)
        {
            return $@"
            <h2>KYC Verification Approved</h2>
            <p>Dear {user.FirstName} {user.LastName},</p>
            <p>Your KYC has been successfully verified by the admin:</p>
            <ul>
                <li><strong>Company Name:</strong> {company.CompanyName}</li>
                <li><strong>Registration Number:</strong> {company.RegistrationNumber}</li>
            </ul>
            <p>You can now participate in all tenders and auctions.</p>";
        }

        private string GenerateKycRejectedEmailBody(Company company, UserList user)
        {
            return $@"
            <h2>KYC Verification Update</h2>
            <p>Dear {user.FirstName} {user.LastName},</p>
            <p>Your KYC verification status has been updated:</p>
            <ul>
                <li><strong>Company Name:</strong> {company.CompanyName}</li>
                <li><strong>Status:</strong> Not Verified</li>
            </ul>
            <p>Please contact the admin for more information.</p>";
        }
    }
}
