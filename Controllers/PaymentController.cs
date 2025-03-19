using Microsoft.AspNetCore.Mvc;
using payment_gateway_nepal;
using FYPBidNetra.Models;
using Microsoft.EntityFrameworkCore;

namespace FYPBidNetra.Controllers
{
    public class PaymentController : Controller
    {
        private readonly FypContext _context;
        private readonly string eSewa_TestKey = "8gBm/:&EnhH.1/q";
        private readonly bool testMode = true;

        public PaymentController(FypContext context)
        {
            _context = context;
        }


        
        public async Task<IActionResult> Index()
        {
            int currentUserId = Convert.ToInt16(User.Identity.Name);

            var pendingPayments = await _context.Payments
                .Include(p => p.PayTender) 
                .Include(p => p.PayToUserNavigation) 
                .Include(p => p.PayByUserNavigation) 
                .Include(p => p.PayCompany) 
                .Where(p => p.PayByUser == currentUserId && p.PaymentStatus == "Pending")
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(pendingPayments);
        }

        [HttpPost]
        public async Task<IActionResult> InitiatePayment(short tenderId)
        {
            try
            {
                int currentUserId = Convert.ToInt16(User.Identity.Name);
                var tender = await _context.TenderDetails
                    .Include(t => t.PublishedByUser)
                    .FirstOrDefaultAsync(t => t.TenderId == tenderId);

                if (tender == null)
                    return Json(new { success = false, message = "Tender not found." });

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserbidId == currentUserId);

                if (company == null)
                    return Json(new { success = false, message = "Company details not found." });

                PaymentManager paymentManager = new PaymentManager(
                    PaymentMethod.eSewa,
                    PaymentVersion.v2,
                    PaymentMode.Sandbox,
                    eSewa_TestKey
                );

                string currentUrl = new Uri($"{Request.Scheme}://{Request.Host}").AbsoluteUri;
                string referenceId = $"bid-{DateTime.Now.Ticks}";

                

                dynamic request = new
                {
                    Amount = 10,
                    TaxAmount = 0,
                    TotalAmount = 10,
                    TransactionUuid = referenceId,
                    ProductCode = "EPAYTEST",
                    ProductServiceCharge = 0,
                    ProductDeliveryCharge = 0,
                    // Use HttpUtility.UrlEncode for the URLs
                    SuccessUrl = $"{currentUrl.TrimEnd('/')}/Payment/VerifyPayment",
                    FailureUrl = $"{currentUrl.TrimEnd('/')}/Payment/PaymentFailure",
                    SignedFieldNames = "total_amount,transaction_uuid,product_code"
                };

                

                var response = await paymentManager.InitiatePaymentAsync<ApiResponse>(request);
                return Json(new { success = true, redirectUrl = response.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Payment initiation failed. Please try again." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyPayment(string data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"VerifyPayment called with data: {data}");

                PaymentManager paymentManager = new PaymentManager(
                    PaymentMethod.eSewa,
                    PaymentVersion.v2,
                    PaymentMode.Sandbox,
                    eSewa_TestKey
                );

                // Parse the response data
                eSewaResponse response = await paymentManager.VerifyPaymentAsync<eSewaResponse>(data);
                System.Diagnostics.Debug.WriteLine($"eSewa Response Status: {response?.status}");

                if (!string.IsNullOrEmpty(response?.status) &&
                    string.Equals(response.status, "COMPLETE", StringComparison.OrdinalIgnoreCase))
                {
                    // Find the most recent pending payment for the current user
                    int currentUserId = Convert.ToInt16(User.Identity.Name);
                    var payment = await _context.Payments
                        .Where(p => p.PayByUser == currentUserId && p.PaymentStatus == "Pending")
                        .OrderByDescending(p => p.PaymentDate)
                        .FirstOrDefaultAsync();

                    if (payment != null)
                    {
                        payment.PaymentStatus = "Verified";
                        await _context.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine("Payment verified successfully");

                        return RedirectToAction("TenderBidTab", "BidTender", new { activeTab = "ApplyTenderList" });
                    }
                }

                System.Diagnostics.Debug.WriteLine("Payment verification failed");
                return RedirectToAction("PaymentFailure");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in VerifyPayment: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("PaymentFailure");
            }
        }

        [HttpGet]
        public IActionResult PaymentFailure()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(short paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);

            if (payment == null)
                return Json(new { success = false, message = "Payment not found" });

            return Json(new
            {
                success = true,
                status = payment.PaymentStatus,
                completed = payment.PaymentStatus == "Verified"
            });
        }
    }
}