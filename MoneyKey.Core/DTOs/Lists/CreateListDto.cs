using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Lists;

public record CreateListDto(
    string Name,
    ListType ListType,
    int BudgetId, // <--- Lägg till denna
    string? Description,
    string? Content,
    string? Tags,
    EntryScope Scope = EntryScope.BudgetSpecific,
    EntryVisibility Visibility = EntryVisibility.BudgetShared
);