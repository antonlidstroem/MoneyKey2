using MoneyKey.Core.DTOs.Lists;
using System.Text.Json;

namespace MoneyKey.Core.Services.Lists;

/// <summary>
/// Pure logic service for habit tracking.
/// No EF dependencies — testable in isolation.
/// </summary>
public static class HabitService
{
    // ── Core computation ──────────────────────────────────────────────────────

    public static HabitStatus Compute(HabitItemData data, DateTime today)
    {
        var completions = data.CompletionDates ?? new List<string>();
        var dates = completions
            .Select(s => DateTime.TryParse(s, out var d) ? (DateTime?)d.Date : null)
            .Where(d => d.HasValue).Select(d => d!.Value).ToHashSet();

        var window30 = Enumerable.Range(0, 30)
            .Select(i => today.AddDays(-i).Date)
            .ToList();

        var doneIn30 = window30.Count(d => dates.Contains(d));
        var targetIn30 = ComputeExpectedInWindow(data.TargetPerWeek, 30);
        var completionPct = targetIn30 > 0 ? (int)(doneIn30 * 100.0 / targetIn30) : 0;

        var streak = ComputeStreak(data, dates, today);
        var isDoneToday = dates.Contains(today.Date);
        var isScheduled = IsScheduledToday(data, today);

        return new HabitStatus(
            Streak: streak,
            LongestStreak: Math.Max(data.LongestStreak, streak),
            CompletionPct: Math.Min(100, completionPct),
            IsDoneToday: isDoneToday,
            IsScheduledToday: isScheduled,
            Last7: BuildWeekView(dates, today));
    }

    /// <summary>
    /// Mark today as done or undone, returning updated HabitItemData.
    /// </summary>
    public static HabitItemData Toggle(HabitItemData data, DateTime today)
    {
        var dates = new HashSet<string>(data.CompletionDates ?? new());
        var key = today.Date.ToString("yyyy-MM-dd");

        if (dates.Contains(key)) dates.Remove(key);
        else dates.Add(key);

        // Prune dates older than 90 days to keep payload small
        var cutoff = today.AddDays(-90).Date;
        var pruned = dates
            .Where(s => DateTime.TryParse(s, out var d) && d >= cutoff)
            .ToList();

        var updated = data with { CompletionDates = pruned };
        var newStatus = Compute(updated, today);

        return updated with
        {
            CurrentStreak = newStatus.Streak,
            LongestStreak = newStatus.LongestStreak,
            LastCompletedDate = pruned.Any() ? pruned.Max() : null
        };
    }

    /// <summary>
    /// Mark a specific date as "excused" (sick day, holiday etc).
    /// Stored as "{date}:excused" in CompletionDates.
    /// </summary>
    public static HabitItemData Excuse(HabitItemData data, DateTime date)
    {
        var dates = new HashSet<string>(data.CompletionDates ?? new());
        dates.Add($"{date.Date:yyyy-MM-dd}:excused");
        return data with { CompletionDates = dates.ToList() };
    }

    // ── Scheduling ────────────────────────────────────────────────────────────

    public static bool IsScheduledToday(HabitItemData data, DateTime today) =>
        data.Frequency switch
        {
            "daily" => true,
            "weekly" => true,          // show every day, target is X/week
            "alternate" => ((today.Date - new DateTime(2024, 1, 1)).Days % 2 == 0),
            _ => true
        };

    // ── Streak ────────────────────────────────────────────────────────────────

    private static int ComputeStreak(HabitItemData data, HashSet<DateTime> dates, DateTime today)
    {
        // Forgiving streak: a streak is not broken by one missed day if
        // the habit is not scheduled daily or if target is < 7/week.
        var streak = 0;
        var current = today.Date;
        var toleranceUsed = 0;
        var maxTolerance = data.TargetPerWeek < 7 ? 1 : 0; // 1 grace day for non-daily habits

        for (var i = 0; i < 365; i++)
        {
            var day = today.AddDays(-i).Date;

            if (IsDoneOrExcused(day, dates))
            {
                streak++;
                toleranceUsed = 0;
            }
            else if (IsScheduledDay(data, day) && day < today.Date)
            {
                if (toleranceUsed < maxTolerance) { toleranceUsed++; streak++; }
                else break;
            }
        }

        return streak;
    }

    private static bool IsDoneOrExcused(DateTime date, HashSet<DateTime> dates)
    {
        var key = date.ToString("yyyy-MM-dd");
        return dates.Contains(date) || dates.Contains($"{key}:excused");
    }

    private static bool IsScheduledDay(HabitItemData data, DateTime day) =>
        data.Frequency != "alternate" || ((day - new DateTime(2024, 1, 1)).Days % 2 == 0);

    private static int ComputeExpectedInWindow(int targetPerWeek, int days) =>
        (int)Math.Round(targetPerWeek * days / 7.0);

    // ── Week view ─────────────────────────────────────────────────────────────

    private static List<DayState> BuildWeekView(HashSet<DateTime> dates, DateTime today) =>
        Enumerable.Range(-6, 7).Select(offset =>
        {
            var day = today.AddDays(offset).Date;
            var key = day.ToString("yyyy-MM-dd");
            var state = day > today.Date ? "future"
                      : dates.Contains(day) ? "done"
                      : dates.Contains($"{key}:excused") ? "excused"
                      : day == today.Date ? "today"
                      : "missed";
            return new DayState(day, state);
        }).ToList();
}

// ── Data types ─────────────────────────────────────────────────────────────────

public record HabitStatus(
    int Streak,
    int LongestStreak,
    int CompletionPct,
    bool IsDoneToday,
    bool IsScheduledToday,
    List<DayState> Last7);

public record DayState(DateTime Date, string State);