namespace Xero.DataComparer.UI;

/// <summary>Every colour of the interface, by role. <see cref="Theme.Colors"/> is the palette in use.</summary>
sealed record Palette
{
    // Surfaces
    public required Color Body { get; init; }        // window background
    public required Color Surface { get; init; }     // grids, text boxes, menus
    public required Color Toolbar { get; init; }
    public required Color Accent { get; init; }

    // Indicator tiles; their border is also the border of every framed control
    public required Color TileBack { get; init; }
    public required Color TileHover { get; init; }
    public required Color Border { get; init; }

    // Text
    public required Color Ink { get; init; }
    public required Color Caption { get; init; }     // tile captions, column headers
    public required Color Muted { get; init; }
    public required Color Disabled { get; init; }
    public required Color Advice { get; init; }      // suggestion text

    // Grid cells
    public required Color GridLine { get; init; }
    public required Color AltRow { get; init; }
    public required Color Selection { get; init; }
    public required Color Hot { get; init; }         // large consumer, actionable suggestion
    public required Color Warm { get; init; }        // medium consumer

    // Column and row headers
    public required Color GroupBack { get; init; }
    public required Color HeaderBack { get; init; }
    public required Color HeaderHover { get; init; }
    public required Color HeaderSorted { get; init; }
    public required Color HeaderLine { get; init; }  // between columns of a group
    public required Color GroupLine { get; init; }   // between groups, menu border

    // Toolbar and menu items
    public required Color ItemHover { get; init; }
    public required Color ItemHoverBorder { get; init; }
    public required Color ItemPressed { get; init; }
    public required Color Separator { get; init; }

    // Icons
    public required Color IconInk { get; init; }     // neutral icons
    public required Color Info { get; init; }
    public required Color Good { get; init; }
    public required Color Warning { get; init; }
    public required Color Danger { get; init; }
    public required Color ContainerIcon { get; init; }
    public required Color ProcessIcon { get; init; }
    public required Color SettingIcon { get; init; }

    public static readonly Palette Light = new()
    {
        Body = Rgb(0xF1F3F6), Surface = Rgb(0xFFFFFF), Toolbar = Rgb(0xF5F6F8), Accent = Rgb(0x34548C),
        TileBack = Rgb(0xDDE4EE), TileHover = Rgb(0xE6ECF4), Border = Rgb(0xB2BED0),
        Ink = Rgb(0x1E2024), Caption = Rgb(0x263A5E), Muted = Rgb(0x58606E), Disabled = Rgb(0xA0A6B0), Advice = Rgb(0x963214),
        GridLine = Rgb(0xDDE2EA), AltRow = Rgb(0xEBF4E5), Selection = Rgb(0xFFF6A0), Hot = Rgb(0xF7DCDC), Warm = Rgb(0xFCECD6),
        GroupBack = Rgb(0xE8EDF4), HeaderBack = Rgb(0xF8FAFC), HeaderHover = Rgb(0xE5F0FB), HeaderSorted = Rgb(0xDCE8F6),
        HeaderLine = Rgb(0xD3DAE5), GroupLine = Rgb(0xA8B5C8),
        ItemHover = Rgb(0xC9DEF5), ItemHoverBorder = Rgb(0x3399FF), ItemPressed = Rgb(0x99CCFF), Separator = Rgb(0xCCCEDB),
        IconInk = Rgb(0x2E426A), Info = Rgb(0x0066CC), Good = Rgb(0x107C10), Warning = Rgb(0xCA8C00), Danger = Rgb(0xC42B1C),
        ContainerIcon = Rgb(0x0078D4), ProcessIcon = Rgb(0x3C3C3C), SettingIcon = Rgb(0x885C00),
    };

    /// <summary>Neutral greys, green accent and orange warnings, after SquashSync's dark_theme.png.</summary>
    public static readonly Palette Dark = new()
    {
        Body = Rgb(0x0A0A0A), Surface = Rgb(0x232323), Toolbar = Rgb(0x1A1A1A), Accent = Rgb(0x22CD5A),
        TileBack = Rgb(0x1A1A1A), TileHover = Rgb(0x2C2C2C), Border = Rgb(0x373737),
        Ink = Rgb(0xD7D7D7), Caption = Rgb(0xEEEEEE), Muted = Rgb(0xB0B0B0), Disabled = Rgb(0x686868), Advice = Rgb(0xFFA217),
        GridLine = Rgb(0x2C2C2C), AltRow = Rgb(0x282828), Selection = Rgb(0x424242), Hot = Rgb(0x542424), Warm = Rgb(0x503E18),
        GroupBack = Rgb(0x202020), HeaderBack = Rgb(0x2C2C2C), HeaderHover = Rgb(0x373737), HeaderSorted = Rgb(0x3C3C3C),
        HeaderLine = Rgb(0x373737), GroupLine = Rgb(0x424242),
        ItemHover = Rgb(0x373737), ItemHoverBorder = Rgb(0x4C4C4C), ItemPressed = Rgb(0x424242), Separator = Rgb(0x373737),
        IconInk = Rgb(0xC4C4C4), Info = Rgb(0x60AAFF), Good = Rgb(0x41DE7A), Warning = Rgb(0xFFA217), Danger = Rgb(0xFF786E),
        ContainerIcon = Rgb(0x60AAFF), ProcessIcon = Rgb(0xB0B0B0), SettingIcon = Rgb(0xE6B450),
    };

    /// <summary>Notification-area icon: fixed, the taskbar has its own light/dark mode.</summary>
    public static class Tray
    {
        public static readonly Color ChipInk = Rgb(0x34548C);
        public static readonly Color ChipBack = Rgb(0xFFFFFF);
        public static readonly Color Unknown = Rgb(0xAAB0BA);
        public static readonly Color Low = Rgb(0x1C963C);
        public static readonly Color Medium = Rgb(0xE88C00);
        public static readonly Color High = Rgb(0xD63020);
    }

    public static Color Rgb(int rgb) => Color.FromArgb(unchecked((int)0xFF000000) | rgb);
}
