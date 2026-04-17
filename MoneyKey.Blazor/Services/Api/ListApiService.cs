using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Lists;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.Services.Api;

public class ListApiService : ApiServiceBase
{
    public ListApiService(HttpClient http) : base(http) { }

    public Task<List<UserListDto>?> GetAllAsync(int budgetId, bool includeArchived = false) =>
        GetAsync<List<UserListDto>>($"api/budgets/{budgetId}/lists?includeArchived={includeArchived}");

    public Task<UserListDto?> GetByIdAsync(int budgetId, int listId) =>
        GetAsync<UserListDto>($"api/budgets/{budgetId}/lists/{listId}");

    public Task<UserListDto?> CreateAsync(int budgetId, CreateListDto dto) =>
        PostAsync<UserListDto>($"api/budgets/{budgetId}/lists", dto);

    public async Task<UserListDto?> UpdateAsync(int budgetId, int listId, CreateListDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/lists/{listId}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<UserListDto>() : null;
    }

    public async Task<UserListDto?> ArchiveAsync(int budgetId, int listId)
    {
        var r = await Http.PatchAsync($"api/budgets/{budgetId}/lists/{listId}/archive", null);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<UserListDto>() : null;
    }

    public async Task DeleteAsync(int budgetId, int listId) =>
        await Http.DeleteAsync($"api/budgets/{budgetId}/lists/{listId}");

    public Task<ListItemDto?> AddItemAsync(int budgetId, int listId, CreateListItemDto dto) =>
        PostAsync<ListItemDto>($"api/budgets/{budgetId}/lists/{listId}/items", dto);

    public async Task<ListItemDto?> ToggleItemAsync(int budgetId, int listId, int itemId)
    {
        var r = await Http.PatchAsync($"api/budgets/{budgetId}/lists/{listId}/items/{itemId}/toggle", null);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ListItemDto>() : null;
    }

    public async Task<ListItemDto?> UpdateItemAsync(int budgetId, int listId, int itemId, UpdateListItemDto dto)
    {
        var r = await Http.PatchAsJsonAsync($"api/budgets/{budgetId}/lists/{listId}/items/{itemId}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ListItemDto>() : null;
    }

    public async Task DeleteItemAsync(int budgetId, int listId, int itemId) =>
        await Http.DeleteAsync($"api/budgets/{budgetId}/lists/{listId}/items/{itemId}");

    public async Task<UserListDto?> ClearCheckedAsync(int budgetId, int listId)
    {
        var r = await Http.PostAsync($"api/budgets/{budgetId}/lists/{listId}/items/clear-checked", null);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<UserListDto>() : null;
    }
}
