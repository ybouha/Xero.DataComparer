namespace Xero.DataComparer.UI;

static class ErrorLog
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xero.DataComparer.UI", "errors.log");

    static bool _showing;

    public static void Write(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
    }

    public static void Report(Exception ex)
    {
        Write(ex);
        if (_showing) return; // never stack dialogs if the same error repeats
        _showing = true;
        try
        {
            MessageBox.Show($"{ex.GetType().Name}: {ex.Message}\n\nThe application keeps running. Details were written to:\n{Path}",
                "Xero.DataComparer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { _showing = false; }
    }
}
