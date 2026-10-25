using System.Globalization;

namespace Xero.DataComparer.UI;

/// <summary>Gaps up to <see cref="Value"/> (absolute, or in % of the reference value) are not relevant.</summary>
readonly record struct RelevanceThreshold(double Value, bool Percent)
{
    public bool IsSet => Value > 0;

    /// <summary>Empty text: no threshold. Accepts the current culture and the invariant one; negative values are invalid.</summary>
    public static bool TryParse(string text, bool percent, out RelevanceThreshold threshold)
    {
        text = text.Trim();
        double value = 0;
        bool valid = text.Length == 0
            || (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) && value >= 0;
        threshold = new RelevanceThreshold(valid ? value : 0, percent);
        return valid;
    }

    /// <summary>A non-numeric difference is always relevant; a numeric one when its gap is above the threshold.</summary>
    public bool Exceeds(Field property, RiskIndicator reference, RiskIndicator target)
    {
        if (!property.IsNumeric || !IsSet) return true;
        double r = property.Number(reference), gap = Math.Abs(property.Number(target) - r);
        return Percent ? r == 0 || gap / Math.Abs(r) * 100 > Value : gap > Value;
    }

    public override string ToString() => Percent ? $"{Value:0.####} % of the reference" : $"{Value:#,##0.####}";
}
