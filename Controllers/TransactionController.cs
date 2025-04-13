using FYPBidNetra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYPBidNetra.Controllers
{
    public class TransactionController : Controller
    {
        private readonly FypContext _context;

        public TransactionController(FypContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, string paymentType, string paymentStatus, DateTime? startDate, DateTime? endDate)
        {
            int userId = Convert.ToInt16(User.Identity!.Name);

            // Get all payments where user is either sender or receiver
            var query = _context.Payments
                .Where(p => p.PayByUser == userId || p.PayToUser == userId)
                .Include(p => p.PayTender)
                .Include(p => p.PayAuction)
                .Include(p => p.PayByUserNavigation)
                .Include(p => p.PayToUserNavigation)
                .Include(p => p.PayCompany)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    (p.PayTender != null && p.PayTender.Title.Contains(searchString)) ||
                    (p.PayAuction != null && p.PayAuction.Title.Contains(searchString)) ||
                    p.PayByUserNavigation.FirstName.Contains(searchString) ||
                    p.PayToUserNavigation.FirstName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(paymentType))
            {
                query = query.Where(p => p.PaymentMethod == paymentType);
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                query = query.Where(p => p.PaymentStatus == paymentStatus);
            }

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= endDate);
            }

            var payments = await query.ToListAsync();

            // Calculate totals
            var totalEarnings = payments
                .Where(p => p.PayToUser == userId && p.PaymentStatus == "Verified")
                .Sum(p => p.PaymentAmount);

            var totalSpent = payments
                .Where(p => p.PayByUser == userId && p.PaymentStatus == "Verified")
                .Sum(p => p.PaymentAmount);

            ViewBag.TotalEarnings = totalEarnings;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.CurrentFilter = searchString;
            ViewBag.PaymentTypeFilter = paymentType;
            ViewBag.PaymentStatusFilter = paymentStatus;
            ViewBag.StartDateFilter = startDate;
            ViewBag.EndDateFilter = endDate;

            return View(payments);
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.PayTender)
                .Include(p => p.PayAuction)
                .Include(p => p.PayByUserNavigation)
                .Include(p => p.PayToUserNavigation)
                .Include(p => p.PayCompany)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}