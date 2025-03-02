using Microsoft.AspNetCore.Mvc;

namespace FYPBidNetra.Controllers
{
    public class StaticController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
