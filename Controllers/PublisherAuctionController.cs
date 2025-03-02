﻿using FYPBidNetra.Models;
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

    public class PublisherAuctionController : Controller
    {

        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public PublisherAuctionController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
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
        public IActionResult PublishAuction(AuctionEdit a)
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
                _context.SaveChanges();

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
            var nepalTime = DateTime.UtcNow.AddMinutes(345); // Current time in Nepal Time (UTC+5:45)
            var auctions = _context.AuctionDetails.ToList();

            foreach (var auction in auctions)
            {
                // Convert start and end times to Nepal TimeZone
                var openingDateTime = new DateTime(auction.StartDate.Year, auction.StartDate.Month, auction.StartDate.Day,
                                                   auction.StartTime.Hour, auction.StartTime.Minute, auction.StartTime.Second,
                                                   DateTimeKind.Utc).AddMinutes(345); // Convert to Nepal time

                var closingDateTime = new DateTime(auction.EndDate.Year, auction.EndDate.Month, auction.EndDate.Day,
                                                   auction.EndTime.Hour, auction.EndTime.Minute, auction.EndTime.Second,
                                                   DateTimeKind.Utc).AddMinutes(345); // Convert to Nepal time

                // Compare Nepal Time with auction start and end times
                if (nepalTime >= openingDateTime && nepalTime < closingDateTime)
                {
                    auction.AuctionStatus = "Active";
                }
                else if (nepalTime >= closingDateTime)
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
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                  .ToList();
            //return Json(tenders);
            return PartialView("_ActiveAuction", auctions);
        }

        public IActionResult CompleteAuction()
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
                    IsVerified = t.IsVerified,
                    EncId = _protector.Protect(t.AuctionId.ToString())
                })
                  .ToList();
            //return Json(tenders);
            return PartialView("_ActiveAuction", auctions);
        }


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

            // Get Distinct Bidders for the Auction
            var bidders = await _context.AuctionBids
                .Where(b => b.AuctionBidId == id)
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
            // Decrypt the application ID
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

        [HttpPost]
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


    }
}
