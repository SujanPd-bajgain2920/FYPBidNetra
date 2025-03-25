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


        public IActionResult BlogPage()
        {
            var blogPosts = _context.BlogContents
                .Include(b => b.UploadUser)
                .OrderByDescending(b => b.Postdate)
                .ToList();
            return View(blogPosts);
        }
    }
}
