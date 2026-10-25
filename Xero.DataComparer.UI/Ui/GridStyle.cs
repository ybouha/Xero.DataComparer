namespace Xero.DataComparer.UI;

/// <summary>Look shared by the grids: read-only cell selection, borderless (the hosting panel draws the border), theme colours.</summary>
static class GridStyle
{
    public static void Apply(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.BorderStyle = BorderStyle.None;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.MultiSelect = false;
        g.DefaultCellStyle.Font = Theme.UI; // colours: ApplyColors
        g.DefaultCellStyle.Padding = new Padding(Metrics.CellPadding, 0, Metrics.CellPadding, 0);
        g.ShowCellToolTips = true;
        g.SelectionMode = DataGridViewSelectionMode.CellSelect;
        g.ScrollBars = ScrollBars.Both;
    }

    public static void ApplyColors(DataGridView g)
    {
        g.BackgroundColor = g.DefaultCellStyle.BackColor = Theme.Colors.Surface;
        g.GridColor = Theme.Colors.GridLine;
        g.DefaultCellStyle.ForeColor = g.DefaultCellStyle.SelectionForeColor = Theme.Colors.Ink;
        g.DefaultCellStyle.SelectionBackColor = Theme.Colors.Selection;
    }
}
