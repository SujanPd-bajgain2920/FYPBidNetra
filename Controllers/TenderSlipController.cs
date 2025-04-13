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

    /* [HttpPost]
     [ValidateAntiForgeryToken]
     public async Task<IActionResult> TenderPayment(PaymentEdit model)
     {
         try
         {
             if (model.SlipFile == null)
             {
                 ModelState.AddModelError("SlipFile", "Payment slip is required");
                 return View(model);
             }

             // Call Flask verification API
             using (var httpClient = new HttpClient())
             {
                 var form = new MultipartFormDataContent();
                 form.Add(new StringContent(model.PayTenderId.TenderId.ToString()), "pay_tender_id");
                 form.Add(new StringContent(model.PayToCompany.CompanyId.ToString()), "pay_company_id");
                 form.Add(new StringContent(model.PayFromUser.UserId.ToString()), "pay_by_user");
                 form.Add(new StringContent(model.PayToUser.UserId.ToString()), "pay_to_user");
                 form.Add(new StringContent(model.PaymentAmount.ToString()), "payment_amount");
                 form.Add(new StringContent("Deposit"), "payment_method");

                 using (var fileStream = model.SlipFile.OpenReadStream())
                 {
                     form.Add(new StreamContent(fileStream), "file", model.SlipFile.FileName);
                 }

                 var response = await httpClient.PostAsync("http://127.0.0.1:5000/verify-payment", form);

                 if (!response.IsSuccessStatusCode)
                 {
                     var errorContent = await response.Content.ReadAsStringAsync();
                     ModelState.AddModelError("", $"Payment verification failed: {errorContent}");
                     return View(model);
                 }

                 var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

                 if (result["payment_status"].ToString() != "Verified")
                 {
                     ModelState.AddModelError("", "Payment verification failed. Please upload a valid payment slip.");
                     return View(model);
                 }

                 // If verification succeeded, show success message
                 TempData["SuccessMessage"] = "Payment submitted and verified successfully!";
                 return RedirectToAction("AwardedTender", "PublisherTender");
             }
         }
         catch (Exception ex)
         {
             ModelState.AddModelError("", $"An error occurred while processing the payment: {ex.Message}");
             return View(model);
         }
     }*/
}