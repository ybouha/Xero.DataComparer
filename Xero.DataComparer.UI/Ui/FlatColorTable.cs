namespace Xero.DataComparer.UI;

sealed class FlatColorTable : ProfessionalColorTable
{
    static Color HoverBack => Theme.Colors.ItemHover;
    static Color HoverBorder => Theme.Colors.ItemHoverBorder;
    static Color Pressed => Theme.Colors.ItemPressed;

    public override Color ToolStripGradientBegin => Theme.Colors.Toolbar;
    public override Color ToolStripGradientMiddle => Theme.Colors.Toolbar;
    public override Color ToolStripGradientEnd => Theme.Colors.Toolbar;
    public override Color ToolStripBorder => Theme.Colors.Toolbar;
    public override Color ButtonSelectedHighlight => HoverBack;
    public override Color ButtonSelectedGradientBegin => HoverBack;
    public override Color ButtonSelectedGradientMiddle => HoverBack;
    public override Color ButtonSelectedGradientEnd => HoverBack;
    public override Color ButtonSelectedBorder => HoverBorder;
    public override Color ButtonPressedGradientBegin => Pressed;
    public override Color ButtonPressedGradientMiddle => Pressed;
    public override Color ButtonPressedGradientEnd => Pressed;
    public override Color ButtonPressedBorder => HoverBorder;
    public override Color ButtonCheckedGradientBegin => Pressed;
    public override Color ButtonCheckedGradientMiddle => Pressed;
    public override Color ButtonCheckedGradientEnd => Pressed;
    public override Color MenuItemSelected => HoverBack;
    public override Color MenuItemBorder => HoverBorder;
    public override Color MenuItemSelectedGradientBegin => HoverBack;
    public override Color MenuItemSelectedGradientEnd => HoverBack;
    public override Color MenuItemPressedGradientBegin => Theme.Colors.Toolbar;
    public override Color MenuItemPressedGradientEnd => Theme.Colors.Toolbar;
    public override Color MenuBorder => Theme.Colors.GroupLine;
    public override Color ToolStripDropDownBackground => Theme.Colors.Surface;
    public override Color ImageMarginGradientBegin => Theme.Colors.Toolbar;
    public override Color ImageMarginGradientMiddle => Theme.Colors.Toolbar;
    public override Color ImageMarginGradientEnd => Theme.Colors.Toolbar;
    public override Color SeparatorDark => Theme.Colors.Separator;
    public override Color SeparatorLight => Theme.Colors.Toolbar;
    public override Color StatusStripGradientBegin => Theme.Colors.Body;
    public override Color StatusStripGradientEnd => Theme.Colors.Body;
    public override Color OverflowButtonGradientBegin => Theme.Colors.Toolbar;
    public override Color OverflowButtonGradientMiddle => Theme.Colors.Toolbar;
    public override Color OverflowButtonGradientEnd => Theme.Colors.Toolbar;
}
