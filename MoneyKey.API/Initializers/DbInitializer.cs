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

        // Ensure SystemSettings table exists — this migration may not have been applied
        // by EF Migrate() if the migration file was manually created without a proper snapshot.
        // Auto-create tables that may not exist if migrations weren't applied
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemSettings')
            BEGIN
                CREATE TABLE [SystemSettings] (
                    [Id]    INT            NOT NULL IDENTITY(1,1),
                    [Key]   NVARCHAR(200)  NOT NULL,
                    [Value] NVARCHAR(MAX)  NOT NULL,
                    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_SystemSettings_Key] ON [SystemSettings] ([Key]);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserLists')
            BEGIN
                CREATE TABLE [UserLists] (
                    [Id]              INT            NOT NULL IDENTITY(1,1),
                    [BudgetId]        INT            NOT NULL,
                    [Name]            NVARCHAR(200)  NOT NULL,
                    [ListType]        INT            NOT NULL DEFAULT 3,
                    [Description]     NVARCHAR(MAX)  NULL,
                    [CreatedAt]       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt]       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedByUserId] NVARCHAR(MAX)  NULL,
                    [IsArchived]      BIT            NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_UserLists] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_UserLists_Budgets] FOREIGN KEY ([BudgetId]) REFERENCES [Budgets]([Id]) ON DELETE CASCADE
                );
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ListItems')
            BEGIN
                CREATE TABLE [ListItems] (
                    [Id]          INT            NOT NULL IDENTITY(1,1),
                    [ListId]      INT            NOT NULL,
                    [Text]        NVARCHAR(500)  NOT NULL,
                    [IsChecked]   BIT            NOT NULL DEFAULT 0,
                    [SortOrder]   INT            NOT NULL DEFAULT 0,
                    [CreatedAt]   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CompletedAt] DATETIME2      NULL,
                    CONSTRAINT [PK_ListItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ListItems_UserLists] FOREIGN KEY ([ListId]) REFERENCES [UserLists]([Id]) ON DELETE CASCADE
                );
            END
            """);


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
