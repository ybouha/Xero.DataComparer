using System.ComponentModel;

namespace Xero.DataComparer.UI;

/// <summary>Clickable summary card: uppercase caption, big value and a sub-line underneath.</summary>
sealed class IndicatorTile : Control
{
    string _caption = "", _value = "", _sub = "";
    bool _selected, _hover;

    public IndicatorTile()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Font = Theme.UI;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption { get => _caption; set { _caption = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value { get => _value; set { _value = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Sub { get => _sub; set { _sub = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Colors.Body);
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var b = new SolidBrush(_hover || _selected ? Theme.Colors.TileHover : Theme.Colors.TileBack)) g.FillRectangle(b, r);
        using (var p = new Pen(_selected ? Theme.Colors.Accent : Theme.Colors.Border)) g.DrawRectangle(p, r);
        if (_selected)
            using (var accent = new SolidBrush(Theme.Colors.Accent)) g.FillRectangle(accent, 0, 0, Metrics.TileAccent, Height);

        const TextFormatFlags line = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        int x = Metrics.TileTextLeft, w = Width - x - 6;
        int captionH = Theme.TileCaption.Height, valueH = Theme.TileValue.Height;
        TextRenderer.DrawText(g, _caption.ToUpperInvariant(), Theme.TileCaption, new Rectangle(x, 6, w, captionH), Theme.Colors.Caption, line);
        TextRenderer.DrawText(g, _value, Theme.TileValue, new Rectangle(x - 2, 6 + captionH, w, valueH), Theme.Colors.Ink, line);
        TextRenderer.DrawText(g, _sub, Font, new Rectangle(x, 6 + captionH + valueH, w, Font.Height), Theme.Colors.Muted, line);
    }
}
