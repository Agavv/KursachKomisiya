using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class VwProductsWithCategory
{
    public int IdProduct { get; set; }

    public string ProductName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string NameCategory { get; set; } = null!;
}
