using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class Order
{
    public int IdOrder { get; set; }

    public int UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    // Новые поля
    public string DeliveryType { get; set; }
    public string DeliveryAddress { get; set; }
    public string PaymentType { get; set; }
    // -

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;
}
