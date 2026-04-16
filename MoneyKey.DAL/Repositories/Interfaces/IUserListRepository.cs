using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IUserListRepository
{
    Task<List<UserList>> GetAllAsync(int budgetId, bool includeArchived = false);
    Task<UserList?>      GetByIdAsync(int id, int budgetId);
    Task<UserList>       CreateAsync(UserList list);
    Task<UserList>       UpdateAsync(UserList list);
    Task                 DeleteAsync(int id, int budgetId);
    Task<ListItem>       AddItemAsync(ListItem item);
    Task<ListItem?>      GetItemAsync(int itemId, int listId);
    Task<ListItem>       UpdateItemAsync(ListItem item);
    Task                 DeleteItemAsync(int itemId, int listId);
    Task                 ToggleItemAsync(int itemId, int listId, bool isChecked);
    Task                 ReorderItemsAsync(int listId, List<int> orderedItemIds);
}
