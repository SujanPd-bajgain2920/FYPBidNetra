using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace FYPBidNetra.Controllers
{
    public class ChatController : Controller
    {

        private readonly FypContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(FypContext context, IWebHostEnvironment env, DataSecurityProvider key, IDataProtectionProvider provider, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _env = env;
            _protector = provider.CreateProtector(key.Key);
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> UserMessage(short receiverId)
        {
            var currentUserId = Convert.ToInt16(User.Identity!.Name); // Assuming UserId is stored in Identity

            // Fetch messages between the current user and the receiver
            var messages = await _context.Chats
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                             (m.SenderId == receiverId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.CreatedDate)
                .ToListAsync();

            // Fetch the receiver's details
            /* var receiver = await _context.UserLists
                 .Where(u => u.UserId == receiverId)
                 .Select(u => new { u.FirstName, u.MiddleName, u.LastName })
                 .FirstOrDefaultAsync();

             ViewBag.ReceiverName = receiver?.FirstName;*/

            var receiver = await _context.UserLists
                .Where(u => u.UserId == receiverId)
                .Select(u => new
                {
                    FullName = (u.FirstName + " " + (u.MiddleName ?? "") + " " + u.LastName).Trim(),
                    UserPhoto = u.UserPhoto
                })
                .FirstOrDefaultAsync();

            ViewBag.ReceiverName = receiver?.FullName;
            ViewBag.ProfilePic = receiver?.UserPhoto;
            ViewBag.CurrentUserId = currentUserId; // Pass current user ID for comparison
            ViewBag.ReceiverId = receiverId; // Pass receiver ID to the view

            // Return the messages to the view
            return View(messages);
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
