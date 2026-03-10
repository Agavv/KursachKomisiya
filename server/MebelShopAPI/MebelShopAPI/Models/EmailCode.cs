using System;
using System.Collections.Generic;

namespace MebelShopAPI.Models;

public partial class EmailCode
{
    public int IdCode { get; set; }

    public int? UserId { get; set; }

    public string Email { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime ExpirationTime { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
