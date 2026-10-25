using System.Drawing.Drawing2D;

namespace Xero.DataComparer.UI;

/// <summary>
/// Drop-down list whose button and inner border take its BackColor/ForeColor: the flat ComboBox paints
/// them in system colours (white border, light grey button), which do not fit the dark theme.
/// The visible 1 px border is drawn around it by its container, like the tiles.
/// </summary>
sealed class FlatCombo : ComboBox
{
    public FlatCombo()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != 0x000F /* WM_PAINT */ || !IsHandleCreated) return;
        using var g = Graphics.FromHwnd(Handle);
        var r = ClientRectangle;
        int button = SystemInformation.VerticalScrollBarWidth + 2;
        using (var back = new SolidBrush(BackColor))
        using (var pen = new Pen(BackColor))
        {
            g.FillRectangle(back, r.Right - button, r.Y, button, r.Height);
            g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
        int x = r.Right - button / 2 - 4, y = r.Y + r.Height / 2 - 2;
        using var ink = new SolidBrush(Enabled ? ForeColor : Theme.Colors.Disabled);
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillPolygon(ink, new[] { new Point(x, y), new Point(x + 8, y), new Point(x + 4, y + 4) });
        g.SmoothingMode = old;
    }
}
