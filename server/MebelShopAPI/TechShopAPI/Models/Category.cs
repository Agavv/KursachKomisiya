using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class Category
{
    public int IdCategory { get; set; }

    public string NameCategory { get; set; } = null!;

    public virtual ICollection<Characteristic> Characteristics { get; set; } = new List<Characteristic>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
