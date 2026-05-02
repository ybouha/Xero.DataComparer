using System.ComponentModel;

namespace Xero.DataComparer.Core;

public class CompareResult<T> : List<PooledDictionary<string, object>>, ITypedList
{
    public PooledDictionary<string, object> FirstRow
    {
        get
        {
            if (Count == 0) return new PooledDictionary<string, object>();
            var best = this[0];
            for (int i = 1; i < Count; i++)
                if (this[i].Count > best.Count) best = this[i];
            return best;
        }
    }

    public Dictionary<string, Type> ColumnTypes { get; }

    public List<T>? OnlyInReference { get; internal set; } = new();

    public List<T>? OnlyInTarget { get; internal set; } = new();


    public CompareResult(List<PooledDictionary<string, object>>? data) : base(data)
    {
        ColumnTypes = FirstRow.Keys.Distinct().ToDictionary(column => column, column => FirstRow[column]?.GetType() ?? typeof(string));
    }

    PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
    {
        var descriptors = FirstRow.Keys.Select(key => new DynamicPropertyDescriptor(key, ColumnTypes[key])).ToArray();
        return new PropertyDescriptorCollection(descriptors);
    }

    string ITypedList.GetListName(PropertyDescriptor[] listAccessors) { throw new NotImplementedException(); }
}