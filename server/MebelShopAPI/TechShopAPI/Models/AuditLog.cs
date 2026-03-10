using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class AuditLog
{
    public int IdAudit { get; set; }

    public int? UserId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
