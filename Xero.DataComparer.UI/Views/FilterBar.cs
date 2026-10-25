namespace Xero.DataComparer.UI;

/// <summary>"Filter:" and its drop-down list, right-aligned above the grid.</summary>
sealed class FilterBar : FlowLayoutPanel
{
    readonly FlatCombo _combo = new() { Width = Metrics.FilterWidth };
    readonly Panel _box = new() { Padding = new Padding(Metrics.Border), Margin = new Padding(0, 1, 0, 0) };

    public event Action<Filter>? FilterChanged;

    public FilterBar()
    {
        Dock = DockStyle.Fill;
        FlowDirection = FlowDirection.RightToLeft;
        Padding = new Padding(0, 3, 8, 0);
        Margin = Padding.Empty;
        WrapContents = false;
        foreach (var f in Filters.Items) _combo.Items.Add(f.Text);
        _combo.SelectedIndex = 0;
        _combo.SelectedIndexChanged += (_, _) =>
        {
            if (_combo.SelectedIndex >= 0) FilterChanged?.Invoke(Filters.Items[_combo.SelectedIndex].Value);
        };
        // A 1 px host panel draws the same border as the tiles around the combo.
        _combo.Dock = DockStyle.Fill;
        _combo.Margin = Padding.Empty;
        _box.Size = new Size(_combo.Width + 2 * Metrics.Border, _combo.Height + 2 * Metrics.Border);
        _box.Controls.Add(_combo);
        Controls.Add(_box);
        Controls.Add(new Label { Text = "Filter:", AutoSize = true, Margin = new Padding(0, 5, 2, 0) });
    }

    public void ShowFilter(Filter filter) => _combo.SelectedIndex = Array.FindIndex(Filters.Items, x => x.Value == filter);

    public void ApplyTheme()
    {
        _box.BackColor = Theme.Colors.Border;
        _combo.BackColor = Theme.Colors.Surface;
        _combo.ForeColor = Theme.Colors.Ink;
    }
}
