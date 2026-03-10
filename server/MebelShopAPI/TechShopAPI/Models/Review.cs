using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class Review
{
    public int IdReview { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public int Rating { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
