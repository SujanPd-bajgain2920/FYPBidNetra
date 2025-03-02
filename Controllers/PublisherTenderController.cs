using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace FYPBidNetra.Controllers
{

    public class PublisherTenderController : Controller
    {

        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public PublisherTenderController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }


        // publisher dashboards
        public IActionResult Index()
        {

            return View();
        }

        // tender related methods
        [Route("tenderpage/{activeTab?}")]
        public IActionResult TenderPage(string activeTab = "TenderList")
        {
            ViewBag.ActiveTab = activeTab; // Set the active tab in ViewBag
            return PartialView("_TenderPage");
        }


        private void UpdateTenderStatuses()
        {
            var currentDate = DateTime.UtcNow.AddMinutes(345); // Current date-time in Nepal time
            var tenders = _context.TenderDetails.ToList();

            foreach (var tender in tenders)
            {

                // Skip tenders that are awarded, as their status should not be changed
                if (tender.TenderStatus == "Awarded")
                {
                    continue;
                }

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




        public IActionResult TenderList()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails
                .Where(t => t.PublishedByUserId == currentUserID)
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

            return PartialView("_TenderList", tenders); // Returns the Tender List partial view
        }

        public IActionResult TenderAward()
        {
            return PartialView("_TenderAward"); // Returns the Tender Award partial view
        }

        public IActionResult OpenTender()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.TenderStatus == "Open" &&
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
            //return Json(tenders);
            return PartialView("_OpenTender", tenders);
        }

        public IActionResult CloseTender()
        {
            UpdateTenderStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var tenders = _context.TenderDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.TenderStatus == "Closed" &&
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
            //return Json(tenders);
            return PartialView("_CloseTender", tenders);
        }

        public IActionResult PublishTender()
        {
            return View("_PublishTender");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublishTender(TenderEdit t)
        {
            //return Json(t);
            try
            {
                // Generate Tender ID
                short maxid;
                if (_context.TenderDetails.Any())
                    maxid = Convert.ToInt16(_context.TenderDetails.Max(x => x.TenderId) + 1);
                else
                    maxid = 1;
                t.TenderId = maxid;

                // Validate and save the file
                if (t.TenderFile != null)
                {
                    if (Path.GetExtension(t.TenderFile.FileName).ToLower() != ".pdf")
                    {
                        ModelState.AddModelError("TenderFile", "Only PDF files are allowed.");
                        return View(t); // Return the view with the validation error
                    }

                    string fileName = "TenderDocument" + Guid.NewGuid() + Path.GetExtension(t.TenderFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "TenderDocument", fileName);

                    // Ensure the directory exists
                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "TenderDocument")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "TenderDocument"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        t.TenderFile.CopyTo(stream);
                    }
                    t.TenderDocument = fileName;
                }

                // Get the current Nepal time
                var currentTime = DateTime.UtcNow.AddMinutes(345);
                var currentDate = DateOnly.FromDateTime(currentTime);

                // Create a new TenderDetail object
                TenderDetail tenderList = new()
                {
                    TenderId = t.TenderId,
                    Title = t.Title,
                    TenderDescription = t.TenderDescription,
                    TenderType = t.TenderType,
                    BudgetEstimation = t.BudgetEstimation,
                    IssuedBy = t.IssuedBy,
                    IssuedDate = currentDate,
                    OpeningDate = t.OpeningDate,
                    ClosingDate = t.ClosingDate,
                    ProjectDuration = t.ProjectDuration,
                    TenderDocument = t.TenderDocument,
                    PublishedByUserId = Convert.ToInt16(User.Identity!.Name),
                    TenderStatus = "Pending",
                    IsVerified = "Pending",
                    AwardStatus = "Pending",

                };

                //return Json(tenderList);
                // Save the tender to the database
                _context.Add(tenderList);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Tender published successfully!";
                return RedirectToAction("TenderPage", "PublisherTender");
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                ModelState.AddModelError("", "An error occurred while publishing the tender. Please try again.");
                return View("_PublishTender", t); // Return the view with the error message
            }
        }

        // view the whole details of tenders
        public IActionResult TenderDetails(string id)
        {

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
                TenderDocument = t.TenderDocument,
                AwardStatus = "Pending",
                EncId = _protector.Protect(t.TenderId.ToString())
            })
            .FirstOrDefault();

            return View(tender);
        }


        public IActionResult MonitorTender(string id)
        {
            UpdateTenderStatuses();

            int tenderid = Convert.ToInt32(_protector.Unprotect(id));

            // Fetch Tender Details
            var tender = _context.TenderDetails
                .Where(t => t.TenderId == tenderid)
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

            if (tender == null)
            {
                return NotFound("Tender not found.");
            }

            // Fetch Applications for the Tender
            var applications = _context.TenderApplications
                .Where(ta => ta.TenderAppllyId == tenderid)
                .Select(ta => new
                {
                    Application = new TenderApplicationEdit
                    {
                        ApplicationId = ta.ApplicationId,
                        ApplicationDocument = ta.ApplicationDocument,
                        ProposedBudget = ta.ProposedBudget,
                        ApplicationStatus = ta.ApplicationStatus,
                        ProposedDuration = ta.ProposedDuration,
                        EncId = _protector.Protect(ta.ApplicationId.ToString())
                    },
                    CompanyApplyId = ta.CompanyApplyId
                })
                .ToList();

            // Fetch Company and Bidder Details
            var companyDetails = _context.Companies
                .Where(c => applications.Select(a => a.CompanyApplyId).Contains(c.CompanyId))
                .Select(c => new
                {
                    Company = new CompanyEdit
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        FullAddress = c.FullAddress,
                        OfficeEmail = c.OfficeEmail,
                        CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                        RegistrationNumber = c.RegistrationNumber,
                        CompanyType = c.CompanyType,
                        Position = c.Position,
                       
                    },
                    UserbidId = c.UserbidId
                })
                .ToList();

            var bidderDetails = _context.UserLists
                .Where(u => companyDetails.Select(c => c.UserbidId).Contains(u.UserId))
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
                .ToList();


            // Creating ViewModel
            var viewModel = new MonitorTenderViewModel
            {
                Tender = tender,
                Applications = applications.Select(a => new TenderApplicationViewModel
                {
                    Application = a.Application,
                    Company = companyDetails.FirstOrDefault(c => c.Company.CompanyId == a.CompanyApplyId)?.Company,
                    User = bidderDetails.FirstOrDefault(b => b.UserId == companyDetails.FirstOrDefault(c => c.Company.CompanyId == a.CompanyApplyId)?.UserbidId)
                }).ToList()
            };

            return View(viewModel);
        }



        public IActionResult ApplicationDetails(string id)
        {
            // Decrypt the application ID
            int appId = Convert.ToInt32(_protector.Unprotect(id));

            // Fetch the Application Details
            var application = _context.TenderApplications
                .Where(ta => ta.ApplicationId == appId)
                .Select(ta => new TenderApplicationEdit
                {
                    ApplicationId = ta.ApplicationId,
                    ApplicationDocument = ta.ApplicationDocument,
                    ProposedBudget = ta.ProposedBudget,
                    ApplicationStatus = ta.ApplicationStatus,
                    ProposedDuration = ta.ProposedDuration,
                    TenderAppllyId = ta.TenderAppllyId,
                    CompanyApplyId = ta.CompanyApplyId,
                    EncId = _protector.Protect(ta.ApplicationId.ToString())

                })
                .FirstOrDefault();

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Fetch Tender Details based on TenderId
            var tender = _context.TenderDetails
                .Where(t => t.TenderId == application.TenderAppllyId)
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

            // Fetch Company Details that applied for the tender
            var company = _context.Companies
                .Where(c => c.CompanyId == application.CompanyApplyId)
                .Select(c => new CompanyEdit
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName,
                    FullAddress = c.FullAddress,
                    OfficeEmail = c.OfficeEmail,
                    CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                    RegistrationNumber = c.RegistrationNumber,
                    CompanyType = c.CompanyType,
                    Position = c.Position,
                    
                    PanDocument = c.PanDocument,
                    PanNumber = c.PanNumber,
                    RegistrationDocument = c.RegistrationDocument,
                    UserbidId = c.UserbidId

                })
                .FirstOrDefault();

            //return Json(company);

            // Fetch Bidder (User) Details
            var bidder = _context.UserLists
                .Where(u => u.UserId == company.UserbidId)
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

            // return Json(bidder);

            // fetch bidder bank details
            var bank = _context.Banks
                .Where(b => b.UserbankId == bidder.UserId)
                .Select(b => new BankEdit
                {
                    BankId = b.BankId,
                    BankName = b.BankName,
                    AccountNumber = b.AccountNumber,
                    AccountType = b.AccountType,
                    AccountHolderName = b.AccountHolderName,
                    UserbankId = b.UserbankId

                })
                .FirstOrDefault();

            // Create ViewModel for passing to the view
            var viewModel = new TenderApplicationViewModel
            {
                Application = application,
                Tender = tender,
                Company = company,
                User = bidder,
                Bank = bank
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AwardTender(long ApplicationId, string ApplicationStatus)
        {

            try
            {
                var application = _context.TenderApplications.FirstOrDefault(a => a.ApplicationId == ApplicationId);

                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found." });
                }

                if (application.ApplicationStatus != "Pending")
                {
                    return Json(new { success = false, message = "Application is not in a pending state." });
                }

                // Update the application status
                application.ApplicationStatus = ApplicationStatus;

                // Update TenderDetails if the application is marked as "Won"
                if (ApplicationStatus == "Won")
                {
                    var tender = _context.TenderDetails.FirstOrDefault(t => t.TenderId == application.TenderAppllyId);
                    if (tender == null)
                    {
                        return Json(new { success = false, message = "Tender details not found." });
                    }

                    var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(345));

                    if (tender != null)
                    {
                        tender.AwardStatus = "Awarded";
                        tender.AwardCompanyId = application.CompanyApplyId;
                        tender.AwardDate = currentDate;

                        _context.Update(tender);

                        // Update all other applications for this tender to "Lost"
                        var otherApplications = _context.TenderApplications
                            .Where(a => a.TenderAppllyId == application.TenderAppllyId && a.ApplicationId != ApplicationId)
                            .ToList();

                        foreach (var app in otherApplications)
                        {
                            app.ApplicationStatus = "Lost";
                        }

                        // Now, create the contract
                        var company = _context.Companies.FirstOrDefault(c => c.CompanyId == application.CompanyApplyId);
                        if (company != null)
                        {
                            short newContractId = 1;
                            if (_context.ContractDetails.Any())
                            {
                                newContractId = (short)(_context.ContractDetails.Max(c => c.ContractId) + 1);
                            }

                            var contract = new ContractDetail
                            {
                                ContractId = newContractId,
                                ConTenderId = tender.TenderId,
                                ConCompanyId = application.CompanyApplyId,

                                SellerId = tender.PublishedByUserId, // Assuming seller ID comes from the tender
                                BuyerId = application.CompanyApplyId, // Buyer is the awarded company
                                ContractCreateDate = currentDate,
                                ContractStatus = "Pending",
                                SignedBySeller = false,
                                SignedByBuyer = false,

                            };

                            _context.Add(contract);
                        }
                    }
                }

                _context.SaveChanges();
                return Json(new
                {
                    success = true,
                    message = "Tender status updated successfully. Contract has been generated.",
                    redirectUrl = Url.Action("Index", "TenderContract")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }



    }
}