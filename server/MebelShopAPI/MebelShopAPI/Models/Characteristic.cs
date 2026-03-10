using System;
using System.Collections.Generic;

namespace MebelShopAPI.Models;

public partial class Characteristic
{
    public int IdCharacteristic { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public string ValueType { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductCharacteristic> ProductCharacteristics { get; set; } = new List<ProductCharacteristic>();
}
