namespace MoneyKey.Core.DTOs.Project;
public record CreateProjectDto(string Name, string? Description, decimal BudgetAmount, DateTime StartDate, DateTime? EndDate);
