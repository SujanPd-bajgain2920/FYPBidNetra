using FYPBidNetra.Models;
using FYPBidNetra.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace BidNetra.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly FypContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;

        public AdminController(FypContext context, IWebHostEnvironment env, DataSecurityProvider key, IDataProtectionProvider provider)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
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


            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTenders = totalTenders;
            ViewBag.TotalAuctions = totalAuctions;

            return View();
        }

        public IActionResult UserList()
        {
            var users = _context.UserLists.ToList();
            var totalAdmins = _context.UserLists.Count(u => u.UserRole == "Admin");
            var totalPublishers = _context.UserLists.Count(u => u.UserRole == "Publisher");
            var totalBidders = _context.UserLists.Count(u => u.UserRole == "Bidders");

            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalBidders = totalBidders;
            ViewBag.TotalPublishers = totalPublishers;

            return View(users);
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
        public IActionResult UpdateVerifiedStatus(long TenderId, string IsVerified)
        {
            var tender = _context.TenderDetails.FirstOrDefault(d => d.TenderId == TenderId);
            if (tender != null)
            {
                tender.IsVerified = IsVerified;
                _context.SaveChanges();

                // Return a JSON response indicating success
                return Json(new { success = true });
            }

            return Json(new { success = false });
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


        [HttpPost]
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


    }
}
