using System.ComponentModel;

namespace Xero.DataComparer.UI;

/// <summary>
/// Gaps of a comparison, after the ComparisonScreen of Xatis: a band of keys frozen on the left (with the kind of gap and its icon),
/// then one band per compared property (Reference | Target | Gap | Gap %, the gaps for numbers only). Virtual: the grid asks only
/// for the visible cells, whatever the number of gaps. Sorted by a click on a header, gaps by magnitude.
/// </summary>
sealed class ResultGrid : GroupedHeaderGrid
{
    ResultColumn[] _columns = [];
    List<DiffRow> _rows = new();   // rows shown, in display order
    int _sortColumn;
    SortOrder _sortOrder = SortOrder.Ascending;
    bool _differingOnly;
    bool _reloading;               // rows are being rebuilt: no current row event until done

    /// <summary>The current row changed; raised once after the rows are reloaded.</summary>
    public event Action? CurrentDiffChanged;

    public ResultGrid()
    {
        GridStyle.Apply(this);
        VirtualMode = true;
        RowHeadersWidth = Metrics.RowHeaderWidth;
        RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        RowTemplate.Height = Metrics.RowHeight;
    }

    public DiffRow? CurrentDiff => CurrentCell is { RowIndex: var i } && i >= 0 && i < _rows.Count ? _rows[i] : null;

    /// <summary>Hides the properties without a gap in the rows shown.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DifferingOnly
    {
        get => _differingOnly;
        set
        {
            _differingOnly = value;
            UpdateColumnVisibility();
        }
    }

    public void ApplyTheme()
    {
        GridStyle.ApplyColors(this);
        RowsDefaultCellStyle.BackColor = Theme.Colors.Surface;
        AlternatingRowsDefaultCellStyle.BackColor = Theme.Colors.AltRow; // follows the display order, also after sorting
    }

    public void ClearRows()
    {
        _rows = new();
        Rows.Clear();
    }

    /// <summary>Columns of a comparison: band of the keys, then one band per compared property. Sorted by kind.</summary>
    public void Build(DemoComparison run)
    {
        ClearRows();
        Columns.Clear();
        Groups.Clear();
        var columns = new List<ResultColumn> { new(ColumnPart.Kind) };
        columns.AddRange(run.Keys.Select(k => new ResultColumn(ColumnPart.Key, k)));
        Groups.Add(("Keys", 0, columns.Count - 1));
        for (int i = 0; i < run.Properties.Length; i++)
        {
            var property = run.Properties[i];
            int first = columns.Count;
            columns.Add(new(ColumnPart.Reference, property, i));
            columns.Add(new(ColumnPart.Target, property, i));
            if (property.IsNumeric)
            {
                columns.Add(new(ColumnPart.Gap, property, i));
                columns.Add(new(ColumnPart.Percent, property, i));
            }
            Groups.Add((property.Name, first, columns.Count - 1));
        }
        _columns = columns.ToArray();
        Columns.AddRange(_columns.Select(c => c.CreateColumn()).ToArray());
        Columns[run.Keys.Length].Frozen = true;
        _sortColumn = 0;
        _sortOrder = SortOrder.Ascending;
    }

    /// <summary>Shows these rows in the current sort order; the current row stays current when it is still shown.</summary>
    public void ShowRows(IEnumerable<DiffRow> rows)
    {
        var current = CurrentDiff;
        _rows = rows.ToList();
        Sort();
        Reload(current);
    }

    void SortBy(int column)
    {
        if (column < 0 || column >= _columns.Length) return;
        // First click on a gap: largest first.
        _sortOrder = _sortColumn == column
            ? _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending
            : _columns[column].IsGap ? SortOrder.Descending : SortOrder.Ascending;
        _sortColumn = column;
        var current = CurrentDiff;
        Sort();
        Reload(current);
    }

    void Sort()
    {
        if (_columns.Length == 0) return;
        var column = _columns[_sortColumn];
        int sign = _sortOrder == SortOrder.Descending ? -1 : 1;
        var items = _rows.Select(r => (Row: r, Key: column.SortKey(r))).ToList();
        // Empty values last in both directions; equal values by trade id, so the order is stable.
        items.Sort((x, y) =>
        {
            int result = x.Key == null ? (y.Key == null ? 0 : 1) : y.Key == null ? -1 : sign * ((IComparable)x.Key).CompareTo(y.Key);
            return result != 0 ? result : string.CompareOrdinal(x.Row.Item.TradeId, y.Row.Item.TradeId);
        });
        _rows = items.ConvertAll(x => x.Row);
    }

    /// <summary>Differing columns only: the properties with a gap shown (relevant gaps, or all gaps of the masked rows).</summary>
    void UpdateColumnVisibility()
    {
        ulong shown = 0;
        foreach (var row in _rows) shown |= row.IsRelevant ? row.Relevant : row.Differing;
        foreach (DataGridViewColumn column in Columns)
            if (column.Tag is ResultColumn { Index: >= 0 } c) column.Visible = !_differingOnly || (shown >> c.Index & 1) != 0;
    }

    void Reload(DiffRow? current)
    {
        if (_columns.Length == 0) return;
        int currentColumn = Math.Max(0, CurrentCell?.ColumnIndex ?? 0);
        int firstRow = FirstDisplayedScrollingRowIndex;
        UpdateColumnVisibility();
        _reloading = true;
        try
        {
            Rows.Clear();
            RowCount = _rows.Count;
            SortColumn = Columns[_sortColumn];
            SortDirection = _sortOrder;
            // The current row stays current; otherwise the first one, so its details show.
            int index = current == null ? -1 : _rows.IndexOf(current);
            if (index < 0 && _rows.Count > 0) index = 0;
            CurrentCell = null;
            if (index >= 0 && Columns[currentColumn].Visible) CurrentCell = this[currentColumn, index];
            if (firstRow >= 0 && firstRow < RowCount) FirstDisplayedScrollingRowIndex = firstRow;
        }
        finally { _reloading = false; }
        Invalidate();
        CurrentDiffChanged?.Invoke();
    }

    protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
    {
        base.OnCellValueNeeded(e);
        if (e.RowIndex < _rows.Count && Columns[e.ColumnIndex].Tag is ResultColumn c) e.Value = c.Value(_rows[e.RowIndex]);
    }

    /// <summary>Values that differ highlighted (relevant: hot; within the threshold: warm); gaps green (rise) or red (fall).</summary>
    protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
    {
        base.OnCellFormatting(e);
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.CellStyle == null || Columns[e.ColumnIndex].Tag is not ResultColumn { Index: >= 0 } c) return;
        var row = _rows[e.RowIndex];
        if (!row.Differs(c.Index)) return;
        bool relevant = row.IsRelevantAt(c.Index);
        if (c.IsGap)
            e.CellStyle.ForeColor = !relevant ? Theme.Colors.Muted : e.Value is double d && d > 0 ? Theme.Colors.Good : Theme.Colors.Danger;
        else
            e.CellStyle.BackColor = relevant ? Theme.Colors.Hot : Theme.Colors.Warm;
    }

    /// <summary>Kind cell: icon then text.</summary>
    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.ColumnIndex != 0 || e.Graphics == null)
        {
            base.OnCellPainting(e);
            return;
        }
        e.PaintBackground(e.CellBounds, (e.State & DataGridViewElementStates.Selected) != 0);
        var kind = _rows[e.RowIndex].Kind;
        var icon = KindStyles.Icon(kind);
        var r = e.CellBounds;
        e.Graphics.DrawImage(icon, r.X + Metrics.IconLeft, r.Y + (r.Height - icon.Height) / 2, icon.Width, icon.Height);
        TextRenderer.DrawText(e.Graphics, KindStyles.Text(kind), Theme.UI, new Rectangle(r.X + Metrics.IconTextLeft, r.Y, r.Width - Metrics.IconTextLeft - 2, r.Height),
            Theme.Colors.Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.Handled = true;
    }

    protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
    {
        base.OnColumnHeaderMouseClick(e);
        SortBy(e.ColumnIndex);
    }

    protected override void OnCurrentCellChanged(EventArgs e)
    {
        base.OnCurrentCellChanged(e);
        if (!_reloading) CurrentDiffChanged?.Invoke();
    }
}
