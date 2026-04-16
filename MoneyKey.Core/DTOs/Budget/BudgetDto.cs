using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Budget;
public record BudgetDto(int Id, string Name, string? Description, bool IsActive, DateTime CreatedAt, BudgetMemberRole MyRole);
