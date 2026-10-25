namespace Xero.DataComparer.UI;

/// <summary>
/// Details of a result row: reference and target one under the other, with keys, compared and ignored properties
/// (the comparator returns the whole items of each pair). Values that differ are highlighted like in the result grid.
/// </summary>
sealed class DetailsGrid : GroupedHeaderGrid
{
    Field[] _fields = [];
    DiffRow? _row;

    public DetailsGrid()
    {
        GridStyle.Apply(this);
        RowHeadersVisible = false;
        ColumnHeadersHeight = Metrics.DetailsHeadersHeight;
        RowTemplate.Height = Metrics.DetailsRowHeight;
    }

    public void ApplyTheme() => GridStyle.ApplyColors(this);

    /// <summary>Side, then the groups Keys, Compared and Ignored. A compared column's Tag is its property index (-1 otherwise).</summary>
    public void Build(DemoComparison run)
    {
        Rows.Clear();
        Columns.Clear();
        Groups.Clear();
        Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Side", Tag = -1, SortMode = DataGridViewColumnSortMode.NotSortable, Width = Metrics.KeyWidth });
        AddGroup("Keys", run.Keys, _ => -1);
        AddGroup("Compared", run.Properties, f => Array.IndexOf(run.Properties, f));
        AddGroup("Ignored", run.Ignored, _ => -1);
        _fields = run.Keys.Concat(run.Properties).Concat(run.Ignored).ToArray();
    }

    void AddGroup(string group, IEnumerable<Field> fields, Func<Field, int> index)
    {
        int first = Columns.Count;
        foreach (var f in fields)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = f.Name, Tag = index(f), SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, MinimumWidth = Metrics.DetailsMinWidth,
            };
            column.DefaultCellStyle.Format = Formats.For(f.Type);
            if (f.IsNumeric) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            Columns.Add(column);
        }
        if (Columns.Count > first) Groups.Add((group, first, Columns.Count - 1));
    }

    public void ShowRow(DiffRow? row)
    {
        _row = row;
        Rows.Clear();
        if (row == null || Columns.Count == 0) return;
        foreach (var (side, item) in new[] { ("Reference", row.Reference), ("Target", row.Target) })
            if (item != null) Rows.Add(new object[] { side }.Concat(_fields.Select(f => f.Get(item)!)).ToArray());
        CurrentCell = null;
    }

    protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
    {
        base.OnCellFormatting(e);
        if (e.RowIndex < 0 || e.CellStyle == null) return;
        if (e.ColumnIndex == 0) e.CellStyle.ForeColor = Theme.Colors.Muted;
        else if (_row != null && Columns[e.ColumnIndex].Tag is int index && index >= 0 && _row.Differs(index))
            e.CellStyle.BackColor = _row.IsRelevantAt(index) ? Theme.Colors.Hot : Theme.Colors.Warm;
    }
}
