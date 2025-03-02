using System;
using System.Collections.Generic;

namespace FYPBidNetra.Models;

public partial class AuctionBid
{
    public short BidId { get; set; }

    public short AuctionBidId { get; set; }

    public short BidderId { get; set; }

    public decimal BidAmount { get; set; }

    public DateOnly BidDate { get; set; }

    public TimeOnly BidTime { get; set; }

    public string BidStatus { get; set; } = null!;

    public virtual AuctionDetail AuctionBidNavigation { get; set; } = null!;

    public virtual UserList Bidder { get; set; } = null!;

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
