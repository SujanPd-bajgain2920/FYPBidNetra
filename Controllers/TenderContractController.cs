using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using FYPBidNetra.Models;
using FYPBidNetra.Security;


namespace FYPBidNetra.Controllers
{
    public class TenderContractController : Controller
    {

        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public TenderContractController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }
        public IActionResult Index(string id)
        {


            int applicationid = Convert.ToInt32(_protector.Unprotect(id));
            // return Json(applicationid);
            // Fetch Application Details
            var application = _context.TenderApplications
                .Where(a => a.ApplicationId == applicationid)
                .Select(a => new TenderApplicationEdit
                {
                    ApplicationId = a.ApplicationId,
                    ApplicationDocument = a.ApplicationDocument,
                    ProposedBudget = a.ProposedBudget,
                    ApplicationStatus = a.ApplicationStatus,
                    ProposedDuration = a.ProposedDuration,
                    TenderAppllyId = a.TenderAppllyId,
                    CompanyApplyId = a.CompanyApplyId,
                    EncId = _protector.Protect(a.ApplicationId.ToString())
                })
                .FirstOrDefault();
            // return Json(application);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Fetch Tender Details
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
                    AwardCompanyId = t.AwardCompanyId,
                    AwardDate = t.AwardDate,
                    TenderDocument = t.TenderDocument,
                    AwardStatus = t.AwardStatus,
                    EncId = _protector.Protect(t.TenderId.ToString())
                })
                .FirstOrDefault();

            // return Json(tender);

            // Fetch Publisher (Tender Issuer) Details
            var publisher = _context.UserLists
                .Where(u => u.UserId == tender.PublishedByUserId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    UserPhoto = u.UserPhoto,
                    Gender = u.Gender,
                    UserRole = u.UserRole

                })
                .FirstOrDefault();
            // return Json(publisher);
            var users = _context.UserLists.ToList();
            var userList = users.Select(e => new UserListEdit
            {
                UserId = e.UserId,
                EncId = _protector.Protect(e.UserId.ToString())
            }).ToList();

            // Fetch Company Details
            var company = _context.Companies
                .Where(c => c.CompanyId == application.CompanyApplyId)
                .Select(c => new CompanyEdit
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName,
                    FullAddress = c.FullAddress,
                    OfficeEmail = c.OfficeEmail,
                    CompanyType = c.CompanyType,
                    PanNumber = c.PanNumber,
                    PanDocument = c.PanDocument,
                    RegistrationNumber = c.RegistrationNumber,
                    RegistrationDocument = c.RegistrationDocument,
                    Position = c.Position,
                   
                    CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                    UserbidId = c.UserbidId
                })
                .FirstOrDefault();
            // return Json(company);

            // Fetch Bidder Details
            var bidder = _context.UserLists
                .Where(u => u.UserId == company.UserbidId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    UserPhoto = u.UserPhoto,
                    Gender = u.Gender,
                    UserRole = u.UserRole
                })
                .FirstOrDefault();
            // return Json(bidder);

            // Fetch Contract ID
            var contract = _context.ContractDetails
                .Where(c => c.ConTenderId == application.TenderAppllyId)
                .Select(c => new ContractEdit
                {
                    ContractId = c.ContractId,
                    EncId = _protector.Protect(c.ContractId.ToString()),
                    SignedByBuyer = c.SignedByBuyer,
                    SignedBySeller = c.SignedBySeller,
                    SellerSignature = c.SellerSignature,
                    BuyerSignature = c.BuyerSignature
                })
                .FirstOrDefault();

            // return Json(contract);

            var terms = _context.Terms
                .Where(t => t.ContractId == contract.ContractId)
                .Select(t => new TermEdit
                {
                    TermId = t.TermId,
                    ContractId = t.ContractId,
                    TermDescription = _protector.Unprotect(t.TermDescription),
                    CreatedDate = t.CreatedDate,
                    CreatedBy = t.CreatedBy
                })
                .OrderBy(t => t.TermId)
                .ToList();


            // return Json(term);

            var currentUserId = Convert.ToInt16(User.Identity!.Name);


            // Create ViewModel for passing to the view
            var viewModel = new TenderApplicationViewModel
            {
                Tender = tender,
                Application = application,
                Publisher = publisher,
                Bidder = bidder,
                Company = company,
                Contract = contract,
                Terms = terms,
                Users = userList,
                SelectedUser = userList.FirstOrDefault()
            };
            // return Json(viewModel);
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult AddTerm(string id)
        {
            int contractid = Convert.ToInt32(_protector.Unprotect(id));
            // Fetch contract details
            var contract = _context.ContractDetails
                .Where(c => c.ContractId == contractid)
                .Select(c => new ContractEdit
                {
                    ContractId = c.ContractId,
                    ConTenderId = (short)c.ConTenderId,
                    SellerId = c.SellerId,
                    BuyerId = c.BuyerId,
                    SignedBySeller = c.SignedBySeller,
                    SignedByBuyer = c.SignedByBuyer,
                    EncId = _protector.Protect(c.ContractId.ToString())
                })
                .FirstOrDefault();
            // Fetch TenderApplication details based on ConTenderId from Contract
            var application = _context.TenderApplications
                .Where(a => a.TenderAppllyId == contract.ConTenderId) // Link using TenderAppllyId
                .Select(a => new TenderApplicationEdit
                {
                    ApplicationId = a.ApplicationId,
                    EncId = _protector.Protect(a.ApplicationId.ToString())
                })
                .FirstOrDefault();

            if (contract == null)
            {
                return NotFound("Contract not found.");
            }

            // Create ViewModel for passing to the view
            var viewModel = new TenderApplicationViewModel
            {

                Contract = contract,
                Application = application

            };

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult AddTerm(string id, string termDescription)
        {
            // return Json(id);
            int contractid = Convert.ToInt32(_protector.Unprotect(id));

            if (string.IsNullOrWhiteSpace(termDescription))
            {
                ModelState.AddModelError("termDescription", "Term description is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ContractId = contractid;
                return View();
            }

            // Fetch the application ID associated with the contract


            // Fetch the contract to ensure it exists
            var contract = _context.ContractDetails.FirstOrDefault(c => c.ContractId == contractid);
            if (contract == null)
            {
                return NotFound("Contract not found.");
            }

            var application = _context.TenderApplications
               .FirstOrDefault(a => a.TenderAppllyId == contract.ConTenderId);

            var maxTermId = _context.Terms.Any() ? _context.Terms.Max(c => c.TermId) + 1 : 1;
            Term term = new()
            {
                TermId = (short)(maxTermId),
                ContractId = (short)contractid,
                TermDescription = _protector.Protect(termDescription),
                CreatedDate = DateTime.Now,
                CreatedBy = Convert.ToInt16(User.Identity!.Name)
            };



            //return Json(term);

            _context.Add(term);
            _context.SaveChanges();




            return RedirectToAction("Index", new
            {
                contractid = _protector.Protect(contractid.ToString()),
                id = _protector.Protect(application.ApplicationId.ToString())
            });
        }


        [HttpGet]
        public IActionResult SaveSign()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveSign([FromBody] SignatureModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.SignatureData))
            {
                return BadRequest(new { message = "Invalid signature data" });
            }

            try
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/signatures");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"{Guid.NewGuid()}.png";
                string filePath = Path.Combine(uploadsFolder, fileName);

                // Convert Base64 string to byte array
                byte[] imageBytes = Convert.FromBase64String(model.SignatureData.Replace("data:image/png;base64,", ""));

                // Save the file
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // Save the file path in the database
                var contract = _context.ContractDetails.FirstOrDefault(c => c.ContractId == model.ContractId);
                if (contract == null)
                {
                    return NotFound(new { message = "Contract not found" });
                }

                if (model.Role == "seller")
                {
                    contract.SellerSignature = fileName;
                    contract.SignedBySeller = true;

                }
                else if (model.Role == "buyer")
                {
                    contract.BuyerSignature = fileName;
                    contract.SignedByBuyer = true;
                }
                contract.ContractStatus = "Active";
                _context.ContractDetails.Update(contract);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Signature saved successfully", fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }


    }
}
