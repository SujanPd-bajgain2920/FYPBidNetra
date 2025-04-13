using FYPBidNetra.Models;
using FYPBidNetra.Security;
using FYPBidNetra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using payment_gateway_nepal;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

            var userId = Convert.ToInt16(User.Identity.Name);
           
            var company = _context.Companies
                .FirstOrDefault(c => c.UserbidId == userId);

            var kycStatus = _context.Companies
                .Where(c => c.UserbidId == userId)
                .Select(c => c.IsVerified)
            .FirstOrDefault();

            ViewBag.KycStatus = kycStatus;
            ViewBag.HasCompany = company != null;


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
                    ProposedDuration = ta.ProposedDuration,
                    
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
                     TenderDocument = t.TenderDocument,
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
        public IActionResult AppliedTender(int id) 
        {

            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var company = _context.Companies
                .Where(t => t.UserbidId == currentUserID)
                .FirstOrDefault();

            
            var tenderDetails = _context.TenderDetails
                .Where(t => t.TenderId == id)
                .FirstOrDefault();

            if (tenderDetails == null)
            {
                return NotFound(); 
            }

            
            var model = new TenderApplicationEdit
            {
                TenderAppllyId = tenderDetails.TenderId,
               
                CompanyApplyId = company.CompanyId
               
            };

            return View(model); 
        }

        /* [HttpPost]
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

                 // Create payment record
                 var payment = new Payment
                 {
                     PaymentId = (short)(_context.Payments.Any() ?
                         _context.Payments.Max(p => p.PaymentId) + 1 : 1),
                     PayTenderId = t.TenderAppllyId,
                     PayCompanyId = t.CompanyApplyId,
                     PayToUser = tender.PublishedByUserId,
                     PayByUser = Convert.ToInt16(User.Identity.Name),
                     PaymentAmount = 10,
                     PaymentDate = DateTime.Now,
                     PaymentMethod = "Esewa",
                     PaymentStatus = "Pending"
                 };

                 _context.Payments.Add(payment);
                 await _context.SaveChangesAsync();



                 if (tender?.PublishedByUser?.EmailAddress != null)
                 {
                     // Send email to publisher
                     string emailBody = $@"
                         <!DOCTYPE html>
                         <html>
                         <head>
                             <style>
                                 body {{
                                     font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                                     line-height: 1.6;
                                     color: #333;
                                     margin: 0;
                                     padding: 0;
                                     background-color: #f5f7fa;
                                 }}
                                 .email-container {{
                                     max-width: 600px;
                                     margin: 20px auto;
                                     background: white;
                                     border-radius: 8px;
                                     box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                                     overflow: hidden;
                                 }}
                                 .email-header {{
                                     background: linear-gradient(135deg, #1e40af, #1e3a8a);
                                     color: white;
                                     padding: 25px;
                                     text-align: center;
                                 }}
                                 .email-content {{
                                     padding: 30px;
                                 }}
                                 .info-table {{
                                     width: 100%;
                                     border-collapse: collapse;
                                     margin: 20px 0;
                                 }}
                                 .info-table td {{
                                     padding: 10px;
                                     border-bottom: 1px solid #e5e7eb;
                                 }}
                                 .info-table td:first-child {{
                                     font-weight: bold;
                                     color: #4b5563;
                                     width: 35%;
                                 }}
                                 .action-button {{
                                     display: inline-block;
                                     background: linear-gradient(135deg, #1e40af, #1e3a8a);
                                     color: white !important;
                                     text-decoration: none;
                                     padding: 12px 24px;
                                     border-radius: 6px;
                                     margin: 20px 0;
                                 }}
                                 .status-badge {{
                                     display: inline-block;
                                     padding: 5px 10px;
                                     border-radius: 20px;
                                     font-weight: bold;
                                     background-color: #fef3c7;
                                     color: #92400e;
                                 }}
                                 .email-footer {{
                                     background-color: #f9fafb;
                                     padding: 15px;
                                     text-align: center;
                                     font-size: 14px;
                                     color: #6b7280;
                                     border-top: 1px solid #e5e7eb;
                                 }}
                             </style>
                         </head>
                         <body>
                             <div class='email-container'>
                                 <div class='email-header'>
                                     <h2>New Tender Proposal Received</h2>
                                 </div>
                                 <div class='email-content'>
                                     <p>Dear Publisher,</p>
                                     <p>A new proposal has been submitted for your tender:</p>

                                     <table class='info-table'>
                                         <tr>
                                             <td>Tender Title:</td>
                                             <td>{tender.Title}</td>
                                         </tr>
                                         <tr>
                                             <td>Company Name:</td>
                                             <td>{company?.CompanyName}</td>
                                         </tr>
                                         <tr>
                                             <td>Proposed Budget:</td>
                                             <td>{t.ProposedBudget:C}</td>
                                         </tr>
                                         <tr>
                                             <td>Proposed Duration:</td>
                                             <td>{t.ProposedDuration}</td>
                                         </tr>
                                         <tr>
                                             <td>Submission Date:</td>
                                             <td>{DateTime.Now.ToString("dd MMM yyyy")}</td>
                                         </tr>
                                         <tr>
                                             <td>Status:</td>
                                             <td><span class='status-badge'>Pending Review</span></td>
                                         </tr>
                                     </table>

                                     <p>Please review this proposal at your earliest convenience.</p>

                                     <p>You can accept or reject this proposal after reviewing all details.</p>
                                 </div>
                                 <div class='email-footer'>
                                     <p>This is an automated message from BidNetra. Please do not reply to this email.</p>
                                     <p>&copy; {DateTime.Now.Year} BidNetra. All rights reserved.</p>
                                 </div>
                             </div>
                         </body>
                         </html>";

                     await _emailService.SendEmailAsync(
                         tender.PublishedByUser.EmailAddress,
                         "New Tender Proposal Received",
                         emailBody);
                 }

                 return RedirectToAction("Index", "Payment");
                 //return View(t);

             }
             catch (Exception ex)
             {

                 // Return a generic error message to the user
                 ModelState.AddModelError("", "An error occurred while submitting the proposal. Please try again.");
                 return View("AppliedTender", t); // Return the view with the error message
             }
         }*/


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppliedTender(TenderApplicationEdit t)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(t);
                }

                // Save file to temp location
                if (t.TenderFile != null)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"tender_{Guid.NewGuid()}");
                    using (var stream = System.IO.File.Create(tempPath))
                    {
                        await t.TenderFile.CopyToAsync(stream);
                    }
                    HttpContext.Session.SetString("TenderTempPath", tempPath);
                    HttpContext.Session.SetString("TenderFileName", t.TenderFile.FileName);
                }

                // Generate the next ApplicationId
                short maxid = _context.TenderApplications.Any()
                    ? Convert.ToInt16(_context.TenderApplications.Max(x => x.ApplicationId) + 1)
                    : (short)1;

                t.ApplicationId = maxid;

                // Store tender application data in session
                var tenderData = new
                {
                    t.ApplicationId,
                    t.TenderAppllyId,
                    t.CompanyApplyId,
                    t.ProposedBudget,
                    t.ProposedDuration,
                    t.ApplicationStatus
                };

                HttpContext.Session.SetString("TenderApplicationData", JsonConvert.SerializeObject(tenderData));

                // Get tender and company details for payment
                var tender = await _context.TenderDetails
                    .Include(td => td.PublishedByUser)
                    .FirstOrDefaultAsync(td => td.TenderId == t.TenderAppllyId);

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == t.CompanyApplyId);

                // Create payment details
                var paymentDetails = new
                {
                    TenderId = t.TenderAppllyId,
                    CompanyId = t.CompanyApplyId,
                    PayToUser = tender.PublishedByUserId,
                    PayByUser = Convert.ToInt16(User.Identity.Name),
                    Amount = 10
                };

                HttpContext.Session.SetString("PendingPayment", JsonConvert.SerializeObject(paymentDetails));

                // Initiate payment
                PaymentManager paymentManager = new PaymentManager(
                    PaymentMethod.eSewa,
                    PaymentVersion.v2,
                    PaymentMode.Sandbox,
                    "8gBm/:&EnhH.1/q"
                );

                string currentUrl = new Uri($"{Request.Scheme}://{Request.Host}").AbsoluteUri;
                string referenceId = $"bid-{DateTime.Now.Ticks}";

                dynamic request = new
                {
                    Amount = 10,
                    TaxAmount = 0,
                    TotalAmount = 10,
                    TransactionUuid = referenceId,
                    ProductCode = "EPAYTEST",
                    ProductServiceCharge = 0,
                    ProductDeliveryCharge = 0,
                    SuccessUrl = $"{currentUrl.TrimEnd('/')}/Payment/VerifyPayment",
                    FailureUrl = $"{currentUrl.TrimEnd('/')}/Payment/PaymentFailure",
                    SignedFieldNames = "total_amount,transaction_uuid,product_code"
                };

                var response = await paymentManager.InitiatePaymentAsync<ApiResponse>(request);

                return Json(new
                {
                    success = true,
                    redirectUrl = response.data
                });
            }
            catch (Exception ex)
            {
                CleanTempFiles();
                ModelState.AddModelError("", "An error occurred while submitting the proposal. Please try again.");
                return View(t);
            }
        }

        private void CleanTempFiles()
        {
            if (HttpContext.Session.TryGetValue("TenderTempPath", out var pathBytes))
            {
                var path = Encoding.UTF8.GetString(pathBytes);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
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
