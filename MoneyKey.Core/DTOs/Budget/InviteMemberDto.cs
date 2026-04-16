using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Budget;
public record InviteMemberDto(string Email, BudgetMemberRole Role);
