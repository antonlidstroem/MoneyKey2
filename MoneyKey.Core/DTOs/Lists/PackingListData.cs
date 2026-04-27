namespace MoneyKey.Core.DTOs.Lists;

/// <summary>
/// Type-specific configuration stored in UserList.ListConfig for Packing lists.
/// </summary>
public record PackingListConfig(
    string? TemplateName = null,
    List<string>? Categories = null,   // Custom category order
    string? LastResetDate = null,   // ISO date of last reset
    bool ShowByCategory = true
);

/// <summary>
/// Type-specific payload stored in ListItem.ItemData for Packing list items.
/// </summary>
public record PackingItemData(
    string? Category = "Övrigt",
    int Quantity = 1,
    bool IsEssential = false
);

// ── Future types — defined now to stabilize the namespace ────────────

public record HabitItemData(
    string Frequency = "daily",  // "daily" | "weekly" | "custom"
    int TargetPerWeek = 7,
    int EnergyRequired = 3,        // 1–5
    int CurrentStreak = 0,
    int LongestStreak = 0,
    string? LastCompletedDate = null,
    List<string>? CompletionDates = null // ISO dates, rolling 90 days
);

public record DecisionAlternative(
    string Name = "",
    List<string> Pros = null!,
    List<string> Cons = null!,
    int GutFeeling = 50,     // 0–100 slider
    bool IsSelected = false
);

public record ProjectItemData(
    string Status = "todo",  // "idea" | "todo" | "doing" | "blocked" | "done"
    string? AssignedTo = null,
    string? Deadline = null,    // ISO date
    string? Notes = null,
    List<string>? SubTasks = null
);

public record InventoryItemData(
    string? Category = null,
    string? Location = null,
    decimal? Value = null,
    string? PurchaseDate = null,   // ISO date
    string? WarrantyEnd = null,   // ISO date
    int? LinkedReceiptId = null,
    string? SerialNumber = null
);