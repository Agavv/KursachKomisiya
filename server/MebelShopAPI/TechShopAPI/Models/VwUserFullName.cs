using System;
using System.Collections.Generic;

namespace TechShopAPI.Models;

public partial class VwUserFullName
{
    public int IdUser { get; set; }

    public string Email { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public string? FullName { get; set; }
}
