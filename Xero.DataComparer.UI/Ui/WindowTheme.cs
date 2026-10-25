using System.Runtime.InteropServices;

namespace Xero.DataComparer.UI;

/// <summary>Title bar and system scrollbars follow the theme as well (the WinForms colours do not reach them).</summary>
static class WindowTheme
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string? appName, string? idList);

    public static void Apply(Form form, params DataGridView[] grids)
    {
        if (!form.IsHandleCreated) return;
        int dark = Theme.Dark ? 1 : 0;
        DwmSetWindowAttribute(form.Handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref dark, sizeof(int));
        var visualStyle = Theme.Dark ? "DarkMode_Explorer" : "Explorer";
        SetWindowTheme(form.Handle, visualStyle, null);
        // Bar.Handle creates a scrollbar not shown yet (empty grid), so it gets the theme before it appears.
        foreach (var grid in grids)
            if (grid.IsHandleCreated)
                foreach (Control bar in grid.Controls)
                    if (bar is ScrollBar) SetWindowTheme(bar.Handle, visualStyle, null);
    }
}
