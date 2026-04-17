using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Lists;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/lists")]
public class ListsController : BaseApiController
{
    private readonly IListRepository          _repo;
    private readonly BudgetAuthorizationService _auth;

    public ListsController(IListRepository repo, BudgetAuthorizationService auth)
    {
        _repo = repo;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] bool includeArchived = false)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var lists = await _repo.GetAllAsync(budgetId, includeArchived);
        return Ok(lists.Select(ToDto));
    }

    [HttpGet("{listId:int}")]
    public async Task<IActionResult> GetById(int budgetId, int listId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var list = await _repo.GetByIdAsync(listId, budgetId);
        return list == null ? NotFound() : Ok(ToDto(list));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateListDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var list = new UserList
        {
            BudgetId = budgetId, Name = dto.Name, ListType = dto.ListType,
            Description = dto.Description, CreatedByUserId = UserId
        };
        return Ok(ToDto(await _repo.CreateAsync(list)));
    }

    [HttpPatch("{listId:int}/archive")]
    public async Task<IActionResult> Archive(int budgetId, int listId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var list = await _repo.GetByIdAsync(listId, budgetId);
        if (list == null) return NotFound();
        list.IsArchived = !list.IsArchived;
        return Ok(ToDto(await _repo.UpdateAsync(list)));
    }

    [HttpDelete("{listId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int listId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(listId, budgetId);
        return NoContent();
    }

    // ── Items ──────────────────────────────────────────────────────────────────

    [HttpPost("{listId:int}/items")]
    public async Task<IActionResult> AddItem(int budgetId, int listId, [FromBody] CreateListItemDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var list = await _repo.GetByIdAsync(listId, budgetId);
        if (list == null) return NotFound();
        var nextSort = list.Items.Any() ? list.Items.Max(i => i.SortOrder) + 1 : 0;
        var item     = new ListItem { ListId = listId, Text = dto.Text, SortOrder = nextSort };
        // Update list.UpdatedAt
        list.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(list);
        return Ok(ToItemDto(await _repo.AddItemAsync(item)));
    }

    [HttpPatch("{listId:int}/items/{itemId:int}/toggle")]
    public async Task<IActionResult> ToggleItem(int budgetId, int listId, int itemId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var item = await _repo.GetItemAsync(itemId, listId);
        if (item == null) return NotFound();
        item.IsChecked   = !item.IsChecked;
        item.CompletedAt = item.IsChecked ? DateTime.UtcNow : null;
        return Ok(ToItemDto(await _repo.UpdateItemAsync(item)));
    }

    [HttpPatch("{listId:int}/items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int budgetId, int listId, int itemId, [FromBody] UpdateListItemDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var item = await _repo.GetItemAsync(itemId, listId);
        if (item == null) return NotFound();
        item.Text = dto.Text;
        return Ok(ToItemDto(await _repo.UpdateItemAsync(item)));
    }

    [HttpDelete("{listId:int}/items/{itemId:int}")]
    public async Task<IActionResult> DeleteItem(int budgetId, int listId, int itemId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteItemAsync(itemId, listId);
        return NoContent();
    }

    [HttpPost("{listId:int}/items/clear-checked")]
    public async Task<IActionResult> ClearChecked(int budgetId, int listId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var list = await _repo.GetByIdAsync(listId, budgetId);
        if (list == null) return NotFound();
        foreach (var item in list.Items.Where(i => i.IsChecked))
            await _repo.DeleteItemAsync(item.Id, listId);
        var updated = await _repo.GetByIdAsync(listId, budgetId);
        return Ok(updated == null ? null : ToDto(updated));
    }

    // ── Mappers ────────────────────────────────────────────────────────────────

    private static UserListDto ToDto(UserList l) => new()
    {
        Id = l.Id, BudgetId = l.BudgetId, Name = l.Name, ListType = l.ListType,
        Description = l.Description, CreatedAt = l.CreatedAt, UpdatedAt = l.UpdatedAt,
        IsArchived = l.IsArchived, TotalItems = l.Items.Count,
        CheckedItems = l.Items.Count(i => i.IsChecked),
        Items = l.Items.Select(ToItemDto).ToList()
    };

    private static ListItemDto ToItemDto(ListItem i) => new()
    {
        Id = i.Id, ListId = i.ListId, Text = i.Text, IsChecked = i.IsChecked,
        SortOrder = i.SortOrder, CreatedAt = i.CreatedAt, CompletedAt = i.CompletedAt
    };
}
