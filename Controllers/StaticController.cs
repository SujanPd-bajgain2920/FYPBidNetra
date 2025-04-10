using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYPBidNetra.Controllers
{
    public class StaticController : Controller
    {

        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public StaticController(FypContext context, DataSecurityProvider p,
            IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }


        public IActionResult BlogList()
        {
            var blog = _context.BlogContents
                .Include(b => b.UploadUser)
                .Select(b => new BlogContentEdit
                {
                    Bid = b.Bid,
                    SectionHeading = b.SectionHeading,
                    SectionDescription = b.SectionDescription,
                    SectionImage = b.SectionImage,
                    Postdate = b.Postdate,
                    UploadUserId = b.UploadUserId,
                    EncId = _protector.Protect(b.Bid.ToString()),
                    FirstName = b.UploadUser.FirstName,
                    MiddleName = b.UploadUser.MiddleName,
                    LastName = b.UploadUser.LastName,
                    UserPhoto = b.UploadUser.UserPhoto
                })
                .ToList();
            return View(blog);
        }
    }
}
