namespace MoneyKey.Core.DTOs.Lists;

/// <summary>
/// Stored in UserList.ListConfig for Habit lists.
/// </summary>
public record HabitListConfig(
    string? DefaultFrequency = "daily",
    int DefaultTargetPerWeek = 7,
    bool ShowWeekView = true
);