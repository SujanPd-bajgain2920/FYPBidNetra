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
    public class AuctionContractController : Controller
    {

        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public AuctionContractController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }
        



        public IActionResult Index(string id)
        {


            int auctionBidId = Convert.ToInt32(_protector.Unprotect(id));
            // return Json(auctionBidId);
            // Fetch Application Details
            var application = _context.AuctionBids
                .Where(a => a.BidId == auctionBidId)
                .Select(a => new AuctionBidEdit
                {
                    BidId = a.BidId,
                    AuctionBidId = a.AuctionBidId,
                    BidderId = a.BidderId,
                    BidAmount = a.BidAmount,
                    BidDate = a.BidDate,
                    BidTime = a.BidTime,
                    BidStatus = a.BidStatus,
                    EncId = _protector.Protect(a.BidId.ToString())
                })
                .FirstOrDefault();
            // return Json(application);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Fetch Tender Details
            var auction = _context.AuctionDetails
                .Where(t => t.AuctionId == application.AuctionBidId)
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionDescription = t.AuctionDescription,
                    ItemImage = t.ItemImage,
                    StartingPrice = t.StartingPrice,
                    StartDate = t.StartDate,
                    StartTime = t.StartTime,
                    EndDate = t.EndDate,
                    EndTime = t.EndTime,
                    IsVerified = t.IsVerified,
                    PublishedByUserId = t.PublishedByUserId,
                    AuctionType = t.AuctionType,
                    BuyerId = t.BuyerId,
                    WinningBidAmount = t.WinningBidAmount,
                    AuctionStatus = t.AuctionStatus,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                .FirstOrDefault();

            // return Json(tender);

            // Fetch Publisher (Tender Issuer) Details
            var publisher = _context.UserLists
                .Where(u => u.UserId == auction.PublishedByUserId)
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

            // return Json(company);

            // Fetch Bidder Details
            var bidder = _context.UserLists
                .Where(u => u.UserId == application.BidderId)
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
                .Where(c => c.ConAuctionId == application.AuctionBidId)
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
            var viewModel = new AuctionApplicationViewModel
            {
                Auction = auction,
                AuctinBid = application,
                Publisher = publisher,
                Bidder = bidder,
                Contract = contract,
                Terms = terms,
                Users = userList,
                SelectedUser = userList.FirstOrDefault()
            };
            // return Json(viewModel);
            return View(viewModel);
        }


        [HttpGet]
        public IActionResult AddAuctionTerm(string id)
        {
            int contractid = Convert.ToInt32(_protector.Unprotect(id));

            // Fetch contract details
            var contract = _context.ContractDetails
                .Where(c => c.ContractId == contractid)
                .Select(c => new ContractEdit
                {
                    ContractId = c.ContractId,

                    ConAuctionId = (short)c.ConAuctionId,
                    SellerId = c.SellerId,
                    BuyerId = c.BuyerId,
                    SignedBySeller = c.SignedBySeller,
                    SignedByBuyer = c.SignedByBuyer,
                    EncId = _protector.Protect(c.ContractId.ToString())
                })
                .FirstOrDefault();
            // Fetch TenderApplication details based on ConTenderId from Contract
            var application = _context.AuctionBids
                .Where(a => a.AuctionBidId == contract.ConAuctionId) // Link using TenderAppllyId
                .Select(a => new AuctionBidEdit
                {
                    BidId = a.BidId,
                    EncId = _protector.Protect(a.BidId.ToString())
                })
                .FirstOrDefault();

            if (contract == null)
            {
                return NotFound("Contract not found.");
            }

            // Create ViewModel for passing to the view
            var viewModel = new AuctionApplicationViewModel
            {

                Contract = contract,
                AuctinBid = application

            };

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult AddAuctionTerm(string id, string termDescription)
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

            var application = _context.AuctionBids
               .FirstOrDefault(a => a.AuctionBidId == contract.ConAuctionId);

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
                id = _protector.Protect(application.BidId.ToString())
            });
        }


        [HttpGet]
        public IActionResult SaveAuctionSign()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveAuctionSign([FromBody] SignatureModel model)
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
