/*using Microsoft.AspNetCore.SignalR;

namespace BidNetra.Models
{
    public class AuctionHub : Hub
    {
        // Method to send the new bid details to clients
        public async Task SendBidUpdate(int auctionId, int userId, decimal bidAmount, DateTime bidTime)
        {
            await Clients.All.SendAsync("ReceiveBid", auctionId, userId, bidAmount, bidTime);
        }
    }
}
*/

using FYPBidNetra.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class AuctionHub : Hub
{
    /* public async Task SendBid(int auctionId, int userId, decimal bidAmount, DateTime bidTime)
     {
         await Clients.All.SendAsync("ReceiveBid", auctionId, userId, bidAmount, bidTime);
     }*/

    private readonly FypContext _context;

    public AuctionHub(FypContext context)
    {
        _context = context;
    }

    public async Task SendBid(int auctionId, int userId, decimal bidAmount, DateTime bidTime)
    {
        var user = await _context.UserLists.FindAsync(userId);
        string bidderName = user != null ? $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim() : "Unknown User";
        Console.WriteLine($"Bid By {bidderName}");
        await Clients.All.SendAsync("ReceiveBidUpdate", auctionId, bidderName, bidAmount, bidTime);
    }
}
