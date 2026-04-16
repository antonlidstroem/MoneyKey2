using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class ListRepository : IListRepository
{
    private readonly BudgetDbContext _db;
    public ListRepository(BudgetDbContext db) => _db = db;

    public async Task<List<UserList>> GetAllAsync(int budgetId, bool includeArchived = false)
    {
        var q = _db.UserLists
            .Include(l => l.Items.OrderBy(i => i.SortOrder))
            .Where(l => l.BudgetId == budgetId);
        if (!includeArchived) q = q.Where(l => !l.IsArchived);
        return await q.OrderByDescending(l => l.UpdatedAt).ToListAsync();
    }

    public async Task<UserList?> GetByIdAsync(int listId, int budgetId) =>
        await _db.UserLists
            .Include(l => l.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(l => l.Id == listId && l.BudgetId == budgetId);

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

    public async Task DeleteAsync(int listId, int budgetId)
    {
        var l = await GetByIdAsync(listId, budgetId);
        if (l != null) { _db.UserLists.Remove(l); await _db.SaveChangesAsync(); }
    }

    public async Task<ListItem> AddItemAsync(ListItem item)
    {
        _db.ListItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<ListItem?> GetItemAsync(int itemId, int listId) =>
        await _db.ListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId);

    public async Task<ListItem> UpdateItemAsync(ListItem item)
    {
        _db.ListItems.Update(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task DeleteItemAsync(int itemId, int listId)
    {
        var item = await GetItemAsync(itemId, listId);
        if (item != null) { _db.ListItems.Remove(item); await _db.SaveChangesAsync(); }
    }

    public async Task ReorderItemsAsync(int listId, List<(int Id, int SortOrder)> order)
    {
        var items = await _db.ListItems.Where(i => i.ListId == listId).ToListAsync();
        foreach (var (id, sort) in order)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item != null) item.SortOrder = sort;
        }
        await _db.SaveChangesAsync();
    }
}
