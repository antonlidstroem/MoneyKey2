using MoneyKey.Core.DTOs.Budget;
namespace MoneyKey.Core.DTOs.Auth;
public record UserDto(string Id, string Email, string FirstName, string LastName, List<BudgetMembershipDto> Memberships);
