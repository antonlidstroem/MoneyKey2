namespace MoneyKey.Core.Services.CsvProfiles;

public class SwedbankProfile : BankCsvProfile
{
    public override string BankName          => "Swedbank";
    public override char   Delimiter         => ';';
    public override int    SkipRows          => 4;
    public override string DateColumn        => "Datum";
    public override string AmountColumn      => "Belopp";
    public override string DescriptionColumn => "Text";
    public override string DateFormat        => "yyyy-MM-dd";
}
