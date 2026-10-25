namespace Xero.DataComparer.UI;

/// <summary>Synthetic dataset size: rows per side and rows of each kind of gap (only in reference, only in target, different).</summary>
sealed record Dataset(int Rows, int Gaps)
{
    public static readonly Dataset[] Presets =
    {
        new(100_000, 1_000), new(1_000_000, 10_000), new(5_000_000, 20_000), new(10_000_000, 20_000),
    };

    public override string ToString() => $"{Rows:N0} rows · {Gaps:N0} gaps of each kind";
}
