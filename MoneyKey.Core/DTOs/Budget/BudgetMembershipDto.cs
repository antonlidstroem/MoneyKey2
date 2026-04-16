using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Budget;
public record BudgetMembershipDto(int BudgetId, string BudgetName, BudgetMemberRole Role);
