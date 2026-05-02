// Xero.DataComparer.Console — Risk Indicators NRT example.
//
// Highlight: CompareResult<T> implements ITypedList. That's the same contract
// any data-bound grid uses (DevExpress GridControl, WinForms DataGridView,
// WPF DataGrid, Avalonia DataGrid). Below we walk that exact contract to
// render the result as a console table — no hardcoded knowledge of the
// diff schema, no string parsing of "Reference_X" / "Target_X" keys.

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Xero.DataComparer.Core;

const int TotalRows       = 10_000_000;
const int OnlyInReference = 20_000;
const int OnlyInTarget    = 20_000;
const int InBothButDiff   = 20_000;
const int TopN            = 5;

Console.OutputEncoding = Encoding.UTF8;

// NOTE: do NOT set `SingleLine = true` on the SimpleConsole formatter.
// That option replaces every `\n` in the message body with a space, which
// flattens the multi-line Gantt chart emitted by ListComparer<T>.LogGantt
// into a single line. The default (multi-line) is what we want here.
using var loggerFactory = LoggerFactory.Create(b => b
    .SetMinimumLevel(LogLevel.Information)
    .AddSimpleConsole(o => { o.TimestampFormat = "HH:mm:ss "; }));

Console.WriteLine($"Generating reference + target ({TotalRows:N0} rows each)...");
var (reference, target) = DataBuilder.Generate(TotalRows, OnlyInReference, OnlyInTarget, InBothButDiff);

var comparer = new ListComparer<RiskIndicator>(
    keyProperties:    new() { "TradeId", "Book", "AsOfDate" },
    ignoreProperties: new() { "LastUpdated", "RunId" },
    logger:           loggerFactory.CreateLogger<ListComparer<RiskIndicator>>());

CompareResult<RiskIndicator> result = await comparer.CompareList(reference, target);

Console.WriteLine();
Console.WriteLine($"Only in reference  : {result.OnlyInReference?.Count ?? 0,8:N0}   (expected {OnlyInReference,6:N0})");
Console.WriteLine($"Only in target     : {result.OnlyInTarget?.Count    ?? 0,8:N0}   (expected {OnlyInTarget,6:N0})");
Console.WriteLine($"In both, differing : {result.Count,8:N0}   (expected {InBothButDiff,6:N0})");
Console.WriteLine();
Console.WriteLine("Three result tables, each rendered through the same descriptor-based path");
Console.WriteLine("a real DataGrid would walk:");
Console.WriteLine("  · in-both-but-differ  → CompareResult<T> ITypedList descriptors");
Console.WriteLine("  · only-in-reference   → TypeDescriptor.GetProperties(typeof(T))");
Console.WriteLine("  · only-in-target      → TypeDescriptor.GetProperties(typeof(T))");

ConsoleGrid.Render(
    source: result,
    topN:   TopN,
    title:  $"In both, differing — top {TopN} of {result.Count:N0}");

ConsoleGrid.Render(
    source: result.OnlyInReference,
    topN:   TopN,
    title:  $"Only in reference — top {TopN} of {result.OnlyInReference?.Count ?? 0:N0}",
    excludeProperties: ["LastUpdated", "RunId"]);

ConsoleGrid.Render(
    source: result.OnlyInTarget,
    topN:   TopN,
    title:  $"Only in target — top {TopN} of {result.OnlyInTarget?.Count ?? 0:N0}",
    excludeProperties: ["LastUpdated", "RunId"]);


// ─────────────────────────────────────────────────────────────────────────────
// Domain type
// ─────────────────────────────────────────────────────────────────────────────

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


// ─────────────────────────────────────────────────────────────────────────────
// Synthetic dataset — controlled overlap so the expected output is known
// ─────────────────────────────────────────────────────────────────────────────

internal static class DataBuilder
{
    private static readonly string[] Books =
        { "EQ-DELTA-1", "EQ-VEGA-1", "FX-G10", "IR-EU", "PWR-FR", "PWR-DE", "GAS-NBP", "OIL-BRENT" };

    public static (List<RiskIndicator> reference, List<RiskIndicator> target) Generate(
        int totalRows, int onlyInRef, int onlyInTgt, int inBothButDiff)
    {
        var rng     = new Random(42);
        var asOf    = new DateOnly(2026, 4, 30);
        int matched = totalRows - onlyInRef - inBothButDiff;
        var reference = new List<RiskIndicator>(totalRows);
        var target    = new List<RiskIndicator>(totalRows);

        for (int i = 0; i < matched; i++)
            { var r = MakeRow(i, asOf, rng);                                  reference.Add(r); target.Add(Clone(r, perturb: false, rng)); }
        for (int i = 0; i < inBothButDiff; i++)
            { var r = MakeRow(matched + i, asOf, rng);                        reference.Add(r); target.Add(Clone(r, perturb: true,  rng)); }
        for (int i = 0; i < onlyInRef; i++)
            reference.Add(MakeRow(matched + inBothButDiff + i, asOf, rng));
        for (int i = 0; i < onlyInTgt; i++)
            target.Add(MakeRow(matched + inBothButDiff + onlyInRef + i, asOf, rng));

        Shuffle(reference, rng);
        Shuffle(target,    rng);
        return (reference, target);
    }

    private static RiskIndicator MakeRow(int seed, DateOnly date, Random rng) => new()
    {
        TradeId     = $"TRD-{seed:D9}",
        Book        = Books[seed % Books.Length],
        AsOfDate    = date,
        PnL         = rng.NextDouble() * 200_000d - 100_000d,
        VaR         = rng.NextDouble() * 50_000d,
        Delta       = rng.NextDouble() * 1_000d - 500d,
        Gamma       = rng.NextDouble() * 10d,
        Vega        = rng.NextDouble() * 100d - 50d,
        Theta       = rng.NextDouble() * 10d - 5d,
        LastUpdated = DateTime.UtcNow,
        RunId       = Guid.NewGuid().ToString("N")[..8],
    };

    private static RiskIndicator Clone(RiskIndicator s, bool perturb, Random rng)
    {
        var c = new RiskIndicator
        {
            TradeId = s.TradeId, Book = s.Book, AsOfDate = s.AsOfDate,
            PnL = s.PnL, VaR = s.VaR, Delta = s.Delta, Gamma = s.Gamma, Vega = s.Vega, Theta = s.Theta,
            LastUpdated = DateTime.UtcNow,
            RunId       = Guid.NewGuid().ToString("N")[..8],
        };
        if (perturb)
        {
            c.PnL += rng.NextDouble() * 100d - 50d;
            c.VaR += rng.NextDouble() * 50d;
        }
        return c;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// ITypedList → ASCII console grid
//
// Generic. Knows nothing about the diff schema. Works on ANY IList whose
// item type implements ITypedList — including CompareResult<T>.
// ─────────────────────────────────────────────────────────────────────────────

internal static class ConsoleGrid
{
    /// <summary>
    /// Renders an <see cref="IList{T}"/> as an ASCII table using the same
    /// descriptor-based contract a DataGrid would use:
    ///   - if the source implements ITypedList (e.g. CompareResult&lt;T&gt;)
    ///     the columns come from <c>typed.GetItemProperties(null)</c>;
    ///   - otherwise the columns come from <c>TypeDescriptor.GetProperties(typeof(T))</c>,
    ///     i.e. the same default column inference WinForms / WPF DataGrid /
    ///     DevExpress GridControl perform when bound to a typed list.
    /// </summary>
    public static void Render<T>(
        IList<T>?            source,
        int                  topN,
        string               title,
        IEnumerable<string>? excludeProperties = null)
    {
        Console.WriteLine();
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"─── {title} ".PadRight(78, '─'));
        Console.ForegroundColor = prev;

        if (source is null || source.Count == 0)
        {
            Console.WriteLine("  (empty)");
            return;
        }

        // 1. Resolve column descriptors — ITypedList first, then fall back to
        //    TypeDescriptor reflection over the item type.
        PropertyDescriptorCollection descriptors = source is ITypedList typed
            ? typed.GetItemProperties(listAccessors: null!)
            : TypeDescriptor.GetProperties(typeof(T));

        var hidden = new HashSet<string>(StringComparer.Ordinal) { "ComparedItems" };
        if (excludeProperties is not null)
            foreach (var name in excludeProperties) hidden.Add(name);

        var columns = descriptors.Cast<PropertyDescriptor>()
                                 .Where(p => !hidden.Contains(p.Name))
                                 .OrderBy(p => p.Name.StartsWith("Key_") ? 0 : 1)
                                 .ToList();

        if (columns.Count == 0)
        {
            Console.WriteLine("  (no visible columns)");
            return;
        }

        // 2. Materialize the cells we're going to print so we can size each column.
        var rows  = source.Cast<object>().Take(topN).ToList();
        var cells = rows.Select(row => columns.Select(c => Format(c.GetValue(row))).ToArray()).ToList();
        var widths = columns.Select((c, i) =>
            Math.Max(c.Name.Length, cells.Max(r => r[i].Length))).ToArray();

        // 3. Render
        Border('┌', '┬', '┐', widths);
        WriteRow(columns.Select(c => c.Name).ToArray(), widths, ConsoleColor.Cyan);
        Border('├', '┼', '┤', widths);
        foreach (var r in cells) WriteRow(r, widths, color: null);
        Border('└', '┴', '┘', widths);
    }

    private static string Format(object? v) => v switch
    {
        null        => "",
        double d    => d.ToString("N4"),
        decimal m   => m.ToString("N4"),
        float f     => f.ToString("N4"),
        DateOnly d  => d.ToString("yyyy-MM-dd"),
        DateTime dt => dt.ToString("yyyy-MM-dd"),
        _           => v.ToString() ?? "",
    };

    private static void Border(char left, char mid, char right, int[] widths)
    {
        Console.Write(left);
        for (int i = 0; i < widths.Length; i++)
        {
            Console.Write(new string('─', widths[i] + 2));
            Console.Write(i == widths.Length - 1 ? right : mid);
        }
        Console.WriteLine();
    }

    private static void WriteRow(string[] cells, int[] widths, ConsoleColor? color)
    {
        Console.Write('│');
        for (int i = 0; i < cells.Length; i++)
        {
            Console.Write(' ');
            if (color.HasValue) Console.ForegroundColor = color.Value;
            Console.Write(cells[i].PadRight(widths[i]));
            if (color.HasValue) Console.ResetColor();
            Console.Write(" │");
        }
        Console.WriteLine();
    }
}
