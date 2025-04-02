using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FYPBidNetra.Models;

public partial class Rating
{
    public short RatingId { get; set; }

    public string? RatingDescription { get; set; }

    public decimal? Rating1 { get; set; }

    

    [Column(TypeName = "DECIMAL(3,2)")]
    [Range(0, 5)]
    public decimal? Rate { get; set; }

    

    public short? RatingBy { get; set; }

    public short? RatingFor { get; set; }

    public virtual UserList? RatingByNavigation { get; set; }

    public virtual Company? RatingForNavigation { get; set; }
}
