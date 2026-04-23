namespace MoneyKey.Core.DTOs.BudgetTarget;
public class BudgetTargetDto {
    public int     Id           { get; set; }
    public int     CategoryId   { get; set; }
    public string  CategoryName { get; set; } = "";
    public int     Year         { get; set; }
    public int     Month        { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance     => ActualAmount - TargetAmount;
    public double  VariancePct  => TargetAmount != 0 ? (double)(Variance / Math.Abs(TargetAmount) * 100) : 0;
    public string  TrafficLight => Math.Abs(VariancePct) <= 10 ? "green" : Math.Abs(VariancePct) <= 30 ? "yellow" : "red";
}
