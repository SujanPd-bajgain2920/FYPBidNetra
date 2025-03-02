using Microsoft.AspNetCore.Mvc;

namespace FYPBidNetra.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
