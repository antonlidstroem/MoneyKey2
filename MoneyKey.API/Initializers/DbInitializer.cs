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



        // Add new columns to MilersattningEntries if they don't exist yet
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MilersattningEntries' AND COLUMN_NAME='IsRoundTrip')
                ALTER TABLE [MilersattningEntries] ADD [IsRoundTrip]  BIT           NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MilersattningEntries' AND COLUMN_NAME='PayerName')
                ALTER TABLE [MilersattningEntries] ADD [PayerName]    NVARCHAR(200) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MilersattningEntries' AND COLUMN_NAME='Status')
                ALTER TABLE [MilersattningEntries] ADD [Status]       INT           NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MilersattningEntries' AND COLUMN_NAME='SubmittedAt')
                ALTER TABLE [MilersattningEntries] ADD [SubmittedAt]  DATETIME2     NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MilersattningEntries' AND COLUMN_NAME='ApprovedAt')
                ALTER TABLE [MilersattningEntries] ADD [ApprovedAt]   DATETIME2     NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MilersattningEntries' AND COLUMN_NAME='PaidAt')
                ALTER TABLE [MilersattningEntries] ADD [PaidAt]       DATETIME2     NULL;
            """);

        // Add new columns to UserLists if they don't exist yet
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='UserLists' AND COLUMN_NAME='Content')
                ALTER TABLE [UserLists] ADD [Content]    NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='UserLists' AND COLUMN_NAME='Tags')
                ALTER TABLE [UserLists] ADD [Tags]       NVARCHAR(500) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='UserLists' AND COLUMN_NAME='Scope')
                ALTER TABLE [UserLists] ADD [Scope]      INT           NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='UserLists' AND COLUMN_NAME='Visibility')
                ALTER TABLE [UserLists] ADD [Visibility] INT           NOT NULL DEFAULT 1;
            -- Make BudgetId nullable for Personal scope entries
            """);

        // ── Jobs and TimeEntries tables ───────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Jobs')
            BEGIN
                CREATE TABLE [Jobs] (
                    [Id]              INT              NOT NULL IDENTITY(1,1),
                    [BudgetId]        INT              NOT NULL,
                    [UserId]          NVARCHAR(450)    NOT NULL,
                    [Name]            NVARCHAR(200)    NOT NULL,
                    [EmployerName]    NVARCHAR(200)    NULL,
                    [PayType]         INT              NOT NULL DEFAULT 0,
                    [TransactionMode] INT              NOT NULL DEFAULT 0,
                    [GrossAmount]     DECIMAL(18,2)    NULL,
                    [HourlyRate]      DECIMAL(10,2)    NULL,
                    [ProjectId]       INT              NULL,
                    [IsActive]        BIT              NOT NULL DEFAULT 1,
                    [Notes]           NVARCHAR(MAX)    NULL,
                    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Jobs_Budgets] FOREIGN KEY ([BudgetId])
                        REFERENCES [Budgets]([Id]) ON DELETE CASCADE
                );
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TimeEntries')
            BEGIN
                CREATE TABLE [TimeEntries] (
                    [Id]                  INT              NOT NULL IDENTITY(1,1),
                    [BudgetId]            INT              NOT NULL,
                    [UserId]              NVARCHAR(450)    NOT NULL,
                    [JobId]               INT              NOT NULL,
                    [Date]                DATE             NOT NULL,
                    [StartTime]           TIME             NULL,
                    [EndTime]             TIME             NULL,
                    [DurationMinutes]     INT              NOT NULL DEFAULT 0,
                    [Description]         NVARCHAR(500)    NULL,
                    [IsBreak]             BIT              NOT NULL DEFAULT 0,
                    [HourlyRateOverride]  DECIMAL(10,2)    NULL,
                    [LinkedTransactionId] INT              NULL,
                    [PayrollPeriodKey]    NVARCHAR(20)     NULL,
                    [CreatedAt]           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_TimeEntries] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_TimeEntries_Budgets] FOREIGN KEY ([BudgetId])
                        REFERENCES [Budgets]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_TimeEntries_Jobs] FOREIGN KEY ([JobId])
                        REFERENCES [Jobs]([Id])
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

        // ✅ KEEP ADDITIONAL SQL HERE (inside method)
        await db.Database.ExecuteSqlRawAsync("""
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserSubscriptions')
    BEGIN
        CREATE TABLE [UserSubscriptions] (
            ...
        );
    END
""");

        await db.Database.ExecuteSqlRawAsync("""
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BudgetInvitations')
    BEGIN
        CREATE TABLE [BudgetInvitations] (
            ...
        );
    END
""");

        await db.Database.ExecuteSqlRawAsync("""
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME='AspNetUsers' AND COLUMN_NAME='DisplayName')
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [DisplayName] NVARCHAR(50) NOT NULL DEFAULT '';
    END
""");

    } // ← closes InitializeAsync
} // ← closes class