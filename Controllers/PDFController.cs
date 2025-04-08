using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;

namespace FYPBidNetra.Controllers
{
    public class PDFController : Controller
    {
        private readonly FypContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;

        public PDFController(FypContext context, IWebHostEnvironment env, DataSecurityProvider key, IDataProtectionProvider provider)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DownloadPdf(string id)
        {
            int applicationid = Convert.ToInt32(_protector.Unprotect(id));
            //return Json(id);
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
                    AwardStatus = t.AwardStatus
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

            // return Json(viewModel);

            // Create ViewModel for passing to the view
            var viewModel = new TenderApplicationViewModel
            {
                Tender = tender,
                Application = application,
                User = publisher,
                Bidder = bidder,
                Company = company,
                Contract = contract,
                Terms = terms
            };

            var pdfResult = new ViewAsPdf("ContractPdf", viewModel)
            {
                FileName = $"{contract.ContractId}_{tender.Title}_Contract.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--disable-smart-shrinking" 
            };

            return pdfResult;

        }

        public IActionResult DownloadAuctionPdf(string id)
        {
            int auctionBidId = Convert.ToInt32(_protector.Unprotect(id));

            // Fetch Bid Details
            var bid = _context.AuctionBids
                .Where(b => b.BidId == auctionBidId)
                .Select(b => new AuctionBidEdit
                {
                    BidId = b.BidId,
                    AuctionBidId = b.AuctionBidId,
                    BidderId = b.BidderId,
                    BidAmount = b.BidAmount,
                    BidDate = b.BidDate,
                    BidTime = b.BidTime,
                    BidStatus = b.BidStatus
                })
                .FirstOrDefault();

            if (bid == null)
            {
                return NotFound("Bid not found.");
            }

            // Fetch Auction Details
            var auction = _context.AuctionDetails
                .Where(a => a.AuctionId == bid.AuctionBidId)
                .Select(a => new AuctionEdit
                {
                    AuctionId = a.AuctionId,
                    Title = a.Title,
                    StartingPrice = a.StartingPrice,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    AuctionType = a.AuctionType,
                    AuctionStatus = a.AuctionStatus,
                    PublishedByUserId = a.PublishedByUserId,
                    WinningBidAmount = a.WinningBidAmount
                })
                .FirstOrDefault();

            // Fetch Publisher Details
            var publisher = _context.UserLists
                .Where(u => u.UserId == auction.PublishedByUserId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone
                })
                .FirstOrDefault();

            // Fetch Bidder Details
            var bidder = _context.UserLists
                .Where(u => u.UserId == bid.BidderId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    EmailAddress = u.EmailAddress,
                    Phone = u.Phone
                })
                .FirstOrDefault();

            // Fetch Contract Details
            var contract = _context.ContractDetails
                .Where(c => c.ConAuctionId == auction.AuctionId)
                .Select(c => new ContractEdit
                {
                    ContractId = c.ContractId,
                    ContractStatus = c.ContractStatus,
                    SignedByBuyer = c.SignedByBuyer,
                    SignedBySeller = c.SignedBySeller,
                    SellerSignature = c.SellerSignature,
                    BuyerSignature = c.BuyerSignature,
                    EncId = _protector.Protect(c.ContractId.ToString())
                })
                .FirstOrDefault();

            // Fetch Terms
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

            // Create ViewModel
            var viewModel = new AuctionContractViewModel
            {
                Auction = auction,
                Bid = bid,
                Publisher = publisher,
                Bidder = bidder,
                Contract = contract,
                Terms = terms
            };

            var pdfResult = new ViewAsPdf("AuctionContractPdf", viewModel)
            {
                FileName = $"{contract.ContractId}_{auction.Title}_Contract.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--disable-smart-shrinking"
            };

            return pdfResult;
        }
    }
}
