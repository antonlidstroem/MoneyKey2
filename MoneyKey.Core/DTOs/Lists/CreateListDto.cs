using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Lists;

public record CreateListDto(string Name, ListType ListType, string? Description);
