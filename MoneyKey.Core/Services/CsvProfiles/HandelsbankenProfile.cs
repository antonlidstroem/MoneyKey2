namespace MoneyKey.Core.Services.CsvProfiles;

public class HandelsbankenProfile : BankCsvProfile
{
    public override string BankName          => "Handelsbanken";
    public override char   Delimiter         => '\t';
    public override int    SkipRows          => 1;
    public override string DateColumn        => "Transaktionsdatum";
    public override string AmountColumn      => "Belopp";
    public override string DescriptionColumn => "Transaktionstext";
    public override string DateFormat        => "dd/MM/yyyy";
}
