namespace MoneyKey.Core.Services.CsvProfiles;

public abstract class BankCsvProfile
{
    public abstract string BankName          { get; }
    public abstract char   Delimiter         { get; }
    public abstract int    SkipRows          { get; }
    public abstract string DateColumn        { get; }
    public abstract string AmountColumn      { get; }
    public abstract string DescriptionColumn { get; }
    public abstract string DateFormat        { get; }
}
