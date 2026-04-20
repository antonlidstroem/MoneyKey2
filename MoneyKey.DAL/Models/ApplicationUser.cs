using Microsoft.AspNetCore.Identity;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Models;

public class ApplicationUser : IdentityUser
{
    public string  FirstName        { get; set; } = string.Empty;
    public string  LastName         { get; set; } = string.Empty;
    public string? PreferredCulture { get; set; } = "sv-SE";
    /// <summary>Unique nickname shown to other users. Never exposes email to peers.</summary>
    public string  DisplayName     { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public ICollection<BudgetMembership> Memberships { get; set; } = new List<BudgetMembership>();
}
