using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IListRepository
{
    Task<List<UserList>> GetAllAsync(int budgetId, bool includeArchived = false);
    Task<UserList?>      GetByIdAsync(int listId, int budgetId);
    Task<UserList>       CreateAsync(UserList list);
    Task<UserList>       UpdateAsync(UserList list);
    Task                 DeleteAsync(int listId, int budgetId);
    Task<ListItem>       AddItemAsync(ListItem item);
    Task<ListItem?>      GetItemAsync(int itemId, int listId);
    Task<ListItem>       UpdateItemAsync(ListItem item);
    Task                 DeleteItemAsync(int itemId, int listId);
    Task                 ReorderItemsAsync(int listId, List<(int Id, int SortOrder)> order);
}
