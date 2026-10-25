namespace Xero.DataComparer.UI;

/// <summary>Sizes and spacing in pixels at 96 dpi (the window scales them with the screen).</summary>
static class Metrics
{
    // Window
    public static readonly Size WindowSize = new(1280, 760);
    public static readonly Size WindowMinSize = new(480, 360);
    public static readonly Size ScrollMinSize = new(960, 600);   // below it the whole window scrolls
    public const int Gutter = 10;                                // left/right margin of the content
    public const int Border = 1;                                 // frame around grids and combos

    // Layout rows
    public const int ToolbarHeight = 30;
    public const int HeaderHeight = 62;                          // title + description
    public const int TitleHeight = 26;
    public const int TilesHeight = 78;
    public const int FilterBarHeight = 32;
    public const int DetailsHeight = 230;
    public const int SplitterWidth = 6;
    public const int PanelGap = 6;                               // between the details grid and the timings

    // Toolbar and filter bar
    public static readonly Padding ToolbarPadding = new(6, 2, 6, 2);
    public const int SearchWidth = 140;
    public const int DatasetWidth = 250;
    public const int ThresholdWidth = 70;
    public const int ThresholdModeWidth = 120;
    public const int FilterWidth = 220;

    // Tiles
    public const int TileGap = 6;
    public const int TileAccent = 4;                             // left bar of the selected tile
    public const int TileTextLeft = 12;

    // Grids
    public const int ColumnHeadersHeight = 52;                   // with the group band
    public const int DetailsHeadersHeight = 46;
    public const int RowHeight = 25;
    public const int DetailsRowHeight = 24;
    public const int RowHeaderWidth = 20;
    public const int CellPadding = 4;
    public const int AccentLine = 2;                             // under column headers, right of row headers
    public const int KindWidth = 150;
    public const int KeyWidth = 100;
    public const int ValueWidth = 96;
    public const int GapWidth = 84;
    public const int DateTimeWidth = 160;
    public const int DetailsMinWidth = 70;

    // Timings chart
    public const int GanttPadding = 10;
    public const int GanttTitleHeight = 22;
    public const int GanttLabelWidth = 150;
    public const int GanttValueWidth = 70;
    public const int GanttRowHeight = 22;
    public const int GanttBarHeight = 12;

    // Icons
    public const int Icon = 16;
    public const int IconLeft = 6;                               // icon inside the Kind cell
    public const int IconTextLeft = 28;                          // text after that icon
}
