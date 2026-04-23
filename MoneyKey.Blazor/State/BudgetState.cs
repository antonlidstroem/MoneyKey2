using MoneyKey.Core.DTOs.Budget;
using MoneyKey.Core.DTOs.Subscription;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.State;

public class BudgetState
{
    public int ActiveBudgetId { get; private set; }
    public string ActiveBudgetName { get; private set; } = string.Empty;
    public BudgetMemberRole MyRole { get; private set; }
    public List<BudgetDto> MyBudgets { get; private set; } = new();

    private HashSet<string> _disabledFeatures = new(StringComparer.OrdinalIgnoreCase);

    public event Action? StateChanged;

    public bool CanEdit => MyRole is BudgetMemberRole.Editor or BudgetMemberRole.Owner;
    public bool IsOwner => MyRole == BudgetMemberRole.Owner;
    public bool IsFeatureEnabled(string feature) => !_disabledFeatures.Contains(feature);

    public void SetFeatures(IEnumerable<string> disabledKeys)
    {
        _disabledFeatures = new HashSet<string>(disabledKeys, StringComparer.OrdinalIgnoreCase);
        StateChanged?.Invoke();
    }

    public void SetBudgets(List<BudgetDto> budgets)
    {
        MyBudgets = budgets;

        if (!budgets.Any()) { StateChanged?.Invoke(); return; }

        var current = budgets.FirstOrDefault(b => b.Id == ActiveBudgetId);
        if (current != null)
        {
            // Active budget still exists — update metadata without clearing features
            // or triggering a full context switch. This prevents unwanted reloads
            // when SetBudgets is called after e.g. creating a new budget.
            ActiveBudgetName = current.Name;
            MyRole = current.MyRole;
            StateChanged?.Invoke();
        }
        else
        {
            // Active budget no longer in list — switch to first available
            SetActiveBudget(budgets.First());
        }
    }

    /// <summary>
    /// Switches the active budget context. Only clears feature flags when the
    /// budget actually changes (prevents unnecessary reloads from repeated calls
    /// with the same budget).
    /// </summary>
    public void SetActiveBudget(BudgetDto b)
    {
        var budgetChanged = ActiveBudgetId != b.Id;
        ActiveBudgetId = b.Id;
        ActiveBudgetName = b.Name;
        MyRole = b.MyRole;
        if (budgetChanged) _disabledFeatures.Clear();
        StateChanged?.Invoke();
    }

    public void UpdateActiveBudgetName(string newName)
    {
        ActiveBudgetName = newName;
        var b = MyBudgets.FirstOrDefault(x => x.Id == ActiveBudgetId);
        if (b != null)
        {
            var idx = MyBudgets.IndexOf(b);
            MyBudgets[idx] = new BudgetDto(b.Id, newName, b.Description, b.IsActive, b.CreatedAt, b.MyRole);
        }
        StateChanged?.Invoke();
    }

    // ── Subscription / user info ──────────────────────────────────────────────
    public string DisplayName { get; private set; } = string.Empty;
    public SubscriptionTier Tier { get; private set; } = SubscriptionTier.Free;
    public bool IsSystemAdmin { get; private set; }
    public int MaxBudgets { get; private set; } = 2;
    public int MaxMembers { get; private set; } = 1;
    public int UsedBudgets { get; private set; }

    public void SetSubscription(SubscriptionDto s)
    {
        DisplayName = s.DisplayName;
        Tier = s.Tier;
        IsSystemAdmin = s.IsAdmin;
        MaxBudgets = s.MaxBudgets == int.MaxValue ? 9999 : s.MaxBudgets;
        MaxMembers = s.MaxMembers == int.MaxValue ? 9999 : s.MaxMembers;
        UsedBudgets = s.UsedBudgets;
        StateChanged?.Invoke();
    }

    // ── Pending invitations badge ─────────────────────────────────────────────
    public int PendingInvitations { get; private set; }
    public void SetPendingInvitations(int count) { PendingInvitations = count; StateChanged?.Invoke(); }

    // ── Live timer state ──────────────────────────────────────────────────────
    public int? TimerJobId { get; private set; }
    public string? TimerJobName { get; private set; }
    public DateTime? TimerStartedAt { get; private set; }
    public bool IsTimerRunning => TimerStartedAt.HasValue;

    public void StartTimer(int jobId, string jobName)
    {
        TimerJobId = jobId;
        TimerJobName = jobName;
        TimerStartedAt = DateTime.Now;
        StateChanged?.Invoke();
    }

    public (int minutes, int jobId, string jobName, DateTime start) StopTimer()
    {
        var start = TimerStartedAt ?? DateTime.Now;
        var minutes = (int)(DateTime.Now - start).TotalMinutes;
        var jobId = TimerJobId ?? 0;
        var name = TimerJobName ?? "";
        TimerStartedAt = null; TimerJobId = null; TimerJobName = null;
        StateChanged?.Invoke();
        return (minutes, jobId, name, start);
    }

    public void CancelTimer()
    {
        TimerStartedAt = null; TimerJobId = null; TimerJobName = null;
        StateChanged?.Invoke();
    }
}