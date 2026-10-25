namespace Xero.DataComparer.UI;

/// <summary>Flat toolbar and menu renderer; text colours follow the theme (the default ones are for light backgrounds).</summary>
sealed class FlatRenderer : ToolStripProfessionalRenderer
{
    public FlatRenderer() : base(new FlatColorTable()) => RoundedEdges = false;

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item?.Enabled == false ? Theme.Colors.Disabled : Theme.Colors.Ink;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == false ? Theme.Colors.Disabled : Theme.Colors.Ink;
        base.OnRenderArrow(e);
    }
}
