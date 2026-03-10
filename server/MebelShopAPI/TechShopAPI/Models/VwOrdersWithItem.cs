using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class VwOrdersWithItem
{
    public int IdOrder { get; set; }

    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
