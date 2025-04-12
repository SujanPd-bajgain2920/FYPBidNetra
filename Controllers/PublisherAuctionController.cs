using FYPBidNetra.Models;
using FYPBidNetra.Security;
using FYPBidNetra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace FYPBidNetra.Controllers
{

    public class PublisherAuctionController : Controller
    {

        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public PublisherAuctionController(FypContext context, DataSecurityProvider p,
            IDataProtectionProvider provider, IWebHostEnvironment env,
            EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
            _emailService = emailService;
            _configuration = configuration;
        }


        // Auction Related Methods
        [Route("auctionpage/{activeTab?}")]
        public IActionResult AuctionPage(string activeTab = "AuctionList")
        {
            ViewBag.ActiveTab = activeTab;
            return PartialView("_AuctionPage");
        }

        public IActionResult PublishAuction()
        {
            return View("_PublishAuction");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishAuction(AuctionEdit a)
        {
            //return Json(t);
            try
            {

                short maxid;
                if (_context.AuctionDetails.Any())
                    maxid = Convert.ToInt16(_context.AuctionDetails.Max(x => x.AuctionId) + 1);
                else
                    maxid = 1;
                a.AuctionId = maxid;

                // Validate and save the file
                if (a.AuctionFile != null)
                {
                    string fileName = "AuctionImage" + Guid.NewGuid() + Path.GetExtension(a.AuctionFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "AuctionImage", fileName);
                    // Ensure the EmpImage directory exists
                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "AuctionImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "AuctionImage"));
                    }
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        a.AuctionFile.CopyTo(stream);
                    }
                    a.ItemImage = fileName;
                }
                // Create a new AuctionDetail object
                AuctionDetail auctionList = new()
                {
                    AuctionId = a.AuctionId,
                    Title = a.Title,
                    AuctionDescription = a.AuctionDescription,
                    AuctionType = a.AuctionType,
                    StartingPrice = a.StartingPrice,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    ItemImage = a.ItemImage,
                    PublishedByUserId = Convert.ToInt16(User.Identity!.Name),
                    AuctionStatus = "Pending",
                    IsVerified = "Pending",
                    AwardStatus = "Pending"
                };

                //return Json(auctionList);
                // Save the auction to the database
                _context.Add(auctionList);
                await _context.SaveChangesAsync();
                try
                {
                    var adminEmail = _configuration.GetValue<string>("EmailSettings:AdminEmail");
                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        throw new Exception("Admin email is not configured in appsettings.json");
                    }

                    // Send email notification to admin
                    var subject = "New Auction Verification Required";
                    var body = $@"
                    <h2>New Auction Published</h2>
                    <p>A new auction has been published and requires verification:</p>
                    <ul>
                        <li><strong>Auction ID:</strong> {a.AuctionId}</li>
                        <li><strong>Title:</strong> {a.Title}</li>
                        <li><strong>Type:</strong> {a.AuctionType}</li>
                        <li><strong>Starting Price:</strong> {a.StartingPrice:C}</li>
                        <li><strong>Start Date:</strong> {a.StartDate:d}</li>
                        <li><strong>End Date:</strong> {a.EndDate:d}</li>
                    </ul>
                    <p>Please review and verify this auction.</p>";

                    await _emailService.SendEmailAsync(adminEmail, subject, body);
                    TempData["SuccessMessage"] = "Auction published successfully!";
                }
                catch (Exception emailEx)
                {
                    // Log the email error but don't stop the auction creation process
                    Console.WriteLine($"Email sending failed: {emailEx.Message}");
                    TempData["WarningMessage"] = "Auction saved successfully, but notification email could not be sent.";
                }

                TempData["SuccessMessage"] = "Auction published successfully!";
                return RedirectToAction("AuctionPage", "PublisherAuction");
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                ModelState.AddModelError("", "An error occurred while publishing the auction. Please try again.");
                return View("_PublishAuction", a); // Return the view with the error message
            }
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


        public IActionResult AuctionList()
        {
            UpdateAuctionStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var auctions = _context.AuctionDetails
                .Where(a => a.PublishedByUserId == currentUserID)
                .Select(a => new AuctionEdit
                {
                    AuctionId = a.AuctionId,
                    Title = a.Title,
                    AuctionType = a.AuctionType,
                    StartingPrice = a.StartingPrice,
                    AuctionStatus = a.AuctionStatus,
                    IsVerified = a.IsVerified,
                    EncId = _protector.Protect(a.AuctionId.ToString())
                })
                  .ToList();

            return PartialView("_AuctionList", auctions);
        }

        public IActionResult AuctionDetails(string id)
        {
            //return Json(id);
            int auctionid = Convert.ToInt32(_protector.Unprotect(id));

            //return Json(auctionid);
            var auction = _context.AuctionDetails
            .Where(t => t.AuctionId == auctionid)
            .Select(t => new AuctionEdit
            {
                AuctionId = t.AuctionId,
                Title = t.Title,
                AuctionDescription = t.AuctionDescription,
                AuctionType = t.AuctionType,
                StartingPrice = t.StartingPrice,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                ItemImage = t.ItemImage,
                AuctionStatus = t.AuctionStatus,
                IsVerified = t.IsVerified,
                AwardStatus = t.AwardStatus,
                WinningBidAmount = t.WinningBidAmount,
                BuyerId = t.BuyerId,
                WinnerDetails = t.BuyerId != null ? _context.UserLists
                .Where(u => u.UserId == t.BuyerId)
                .Select(u => new UserListEdit
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    EncId = _protector.Protect(u.UserId.ToString())
                })
                .FirstOrDefault() : null,
                EncId = _protector.Protect(t.AuctionId.ToString())

            })
            .FirstOrDefault();
            //return Json(auction);
            return View(auction);
        }


        public IActionResult ActiveAuction()
        {
            UpdateAuctionStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var auctions = _context.AuctionDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.AuctionStatus == "Active" &&
                            t.IsVerified == "Verified")
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionType = t.AuctionType,
                    StartingPrice = t.StartingPrice,
                    AuctionStatus = t.AuctionStatus,
                    IsVerified = t.IsVerified,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                  .ToList();
            //return Json(tenders);
            return PartialView("_ActiveAuction", auctions);
        }

        public IActionResult ClosedAuction()
        {
            UpdateAuctionStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);
            var auctions = _context.AuctionDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.AuctionStatus == "Completed" &&
                            t.IsVerified == "Verified" &&
                            t.AwardStatus == "Pending")
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionType = t.AuctionType,
                    StartingPrice = t.StartingPrice,
                    AuctionStatus = t.AuctionStatus,

                    EndDate = t.EndDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                  .ToList();
            //return Json(tenders);
            return PartialView("_ClosedAuction", auctions);
        }


        /* public IActionResult AwardedAuction()
         {
             UpdateAuctionStatuses();
             int currentUserID = Convert.ToInt16(User.Identity!.Name);
             var auctions = _context.AuctionDetails
                .Where(t => t.PublishedByUserId == currentUserID &&
                            t.AuctionStatus == "Completed" &&
                            t.IsVerified == "Verified")
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionType = t.AuctionType,
                    StartingPrice = t.StartingPrice,
                    AuctionStatus = t.AuctionStatus,
                    EndDate = t.EndDate,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.AuctionId.ToString()),
                    WinnerDetails = t.BuyerId != null ? new UserListEdit
                    {
                        UserId = (short)t.BuyerId,
                        FirstName = _context.UserLists
                             .Where(u => u.UserId == t.BuyerId)
                             .Select(u => u.FirstName)
                             .FirstOrDefault(),
                        MiddleName = _context.UserLists
                        .Where(u => u.UserId == t.BuyerId)
                             .Select(u => u.MiddleName)
                             .FirstOrDefault(),
                        LastName = _context.UserLists
                        .Where(u => u.UserId == t.BuyerId)
                             .Select(u => u.LastName)
                             .FirstOrDefault(),
                    } : null,
                    // Add payment status
                    PaymentStatus = _context.Payments
                         .Where(p => p.PayAuctionId == t.AuctionId &&
                                    p.PayByUser == currentUserID &&
                                    p.PaymentMethod == "Deposit")
                         .OrderByDescending(p => p.PaymentDate)
                         .Select(p => p.PaymentStatus)
                         .FirstOrDefault() ?? "Not Paid",
                     PaymentId = _context.Payments
                         .Where(p => p.PayAuctionId == t.AuctionId &&
                                    p.PayByUser == currentUserID &&
                                    p.PaymentMethod == "Deposit")
                         .Select(p => p.PaymentId)
                         .FirstOrDefault()
                 })
                       .ToList();
             //return Json(tenders);
             return PartialView("_AwardedAuction", auctions);
         }*/

        public IActionResult AwardedAuction()
        {
            UpdateAuctionStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // First, handle automatic awarding for completed auctions
                var completedAuctions = _context.AuctionDetails
                    .AsNoTracking()
                    .Where(a => a.PublishedByUserId == currentUserID &&
                                a.AuctionStatus == "Completed" &&
                                a.AwardStatus != "Awarded" &&
                                a.IsVerified == "Verified")
                    .ToList();

                foreach (var auction in completedAuctions)
                {
                    try
                    {
                        // Get the highest bid for this auction
                        var highestBid = _context.AuctionBids
                            .AsNoTracking()
                            .Where(b => b.AuctionBidId == auction.AuctionId)
                            .OrderByDescending(b => b.BidAmount)
                            .FirstOrDefault();

                        if (highestBid != null)
                        {
                            // Check if a contract already exists for this auction
                            var existingContract = _context.ContractDetails
                                .AsNoTracking()
                                .FirstOrDefault(c => c.ConAuctionId == auction.AuctionId);

                            if (existingContract == null)
                            {
                                var auctionToUpdate = _context.AuctionDetails.Find(auction.AuctionId);
                                if (auctionToUpdate != null)
                                {
                                    // Award the auction to the highest bidder
                                    auctionToUpdate.BuyerId = highestBid.BidderId;
                                    auctionToUpdate.WinningBidAmount = highestBid.BidAmount;
                                    auctionToUpdate.AwardStatus = "Awarded";

                                    // Update the bid status
                                    var bidToUpdate = _context.AuctionBids.Find(highestBid.BidId);
                                    if (bidToUpdate != null)
                                    {
                                        bidToUpdate.BidStatus = "Accepted";
                                    }

                                    // Reject other bids in batch
                                    var otherBids = _context.AuctionBids
                                        .Where(b => b.AuctionBidId == auction.AuctionId && b.BidId != highestBid.BidId)
                                        .ToList();

                                    foreach (var bid in otherBids)
                                    {
                                        bid.BidStatus = "Rejected";
                                    }

                                    // Get the next available ContractId
                                    short nextContractId = 1;
                                    if (_context.ContractDetails.Any())
                                    {
                                        nextContractId = (short)(_context.ContractDetails
                                            .AsNoTracking()
                                            .Max(c => c.ContractId) + 1);
                                    }

                                    var contract = new ContractDetail
                                    {
                                        ContractId = nextContractId,
                                        ConAuctionId = auction.AuctionId,
                                        SellerId = auction.PublishedByUserId,
                                        BuyerId = highestBid.BidderId,
                                        ContractCreateDate = DateOnly.FromDateTime(DateTime.Now),
                                        ContractStatus = "Pending",
                                        SignedBySeller = false,
                                        SignedByBuyer = false,
                                    };

                                    _context.Add(contract);
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error for this specific auction but continue processing others
                        Console.WriteLine($"Error processing auction {auction.AuctionId}: {ex.Message}");
                        continue;
                    }
                }

                transaction.Commit();

                // Fetch awarded auctions
                var auctions = _context.AuctionDetails
                    .AsNoTracking()
                    .Where(t => t.PublishedByUserId == currentUserID &&
                                t.AuctionStatus == "Completed" &&
                                t.IsVerified == "Verified")
                    .Select(t => new AuctionEdit
                    {
                        AuctionId = t.AuctionId,
                        Title = t.Title,
                        AuctionType = t.AuctionType,
                        StartingPrice = t.StartingPrice,
                        AuctionStatus = t.AuctionStatus,
                        EndDate = t.EndDate,
                        IsVerified = t.IsVerified,
                        EncId = _protector.Protect(t.AuctionId.ToString()),
                        WinnerDetails = t.BuyerId != null ? new UserListEdit
                        {
                            UserId = (short)t.BuyerId,
                            FirstName = _context.UserLists
                             .Where(u => u.UserId == t.BuyerId)
                             .Select(u => u.FirstName)
                             .FirstOrDefault(),
                            MiddleName = _context.UserLists
                        .Where(u => u.UserId == t.BuyerId)
                             .Select(u => u.MiddleName)
                             .FirstOrDefault(),
                            LastName = _context.UserLists
                        .Where(u => u.UserId == t.BuyerId)
                             .Select(u => u.LastName)
                             .FirstOrDefault(),
                        } : null,
                        // Add payment status
                        PaymentStatus = _context.Payments
                         .Where(p => p.PayAuctionId == t.AuctionId &&
                                    p.PayByUser == currentUserID &&
                                    p.PaymentMethod == "Deposit")
                         .OrderByDescending(p => p.PaymentDate)
                         .Select(p => p.PaymentStatus)
                         .FirstOrDefault() ?? "Not Paid",
                        PaymentId = _context.Payments
                         .Where(p => p.PayAuctionId == t.AuctionId &&
                                    p.PayByUser == currentUserID &&
                                    p.PaymentMethod == "Deposit")
                         .Select(p => p.PaymentId)
                         .FirstOrDefault()
                    })
                       .ToList();
                //return Json(tenders);
                return PartialView("_AwardedAuction", auctions);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // Log the error
                Console.WriteLine($"Error in AwardedAuction: {ex.Message}");
                return StatusCode(500, "An error occurred while processing auctions.");
            }
        }

        /*public IActionResult AwardedAuction()
        {
            UpdateAuctionStatuses();
            int currentUserID = Convert.ToInt16(User.Identity!.Name);

           
            var completedAuctions = _context.AuctionDetails
                .AsNoTracking()  
                .Where(a => a.PublishedByUserId == currentUserID &&
                            a.AuctionStatus == "Completed" &&
                            a.AwardStatus != "Awarded" &&
                            a.IsVerified == "Verified")
                .ToList();

            foreach (var auction in completedAuctions)
            {
                // Get the highest bid for this auction
                var highestBid = _context.AuctionBids
                    .AsNoTracking()
                    .Include(b => b.Bidder)
                    .Where(b => b.AuctionBidId == auction.AuctionId)
                    .OrderByDescending(b => b.BidAmount)
                    .FirstOrDefault();

                if (highestBid != null)
                {
                    // Check if a contract already exists for this auction
                    var existingContract = _context.ContractDetails
                        .AsNoTracking()  // Add this to prevent tracking
                        .FirstOrDefault(c => c.ConAuctionId == auction.AuctionId);

                    if (existingContract == null)
                    {
                        var auctionToUpdate = _context.AuctionDetails.Find(auction.AuctionId);
                        if (auctionToUpdate != null)
                        {
                            // Award the auction to the highest bidder
                            auctionToUpdate.BuyerId = highestBid.BidderId;
                            auctionToUpdate.WinningBidAmount = highestBid.BidAmount;
                            auctionToUpdate.AwardStatus = "Awarded";

                            // Update the bid status
                            var bidToUpdate = _context.AuctionBids.Find(highestBid.BidId);
                            if (bidToUpdate != null)
                            {
                                bidToUpdate.BidStatus = "Accepted";
                            }

                            // Reject other bids
                            var otherBids = _context.AuctionBids
                                .Where(b => b.AuctionBidId == auction.AuctionId && b.BidId != highestBid.BidId);

                            foreach (var bid in otherBids)
                            {
                                bid.BidStatus = "Rejected";
                            }

                            // Get the next available ContractId
                            short nextContractId = 1;
                            if (_context.ContractDetails.Any())
                            {
                                nextContractId = (short)(_context.ContractDetails.Max(c => c.ContractId) + 1);
                            }

                            var contract = new ContractDetail
                            {
                                ContractId = nextContractId,
                                ConAuctionId = auction.AuctionId,
                                SellerId = auction.PublishedByUserId,
                                BuyerId = highestBid.BidderId,
                                ContractCreateDate = DateOnly.FromDateTime(DateTime.Now),
                                ContractStatus = "Pending",
                                SignedBySeller = false,
                                SignedByBuyer = false,
                            };

                            _context.Add(contract);

                        }
                    }
                }
            }

            _context.SaveChanges();



            // Fetch awarded auctions
            var auctions = _context.AuctionDetails
                .AsNoTracking()
                .Where(a => a.PublishedByUserId == currentUserID &&
                            a.AuctionStatus == "Completed" &&
                            a.IsVerified == "Verified")
                .Select(a => new AuctionEdit
                {
                    AuctionId = a.AuctionId,
                    Title = a.Title,
                    AuctionType = a.AuctionType,
                    StartingPrice = a.StartingPrice,
                    AuctionStatus = a.AuctionStatus,
                    IsVerified = a.IsVerified,
                    WinnerDetails = a.BuyerId != null ? new UserListEdit
                    {
                        UserId = (short)a.BuyerId,
                        FirstName = _context.UserLists
                            .Where(u => u.UserId == a.BuyerId)
                            .Select(u => u.FirstName)
                            .FirstOrDefault() ?? "",
                        MiddleName = _context.UserLists
                            .Where(u => u.UserId == a.BuyerId)
                            .Select(u => u.MiddleName)
                            .FirstOrDefault(),
                        LastName = _context.UserLists
                            .Where(u => u.UserId == a.BuyerId)
                            .Select(u => u.LastName)
                            .FirstOrDefault() ?? ""
                    } : null,
                    PaymentStatus = _context.Payments
                        .Where(p => p.PayAuctionId == a.AuctionId)
                        .OrderByDescending(p => p.PaymentDate)
                        .Select(p => p.PaymentStatus)
                        .FirstOrDefault() ?? "Not Paid",
                    PaymentId = _context.Payments
                        .Where(p => p.PayAuctionId == a.AuctionId)
                        .Select(p => p.PaymentId)
                        .FirstOrDefault(),
                    EncId = _protector.Protect(a.AuctionId.ToString())
                })
                .ToList();

            return PartialView("_AwardedAuction", auctions);
        }*/


        public IActionResult MonitorAuction(string id)
        {

            try
            {
                UpdateAuctionStatuses();
                //return Json(id);
                // Decrypt the auction ID to retrieve auction details
                int auctionId = Convert.ToInt32(_protector.Unprotect(id));

                // return Json(auctionId);
                // Fetch the auction details
                var auction = _context.AuctionDetails
                    .Where(a => a.AuctionId == auctionId)
                    .Select(a => new AuctionEdit
                    {
                        AuctionId = a.AuctionId,
                        Title = a.Title,
                        AuctionDescription = a.AuctionDescription,
                        ItemImage = a.ItemImage,
                        StartingPrice = a.StartingPrice,
                        StartDate = a.StartDate,
                        StartTime = a.StartTime,
                        EndDate = a.EndDate,
                        EndTime = a.EndTime,
                        AuctionType = a.AuctionType,
                        AuctionStatus = a.AuctionStatus,
                        WinningBidAmount = a.WinningBidAmount,
                        EncId = _protector.Protect(a.AuctionId.ToString())
                    })
                    .FirstOrDefault();



                // If auction is not found, return NotFound
                if (auction == null)
                {
                    return NotFound();
                }

                // Fetch the bids in a separate query
                var bids = _context.AuctionBids
                    .Where(b => b.AuctionBidId == auctionId) // Ensure this filters correctly
                    .OrderByDescending(b => b.BidAmount)
                    .Select(b => new AuctionBidEdit
                    {
                        BidId = b.BidId,
                        BidAmount = b.BidAmount,
                        BidderId = b.BidderId,
                        BidderName = _context.UserLists
                                             .Where(u => u.UserId == b.BidderId)
                                             .Select(user => new UserListEdit
                                             {
                                                 UserId = user.UserId,
                                                 FirstName = user.FirstName,
                                                 MiddleName = user.MiddleName,
                                                 LastName = user.LastName,
                                                 EncId = _protector.Protect(user.UserId.ToString())
                                             })
                                             .ToList(), // Fetch bidder's name
                        BidDate = b.BidDate,
                        BidTime = b.BidTime,
                        BidStatus = b.BidStatus,
                        EncId = _protector.Protect(b.BidId.ToString()),

                        // Fetch the related bids in a separate query
                        Bids = _context.Bids
                            .Where(bid => bid.AucBidId == b.BidId)
                            .Select(bid => new BidEdit
                            {
                                BiddingId = bid.BiddingId,
                                AucBidId = bid.AucBidId,
                                BiddingAmount = bid.BiddingAmount,
                                EncId = _protector.Protect(bid.BiddingId.ToString())
                            })
                            .ToList()
                    })
                    .ToList();

                // Get the highest bid amount
                decimal highestBidAmount = bids.Any() ? bids.Max(b => b.BidAmount) : auction.StartingPrice;

                // Calculate the minimum bid amount
                decimal minBidAmount = highestBidAmount + 1;

                // Create ViewModel to return to the view
                var viewModel = new AuctionDetailsViewModel
                {
                    Auction = auction,
                    AuctionBid = bids,
                    HighestBidAmount = highestBidAmount,
                    MinBidAmount = minBidAmount
                };

                // Return the view with the ViewModel
                return View(viewModel);
            }
            catch (CryptographicException ex)
            {
                // Handle cryptographic exception (e.g., malformed or incorrect encrypted ID)
                return BadRequest("Error decrypting auction ID: " + ex.Message);
            }
            catch (FormatException ex)
            {
                // Handle format exception (e.g., malformed ID)
                return BadRequest("Malformed auction ID: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle any other general exceptions
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserBid(string id)
        {
            int auctionId = Convert.ToInt32(_protector.Unprotect(id));
            var currentUserId = Convert.ToInt16(User.Identity!.Name);

            // Get Auction Details
            var auctionDetails = await _context.AuctionDetails
                .Where(a => a.AuctionId == auctionId)
                .FirstOrDefaultAsync();

            if (auctionDetails == null)
            {
                return NotFound();
            }

            // Get Distinct Bidders for the Auction
            var bidders = await _context.AuctionBids
                .Where(b => b.AuctionBidId == auctionId)
                .Select(b => b.BidderId)
                .Distinct()
                .Join(
                    _context.UserLists,
                    bidId => bidId,
                    user => user.UserId,
                    (bidId, user) => new UserListEdit
                    {
                        UserId = user.UserId,
                        FirstName = user.FirstName,
                        MiddleName = user.MiddleName,
                        LastName = user.LastName,
                        EncId = _protector.Protect(user.UserId.ToString())
                    }
                )
                .ToListAsync();

            // Get Bid History for the Auction
            var bidHistory = await _context.AuctionBids
                .Where(b => b.AuctionBidId == auctionId)
                .OrderByDescending(b => b.BidAmount)
                .Select(b => new AuctionBidEdit
                {
                    BidId = b.BidId,
                    BidderId = b.BidderId,
                    Biddername = _context.UserLists
                        .Where(u => u.UserId == b.BidderId)
                        .Select(user => $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim())
                        .FirstOrDefault(),
                    BidAmount = b.BidAmount,
                    Bids = _context.Bids
                        .Where(bid => bid.AucBidId == b.BidId)
                        .OrderByDescending(bid => bid.BiddingId)
                        .Select(bid => new BidEdit
                        {
                            BidAmount = bid.BiddingAmount,
                            Biddername = _context.UserLists
                            .Where(u => u.UserId == b.BidderId)
                            .Select(user => $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim())
                            .FirstOrDefault()

                        })
                        .ToList()
                })
                .ToListAsync();

            // Get the highest bid amount
            decimal highestBidAmount = bidHistory.Any() ? bidHistory.Max(b => b.BidAmount) : auctionDetails.StartingPrice;

            // Calculate the minimum bid amount
            decimal minBidAmount = highestBidAmount + 1;

            var model = new AuctionDetailsViewModel
            {
                AuctionDetails = auctionDetails,
                Bidders = bidders,
                BidHistoryy = bidHistory,
                HighestBidAmount = highestBidAmount,
                MinBidAmount = minBidAmount
            };

            return View(model);
        }




        public IActionResult BiddingDetails(string id)
        {
            UpdateAuctionStatuses();
            //return Json(id);
            int BidappId = Convert.ToInt32(_protector.Unprotect(id));

            //return Json(BidappId);
            // Fetch the Application Details
            var application = _context.AuctionBids
                .Where(ta => ta.BidId == BidappId)
                .Select(ta => new AuctionBidEdit
                {
                    BidId = ta.BidId,
                    AuctionBidId = ta.AuctionBidId,
                    BidderId = ta.BidderId,
                    BidAmount = ta.BidAmount,
                    BidDate = ta.BidDate,
                    BidTime = ta.BidTime,
                    BidStatus = ta.BidStatus,
                    EncId = _protector.Protect(ta.BidId.ToString())

                })
                .FirstOrDefault();

            // return Json(application);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var bid = _context.Bids
                .Where(b => b.AucBidId == application.BidId)
                .OrderByDescending(b => b.BiddingAmount)
                .Select(b => new BidEdit
                {
                    BiddingId = b.BiddingId,
                    AucBidId = b.AucBidId,
                    BiddingAmount = b.BiddingAmount,
                    EncId = _protector.Protect(b.BiddingId.ToString())

                })
                .ToList();
            // Get the highest bid amount
            decimal? highestBidAmount = bid.Any() ? bid.Max(b => b.BiddingAmount) : (decimal?)null;


            // return Json(bid);

            // Fetch Tender Details based on TenderId
            var auction = _context.AuctionDetails
                .Where(t => t.AuctionId == application.AuctionBidId)
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionDescription = t.AuctionDescription,
                    AuctionType = t.AuctionType,
                    StartingPrice = t.StartingPrice,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    ItemImage = t.ItemImage,
                    AuctionStatus = t.AuctionStatus,
                    IsVerified = t.IsVerified,
                    BuyerId = t.BuyerId,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                .FirstOrDefault();
            //return Json(auction);

            // Fetch Bidder (User) Details
            var bidder = _context.UserLists
                .Where(u => u.UserId == application.BidderId)
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



            // Create ViewModel for passing to the view
            var viewModel = new AuctionDetailsViewModel
            {
                AuctionBidEdit = application,
                Auction = auction,
                Bid = bid,
                User = bidder,
                HighestBidsAmount = highestBidAmount
            };

            // return Json(viewModel);

            return View(viewModel);
        }

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AwardBid(long BidId, string BidStatus)
        {
            try
            {
                var AuctionBid = _context.AuctionBids.FirstOrDefault(a => a.BidId == BidId);

                if (AuctionBid == null)
                {
                    return Json(new { success = false, message = "Bid not found." });
                }

                if (AuctionBid.BidStatus != "Pending")
                {
                    return Json(new { success = false, message = "Bid is not in a pending state." });
                }

                // Update the application status
                AuctionBid.BidStatus = BidStatus;

                // Update TenderDetails if the application is marked as "Won"
                if (BidStatus == "Accepted")
                {
                    var auction = _context.AuctionDetails.FirstOrDefault(t => t.AuctionId == AuctionBid.AuctionBidId);
                    var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(345));

                    if (auction != null)
                    {
                        //auction.AuctionStatus = "Awarded";
                        auction.BuyerId = AuctionBid.BidderId;
                        auction.WinningBidAmount = AuctionBid.BidAmount;

                        _context.Update(auction);

                        // Update all other applications for this tender to "Lost"
                        var otherBids = _context.AuctionBids
                            .Where(a => a.AuctionBidId == AuctionBid.AuctionBidId && a.BidId != BidId)
                            .ToList();

                        foreach (var app in otherBids)
                        {
                            app.BidStatus = "Rejected";
                        }



                        var contract = new ContractDetail
                        {
                            ContractId = (short)(_context.ContractDetails.Max(c => c.ContractId) + 1),
                            ConAuctionId = auction.AuctionId,


                            SellerId = auction.PublishedByUserId, // Assuming seller ID comes from the tender
                            BuyerId = AuctionBid.BidderId, // Buyer is the awarded company
                            ContractCreateDate = currentDate,
                            ContractStatus = "Pending",
                            SignedBySeller = false,
                            SignedByBuyer = false,

                        };

                        _context.Add(contract);

                    }
                }

                _context.SaveChanges();
                return Json(new
                {
                    success = true,
                    message = "Bid status updated successfully. Contract has been generated.",
                    redirectUrl = Url.Action("AuctionIndex", "Contract")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
*/


        // ... existing code ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AwardBid(long BidId, string BidStatus)
        {
            try
            {
                var AuctionBid = await _context.AuctionBids
                    .Include(b => b.Bidder)
                    .FirstOrDefaultAsync(a => a.BidId == BidId);

                if (AuctionBid == null)
                {
                    return Json(new { success = false, message = "Bid not found." });
                }

                if (AuctionBid.BidStatus != "Pending")
                {
                    return Json(new { success = false, message = "Bid is not in a pending state." });
                }

                AuctionBid.BidStatus = BidStatus;

                if (BidStatus == "Accepted")
                {
                    var auction = await _context.AuctionDetails
                        .FirstOrDefaultAsync(t => t.AuctionId == AuctionBid.AuctionBidId);
                    var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(345));

                    if (auction != null)
                    {
                        auction.AuctionStatus = "Completed";
                        auction.AwardStatus = "Awarded";
                        auction.BuyerId = AuctionBid.BidderId;
                        auction.WinningBidAmount = AuctionBid.BidAmount;

                        _context.Update(auction);

                        var otherBids = await _context.AuctionBids
                            .Where(a => a.AuctionBidId == AuctionBid.AuctionBidId && a.BidId != BidId)
                            .ToListAsync();

                        foreach (var bid in otherBids)
                        {
                            bid.BidStatus = "Rejected";
                        }

                        // Check if contract already exists
                        var existingContract = await _context.ContractDetails
                            .FirstOrDefaultAsync(c => c.ConAuctionId == auction.AuctionId);

                        if (existingContract == null)
                        {
                            // Get the next available ContractId
                            short nextContractId = 1;
                            if (await _context.ContractDetails.AnyAsync())
                            {
                                nextContractId = (short)(await _context.ContractDetails.MaxAsync(c => c.ContractId) + 1);
                            }

                            var contract = new ContractDetail
                            {
                                ContractId = nextContractId,
                                ConAuctionId = auction.AuctionId,
                                SellerId = auction.PublishedByUserId,
                                BuyerId = AuctionBid.BidderId,
                                ContractCreateDate = currentDate,
                                ContractStatus = "Pending",
                                SignedBySeller = false,
                                SignedByBuyer = false,
                            };

                            _context.Add(contract);
                        }

                        // Send email to winning bidder
                        if (AuctionBid.Bidder?.EmailAddress != null)
                        {
                            string winnerEmailBody = $@"
                    <h2>Congratulations! You've Won the Auction</h2>
                    <p>Your bid for the following auction has been accepted:</p>
                    <ul>
                        <li><strong>Auction Title:</strong> {auction.Title}</li>
                        <li><strong>Your Winning Bid:</strong> {AuctionBid.BidAmount:C}</li>
                    </ul>
                    <p>A contract has been generated. Please login to your account to review and sign the contract.</p>";

                            await _emailService.SendEmailAsync(
                                AuctionBid.Bidder.EmailAddress,
                                "Congratulations! Auction Award Notification",
                                winnerEmailBody);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Bid status updated successfully. Contract has been generated.",
                    redirectUrl = Url.Action("AuctionIndex", "Contract")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        

        [HttpGet]
        public IActionResult EditAuction(string id)
        {
            UpdateAuctionStatuses();
            int auctionid;
            try
            {
                auctionid = Convert.ToInt32(_protector.Unprotect(id));
            }
            catch
            {
                return BadRequest("Invalid auction ID.");
            }

            var auction = _context.AuctionDetails
                .Where(t => t.AuctionId == auctionid)
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionDescription = t.AuctionDescription,
                    ItemImage = t.ItemImage,
                    StartingPrice = t.StartingPrice,
                    StartDate = t.StartDate,
                    IsVerified = t.IsVerified,
                    PublishedByUserId = t.PublishedByUserId,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    EndDate = t.EndDate,
                    AuctionType = t.AuctionType,
                    AuctionStatus = t.AuctionStatus,
                    BuyerId = t.BuyerId,
                    WinningBidAmount = t.WinningBidAmount,
                    AwardStatus = t.AwardStatus,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                .FirstOrDefault();

            if (auction == null)
            {
                return NotFound("Auction not found.");
            }

            //return Json(auction);

            return View(auction);
        }

        [HttpPost]
        public async Task<IActionResult> EditAuction(AuctionEdit a)
        {
            UpdateAuctionStatuses();
            try
            {
                string? existingFile = _context.AuctionDetails
                    .Where(ad => ad.AuctionId == a.AuctionId)
                    .Select(ad => ad.ItemImage)
                    .FirstOrDefault();

                // return Json(existingFile);

                if (a.AuctionFile != null)
                {
                    string fileName = "AuctionImage" + Guid.NewGuid() + Path.GetExtension(a.AuctionFile.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "AuctionImage", fileName);

                    if (!Directory.Exists(Path.Combine(_env.WebRootPath, "AuctionImage")))
                    {
                        Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "AuctionImage"));
                    }

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await a.AuctionFile.CopyToAsync(stream);
                    }

                    a.ItemImage = fileName;
                }
                else
                {
                    // Retain existing document if no new file is uploaded
                    a.ItemImage = existingFile;
                }

                var auction = _context.AuctionDetails.FirstOrDefault(td => td.AuctionId == a.AuctionId);
                if (auction == null)
                {
                    return NotFound("Auction not found.");
                }
                // return Json(auction);
                // Updating existing auction details

                auction.AuctionId = a.AuctionId;
                auction.Title = a.Title;
                auction.AuctionDescription = a.AuctionDescription;
                auction.ItemImage = a.ItemImage;
                auction.StartingPrice = a.StartingPrice;
                auction.StartDate = a.StartDate;
                auction.IsVerified = a.IsVerified;
                auction.PublishedByUserId = Convert.ToInt16(User.Identity!.Name); 
                auction.StartTime = a.StartTime;
                auction.EndTime = a.EndTime;
                auction.EndDate = a.EndDate;
                auction.AuctionType = a.AuctionType;
                auction.AuctionStatus = a.AuctionStatus;
                auction.BuyerId = a.BuyerId;
                auction.WinningBidAmount = a.WinningBidAmount;
               /* tender.Title = t.Title;
                tender.IssuedBy = t.IssuedBy;
                tender.IssuedDate = t.IssuedDate;
                tender.TenderType = t.TenderType;
                tender.TenderStatus = t.TenderStatus;
                tender.OpeningDate = t.OpeningDate;
                tender.ClosingDate = t.ClosingDate;
                tender.ProjectDuration = t.ProjectDuration;
                tender.BudgetEstimation = t.BudgetEstimation;
                tender.TenderDescription = t.TenderDescription;
                tender.IsVerified = t.IsVerified;
                tender.PublishedByUserId = Convert.ToInt16(User.Identity!.Name);
                tender.AwardStatus = t.AwardStatus;
                tender.AwardCompanyId = t.AwardCompanyId;
                tender.AwardDate = t.AwardDate;
                tender.TenderDocument = t.TenderDocument;*/

                //return Json(tender);
                _context.Update(auction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Auction updated successfully!";
                return RedirectToAction("AuctionPage", "PublisherAuction");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the auction. Please try again.");
                return View(a);
            }
        }

        [HttpGet]
        public ActionResult DeleteAuction(string id)
        {
            int auctionId = Convert.ToInt32(_protector.Unprotect(id));

            // Find the tender in the database
            var auction = _context.AuctionDetails
                .Where(t => t.AuctionId == auctionId)
                .Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionDescription = t.AuctionDescription,
                    ItemImage = t.ItemImage,
                    StartingPrice = t.StartingPrice,
                    StartDate = t.StartDate,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    EndDate = t.EndDate,
                    AuctionType = t.AuctionType,
                    AuctionStatus = t.AuctionStatus,
                    IsVerified = t.IsVerified
                })
                .FirstOrDefault();

            if (auction == null)
            {
                return NotFound();
            }

            return View(auction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAuctionConfirmed(string id)
        {
            short auctionId = Convert.ToInt16(_protector.Unprotect(id));

            var Auction = await _context.AuctionDetails.FindAsync(auctionId);
            if (Auction != null)
            {
                _context.AuctionDetails.Remove(Auction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Auction deleted successfully!";
                return RedirectToAction("AuctionPage", "PublisherAuction");
            }

            TempData["ErrorMessage"] = "Auction not found!";
            return RedirectToAction("AuctionPage", "PublisherAuction");
        }


    }
}
