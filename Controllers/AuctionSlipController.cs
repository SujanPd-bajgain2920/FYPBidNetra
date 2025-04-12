using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace FYPBidNetra.Controllers
{
    public class AuctionSlipController : Controller
    {
        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public AuctionSlipController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }

        [HttpGet]
        public IActionResult AuctionPayment(string auctionId)
        {
            
            try
            {
                int decryptedAuctionId = Convert.ToInt32(_protector.Unprotect(auctionId));
                int currentUserId = Convert.ToInt16(User.Identity!.Name);

                var paymentDetails = _context.AuctionDetails
                    .Where(t => t.AuctionId == decryptedAuctionId)
                    .Select(t => new PaymentEdit
                    {
                        AuctionId = t.AuctionId,
                        AuctionTitle = t.Title,
                        PaymentAmount = (decimal)t.WinningBidAmount,
                        PaymentDate = DateTime.UtcNow.AddMinutes(345),
                        PayFromUser = new UserListEdit
                        {
                            UserId = (short)currentUserId,
                            FirstName = _context.UserLists
                                .Where(u => u.UserId == currentUserId)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault()
                        },
                        PayToUser = new UserListEdit
                        {
                            UserId = (short)t.BuyerId,
                            FirstName = _context.UserLists
                                .Where(u => u.UserId == t.BuyerId)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault()
                        },
                        PayAuctionId = new AuctionEdit
                        {
                            AuctionId = t.AuctionId

                        },
                        EncryptedTenderId = auctionId
                    })
                    .FirstOrDefault();

                if (paymentDetails == null)
                {
                    return NotFound("Auction not found");
                }

                return View(paymentDetails);
            }
            catch (Exception)
            {
                return BadRequest("Invalid auction ID");
            }
        }

        [HttpGet]
        public IActionResult PaymentSuccess(int auctionId)
        {
            int currentUserId = Convert.ToInt16(User.Identity!.Name);

            var paymentDetails = _context.AuctionDetails
                     .Where(t => t.AuctionId == auctionId)
                     .Select(t => new PaymentEdit
                     {
                         AuctionId = t.AuctionId,
                         AuctionTitle = t.Title,
                         PaymentAmount = (decimal)t.WinningBidAmount,
                         PaymentDate = DateTime.UtcNow.AddMinutes(345),
                         PayFromUser = new UserListEdit
                         {
                             UserId = (short)currentUserId,
                             FirstName = _context.UserLists
                                 .Where(u => u.UserId == currentUserId)
                                 .Select(u => u.FirstName + " " + u.LastName)
                                 .FirstOrDefault()
                         },
                         PayToUser = new UserListEdit
                         {
                             UserId = (short)t.BuyerId,
                             FirstName = _context.UserLists
                                 .Where(u => u.UserId == t.BuyerId)
                                 .Select(u => u.FirstName + " " + u.LastName)
                                 .FirstOrDefault()
                         },
                         PayAuctionId = new AuctionEdit
                         {
                             AuctionId = t.AuctionId

                         },
                        
                     })
                     .FirstOrDefault();

            if (paymentDetails == null)
            {
                return NotFound();
            }

            return View(paymentDetails);
        }
    }
}
