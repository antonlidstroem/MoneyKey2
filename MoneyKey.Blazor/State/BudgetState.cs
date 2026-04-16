using MoneyKey.Core.DTOs.Budget;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.State;

public class BudgetState
{
    public int             ActiveBudgetId   { get; private set; }
    public string          ActiveBudgetName { get; private set; } = string.Empty;
    public BudgetMemberRole MyRole          { get; private set; }
    public List<BudgetDto> MyBudgets        { get; private set; } = new();

    public event Action? StateChanged;

    public bool CanEdit => MyRole is BudgetMemberRole.Editor or BudgetMemberRole.Owner;
    public bool IsOwner => MyRole == BudgetMemberRole.Owner;

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
