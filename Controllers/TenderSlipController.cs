using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

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
                    PayFromUser = new UserListEdit
                    {
                        UserId = (short)currentUserId,
                        FirstName = _context.UserLists
                            .Where(u => u.UserId == currentUserId)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TenderPayment(PaymentEdit model)
    {
        //return Json(model);

        try
        {
            if (model.SlipFile != null)
            {
                string fileName = "SlipImage" + Guid.NewGuid() + Path.GetExtension(model.SlipFile.FileName);
                string filePath = Path.Combine(_env.WebRootPath, "SlipImage", fileName);

                if (!Directory.Exists(Path.Combine(_env.WebRootPath, "SlipImage")))
                {
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "SlipImage"));
                }
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    model.SlipFile.CopyTo(stream);
                }
                model.SlipUpload = fileName;
            }



            // Create new payment record
            var payment = new Payment
            {
                PaymentId = (short)(_context.Payments.Any() ?
                        _context.Payments.Max(p => p.PaymentId) + 1 : 1),
                PayTenderId = model.TenderId,
                PayCompanyId = model.PayToCompany.CompanyId,
                PayToUser = (short)model.PayToCompany.UserbidId,
                PayByUser = model.PayFromUser.UserId,
                PaymentAmount = model.PaymentAmount,
                PaymentDate = model.PaymentDate,
                PaymentMethod = "Deposit",
                SlipUpload = model.SlipUpload,
                PaymentStatus = "Pending"
            };

            return Json(payment);

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment submitted successfully!";
            return RedirectToAction("AwardedTender", "PublisherTender");
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "An error occurred while processing the payment.");
            return View(model);
        }
    }
}