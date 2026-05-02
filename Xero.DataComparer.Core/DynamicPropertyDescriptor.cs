using System.ComponentModel;

namespace Xero.DataComparer.Core;

public class DynamicPropertyDescriptor : PropertyDescriptor
{
    public DynamicPropertyDescriptor(string name, Type type) : base(name, null)
    {
        Type = type;
    }

    public Type Type { get; }

    public override object? GetValue(object component)
    {
        // Support both PooledDictionary (used by CompareResult<T>) and the
        // standard Dictionary, so the descriptor works for any ITypedList
        // consumer — DevExpress GridControl, WinForms DataGridView, WPF
        // DataGrid, or a custom console renderer.
        return component switch
        {
            PooledDictionary<string, object> pooled
                => pooled.TryGetValue(Name, out var pv) ? pv : null,
            IDictionary<string, object> dict
                => dict.TryGetValue(Name, out var dv) ? dv : null,
            _ => null,
        };
    }

    public override bool IsReadOnly => false;

    public override Type PropertyType => Type;

    public override bool CanResetValue(object component) { throw new NotImplementedException(); }

    public override Type ComponentType => typeof(object);

    public override void ResetValue(object component) { throw new NotImplementedException(); }

    public override void SetValue(object? component, object? value) { throw new NotImplementedException(); }

    public override bool ShouldSerializeValue(object component) { throw new NotImplementedException(); }
}