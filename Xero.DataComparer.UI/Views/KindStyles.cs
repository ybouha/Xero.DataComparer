namespace Xero.DataComparer.UI;

/// <summary>Icon and text of each kind of gap: only in reference (−), only in target (+), different (pencil).</summary>
static class KindStyles
{
    static Dictionary<DiffKind, Bitmap> _icons = BuildIcons();

    static Dictionary<DiffKind, Bitmap> BuildIcons() => new()
    {
        [DiffKind.OnlyInReference] = Theme.Glyph(Glyphs.Remove, Theme.Colors.Danger),
        [DiffKind.OnlyInTarget] = Theme.Glyph(Glyphs.Add, Theme.Colors.Good),
        [DiffKind.Different] = Theme.Glyph(Glyphs.Edit, Theme.Colors.Warning),
    };

    /// <summary>Redraws the icons in the colours of the current theme.</summary>
    public static void ApplyTheme() => _icons = BuildIcons();

    public static Bitmap Icon(DiffKind kind) => _icons[kind];

    public static string Text(DiffKind kind) => kind switch
    {
        DiffKind.OnlyInReference => "Only in reference",
        DiffKind.OnlyInTarget => "Only in target",
        _ => "Different",
    };
}
