namespace Xero.DataComparer.UI;

// Same synthetic dataset as Xero.DataComparer.Console (Program.cs).

/// <summary>Controlled overlap so the expected output is known.</summary>
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
