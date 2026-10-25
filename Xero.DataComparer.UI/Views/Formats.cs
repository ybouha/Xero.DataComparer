namespace Xero.DataComparer.UI;

/// <summary>Display formats of the grid cells.</summary>
static class Formats
{
    public const string Gap = "+#,##0.00;-#,##0.00;0.00";
    public const string Percent = "+0.0000%;-0.0000%;0.0000%";   // the value is a fraction: % multiplies it by 100

    public static string For(Type? type) =>
        type == typeof(double) ? "#,##0.00"
        : type == typeof(DateOnly) ? "yyyy-MM-dd"
        : type == typeof(DateTime) ? "yyyy-MM-dd HH:mm:ss.fff"
        : "";
}
