namespace Xero.DataComparer.UI;

enum Filter { All, OnlyInReference, OnlyInTarget, Different, WithinThreshold }

static class Filters
{
    public static readonly (string Text, Filter Value)[] Items =
    {
        ("All relevant gaps", Filter.All), ("Only in reference", Filter.OnlyInReference), ("Only in target", Filter.OnlyInTarget),
        ("Different", Filter.Different), ("Within the relevance threshold", Filter.WithinThreshold),
    };

    /// <summary>Rows of the filter, then of the search (trade id or book) when there is one.</summary>
    public static bool Matches(this Filter filter, DiffRow row, string search)
    {
        bool ok = filter switch
        {
            Filter.OnlyInReference => row.Kind == DiffKind.OnlyInReference,
            Filter.OnlyInTarget => row.Kind == DiffKind.OnlyInTarget,
            Filter.Different => row.Kind == DiffKind.Different && row.IsRelevant,
            Filter.WithinThreshold => !row.IsRelevant,
            _ => row.IsRelevant,
        };
        return ok && (search.Length == 0
            || row.Item.TradeId.Contains(search, StringComparison.OrdinalIgnoreCase)
            || row.Item.Book.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
}
