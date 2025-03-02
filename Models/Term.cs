using System;
using System.Collections.Generic;

namespace FYPBidNetra.Models;

public partial class Term
{
    public short TermId { get; set; }

    public short ContractId { get; set; }

    public string TermDescription { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public short CreatedBy { get; set; }

    public virtual ContractDetail Contract { get; set; } = null!;

    public virtual UserList CreatedByNavigation { get; set; } = null!;
}
