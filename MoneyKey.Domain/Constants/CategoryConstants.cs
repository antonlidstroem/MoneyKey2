namespace MoneyKey.Domain.Constants;

/// <summary>
/// Stable identifiers for system seed categories defined in BudgetDbContext.OnModelCreating.
/// These IDs must match the HasData seed in BudgetDbContext.
///
/// NOTE: CategoryConstants.Salary (Id=8) maps to the "Löneinbetalning" system category
/// which is NOT user-selectable. It is only used for auto-created salary transactions
/// from the payroll posting flow (TimeTrackingService.PostToPayrollAsync).
/// Users register income sources via the Job system instead.
/// </summary>
public static class CategoryConstants
{
    public const int Salary = 8;  // "Löneinbetalning" — system only, not user-selectable
    public const int Milersattning = 12;
    public const int VabSjukfranvaro = 11;
    public const int Transport = 3;
    public const int Mat = 1;
    public const int HusDrift = 2;
    public const int Fritid = 4;
    public const int Barn = 5;
    public const int StreamingTjanster = 6;
    public const int SaasProdukter = 7;
    public const int Bidrag = 9;
    public const int Hobbyverksamhet = 10;
}