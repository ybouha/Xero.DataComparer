namespace Xero.DataComparer.UI;

enum ColumnPart { Kind, Key, Reference, Target, Gap, Percent }

/// <summary>What a column of the result grid shows; Index is the position of its property in the compared ones (-1: kind, keys).</summary>
sealed record ResultColumn(ColumnPart Part, Field? Field = null, int Index = -1)
{
    public bool IsGap => Part is ColumnPart.Gap or ColumnPart.Percent;

    public object? Value(DiffRow row) => Part switch
    {
        ColumnPart.Kind => KindStyles.Text(row.Kind),
        ColumnPart.Key => Field!.Get(row.Item),
        ColumnPart.Reference => row.Reference is { } r ? Field!.Get(r) : null,
        ColumnPart.Target => row.Target is { } t ? Field!.Get(t) : null,
        ColumnPart.Gap => row.Gap(Field!),
        _ => row.Percent(Field!),
    };

    /// <summary>Gaps sort by magnitude: the largest ones first in descending order, whatever their sign.</summary>
    public object? SortKey(DiffRow row) => Part switch
    {
        ColumnPart.Kind => (int)row.Kind,
        ColumnPart.Gap or ColumnPart.Percent => Value(row) is double d ? Math.Abs(d) : null,
        _ => Value(row),
    };

    public DataGridViewColumn CreateColumn()
    {
        var type = Field?.Type;
        var column = new DataGridViewTextBoxColumn
        {
            Tag = this, SortMode = DataGridViewColumnSortMode.Programmatic,
            HeaderText = Part switch
            {
                ColumnPart.Kind => "Kind", ColumnPart.Key => Field!.Name, ColumnPart.Reference => "Reference", ColumnPart.Target => "Target",
                ColumnPart.Gap => "Gap", _ => "Gap %",
            },
            Width = Part switch
            {
                ColumnPart.Kind => Metrics.KindWidth, ColumnPart.Key => Metrics.KeyWidth, ColumnPart.Gap or ColumnPart.Percent => Metrics.GapWidth,
                _ => type == typeof(DateTime) ? Metrics.DateTimeWidth : Metrics.ValueWidth,
            },
        };
        column.DefaultCellStyle.Format = Part switch
        {
            ColumnPart.Gap => Formats.Gap,
            ColumnPart.Percent => Formats.Percent,
            _ => Formats.For(type),
        };
        if (IsGap || type == typeof(double)) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }
}
