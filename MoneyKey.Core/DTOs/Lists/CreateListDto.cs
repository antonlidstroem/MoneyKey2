using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Lists;

public record CreateListDto(
    string Name,
    ListType ListType,
    string? Description,
    string? Content,
    string? Tags,
    EntryScope Scope = EntryScope.BudgetSpecific,
    EntryVisibility Visibility = EntryVisibility.BudgetShared,
    string? ListConfig = null);