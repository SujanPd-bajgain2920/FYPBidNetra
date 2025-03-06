using FYPBidNetra.Models;
using Microsoft.EntityFrameworkCore;

namespace FYPBidNetra.Services
{
    public class AuctionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionBackgroundService> _logger;

        public AuctionBackgroundService(IServiceProvider serviceProvider, ILogger<AuctionBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<FypContext>();
                        await ProcessAuctions(context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing auctions");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessAuctions(FypContext context)
        {
            var currentDate = DateTime.UtcNow.AddMinutes(345);
            var auctions = await context.AuctionDetails.ToListAsync();

            foreach (var auction in auctions)
            {
                var openingDateTime = new DateTime(auction.StartDate.Year, auction.StartDate.Month, auction.StartDate.Day,
                                               auction.StartTime.Hour, auction.StartTime.Minute, auction.StartTime.Second);
                var closingDateTime = new DateTime(auction.EndDate.Year, auction.EndDate.Month, auction.EndDate.Day,
                                               auction.EndTime.Hour, auction.EndTime.Minute, auction.EndTime.Second);

                if (currentDate >= openingDateTime && currentDate < closingDateTime)
                {
                    auction.AuctionStatus = "Active";
                }
                else if (currentDate >= closingDateTime && auction.AuctionStatus != "Completed")
                {
                    await CompleteAuction(context, auction);
                }
                else if (currentDate < openingDateTime)
                {
                    auction.AuctionStatus = "Pending";
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task CompleteAuction(FypContext context, AuctionDetail auction)
        {
            auction.AuctionStatus = "Completed";

            var highestBid = await context.AuctionBids
                .Where(b => b.AuctionBidId == auction.AuctionId)
                .OrderByDescending(b => b.BidAmount)
                .FirstOrDefaultAsync();

            if (highestBid != null)
            {
                highestBid.BidStatus = "Accepted";
                auction.BuyerId = highestBid.BidderId;
                auction.WinningBidAmount = highestBid.BidAmount;

                var otherBids = await context.AuctionBids
                    .Where(b => b.AuctionBidId == auction.AuctionId && b.BidId != highestBid.BidId)
                    .ToListAsync();

                foreach (var bid in otherBids)
                {
                    bid.BidStatus = "Rejected";
                }

                var nextContractId = (await context.ContractDetails.MaxAsync(c => (int?)c.ContractId) ?? 0) + 1;
                var contract = new ContractDetail
                {
                    ContractId = (short)nextContractId,
                    ConAuctionId = auction.AuctionId,
                    SellerId = auction.PublishedByUserId,
                    BuyerId = highestBid.BidderId,
                    ContractCreateDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(345)),
                    ContractStatus = "Pending",
                    SignedBySeller = false,
                    SignedByBuyer = false
                };

                await context.AddAsync(contract);
            }
        }
    }
}