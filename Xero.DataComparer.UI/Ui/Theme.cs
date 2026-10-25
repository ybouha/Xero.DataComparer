using System.Drawing.Text;

namespace Xero.DataComparer.UI;

static class Theme
{
    const string SettingsKey = @"Software\Xero.DataComparer.UI";

    /// <summary>Saved choice, otherwise the Windows app mode (Settings > Personalization > Colors).</summary>
    public static bool Dark { get; private set; } = ReadSavedDark();

    public static Palette Colors => Dark ? Palette.Dark : Palette.Light;

    public static void SetDark(bool dark)
    {
        Dark = dark;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(SettingsKey);
            key.SetValue("DarkTheme", dark ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch (Exception ex) { ErrorLog.Write(ex); } // the theme still changes, it just won't be remembered
    }

    static bool ReadSavedDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SettingsKey);
            if (key?.GetValue("DarkTheme") is int saved) return saved != 0;
            using var personalize = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return personalize?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch { return false; }
    }

    public static readonly Font UI = new("Segoe UI", 9F);
    public static readonly Font UIBold = new("Segoe UI", 9F, FontStyle.Bold);
    public static readonly Font Semibold = new("Segoe UI Semibold", 9F);
    public static readonly Font Title = new("Segoe UI", 12F);
    public static readonly Font TileCaption = new("Segoe UI", 7.5F, FontStyle.Bold);
    public static readonly Font TileValue = new("Segoe UI", 14F, FontStyle.Bold);

    static readonly string IconFont =
        new InstalledFontCollection().Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    /// <summary>Renders a Segoe icon-font glyph into a bitmap (no image resources needed).</summary>
    public static Bitmap Glyph(char glyph, Color color, int size = Metrics.Icon)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var font = new Font(IconFont, size * 0.8f, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, size, size), sf);
        return bmp;
    }

    /// <summary>
    /// Theme button icon: the "miscellaneous/palette" icon of the Xatis shell (DevExpress Office 2013 set),
    /// redrawn with the same 3×3 colour squares instead of shipping DevExpress images.
    /// </summary>
    public static Bitmap PaletteIcon()
    {
        int[] colors = { 0x4D82B8, 0x76A797, 0xEEC472, 0x91C4D7, 0x9DCA8C, 0xE4A861, 0x77A3A7, 0xD7CF88, 0xD86344 };
        var bmp = new Bitmap(Metrics.Icon, Metrics.Icon);
        using var g = Graphics.FromImage(bmp);
        for (int i = 0; i < colors.Length; i++)
        {
            using var b = new SolidBrush(Palette.Rgb(colors[i]));
            g.FillRectangle(b, 1 + i % 3 * 5, 1 + i / 3 * 5, 4, 4);
        }
        return bmp;
    }
}
