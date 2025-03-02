using System;
using System.Collections.Generic;

namespace FYPBidNetra.Models;

public partial class ContractDetail
{
    public short ContractId { get; set; }

    public short? ConTenderId { get; set; }

    public short? ConCompanyId { get; set; }

    public short? ConAuctionId { get; set; }

    public short SellerId { get; set; }

    public short BuyerId { get; set; }

    public DateOnly ContractCreateDate { get; set; }

    public string ContractStatus { get; set; } = null!;

    public bool SignedBySeller { get; set; }

    public bool SignedByBuyer { get; set; }

    public string? SellerSignature { get; set; }

    public string? BuyerSignature { get; set; }

    public virtual UserList Buyer { get; set; } = null!;

    public virtual AuctionDetail? ConAuction { get; set; }

    public virtual Company? ConCompany { get; set; }

    public virtual TenderDetail? ConTender { get; set; }

    public virtual UserList Seller { get; set; } = null!;

    public virtual ICollection<Term> Terms { get; set; } = new List<Term>();
}
