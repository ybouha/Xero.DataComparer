using Xero.DataComparer.Core;

namespace Xero.DataComparer.UI;

/// <summary>Timeline of the ListComparer steps: one bar per step on a common ms axis, so the parallel ones overlap.</summary>
sealed class GanttChart : Control
{
    IReadOnlyList<TaskTiming> _tasks = [];

    public GanttChart()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Font = Theme.UI;
    }

    public void SetTasks(IReadOnlyList<TaskTiming> tasks)
    {
        _tasks = tasks;
        Invalidate();
    }

    static Color BarColor(string name) =>
        name.Contains("HashSet") ? Theme.Colors.Info
        : name.Contains("InBoth") ? Theme.Colors.Accent
        : Theme.Colors.Warning;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Colors.Surface);
        int pad = LogicalToDeviceUnits(Metrics.GanttPadding);
        int titleH = LogicalToDeviceUnits(Metrics.GanttTitleHeight);
        const TextFormatFlags line = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, "PARALLEL STEPS OF LISTCOMPARER.COMPARELISTASYNC", Theme.TileCaption, new Rectangle(pad, pad / 2, Width - 2 * pad, titleH), Theme.Colors.Caption, line);
        if (_tasks.Count == 0)
        {
            TextRenderer.DrawText(g, "Run a comparison to see how its steps overlap.", Font, new Rectangle(pad, pad / 2 + titleH, Width - 2 * pad, Font.Height + 4), Theme.Colors.Muted, line);
            return;
        }

        double total = Math.Max(1, _tasks.Max(t => t.End.TotalMilliseconds));
        int labelW = LogicalToDeviceUnits(Metrics.GanttLabelWidth), valueW = LogicalToDeviceUnits(Metrics.GanttValueWidth);
        int rowH = LogicalToDeviceUnits(Metrics.GanttRowHeight), barH = LogicalToDeviceUnits(Metrics.GanttBarHeight);
        int x0 = pad + labelW, barsW = Math.Max(10, Width - x0 - valueW - pad);
        int y = pad / 2 + titleH;

        // Axis: 0, 25, 50, 75 and 100 % of the wall clock, with a light vertical guide.
        // Labelled from the end, which matters most; a label that would overlap the one on its right is left out.
        int labelsLeft = int.MaxValue;
        using (var guide = new Pen(Theme.Colors.GridLine))
            for (int i = 4; i >= 0; i--)
            {
                int x = x0 + barsW * i / 4;
                g.DrawLine(guide, x, y + Font.Height, x, y + Font.Height + 4 + _tasks.Count * rowH);
                var text = $"{total * i / 4:N0} ms";
                var size = TextRenderer.MeasureText(text, Font);
                int tx = Math.Clamp(x - size.Width / 2, x0, x0 + barsW - size.Width);
                if (tx + size.Width > labelsLeft) continue;
                TextRenderer.DrawText(g, text, Font, new Point(tx, y), Theme.Colors.Muted, TextFormatFlags.NoPrefix);
                labelsLeft = tx;
            }
        y += Font.Height + 4;

        using var track = new SolidBrush(Theme.Colors.TileBack);
        foreach (var task in _tasks)
        {
            TextRenderer.DrawText(g, task.Name, Font, new Rectangle(pad, y, labelW - 6, rowH), Theme.Colors.Ink, line);
            int barY = y + (rowH - barH) / 2;
            g.FillRectangle(track, x0, barY, barsW, barH);
            int bx = x0 + (int)(task.Start.TotalMilliseconds / total * barsW);
            int bw = Math.Max(2, (int)(task.Duration.TotalMilliseconds / total * barsW));
            using (var bar = new SolidBrush(BarColor(task.Name))) g.FillRectangle(bar, bx, barY, Math.Min(bw, x0 + barsW - bx), barH);
            TextRenderer.DrawText(g, $"{task.Duration.TotalMilliseconds:N0} ms", Font, new Rectangle(x0 + barsW, y, valueW, rowH), Theme.Colors.Ink,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            y += rowH;
        }

        // Sum of the step durations over the wall clock: how much the steps overlap.
        double work = _tasks.Sum(t => t.Duration.TotalMilliseconds);
        double wall = Math.Max(1, (_tasks.Max(t => t.End) - _tasks.Min(t => t.Start)).TotalMilliseconds);
        TextRenderer.DrawText(g, $"Wall clock {wall:N0} ms · steps {work:N0} ms · ×{work / wall:0.0} overlap · {Environment.ProcessorCount} logical cores",
            Font, new Rectangle(pad, y + 4, Width - 2 * pad, Font.Height + 4), Theme.Colors.Muted, line);
    }
}
