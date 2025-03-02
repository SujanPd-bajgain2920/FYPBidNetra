using FYPBidNetra.Models;
using FYPBidNetra.Security;
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
    public class BidAuctionController : Controller
    {

        private readonly FypContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;
        private readonly IHubContext<AuctionHub> _hubContext;

        public BidAuctionController(FypContext context, IWebHostEnvironment env, DataSecurityProvider key, IDataProtectionProvider provider, IHubContext<AuctionHub> hubContext)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _hubContext = hubContext;
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

        // ############################ Auctions ############################

        [Route("auctionbidtab/{activeTab?}")]
        public IActionResult AuctionBidTab(string activeTab = "UpcommingAuctions")
        {
            ViewBag.ActiveTab = activeTab;
            return PartialView("_AuctionBidTab");
        }

        public IActionResult UpcomingAuction()
        {
            UpdateAuctionStatuses();
            var auctions = _context.AuctionDetails
                .Where(t =>
                            t.AuctionStatus == "Pending" &&
                            t.IsVerified == "Verified")
                .OrderByDescending(t => t.StartDate).Select(t => new AuctionEdit
                {
                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionType = t.AuctionType,
                    StartingPrice = t.StartingPrice,
                    AuctionStatus = t.AuctionStatus,
                    ItemImage = t.ItemImage,
                    IsVerified = t.IsVerified,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                  .ToList();

            return PartialView("_UpcomingAuction", auctions);
        }

        public IActionResult ActiveAuction()
        {
            UpdateAuctionStatuses();

            int currentUserID = Convert.ToInt16(User.Identity!.Name);


            var userCompanyId = _context.UserLists
                .Where(c => c.UserId == currentUserID)
                .Select(c => c.UserId)
                .FirstOrDefault();

            var auctions = _context.AuctionDetails
                .Where(t =>
                            t.AuctionStatus == "Active" &&
                            t.IsVerified == "Verified")
                .OrderByDescending(t => t.StartDate).Select(t => new AuctionEdit
                {

                    AuctionId = t.AuctionId,
                    Title = t.Title,
                    AuctionType = t.AuctionType,
                    StartingPrice = t.StartingPrice,
                    AuctionStatus = t.AuctionStatus,
                    ItemImage = t.ItemImage,
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.AuctionId.ToString()),
                    StartDate = t.StartDate,
                    EndDate = t.EndDate
                })
                  .ToList();
            return PartialView("_ActiveAuction", auctions);
        }

        public IActionResult ApplyAuctionList()
        {
            UpdateAuctionStatuses();

            int currentUserID = Convert.ToInt16(User.Identity!.Name);

            var userCompanyId = _context.UserLists
                 .Where(c => c.UserId == currentUserID)
                 .Select(c => c.UserId)
                 .FirstOrDefault();

            var auctions = _context.AuctionDetails
                .Where(t => _context.AuctionBids
                    .Any(ta => ta.AuctionBidId == t.AuctionId && ta.BidderId == userCompanyId))
                    .OrderByDescending(t => t.StartDate)
                 .Select(t => new AuctionEdit
                 {
                     AuctionId = t.AuctionId,
                     Title = t.Title,
                     AuctionType = t.AuctionType,
                     StartingPrice = t.StartingPrice,
                     AuctionStatus = t.AuctionStatus,
                     StartDate = t.StartDate,
                     EndDate = t.EndDate,
                     StartTime = t.StartTime,
                     EndTime = t.EndTime,
                     ItemImage = t.ItemImage,
                     IsVerified = t.IsVerified,
                     EncId = _protector.Protect(t.AuctionId.ToString()),
                     Bid = _context.AuctionBids
                .Where(ta => ta.AuctionBidId == t.AuctionId && ta.BidderId == userCompanyId)
                .Select(ta => new AuctionBidEdit
                {
                    BidId = ta.BidId,
                    BidDate = ta.BidDate,
                    BidAmount = ta.BidAmount,
                    BidStatus = ta.BidStatus,
                })
                .FirstOrDefault()
                 })
                 .ToList();

            return PartialView("_ApplyAuctionList", auctions);
        }

        public IActionResult BidderAuctionDetails(string id)
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
                    ItemImage = t.ItemImage,
                    StartingPrice = t.StartingPrice,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    PublishedByUserId = t.PublishedByUserId,
                    IsVerified = t.IsVerified,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    AuctionType = t.AuctionType,
                    AuctionStatus = t.AuctionStatus,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                .FirstOrDefault();

            // return Json(auction);

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
                User = user,

            };

            // return Json(viewModel);
            return View(viewModel);
        }



        public IActionResult MonitorAuction(string id)
        {
            try
            {
                UpdateAuctionStatuses();
                // Decrypt the auction ID to retrieve auction details
                int auctionId = Convert.ToInt32(_protector.Unprotect(id));

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
                        WinningBidAmount = a.WinningBidAmount
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
                                                 LastName = user.LastName
                                             })
                                             .ToList(), // Fetch bidder's name
                        BidDate = b.BidDate,
                        BidTime = b.BidTime,
                        BidStatus = b.BidStatus,

                        // Fetch the related bids in a separate query
                        Bids = _context.Bids
                            .Where(bid => bid.AucBidId == b.BidId)
                            .Select(bid => new BidEdit
                            {
                                BiddingId = bid.BiddingId,
                                AucBidId = bid.AucBidId,
                                BiddingAmount = bid.BiddingAmount
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


        /* [HttpGet]
         public async Task<IActionResult> UserBid(short id)
         {
             var currentUserId = Convert.ToInt16(User.Identity!.Name);

             // Get Auction Details
             var auctionDetails = await _context.AuctionDetails
                 .Where(a => a.AuctionId == id)
                 .FirstOrDefaultAsync();

             if (auctionDetails == null)
             {
                 return NotFound();
             }

             // Get Bid History for the Auction
             var bidHistory = await _context.AuctionBids
                 .Where(b => b.AuctionBidId == id)
                 .OrderByDescending(b => b.BidDate).ThenByDescending(b => b.BidTime)
                 .Select(b => new AuctionBidEdit 
                 {
                     BidId = b.BidId,
                     BidderId = b.BidderId,
                     BidderName = _context.UserLists
                                              .Where(u => u.UserId == b.BidderId)
                                              .Select(user => new UserListEdit
                                              {
                                                  UserId = user.UserId,
                                                  FirstName = user.FirstName,
                                                  MiddleName = user.MiddleName,
                                                  LastName = user.LastName
                                              })
                                              .ToList(), // Fetch bidder's
                     BidAmount = b.BidAmount,
                     BidDate = b.BidDate,
                     BidTime = b.BidTime,
                     BidStatus = b.BidStatus,

                     // Fetch the related bids in a separate query
                     Bids = _context.Bids
                             .Where(bid => bid.AucBidId == b.BidId)
                             .Select(bid => new BidEdit
                             {
                                 BiddingId = bid.BiddingId,
                                 AucBidId = bid.AucBidId,
                                 BiddingAmount = bid.BiddingAmount
                             })
                             .ToList()
                 }).ToListAsync();

             // Get the highest bid amount
             decimal highestBidAmount = bidHistory.Any() ? bidHistory.Max(b => b.BidAmount) : auctionDetails.StartingPrice;

             // Calculate the minimum bid amount
             decimal minBidAmount = highestBidAmount + 1;

             // Pass Auction Details and Bid History to the View
             var model = new AuctionDetailsViewModel
             {
                 AuctionDetails = auctionDetails,
                 BidHistoryy = bidHistory,
                 HighestBidAmount = highestBidAmount,
                 MinBidAmount = minBidAmount,
             };

             return View(model);
         }*/





        [HttpGet]
        public async Task<IActionResult> UserBid(short id)
        {
            var currentUserId = Convert.ToInt16(User.Identity!.Name);

            // Get Auction Details
            var auctionDetails = await _context.AuctionDetails
                .Where(a => a.AuctionId == id)
                .FirstOrDefaultAsync();

            if (auctionDetails == null)
            {
                return NotFound();
            }

            // Get Bid History for the Auction
            var bidHistory = await _context.AuctionBids
                .Where(b => b.AuctionBidId == id)
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
                BidHistoryy = bidHistory,
                HighestBidAmount = highestBidAmount,
                MinBidAmount = minBidAmount
            };

            return View(model);
        }








        [HttpPost]
        public async Task<IActionResult> PlaceBid(int auctionId, decimal bidAmount)
        {
            UpdateAuctionStatuses();
            var userId = Convert.ToInt16(User.Identity!.Name);
            var auction = _context.AuctionDetails.FirstOrDefault(a => a.AuctionId == auctionId);

            if (auction == null || auction.AuctionStatus != "Active")
            {
                return BadRequest("Auction is not active or does not exist.");
            }

            // Fetch the highest bid for this auction
            var highestBid = _context.Bids
                .Where(b => b.AucBidId == auctionId)
                .OrderByDescending(b => b.BiddingAmount)
                .FirstOrDefault();

            // Check if the current bid is higher than the highest bid
            if (highestBid != null && bidAmount <= highestBid.BiddingAmount)
            {
                return BadRequest("Your bid must be higher than the current highest bid.");
            }

            // Get the current time in Nepal Standard Time
            TimeZoneInfo nepaliTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
            DateTime nepaliNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nepaliTimeZone);

            // Check if the user has already placed a bid for this auction
            var existingBid = _context.AuctionBids
                .Where(b => b.AuctionBidId == auctionId && b.BidderId == userId)
                .FirstOrDefault();

            if (existingBid == null)
            {
                // No existing bid, create a new AuctionBid record for the user
                var maxBidId = _context.AuctionBids.Any() ? _context.AuctionBids.Max(c => c.BidId) : 0;
                var auctionBid = new AuctionBid
                {
                    BidId = (short)(maxBidId + 1),
                    AuctionBidId = (short)auctionId,
                    BidderId = userId,
                    BidAmount = bidAmount,
                    BidDate = DateOnly.FromDateTime(nepaliNow),
                    BidTime = TimeOnly.FromDateTime(nepaliNow),
                    BidStatus = "Pending"
                };

                _context.AuctionBids.Add(auctionBid);
                _context.SaveChanges();

                // Create a new Bid record for this auctionBid
                var maxBiddingId = _context.Bids.Any() ? _context.Bids.Max(c => c.BiddingId) : 0;
                var newBid = new Bid
                {
                    BiddingId = (short)(maxBiddingId + 1),
                    AucBidId = auctionBid.BidId,
                    BiddingAmount = bidAmount
                };

                _context.Bids.Add(newBid);
                _context.SaveChanges();
            }
            else
            {
                // Existing bid, just add a new Bid record with the new bid amount
                var maxBiddingId = _context.Bids.Any() ? _context.Bids.Max(c => c.BiddingId) : 0;
                var newBid = new Bid
                {
                    BiddingId = (short)(maxBiddingId + 1),
                    AucBidId = existingBid.BidId,
                    BiddingAmount = bidAmount,

                };

                _context.Bids.Add(newBid);
                _context.SaveChanges();
            }

            // Now, update the AuctionBid with the highest bid from all the Bids related to this auctionBid
            var auctionBids = _context.AuctionBids
                .Where(b => b.AuctionBidId == auctionId)
                .ToList();

            foreach (var auctionBid in auctionBids)
            {
                // Get the highest BiddingAmount for each AuctionBid
                decimal highestBiddingAmount = _context.Bids
                    .Where(bid => bid.AucBidId == auctionBid.BidId)
                    .Max(bid => bid.BiddingAmount);

                // If no bids exist, set the highestBiddingAmount to the starting price
                if (highestBiddingAmount == 0)
                {
                    highestBiddingAmount = auction.StartingPrice;
                }

                // Update the AuctionBid with the new highest bid amount
                auctionBid.BidAmount = highestBiddingAmount;
            }

            // Save the changes after updating AuctionBids
            _context.SaveChanges();

            // Notify all connected clients with the new bid
            // _hubContext.Clients.All.SendAsync("ReceiveBid", auctionId, userId, bidAmount, nepaliNow);

            // Redirect to the auction monitoring page
            //return RedirectToAction("MonitorAuction", new { id = _protector.Protect(auctionId.ToString()) });

            var user = _context.UserLists.Find(userId);
            string bidderName = user != null ? $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim() : "Unknown User";

            await _hubContext.Clients.All.SendAsync("ReceiveBidUpdate", auctionId, bidderName, bidAmount, nepaliNow);

            return RedirectToAction("UserBid", new { id = auctionId });

            //return RedirectToAction("UserBid", new { id = auctionId });
        }



    }
}
