namespace Xero.DataComparer.UI;

/// <summary>Message (step in progress, rows shown, hint), scrolling progress bar while comparing, timings of the last comparison.</summary>
sealed class ComparisonStatusBar : StatusStrip
{
    readonly ToolStripStatusLabel _message = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    readonly ToolStripProgressBar _progress = new() { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Visible = false };
    readonly ToolStripStatusLabel _timings = new();

    public ComparisonStatusBar()
    {
        SizingGrip = true;
        Renderer = new FlatRenderer();
        Items.AddRange(new ToolStripItem[] { _message, _progress, _timings });
    }

    public void ShowMessage(string text) => _message.Text = text;

    public void SetBusy(bool busy) => _progress.Visible = busy;

    public void ShowTimings(DemoComparison run) =>
        _timings.Text = $"Generated in {run.Generation.TotalMilliseconds:N0} ms · compared in {run.Comparison.TotalMilliseconds:N0} ms"
            + $" · grid rows built in {run.RowsBuild.TotalMilliseconds:N0} ms";

    public void ApplyTheme() => BackColor = Theme.Colors.Body;
}
