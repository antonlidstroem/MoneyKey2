using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetAsync(string userId);
    Task<UserSubscription>  UpsertAsync(UserSubscription sub);
    Task<List<(string UserId, string Email, string DisplayName, UserSubscription Sub)>>
        GetAllForAdminAsync(string? search = null, int page = 1, int pageSize = 50);
    Task<int> CountAsync();
    Task<string?> FindUserIdByDisplayNameAsync(string displayName);
    Task<List<(string UserId, string DisplayName)>> SearchByDisplayNameAsync(string prefix, int limit = 8);
}
