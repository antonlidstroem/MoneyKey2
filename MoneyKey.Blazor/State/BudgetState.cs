using MoneyKey.Core.DTOs.Budget;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.State;

public class BudgetState
{
    public int             ActiveBudgetId   { get; private set; }
    public string          ActiveBudgetName { get; private set; } = string.Empty;
    public BudgetMemberRole MyRole          { get; private set; }
    public List<BudgetDto> MyBudgets        { get; private set; } = new();

    /// <summary>
    /// Feature flags for the active budget.
    /// Keys: "Milersattning", "Vab", "Receipts", "Lists", "Kontering", "Export".
    /// If a key is absent, the feature is enabled by default.
    /// </summary>
    private HashSet<string> _disabledFeatures = new(StringComparer.OrdinalIgnoreCase);

    public event Action? StateChanged;

    public bool CanEdit => MyRole is BudgetMemberRole.Editor or BudgetMemberRole.Owner;
    public bool IsOwner => MyRole == BudgetMemberRole.Owner;

    /// <summary>Returns true unless the feature has been explicitly disabled for this budget.</summary>
    public bool IsFeatureEnabled(string feature) => !_disabledFeatures.Contains(feature);

    public void SetFeatures(IEnumerable<string> disabledKeys)
    {
        _disabledFeatures = new HashSet<string>(disabledKeys, StringComparer.OrdinalIgnoreCase);
        StateChanged?.Invoke();
    }

    public void SetBudgets(List<BudgetDto> budgets)
    {
        MyBudgets = budgets;
        if (budgets.Any())
        {
            var current = budgets.FirstOrDefault(b => b.Id == ActiveBudgetId);
            SetActiveBudget(current ?? budgets.First());
        }
        else
        {
            StateChanged?.Invoke();
        }
    }

    public void SetActiveBudget(BudgetDto b)
    {
        ActiveBudgetId   = b.Id;
        ActiveBudgetName = b.Name;
        MyRole           = b.MyRole;
        // Clear features when switching budget — MainLayout will reload them
        _disabledFeatures.Clear();
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
}
