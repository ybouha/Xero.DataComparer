namespace Xero.DataComparer.UI;

/// <summary>One tile per kind of gap (a click filters the grid), then the identical rows and the speed, which only inform.</summary>
sealed class IndicatorPanel : TableLayoutPanel
{
    static readonly (string Caption, Filter? Filter)[] Definitions =
    {
        ("Gaps", Filter.All), ("Only in reference", Filter.OnlyInReference), ("Only in target", Filter.OnlyInTarget),
        ("Different", Filter.Different), ("Within threshold", Filter.WithinThreshold), ("Identical", null), ("Comparison time", null),
    };

    readonly IndicatorTile[] _tiles = new IndicatorTile[Definitions.Length];

    public event Action<Filter>? FilterClicked;

    public IndicatorPanel()
    {
        Dock = DockStyle.Fill;
        ColumnCount = _tiles.Length;
        RowCount = 1;
        Padding = new Padding(Metrics.Gutter, 4, 4, 4);
        Margin = Padding.Empty;
        for (int i = 0; i < _tiles.Length; i++)
        {
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / _tiles.Length));
            _tiles[i] = new IndicatorTile { Caption = Definitions[i].Caption, Value = "—", Dock = DockStyle.Fill, Margin = new Padding(0, 0, Metrics.TileGap, 0) };
            if (Definitions[i].Filter is Filter filter) _tiles[i].Click += (_, _) => FilterClicked?.Invoke(filter);
            else _tiles[i].Cursor = Cursors.Default;
            Controls.Add(_tiles[i], i, 0);
        }
        Highlight(Filter.All);
    }

    /// <summary>Highlights the tile of the filter, if it has one.</summary>
    public void Highlight(Filter filter)
    {
        for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = Definitions[i].Filter == filter;
    }

    public void Display(DemoComparison r, RelevanceThreshold threshold)
    {
        int different = r.Different - r.Masked;
        Set(0, (r.OnlyInReference + r.OnlyInTarget + different).ToString("N0"), $"relevant, of {r.ReferenceCount:N0} / {r.TargetCount:N0} rows");
        Set(1, r.OnlyInReference.ToString("N0"), "missing from the target");
        Set(2, r.OnlyInTarget.ToString("N0"), "missing from the reference");
        Set(3, different.ToString("N0"), r.Masked > 0 ? $"{r.Different:N0} before the threshold" : "values differ on a compared property");
        Set(4, r.Masked.ToString("N0"), threshold.IsSet ? $"all gaps ≤ {threshold}" : "no relevance threshold");
        Set(5, r.Identical.ToString("N0"), "same values on the compared properties");
        double rate = (r.ReferenceCount + r.TargetCount) / Math.Max(r.Comparison.TotalSeconds, 1e-6);
        Set(6, $"{r.Comparison.TotalMilliseconds:N0} ms", $"{rate / 1e6:N1} M rows/s · {Environment.ProcessorCount} cores");
    }

    void Set(int tile, string value, string sub)
    {
        _tiles[tile].Value = value;
        _tiles[tile].Sub = sub;
    }
}
