namespace MoneyKey.Domain.Enums;

public enum JobPayType
{
    /// <summary>Fixed gross paid monthly (e.g. 45 000 kr/mån)</summary>
    Monthly      = 0,
    /// <summary>Fixed gross paid yearly, distributed as 12 equal months</summary>
    Yearly       = 1,
    /// <summary>Per-hour rate — time entries drive the payroll</summary>
    Hourly       = 2,
    /// <summary>Fixed project lump-sum, posted manually</summary>
    ProjectFixed = 3
}
