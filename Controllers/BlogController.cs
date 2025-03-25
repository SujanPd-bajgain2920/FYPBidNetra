using FYPBidNetra.Models;
using FYPBidNetra.Security;
using FYPBidNetra.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FYPBidNetra.Controllers
{
    public class BlogController : Controller
    {
        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;

        public BlogController(FypContext context, DataSecurityProvider p,
            IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
           
        }
        public IActionResult Index()
        {
            var blogPosts = _context.BlogContents
                .Include(b => b.UploadUser)
                .OrderByDescending(b => b.Postdate)
                .ToList();
            return View(blogPosts);
        }


        public IActionResult PublishBlog()
        {
            return View("PublishBlog");
        }

        [HttpPost]
       
        public ActionResult PublishBlog(BlogContentEdit edit)
        {
            //return Json(edit);
            short maxid;
            try
            {
                // any and max are Linq
                // if data is present plus 1.
                if (_context.BlogContents.Any())
                    maxid = Convert.ToInt16(_context.BlogContents.Max(x => x.Bid + 1));
                else
                    maxid = 1;
                edit.Bid = maxid;

                if (edit.BlogFile != null)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(edit.BlogFile.FileName);
                    // webRootPath is for directory 
                    string filePath = Path.Combine(_env.WebRootPath, "BlogImage", fileName);
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        edit.BlogFile.CopyTo(stream);
                    }
                    edit.SectionImage = fileName;
                }

                var currentTime = DateTime.UtcNow.AddMinutes(345);
                var currentDate = DateOnly.FromDateTime(currentTime);
                BlogContent p = new()
                {
                    Bid = edit.Bid,
                    SectionHeading = edit.SectionHeading,
                    SectionDescription = edit.SectionDescription,
                    SectionImage = edit.SectionImage,
                    Postdate = currentDate,
                    UploadUserId = Convert.ToInt16(User.Identity.Name)
                };

                
                _context.Add(p);
                _context.SaveChanges();
                return RedirectToAction("Index", "Blog");
            }
            catch
            {
                return View();
            }
        }
    }
}
