namespace Xero.DataComparer.UI;

/// <summary>
/// Compare, dataset, ignored properties, relevance threshold (absolute or in %), differing columns only, search and theme.
/// Raises an event per action; the form reads the choices from the properties.
/// </summary>
sealed class ComparisonToolbar : ToolStrip
{
    /// <summary>Items whose glyph is redrawn in the colours of the current theme.</summary>
    readonly List<(ToolStripItem Item, char Glyph, Func<Color> Color)> _glyphItems = new();
    readonly ToolStripButton _compare, _differingOnly;
    readonly ToolStripDropDownButton _ignored;
    readonly FlatCombo _dataset = new() { Width = Metrics.DatasetWidth };
    readonly ToolStripTextBox _threshold = new() { BorderStyle = BorderStyle.None, AutoSize = false, Width = Metrics.ThresholdWidth, Margin = new Padding(4, 3, 4, 2), Text = "0" };
    readonly FlatCombo _thresholdMode = new() { Width = Metrics.ThresholdModeWidth };
    readonly ToolStripTextBox _search = new() { BorderStyle = BorderStyle.None, AutoSize = false, Width = Metrics.SearchWidth, Margin = new Padding(4, 3, 4, 2) };

    public event Action? CompareClicked;
    /// <summary>The ignored properties menu closed (its choice may be unchanged).</summary>
    public event Action? IgnoredChanged;
    /// <summary>A valid threshold different from the previous one was entered.</summary>
    public event Action? ThresholdChanged;
    public event Action? DifferingOnlyChanged;
    public event Action? SearchChanged;
    public event Action? ThemeClicked;

    public ComparisonToolbar()
    {
        var renderer = new FlatRenderer();
        Renderer = renderer;
        GripStyle = ToolStripGripStyle.Hidden;
        Padding = Metrics.ToolbarPadding;
        Dock = DockStyle.Fill;

        _compare = Glyphed(new ToolStripButton("Compare") { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, ToolTipText = "Generate both lists and compare them (F5)" },
            Glyphs.Play, () => Theme.Colors.Info);
        _compare.Click += (_, _) => CompareClicked?.Invoke();
        foreach (var d in Dataset.Presets) _dataset.Items.Add(d);
        _dataset.SelectedIndex = 1;

        _ignored = Glyphed(new ToolStripDropDownButton("Ignored properties"), Glyphs.Hide, () => Theme.Colors.IconInk);
        _ignored.ToolTipText = "Properties the comparator skips. LastUpdated and RunId change on every run: compared, they make almost every row differ.";
        _ignored.DropDown.Renderer = renderer;
        foreach (var field in Field.All.Where(f => !DemoComparison.KeyNames.Contains(f.Name)))
            _ignored.DropDownItems.Add(new ToolStripMenuItem(field.Name) { Tag = field.Name, CheckOnClick = true, Checked = DemoComparison.DefaultIgnored.Contains(field.Name) });
        // Keep the menu open while ticking; the form compares again once it closes with another choice.
        _ignored.DropDown.Closing += (_, e) => e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
        _ignored.DropDownClosed += (_, _) =>
        {
            UpdateIgnoredText();
            IgnoredChanged?.Invoke();
        };
        UpdateIgnoredText();

        _thresholdMode.Items.AddRange(new object[] { "absolute", "% of reference" });
        _thresholdMode.SelectedIndex = 0;
        _threshold.TextBox.TextAlign = HorizontalAlignment.Right;
        _threshold.ToolTipText = "Gaps up to this value are not relevant: a pair whose gaps all stay within it is masked.";
        _threshold.TextChanged += (_, _) => ReadThreshold();
        _thresholdMode.SelectedIndexChanged += (_, _) => ReadThreshold();

        _differingOnly = Glyphed(new ToolStripButton("Differing columns only") { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, CheckOnClick = true },
            Glyphs.Columns, () => Theme.Colors.IconInk);
        _differingOnly.ToolTipText = "Hide the properties without a relevant gap in the rows shown";
        _differingOnly.CheckedChanged += (_, _) => DifferingOnlyChanged?.Invoke();
        _search.TextChanged += (_, _) => SearchChanged?.Invoke();

        // Same icon as the theme switch of the Xatis shell (palette).
        var themeButton = new ToolStripButton(Theme.PaletteIcon())
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image, Alignment = ToolStripItemAlignment.Right, ToolTipText = "Light / dark theme",
        };
        themeButton.Click += (_, _) => ThemeClicked?.Invoke();

        Items.AddRange(new ToolStripItem[]
        {
            // Combo hosts: a margin leaves room for the border drawn around them (see OnPaint).
            _compare, new ToolStripLabel("Dataset"), new ToolStripControlHost(_dataset) { Margin = new Padding(4, 1, 4, 2) }, _ignored,
            new ToolStripSeparator(),
            Glyphed(new ToolStripLabel("Relevance threshold"), Glyphs.Filter, () => Theme.Colors.Warning), _threshold,
            new ToolStripControlHost(_thresholdMode) { Margin = new Padding(4, 1, 4, 2) },
            _differingOnly,
            new ToolStripSeparator(),
            Glyphed(new ToolStripLabel("Search (Ctrl+F)"), Glyphs.Search, () => Theme.Colors.IconInk), _search,
            themeButton,
        });
    }

    public Dataset SelectedDataset => (Dataset)_dataset.SelectedItem!;

    public string[] Ignored =>
        _ignored.DropDownItems.OfType<ToolStripMenuItem>().Where(i => i.Checked).Select(i => (string)i.Tag!).ToArray();

    /// <summary>Last valid threshold entered.</summary>
    public RelevanceThreshold Threshold { get; private set; }

    public bool DifferingOnly => _differingOnly.Checked;

    public string Search => _search.Text.Trim();

    /// <summary>While comparing, the choices that start a new comparison are disabled.</summary>
    public void SetBusy(bool busy) => _compare.Enabled = _dataset.Enabled = _ignored.Enabled = !busy;

    public void FocusSearch()
    {
        _search.Focus();
        _search.SelectAll();
    }

    /// <summary>Empties the search box when it has the focus and some text (Escape).</summary>
    public bool ClearSearch()
    {
        if (!_search.Focused || _search.Text.Length == 0) return false;
        _search.Text = "";
        return true;
    }

    /// <summary>Re-applies the colours set on items; the rest reads the theme when it paints.</summary>
    public void ApplyTheme()
    {
        foreach (var (item, glyph, color) in _glyphItems)
        {
            var old = item.Image;
            item.Image = Theme.Glyph(glyph, color());
            old?.Dispose();
        }
        foreach (var c in new Control[] { _search.TextBox, _threshold.TextBox, _dataset, _thresholdMode })
        {
            c.BackColor = Theme.Colors.Surface;
            c.ForeColor = Theme.Colors.Ink;
        }
        ReadThreshold(); // invalid value: in the danger colour
    }

    /// <summary>Registers an item whose glyph ApplyTheme draws (and redraws on a theme change).</summary>
    T Glyphed<T>(T item, char glyph, Func<Color> color) where T : ToolStripItem
    {
        _glyphItems.Add((item, glyph, color));
        return item;
    }

    void UpdateIgnoredText()
    {
        var ignored = Ignored;
        _ignored.Text = ignored.Length == 0 ? "Ignored: none" : "Ignored: " + string.Join(", ", ignored);
    }

    void ReadThreshold()
    {
        bool valid = RelevanceThreshold.TryParse(_threshold.Text, _thresholdMode.SelectedIndex == 1, out var threshold);
        _threshold.TextBox.ForeColor = valid ? Theme.Colors.Ink : Theme.Colors.Danger;
        if (!valid || threshold == Threshold) return;
        Threshold = threshold;
        ThresholdChanged?.Invoke();
    }

    /// <summary>Text boxes and combos hosted in the toolbar: same 1 px border as the tiles, drawn around them.</summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Colors.Border);
        using var surface = new SolidBrush(Theme.Colors.Surface);
        foreach (ToolStripItem item in Items)
        {
            // An item moved to the overflow menu has bounds in that menu: nothing to draw on the toolbar.
            if (item.Placement != ToolStripItemPlacement.Main) continue;
            if (item is ToolStripTextBox box)
            {
                var s = box.TextBox.Bounds;
                e.Graphics.FillRectangle(surface, Rectangle.Inflate(s, 3, 3));
                e.Graphics.DrawRectangle(pen, s.X - 4, s.Y - 4, s.Width + 7, s.Height + 7);
            }
            else if (item is ToolStripControlHost { Control: FlatCombo combo })
            {
                var c = combo.Bounds;
                e.Graphics.DrawRectangle(pen, c.X - 1, c.Y - 1, c.Width + 1, c.Height + 1);
            }
        }
    }
}
