using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYPBidNetra.Controllers
{
    public class CompanyController : Controller
    {
        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;


        public CompanyController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }

        public IActionResult Index(string id)
        {
            try
            {
                int companyId = Convert.ToInt32(_protector.Unprotect(id));

                var company = _context.Companies
                    .Include(c => c.Userbid)
                    .Where(c => c.CompanyId == companyId)
                    .Select(c => new CompanyEdit
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        FullAddress = c.FullAddress,
                        OfficeEmail = c.OfficeEmail,
                        CompanyWebsiteUrl = c.CompanyWebsiteUrl,
                        RegistrationNumber = c.RegistrationNumber,
                        RegistrationDocument = c.RegistrationDocument,
                        PanNumber = c.PanNumber,
                        PanDocument = c.PanDocument,
                        CompanyType = c.CompanyType,
                        Position = c.Position,
                        Rating = c.Rating,
                        UserbidId = c.UserbidId,
                        EncId = _protector.Protect(c.CompanyId.ToString()),
                        // Include user details
                        User = new UserListEdit
                        {
                            UserId = c.Userbid.UserId,
                            FirstName = c.Userbid.FirstName,
                            MiddleName = c.Userbid.MiddleName,
                            LastName = c.Userbid.LastName,
                            EmailAddress = c.Userbid.EmailAddress,
                            Phone = c.Userbid.Phone,
                            Province = c.Userbid.Province,
                            District = c.Userbid.District,
                            City = c.Userbid.City,
                            UserRole = c.Userbid.UserRole,
                            UserPhoto = c.Userbid.UserPhoto
                        }
                    })
                    .FirstOrDefault();

                if (company == null)
                {
                    return NotFound("Company not found.");
                }

                return View(company);
            }
            catch (Exception ex)
            {
                return NotFound("Invalid company ID or company not found.");
            }
        }



        [HttpGet]
        public IActionResult ViewReview(string id)
        {
            try
            {
                int companyId = Convert.ToInt32(_protector.Unprotect(id));
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return NotFound("Company not found.");
                }

                var model = new CompanyReviewViewModel
                {
                    CompanyId = id,
                    CompanyName = company.CompanyName,
                    CompanyRating = company.Rating,
                    NewRating = new RatingEdit(),
                    Reviews = _context.Ratings
                        .Include(r => r.RatingByNavigation)
                        .Where(r => r.RatingFor == companyId)
                        .Select(r => new RatingEdit
                        {
                            RatingId = r.RatingId,
                            Rate = (decimal)r.Rate,
                            RatingDescription = r.RatingDescription,
                            ReviewerName = $"{r.RatingByNavigation.FirstName} {r.RatingByNavigation.LastName}",
                            ReviewerPhoto = r.RatingByNavigation.UserPhoto
                        })
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return NotFound("Invalid company ID or reviews not found.");
            }
        }


        [HttpPost]

        public IActionResult AddRating(RatingEdit r)
        {
            try
            {
                int decryptedCompanyId = Convert.ToInt32(_protector.Unprotect(r.CompanyId));

                // Add new rating
                short newRatingId = 1;
                if (_context.Ratings.Any())
                {
                    newRatingId = (short)(_context.Ratings.Max(c => c.RatingId) + 1);
                }

                var newRating = new Rating
                {
                    RatingId = newRatingId,
                    RatingDescription = r.RatingDescription,
                    Rate = r.Rate,  // Ensure this is decimal in your Rating model
                    RatingBy = Convert.ToInt16(User.Identity!.Name),
                    RatingFor = (short?)decryptedCompanyId
                };

                _context.Ratings.Add(newRating);
                _context.SaveChanges();



                 // Calculate new average rating
                var avgrating = _context.Ratings
                    .Where(rt => rt.RatingFor == decryptedCompanyId)
                    .Average(rt => rt.Rate);

                // Find the company and update rating
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == decryptedCompanyId);
                if (company == null)
                {
                    throw new Exception("Company not found.");
                }

                company.Rating = avgrating;
                _context.Entry(company).State = EntityState.Modified;
                _context.SaveChanges();

                return RedirectToAction("ViewReview", new { id = r.CompanyId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return RedirectToAction("ViewReview", new { id = r.CompanyId, error = ex.Message });
            }
        }


        /* private void UpdateCompanyAverageRating(int companyId)
         {
             // Check if there are any ratings for this company
             if (_context.Ratings.Any(r => r.RatingFor == companyId))
             {
                 // Calculate average rating
                 var averageRating = _context.Ratings
                     .Where(rt => rt.RatingFor == companyId)
                     .Average(rt => rt.Rate);

                 // Update company rating
                 var company = _context.Companies.Find(companyId);
                 if (company != null)
                 {
                     company.Rating = averageRating;
                     _context.SaveChanges();
                 }
             }
         }*/
    }
}
