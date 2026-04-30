using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Models;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Initializers;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db = sp.GetRequiredService<BudgetDbContext>();
        var cfg = sp.GetRequiredService<IConfiguration>();
        var log = sp.GetRequiredService<ILogger<BudgetDbContext>>();

        // ── 1. Säkerställ att vi inte seedar om det redan finns användare ────────
        if (await users.Users.AnyAsync())
        {
            return;
        }

        // ── 2. Hämta config ──────────────────────────────────────────────────────
        var email = cfg["AdminSetup:Email"];
        var password = cfg["AdminSetup:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        // ── 3. Skapa Admin ───────────────────────────────────────────────────────
        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = cfg["AdminSetup:FirstName"] ?? "Admin",
            LastName = cfg["AdminSetup:LastName"] ?? "Budgetsson",
            DisplayName = "admin"
        };

        var result = await users.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            log.LogError("Seed failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        // ── 4. Skapa Budget & Membership via EF (inte SQL) ───────────────────────
        var budget = new Budget { Name = $"{admin.FirstName}s budget", OwnerId = admin.Id };
        db.Budgets.Add(budget);

        // Vi måste spara här för att få ett BudgetId
        await db.SaveChangesAsync();

        db.BudgetMemberships.Add(new BudgetMembership
        {
            BudgetId = budget.Id,
            UserId = admin.Id,
            Role = BudgetMemberRole.Owner,
            AcceptedAt = DateTime.UtcNow
        });

        // ── 5. Skapa Subscription via EF (Antar att du har en Model för denna) ──
        // Om du har en modell för UserSubscription, använd den. 
        // Annars kan du köra din SQL, men bara för INSERT, inte för CREATE TABLE.
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO [UserSubscriptions] ([UserId],[Tier],[IsAdmin],[CreatedAt],[UpdatedAt]) VALUES ({0}, 0, 1, GETUTCDATE(), GETUTCDATE())",
            admin.Id);

        await db.SaveChangesAsync();
        log.LogInformation("Database seeded successfully with admin: {Email}", email);
    }
}
