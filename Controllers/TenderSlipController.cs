using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
public class TenderSlipController : Controller
{
    private readonly FypContext _context;
    private readonly IDataProtector _protector;
    private readonly IWebHostEnvironment _env;

    public TenderSlipController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
    {
        _context = context;
        _protector = provider.CreateProtector(p.Key);
        _env = env;
    }

    [HttpGet]
    public IActionResult TenderPayment(string tenderId)
    {
        try
        {
            int decryptedTenderId = Convert.ToInt32(_protector.Unprotect(tenderId));
            int currentUserId = Convert.ToInt16(User.Identity!.Name);

            var paymentDetails = _context.TenderDetails
                .Where(t => t.TenderId == decryptedTenderId)
                .Select(t => new PaymentEdit
                {
                    TenderId = t.TenderId,
                    TenderTitle = t.Title,
                    PaymentAmount = t.BudgetEstimation,
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
                        UserId = t.AwardCompany.CompanyId,
                        FirstName = _context.UserLists
                            .Where(u => u.UserId == t.AwardCompany.CompanyId)
                            .Select(u => u.FirstName + " " + u.LastName)
                            .FirstOrDefault()
                    },
                    PayToCompany = new CompanyEdit
                    {
                        CompanyId = t.AwardCompany.CompanyId,
                        CompanyName = t.AwardCompany.CompanyName,
                        FullAddress = t.AwardCompany.FullAddress
                    },
                    PayTenderId = new TenderEdit
                    {
                        TenderId = t.TenderId
                       
                    },
                    EncryptedTenderId = tenderId
                })
                .FirstOrDefault();

            if (paymentDetails == null)
            {
                return NotFound("Tender not found");
            }

            return View(paymentDetails);
        }
        catch (Exception)
        {
            return BadRequest("Invalid tender ID");
        }
    }

    [HttpGet]
    public IActionResult PaymentSuccess(int tenderId)
    {
        var paymentDetails = _context.TenderDetails
            .Where(t => t.TenderId == tenderId)
            .Select(t => new PaymentEdit
            {
                TenderId = t.TenderId,
                TenderTitle = t.Title,
                PaymentAmount = t.BudgetEstimation,
                PaymentDate = DateTime.UtcNow.AddMinutes(345), // Nepali time
                PayFromUser = new UserListEdit
                {
                    UserId = (short)Convert.ToInt16(User.Identity!.Name),
                    FirstName = _context.UserLists
                        .Where(u => u.UserId == Convert.ToInt16(User.Identity.Name))
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault()
                },
                PayToUser = new UserListEdit
                {
                    UserId = t.PublishedByUserId,
                    FirstName = _context.UserLists
                        .Where(u => u.UserId == t.PublishedByUserId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault()
                },
                PayToCompany = new CompanyEdit
                {
                    CompanyId = t.AwardCompany.CompanyId,
                    CompanyName = t.AwardCompany.CompanyName,
                    FullAddress = t.AwardCompany.FullAddress
                }
            })
            .FirstOrDefault();

        if (paymentDetails == null)
        {
            return NotFound();
        }

        return View(paymentDetails);
    }


}