using System.ComponentModel;

namespace Xero.DataComparer.Core;

/// <summary>
/// Result of <see cref="ListComparer{T}.CompareListAsync"/>: one diff row per pair whose compared properties differ,
/// plus the items found on one side only and the items whose key appears more than once on a side.
/// </summary>
public class CompareResult<T> : List<PooledDictionary<string, object?>>, ITypedList
{
    private readonly PropertyDescriptorCollection _columns;

    internal CompareResult(List<PooledDictionary<string, object?>> rows, IReadOnlyList<(string Name, Type Type)> columns) : base(rows)
    {
        _columns = new PropertyDescriptorCollection(columns.Select(c => new DynamicPropertyDescriptor(c.Name, c.Type)).ToArray<PropertyDescriptor>(), readOnly: true);
        ColumnTypes = columns.ToDictionary(c => c.Name, c => c.Type);
    }

    /// <summary>
    /// Every column a diff row can hold, whatever the rows found: Key_X per key, Reference_X and Target_X per compared property,
    /// and ComparedItems. A row holds the Reference_/Target_ entries of the properties that differ only.
    /// </summary>
    public Dictionary<string, Type> ColumnTypes { get; }

    public List<T> OnlyInReference { get; internal init; } = [];

    public List<T> OnlyInTarget { get; internal init; } = [];

    /// <summary>Reference items whose key was already found in the reference: they are left out of the comparison.</summary>
    public List<T> DuplicatesInReference { get; internal init; } = [];

    /// <summary>Target items whose key was already found in the target: they are left out of the comparison.</summary>
    public List<T> DuplicatesInTarget { get; internal init; } = [];

    /// <summary>The timed steps of the comparison, by start time.</summary>
    public IReadOnlyList<TaskTiming> Timings { get; internal init; } = [];

    PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[]? listAccessors) => _columns;

    string ITypedList.GetListName(PropertyDescriptor[]? listAccessors) => typeof(T).Name;
}
