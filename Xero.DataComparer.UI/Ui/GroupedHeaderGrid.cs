using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Xero.DataComparer.UI;

/// <summary>
/// DataGridView with flat, owner-drawn column headers: an optional upper band grouping adjacent
/// columns, hover and sorted-column highlights, a drawn sort arrow and an accent line underneath.
/// </summary>
class GroupedHeaderGrid : DataGridView
{
    public readonly List<(string Text, int First, int Last)> Groups = new();
    int _hoverColumn = -2, _hoverRow = -2;

    public GroupedHeaderGrid()
    {
        DoubleBuffered = true;
        EnableHeadersVisualStyles = false;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = Metrics.ColumnHeadersHeight;
    }

    /// <summary>
    /// Column drawn as sorted. Set by the form: a virtual-mode grid sorts its own rows, so DataGridView.SortedColumn stays empty.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DataGridViewColumn? SortColumn { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SortOrder SortDirection { get; set; }

    int GroupHeight => Groups.Count == 0 ? 0 : ColumnHeadersHeight * 11 / 26;

    bool IsGroupEdge(int column) => Groups.Any(g => g.Last == column);

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics != null && e.RowIndex >= 0 && e.ColumnIndex == -1) { PaintRowHeader(e); return; }
        if (e.RowIndex != -1 || e.Graphics == null) { base.OnCellPainting(e); return; }

        var g = e.Graphics;
        var r = e.CellBounds;
        var band = new Rectangle(r.X, r.Y + GroupHeight, r.Width, r.Height - GroupHeight);
        var col = e.ColumnIndex >= 0 ? Columns[e.ColumnIndex] : null;

        using (var b = new SolidBrush(Theme.Colors.GroupBack)) g.FillRectangle(b, r.X, r.Y, r.Width, GroupHeight);
        var back = col == null ? Theme.Colors.HeaderBack
            : SortColumn == col ? Theme.Colors.HeaderSorted
            : _hoverColumn == e.ColumnIndex ? Theme.Colors.HeaderHover
            : Theme.Colors.HeaderBack;
        using (var b = new SolidBrush(back)) g.FillRectangle(b, band);

        if (col != null)
        {
            bool right = col.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight;
            bool sorted = SortColumn == col;
            // The arrow always sits at the right edge so it never looks attached to the neighbouring column.
            int arrowSpace = sorted ? 14 : 0;
            var textRect = new Rectangle(band.X + 6, band.Y, band.Width - 12 - arrowSpace, band.Height);
            TextRenderer.DrawText(g, col.HeaderText, Theme.Semibold, textRect, Theme.Colors.Caption,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                | (right ? TextFormatFlags.Right : TextFormatFlags.Left));
            if (sorted) DrawSortArrow(g, band.Right - 16, band.Y + band.Height / 2, SortDirection == SortOrder.Ascending);
        }

        using (var thin = new Pen(Theme.Colors.HeaderLine))
        using (var strong = new Pen(Theme.Colors.GroupLine))
        {
            if (GroupHeight > 0) g.DrawLine(thin, r.X, band.Y, r.Right, band.Y);
            // Separators: strong between groups (from the top), thin between columns of a group (lower band only).
            bool edge = col == null || IsGroupEdge(e.ColumnIndex);
            g.DrawLine(edge ? strong : thin, r.Right - 1, edge ? r.Y : band.Y, r.Right - 1, r.Bottom - 1);
        }
        using (var accent = new SolidBrush(Theme.Colors.Accent)) g.FillRectangle(accent, r.X, r.Bottom - Metrics.AccentLine, r.Width, Metrics.AccentLine);
        e.Handled = true;
    }

    /// <summary>Row headers mirror the column headers: same backgrounds, thin separators and an accent line on the right.</summary>
    void PaintRowHeader(DataGridViewCellPaintingEventArgs e)
    {
        var g = e.Graphics!;
        var r = e.CellBounds;
        bool current = CurrentCell?.RowIndex == e.RowIndex;
        var back = current ? Theme.Colors.HeaderSorted : _hoverRow == e.RowIndex ? Theme.Colors.HeaderHover : Theme.Colors.HeaderBack;
        using (var b = new SolidBrush(back)) g.FillRectangle(b, r);
        using (var thin = new Pen(Theme.Colors.HeaderLine)) g.DrawLine(thin, r.X, r.Bottom - 1, r.Right - Metrics.AccentLine - 1, r.Bottom - 1);
        if (current) DrawRowMarker(g, r.X + (r.Width - Metrics.AccentLine) / 2 - 2, r.Y + r.Height / 2);
        using (var accent = new SolidBrush(Theme.Colors.Accent)) g.FillRectangle(accent, r.Right - Metrics.AccentLine, r.Y, Metrics.AccentLine, r.Height);
        e.Handled = true;
    }

    static void DrawRowMarker(Graphics g, int x, int y)
    {
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var pts = new[] { new Point(x, y - 4), new Point(x, y + 4), new Point(x + 5, y) };
        using (var b = new SolidBrush(Theme.Colors.Accent)) g.FillPolygon(b, pts);
        g.SmoothingMode = old;
    }

    static void DrawSortArrow(Graphics g, int x, int y, bool ascending)
    {
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var pts = ascending
            ? new[] { new Point(x, y + 2), new Point(x + 8, y + 2), new Point(x + 4, y - 3) }
            : new[] { new Point(x, y - 2), new Point(x + 8, y - 2), new Point(x + 4, y + 3) };
        using (var b = new SolidBrush(Theme.Colors.Accent)) g.FillPolygon(b, pts);
        g.SmoothingMode = old;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // The square between the two scrollbars is painted in a system colour: give it the grid's background.
        if (VerticalScrollBar.Visible && HorizontalScrollBar.Visible)
            using (var corner = new SolidBrush(BackgroundColor))
                e.Graphics.FillRectangle(corner, VerticalScrollBar.Left, HorizontalScrollBar.Top, VerticalScrollBar.Width, HorizontalScrollBar.Height);
        if (!ColumnHeadersVisible || GroupHeight == 0) return;
        using var back = new SolidBrush(Theme.Colors.GroupBack);
        using var strong = new Pen(Theme.Colors.GroupLine);
        foreach (var (text, first, last) in Groups)
        {
            Rectangle band = Rectangle.Empty;
            for (int i = first; i <= last && i < Columns.Count; i++)
            {
                var c = GetColumnDisplayRectangle(i, true);
                if (c.Width > 0) band = band.IsEmpty ? c : Rectangle.Union(band, c);
            }
            if (band.IsEmpty) continue;
            // The header cells start below the grid's own border (painted in the client area): don't cover it.
            band = new Rectangle(band.X, band.Y, band.Width, GroupHeight);
            e.Graphics.FillRectangle(back, band.X, band.Y, band.Width - 1, band.Height);
            TextRenderer.DrawText(e.Graphics, text.ToUpperInvariant(), Theme.Semibold, band, Theme.Colors.Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.Graphics.DrawLine(strong, band.Right - 1, band.Y, band.Right - 1, band.Bottom);
        }
    }

    protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
    {
        base.OnCellMouseEnter(e);
        if (e.RowIndex == -1 && e.ColumnIndex >= 0) { _hoverColumn = e.ColumnIndex; InvalidateColumnHeaders(); }
        if (e.RowIndex >= 0 && e.ColumnIndex == -1) { _hoverRow = e.RowIndex; InvalidateRowHeaders(); }
    }

    protected override void OnCellMouseLeave(DataGridViewCellEventArgs e)
    {
        base.OnCellMouseLeave(e);
        if (_hoverColumn != -2) { _hoverColumn = -2; InvalidateColumnHeaders(); }
        if (_hoverRow != -2) { _hoverRow = -2; InvalidateRowHeaders(); }
    }

    void InvalidateColumnHeaders()
    {
        if (ColumnHeadersVisible) Invalidate(new Rectangle(0, 0, Width, ColumnHeadersHeight));
    }

    void InvalidateRowHeaders()
    {
        if (RowHeadersVisible) Invalidate(new Rectangle(0, 0, RowHeadersWidth, Height));
    }

    protected override void OnCurrentCellChanged(EventArgs e) { base.OnCurrentCellChanged(e); InvalidateRowHeaders(); }

    protected override void OnScroll(ScrollEventArgs e) { base.OnScroll(e); InvalidateColumnHeaders(); }
    protected override void OnColumnWidthChanged(DataGridViewColumnEventArgs e) { base.OnColumnWidthChanged(e); Invalidate(); }
    protected override void OnSorted(EventArgs e) { base.OnSorted(e); InvalidateColumnHeaders(); }
}
