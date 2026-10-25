using System.Linq.Expressions;
using System.Reflection;

namespace Xero.DataComparer.UI;

/// <summary>A property of <see cref="RiskIndicator"/> with a compiled getter: the grid reads it for every visible cell.</summary>
sealed class Field
{
    public static readonly Field[] All = typeof(RiskIndicator).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => new Field(p)).ToArray();

    Field(PropertyInfo property)
    {
        Name = property.Name;
        Type = property.PropertyType;
        var item = Expression.Parameter(typeof(RiskIndicator));
        Get = Expression.Lambda<Func<RiskIndicator, object?>>(Expression.Convert(Expression.Property(item, property), typeof(object)), item).Compile();
    }

    public string Name { get; }
    public Type Type { get; }
    public Func<RiskIndicator, object?> Get { get; }
    public bool IsNumeric => Type == typeof(double);

    public double Number(RiskIndicator item) => (double)Get(item)!;

    public static Field Named(string name) => All.First(f => f.Name == name);
}
