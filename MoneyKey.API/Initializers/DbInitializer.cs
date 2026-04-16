using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Models;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Initializers;

/// <summary>
/// Creates a first-run admin account when the database contains no users.
/// Credentials come from AdminSetup config — never hardcoded.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp       = scope.ServiceProvider;
        var users    = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db       = sp.GetRequiredService<BudgetDbContext>();
        var cfg      = sp.GetRequiredService<IConfiguration>();
        var log      = sp.GetRequiredService<ILogger<BudgetDbContext>>();

        if (await users.Users.AnyAsync())
        {
            log.LogDebug("DbInitializer: users already exist, skipping.");
            return;
        }

        var email     = cfg["AdminSetup:Email"];
        var password  = cfg["AdminSetup:Password"];
        var firstName = cfg["AdminSetup:FirstName"] ?? "Admin";
        var lastName  = cfg["AdminSetup:LastName"]  ?? "Budgetsson";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            log.LogWarning("DbInitializer: AdminSetup:Email / AdminSetup:Password not configured. No initial admin created.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            FirstName = firstName, LastName = lastName
        };

        var result = await users.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            log.LogError("DbInitializer: failed to create admin — {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        var budget = new Budget { Name = $"{firstName}s budget", OwnerId = admin.Id };
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        db.BudgetMemberships.Add(new BudgetMembership
        {
            BudgetId = budget.Id, UserId = admin.Id,
            Role = BudgetMemberRole.Owner, AcceptedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        log.LogInformation("DbInitializer: initial admin created — {Email}", email);
    }
}
