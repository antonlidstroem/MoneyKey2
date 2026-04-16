namespace MoneyKey.Core.Services.CsvProfiles;

public class SebProfile : BankCsvProfile
{
    public override string BankName          => "SEB";
    public override char   Delimiter         => ';';
    public override int    SkipRows          => 1;
    public override string DateColumn        => "Bokföringsdag";
    public override string AmountColumn      => "Belopp";
    public override string DescriptionColumn => "Beskrivning";
    public override string DateFormat        => "yyyy-MM-dd";
}
