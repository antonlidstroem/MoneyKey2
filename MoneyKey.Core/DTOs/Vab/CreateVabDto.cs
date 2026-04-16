namespace MoneyKey.Core.DTOs.Vab;
public record CreateVabDto(string? ChildName, DateTime StartDate, DateTime EndDate, decimal DailyBenefit, decimal Rate);
