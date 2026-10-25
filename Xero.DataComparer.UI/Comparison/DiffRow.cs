namespace Xero.DataComparer.UI;

enum DiffKind { OnlyInReference, OnlyInTarget, Different }

/// <summary>One line of the result: a row present on one side only, or a pair whose compared properties differ.</summary>
sealed class DiffRow
{
    public required DiffKind Kind { get; init; }
    public RiskIndicator? Reference { get; init; }
    public RiskIndicator? Target { get; init; }
    public RiskIndicator Item => Reference ?? Target!;

    /// <summary>Bit i: compared property i differs (read from the Reference_/Target_ entries of the diff row).</summary>
    public ulong Differing { get; init; }

    /// <summary>Bit i: compared property i differs by more than the relevance threshold.</summary>
    public ulong Relevant { get; set; }

    /// <summary>A pair whose gaps are all within the threshold is not relevant; a row on one side only always is.</summary>
    public bool IsRelevant => Kind != DiffKind.Different || Relevant != 0;

    public bool Differs(int property) => (Differing >> property & 1) != 0;
    public bool IsRelevantAt(int property) => (Relevant >> property & 1) != 0;

    /// <summary>Target − reference of a numeric property of a pair; none when there is no gap.</summary>
    public double? Gap(Field property)
    {
        if (Kind != DiffKind.Different || !property.IsNumeric) return null;
        double gap = property.Number(Target!) - property.Number(Reference!);
        return gap == 0 ? null : gap;
    }

    /// <summary>Gap as a fraction of the reference value (formatted with %).</summary>
    public double? Percent(Field property)
    {
        if (Gap(property) is not double gap) return null;
        double r = property.Number(Reference!);
        return r == 0 ? null : gap / Math.Abs(r);
    }
}
