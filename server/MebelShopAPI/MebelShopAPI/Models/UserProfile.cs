using System;
using System.Collections.Generic;

namespace MebelShopAPI.Models;

public partial class UserProfile
{
    public int UserId { get; set; }

    public string? FirstName { get; set; }

    public string? Surname { get; set; }

    public string? Theme { get; set; }

    public virtual User User { get; set; } = null!;
}
