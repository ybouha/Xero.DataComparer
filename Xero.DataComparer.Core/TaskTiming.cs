namespace Xero.DataComparer.Core;

/// <summary>One step of <see cref="ListComparer{T}.CompareListAsync"/>, timed from the start of the comparison.</summary>
public sealed record TaskTiming(string Name, TimeSpan Start, TimeSpan End)
{
    public TimeSpan Duration => End - Start;
}
