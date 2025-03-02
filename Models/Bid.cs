using System;
using System.Collections.Generic;

namespace FYPBidNetra.Models;

public partial class Bid
{
    public short BiddingId { get; set; }

    public short AucBidId { get; set; }

    public decimal BiddingAmount { get; set; }

    public virtual AuctionBid AucBid { get; set; } = null!;
}
