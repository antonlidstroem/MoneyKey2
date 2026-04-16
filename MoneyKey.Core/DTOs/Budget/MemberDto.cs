using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Budget;
public record MemberDto(string UserId, string Email, string FirstName, string LastName, BudgetMemberRole Role, DateTime JoinedAt);
