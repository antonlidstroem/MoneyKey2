namespace MoneyKey.Domain.Enums;

public enum JobTransactionMode
{
    /// <summary>
    /// MoneyKey creates/manages the salary transaction.
    /// For Hourly jobs this happens only when the user explicitly posts to payroll.
    /// For Monthly/Yearly a recurring transaction is generated automatically.
    /// </summary>
    AutoCreate = 0,
    /// <summary>
    /// User manages transactions manually. Time entries are reference-only,
    /// no transactions are ever created from them.
    /// </summary>
    ManualOnly = 1
}
