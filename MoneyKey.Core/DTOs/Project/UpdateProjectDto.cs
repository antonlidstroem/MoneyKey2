namespace MoneyKey.Core.DTOs.Project;
public record UpdateProjectDto(int Id, string Name, string? Description, decimal BudgetAmount, DateTime StartDate, DateTime? EndDate, bool IsActive);
