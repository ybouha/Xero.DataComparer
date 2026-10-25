namespace Xero.DataComparer.UI;

// Same domain type as Xero.DataComparer.Console (Program.cs).

public sealed class RiskIndicator
{
    public string   TradeId  { get; set; } = "";
    public string   Book     { get; set; } = "";
    public DateOnly AsOfDate { get; set; }
    public double   PnL      { get; set; }
    public double   VaR      { get; set; }
    public double   Delta    { get; set; }
    public double   Gamma    { get; set; }
    public double   Vega     { get; set; }
    public double   Theta    { get; set; }
    public DateTime LastUpdated { get; set; }   // ignored
    public string   RunId       { get; set; } = "";   // ignored
}
