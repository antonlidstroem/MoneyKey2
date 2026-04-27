using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Filters;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Lists;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[RequireFeature("Lists")]
[Authorize, Route("api/budgets/{budgetId:int}/lists")]
public class ListsController : BaseApiController
{
    private readonly IListRepository            _repo;
    private readonly BudgetAuthorizationService _auth;

    public ListsController(IListRepository repo, BudgetAuthorizationService auth)
    { _repo = repo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] bool includeArchived = false)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var lists = await _repo.GetAllAsync(budgetId, includeArchived);
        // Filter by visibility: show own private entries + all shared entries
        var visible = lists.Where(l =>
            l.Visibility == EntryVisibility.BudgetShared ||
            l.CreatedByUserId == UserId).ToList();
        return Ok(visible.Select(ToDto));
    }

    [HttpGet("{listId:int}")]
    public async Task<IActionResult> GetById(int budgetId, int listId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var list = await _repo.GetByIdAsync(listId, budgetId);
        if (list == null) return NotFound();
        if (list.Visibility == EntryVisibility.Private && list.CreatedByUserId != UserId) return Forbid();
        return Ok(ToDto(list));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateListDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var list = new UserList
        {
            BudgetId        = dto.Scope == EntryScope.Personal ? null : budgetId,
            Name            = dto.Name,
            ListType        = dto.ListType,
            Description     = dto.Description,
            Content         = dto.Content,
            Tags            = dto.Tags,
            Scope           = dto.Scope,
            Visibility      = dto.Visibility,
            ListConfig = dto.ListConfig,
            CreatedByUserId = UserId
        };
        return Ok(ToDto(await _repo.CreateAsync(list)));
    }

    [HttpPut("{listId:int}")]
    public async Task<IActionResult> Update(int budgetId, int listId, [FromBody] CreateListDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var list = await _repo.GetByIdAsync(listId, budgetId);
        if (list == null) return NotFound();
        if (list.CreatedByUserId != UserId && !await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Owner))
            return Forbid();
        list.Name = dto.Name; list.Description = dto.Description; list.Content = dto.Content;
        list.Tags = dto.Tags; list.Visibility = dto.Visibility;
        return Ok(ToDto(await _repo.UpdateAsync(list)));
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
        list.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(list);
        return Ok(ToItemDto(await _repo.AddItemAsync(new ListItem { ListId = listId, Text = dto.Text, SortOrder = nextSort, ItemData = dto.ItemData  })));
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
        foreach (var item in list.Items.Where(i => i.IsChecked).ToList())
            await _repo.DeleteItemAsync(item.Id, listId);
        return Ok(ToDto((await _repo.GetByIdAsync(listId, budgetId))!));
    }

    // ── Mappers ────────────────────────────────────────────────────────────────

    private static UserListDto ToDto(UserList l) => new()
    {
        Id = l.Id, BudgetId = l.BudgetId, Name = l.Name, ListType = l.ListType,
        Description = l.Description, Content = l.Content, Tags = l.Tags,
        Scope = l.Scope, Visibility = l.Visibility,
        CreatedAt = l.CreatedAt, UpdatedAt = l.UpdatedAt,
        IsArchived = l.IsArchived, TotalItems = l.Items.Count,
        CheckedItems = l.Items.Count(i => i.IsChecked),
        ListConfig = l.ListConfig,
        Items = l.Items.Select(ToItemDto).ToList()
    };

    private static ListItemDto ToItemDto(ListItem i) => new()
    {
        Id = i.Id, ListId = i.ListId, Text = i.Text,
        IsChecked = i.IsChecked, SortOrder = i.SortOrder,
        ItemData = i.ItemData,
        CreatedAt = i.CreatedAt, CompletedAt = i.CompletedAt
    };

    [HttpPatch("{listId:int}/items/{itemId:int}/data")]
    public async Task<IActionResult> UpdateItemData(
    int budgetId, int listId, int itemId,
    [FromBody] UpdateItemDataDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var item = await _listRepo.GetItemAsync(itemId, listId);
        if (item == null) return NotFound();
        item.ItemData = dto.ItemData;
        await _listRepo.UpdateItemAsync(item);
        return Ok();
    }

    public record UpdateItemDataDto(string ItemData);
}
