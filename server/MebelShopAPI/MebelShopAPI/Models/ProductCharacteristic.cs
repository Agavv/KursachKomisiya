using System;
using System.Collections.Generic;

namespace MebelShopAPI.Models;

public partial class ProductCharacteristic
{
    public int IdProductCharacteristic { get; set; }

    public int ProductId { get; set; }

    public int CharacteristicId { get; set; }

    public string Value { get; set; } = null!;

    public virtual Characteristic Characteristic { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
