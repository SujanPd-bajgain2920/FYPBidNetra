using FYPBidNetra.Models;
using FYPBidNetra.Security;
using FYPBidNetra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;

namespace FYPBidNetra.Controllers
{
    [Authorize]
    public class BidTenderController : Controller
    {

        private readonly FypContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;
       
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public BidTenderController(FypContext context, IWebHostEnvironment env, 
            DataSecurityProvider key, IDataProtectionProvider provider, EmailService emailService )
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _emailService = emailService;

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

        public IActionResult Index()
        {
            UpdateTenderStatuses();

            var tenders = _context.TenderDetails
                .Where(t =>
                            t.TenderStatus == "Pending" &&
                            t.IsVerified == "Verified")
                .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                  .ToList();

            UpdateAuctionStatuses();
            var auctions = _context.AuctionDetails
                .Where(t =>
                            t.AuctionStatus == "Pending" &&
                            t.IsVerified == "Verified")
                .Select(a => new AuctionEdit
                {
                    AuctionId = a.AuctionId,
                    Title = a.Title,
                    AuctionType = a.AuctionType,
                    StartingPrice = a.StartingPrice,
                    AuctionStatus = a.AuctionStatus,
                    
                    ItemImage = a.ItemImage,
                    IsVerified = a.IsVerified,
                    EncId = _protector.Protect(a.AuctionId.ToString())
                })
                  .ToList();

            ViewBag.Tenders = tenders;
            ViewBag.Auctions = auctions;

            return View();
        }

        // ############################ Tenders ############################

        [Route("tenderbidtab/{activeTab?}")]
        public IActionResult TenderBidTab(string activeTab = "UpcommingTenders")
        {
            ViewBag.ActiveTab = activeTab;
            return PartialView("_TenderBidTab");
        }

        public IActionResult UpcomingTender()
        {
            UpdateTenderStatuses();
            var tenders = _context.TenderDetails
                .Where(t =>
                            t.TenderStatus == "Pending" &&
                            t.IsVerified == "Verified")
                .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    TenderDescription = t.TenderDescription,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                  .ToList();
            return PartialView("_UpcomingTender", tenders);
        }

        public IActionResult OngoingTender()
        {
            UpdateTenderStatuses();

            int currentUserID = Convert.ToInt16(User.Identity!.Name);

            // Get the current user's company ID
            var userCompanyId = _context.Companies
                .Where(c => c.UserbidId == currentUserID)
                .Select(c => c.CompanyId)
                .FirstOrDefault();

            var tenders = _context.TenderDetails
                .Where(t =>
                            t.TenderStatus == "Open" &&
                            t.IsVerified == "Verified" &&
                            !_context.TenderApplications
                            .Any(ta => ta.TenderAppllyId == t.TenderId && ta.CompanyApplyId == userCompanyId))
                .OrderByDescending(t => t.IssuedDate).Select(t => new TenderEdit
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = t.IssuedDate,
                    TenderType = t.TenderType,
                    TenderStatus = t.TenderStatus,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                  .ToList();
            return PartialView("_OngoingTender", tenders);
        }

        public IActionResult ApplyTenderList()
        {
            UpdateTenderStatuses();

            int currentUserID = Convert.ToInt16(User.Identity!.Name);

            // Get the current user's company ID
            var userCompanyId = _context.Companies
                .Where(c => c.UserbidId == currentUserID)
                .Select(c => c.CompanyId)
                .FirstOrDefault();

            var tenders = _context.TenderDetails
                .Where(t => _context.TenderApplications
                    .Any(ta => ta.TenderAppllyId == t.TenderId && ta.CompanyApplyId == userCompanyId))
                    .OrderByDescending(t => t.IssuedDate)
                 .Select(t => new TenderEdit
                 {
                     TenderId = t.TenderId,
                     Title = t.Title,
                     IssuedBy = t.IssuedBy,
                     IssuedDate = t.IssuedDate,
                     TenderType = t.TenderType,
                     TenderStatus = t.TenderStatus,
                     OpeningDate = t.OpeningDate,
                     ClosingDate = t.ClosingDate,
                     IsVerified = t.IsVerified,
                     EncId = _protector.Protect(t.TenderId.ToString()),
                     Application = _context.TenderApplications
                .Where(ta => ta.TenderAppllyId == t.TenderId && ta.CompanyApplyId == userCompanyId)
                .Select(ta => new TenderApplicationEdit
                {
                    ApplicationId = ta.ApplicationId,
                    ApplicationDocument = ta.ApplicationDocument,
                    ProposedBudget = ta.ProposedBudget,
                    ApplicationStatus = ta.ApplicationStatus,
                    ProposedDuration = ta.ProposedDuration
                })
                .FirstOrDefault()
                 })
                 .ToList();

            return PartialView("_ApplyTenderList", tenders);
        }

        public IActionResult ApplyTenderDetails(string id)
        {
            UpdateTenderStatuses();

            int currentUserID = Convert.ToInt16(User.Identity!.Name);


            // Get the current user's company ID
            var userCompanyId = _context.Companies
                .Where(c => c.UserbidId == currentUserID)
                .Select(c => c.CompanyId)
                .FirstOrDefault();

            int tenderid = Convert.ToInt32(_protector.Unprotect(id));

            var tenders = _context.TenderDetails
                .Where(t => _context.TenderApplications
                    .Any(ta => ta.TenderAppllyId == tenderid && ta.CompanyApplyId == userCompanyId))
                    .OrderByDescending(t => t.IssuedDate)
                 .Select(t => new TenderEdit
                 {
                     TenderId = t.TenderId,
                     Title = t.Title,
                     IssuedBy = t.IssuedBy,
                     IssuedDate = t.IssuedDate,
                     TenderType = t.TenderType,
                     TenderStatus = t.TenderStatus,
                     OpeningDate = t.OpeningDate,
                     ClosingDate = t.ClosingDate,
                     ProjectDuration = t.ProjectDuration,
                     BudgetEstimation = t.BudgetEstimation,
                     TenderDescription = t.TenderDescription,
                     IsVerified = t.IsVerified,
                     PublishedByUserId = t.PublishedByUserId,
                     EncId = _protector.Protect(t.TenderId.ToString())
                 })
                 .FirstOrDefault();

            var application = _context.TenderApplications
                  .Where(ta => ta.TenderAppllyId == tenderid && ta.CompanyApplyId == userCompanyId)
                      .Select(ta => new TenderApplicationEdit
                      {
                          ApplicationId = ta.ApplicationId,
                          ApplicationDocument = ta.ApplicationDocument,
                          ProposedBudget = ta.ProposedBudget,
                          ApplicationStatus = ta.ApplicationStatus,
                          ProposedDuration = ta.ProposedDuration,
                          EncId = _protector.Protect(ta.ApplicationId.ToString())
                      })
                      .FirstOrDefault();

            var user = _context.UserLists
                .Where(u => u.UserId == tenders.PublishedByUserId)
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

            var viewModel = new TenderApplicationViewModel
            {
                Tender = tenders,
                User = user,
                Application = application
            };

            return View(viewModel);
        }





        public IActionResult BidderTenderDetails(string id)
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

        [HttpGet]
        public IActionResult AppliedTender(int id) // id will hold the TenderId
        {

            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var company = _context.Companies
                .Where(t => t.UserbidId == currentUserID)
                .FirstOrDefault();

            // Retrieve the tender details using the TenderId (id)
            var tenderDetails = _context.TenderDetails
                .Where(t => t.TenderId == id)
                .FirstOrDefault();

            if (tenderDetails == null)
            {
                return NotFound(); // Tender not found, handle as needed
            }

            // You can now pass the tender details to the view for displaying
            var model = new TenderApplicationEdit
            {
                TenderAppllyId = tenderDetails.TenderId,
                CompanyApplyId = company.CompanyId
                // Map other necessary details from tenderDetails as needed
            };

            return View(model); // Display the tender application form
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppliedTender(TenderApplicationEdit t)
        {

            try
            {
                // Generate the next ApplicationId
                short maxid = _context.TenderApplications.Any()
                    ? Convert.ToInt16(_context.TenderApplications.Max(x => x.ApplicationId) + 1)
                    : (short)1;

                t.ApplicationId = maxid;

                // Check if a TenderFile is uploaded and validate its extension
                if (t.TenderFile != null)
                {
                    if (Path.GetExtension(t.TenderFile.FileName).ToLower() != ".pdf")
                    {
                        return Json(new { success = false, message = "Only PDF files are allowed." });
                    }

                    // Generate a unique filename for the uploaded file
                    string fileName = "ProposalTender" + Guid.NewGuid() + Path.GetExtension(t.TenderFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "ProposalTender", fileName);

                    // Ensure the directory exists, create if it doesn't
                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "ProposalTender")))
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "ProposalTender"));

                    // Save the file to the directory
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        t.TenderFile.CopyTo(stream);
                    }
                    t.ApplicationDocument = fileName;
                }




                // Create a new TenderApplication object
                TenderApplication tenders = new()
                {
                    ApplicationId = t.ApplicationId,
                    TenderAppllyId = t.TenderAppllyId,
                    CompanyApplyId = t.CompanyApplyId,
                    ProposedBudget = t.ProposedBudget,
                    ProposedDuration = t.ProposedDuration,
                    ApplicationDocument = t.ApplicationDocument,
                    ApplicationStatus = "Pending"
                };

                //return Json(tenders);


                _context.Add(tenders);

                //return Json(tenders);

                await _context.SaveChangesAsync();

                // Get tender and company details for the email
                var tender = await _context.TenderDetails
                    .Include(t => t.PublishedByUser)
                    .FirstOrDefaultAsync(t => t.TenderId == tenders.TenderAppllyId);

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == t.CompanyApplyId);

                if (tender?.PublishedByUser?.EmailAddress != null)
                {
                    // Send email to publisher
                    string emailBody = $@"
                <h2>New Tender Proposal Received</h2>
                <p>A new proposal has been submitted for your tender:</p>
                <ul>
                    <li><strong>Tender Title:</strong> {tender.Title}</li>
                    <li><strong>Company Name:</strong> {company?.CompanyName}</li>
                    <li><strong>Proposed Budget:</strong> {t.ProposedBudget:C}</li>
                    <li><strong>Proposed Duration:</strong> {t.ProposedDuration}</li>
                    <li><strong>Status:</strong> Pending Review</li>
                </ul>
                <p>Please login to your account to review the proposal.</p>";

                    await _emailService.SendEmailAsync(
                        tender.PublishedByUser.EmailAddress,
                        "New Tender Proposal Received",
                        emailBody);
                }

                return RedirectToAction("TenderBidTab", "BidTender", new { activeTab = "ApplyTenderList" });
                //return View(t);
               
            }
            catch (Exception ex)
            {

                // Return a generic error message to the user
                ModelState.AddModelError("", "An error occurred while submitting the proposal. Please try again.");
                return View("AppliedTender", t); // Return the view with the error message
            }
        }


        [HttpGet]
        public IActionResult EditApplyTender(string id)
        {


            int applicationId = Convert.ToInt32(_protector.Unprotect(id));


            var application = _context.TenderApplications.Where(x => x.ApplicationId == applicationId).FirstOrDefault();
            TenderApplicationEdit applicationEdit = new()
            {
                ApplicationId = application.ApplicationId,
                ProposedBudget = application.ProposedBudget,
                ProposedDuration = application.ProposedDuration,
                ApplicationDocument = application.ApplicationDocument
            };
            return View(applicationEdit);
        }


        [HttpPost]
        public async Task<IActionResult> EditApplyTender(TenderApplicationViewModel ta)
        {
            var existingTender = _context.TenderApplications.Find(ta.Application.ApplicationId);

            if (existingTender == null)
            {
                return Json(new { success = false, message = "Application not found!" });
            }

            // Handle file upload if provided
            if (ta.Application.TenderFile != null)
            {
                if (Path.GetExtension(ta.Application.TenderFile.FileName).ToLower() != ".pdf")
                {
                    return Json(new { success = false, message = "Only PDF files are allowed." });
                }

                // Generate a unique filename
                string fileName = "ProposalTender" + Guid.NewGuid() + Path.GetExtension(ta.Application.TenderFile.FileName);
                string filePath = Path.Combine(_env.WebRootPath, "ProposalTender", fileName);

                // Ensure directory exists
                string directoryPath = Path.Combine(_env.WebRootPath, "ProposalTender");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Save file
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    await ta.Application.TenderFile.CopyToAsync(stream);
                }

                // Update file path in the existing tender application
                existingTender.ApplicationDocument = fileName;
            }

            // Update other fields
            existingTender.ProposedBudget = ta.Application.ProposedBudget;
            existingTender.ProposedDuration = ta.Application.ProposedDuration;
            existingTender.ApplicationDocument = ta.Application.ApplicationDocument;


            _context.Update(existingTender);
            await _context.SaveChangesAsync();

            // Return JSON response
            return Json(new { success = true, message = "Tender updated successfully!", redirectUrl = Url.Action("TenderBidTab", "Bidder") });
        }



    }
}
