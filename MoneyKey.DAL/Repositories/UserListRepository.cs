using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class UserListRepository : IUserListRepository
{
    private readonly BudgetDbContext _db;
    public UserListRepository(BudgetDbContext db) => _db = db;

    public async Task<List<UserList>> GetAllAsync(int budgetId, bool includeArchived = false) =>
        await _db.UserLists
            .Where(l => l.BudgetId == budgetId && (includeArchived || !l.IsArchived))
            .Include(l => l.Items.OrderBy(i => i.SortOrder).ThenBy(i => i.Id))
            .OrderByDescending(l => l.UpdatedAt)
            .ToListAsync();

    public async Task<UserList?> GetByIdAsync(int id, int budgetId) =>
        await _db.UserLists
            .Where(l => l.Id == id && l.BudgetId == budgetId)
            .Include(l => l.Items.OrderBy(i => i.SortOrder).ThenBy(i => i.Id))
            .FirstOrDefaultAsync();

    public async Task<UserList> CreateAsync(UserList list)
    {
        _db.UserLists.Add(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task<UserList> UpdateAsync(UserList list)
    {
        list.UpdatedAt = DateTime.UtcNow;
        _db.UserLists.Update(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var l = await _db.UserLists.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId);
        if (l != null) { _db.UserLists.Remove(l); await _db.SaveChangesAsync(); }
    }

    public async Task<ListItem> AddItemAsync(ListItem item)
    {
        // Assign next sort order
        var maxSort = await _db.ListItems.Where(i => i.ListId == item.ListId)
                                         .MaxAsync(i => (int?)i.SortOrder) ?? -1;
        item.SortOrder = maxSort + 1;
        _db.ListItems.Add(item);
        await _db.SaveChangesAsync();
        // Update parent list timestamp
        var list = await _db.UserLists.FindAsync(item.ListId);
        if (list != null) { list.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        return item;
    }

    public async Task<ListItem?> GetItemAsync(int itemId, int listId) =>
        await _db.ListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId);

    public async Task<ListItem> UpdateItemAsync(ListItem item)
    {
        _db.ListItems.Update(item);
        await _db.SaveChangesAsync();
        var list = await _db.UserLists.FindAsync(item.ListId);
        if (list != null) { list.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        return item;
    }

    public async Task DeleteItemAsync(int itemId, int listId)
    {
        var item = await GetItemAsync(itemId, listId);
        if (item == null) return;
        _db.ListItems.Remove(item);
        var list = await _db.UserLists.FindAsync(listId);
        if (list != null) list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ToggleItemAsync(int itemId, int listId, bool isChecked)
    {
        var item = await GetItemAsync(itemId, listId);
        if (item == null) return;
        item.IsChecked   = isChecked;
        item.CompletedAt = isChecked ? DateTime.UtcNow : null;
        var list = await _db.UserLists.FindAsync(listId);
        if (list != null) list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ReorderItemsAsync(int listId, List<int> orderedItemIds)
    {
        var items = await _db.ListItems.Where(i => i.ListId == listId).ToListAsync();
        for (int i = 0; i < orderedItemIds.Count; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == orderedItemIds[i]);
            if (item != null) item.SortOrder = i;
        }
        await _db.SaveChangesAsync();
    }
}
