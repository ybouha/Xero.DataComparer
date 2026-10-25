using System.Linq.Expressions;
using System.Reflection;

namespace Xero.DataComparer.Core;

/// <summary>
/// Compiled accessors of one property of <typeparamref name="T"/>. Equality and hash code work on the typed value,
/// so comparing two items does not box; <see cref="Get"/> boxes and is only used for the values of a diff row.
/// </summary>
internal sealed class PropertyAccessor<T>
{
    private PropertyAccessor(PropertyInfo property, Func<T, object?> get, Func<T, T, bool> areEqual, Func<T, int> hash)
    {
        Name = property.Name;
        Type = property.PropertyType;
        Get = get;
        AreEqual = areEqual;
        Hash = hash;
    }

    public string Name { get; }

    public Type Type { get; }

    public Func<T, object?> Get { get; }

    public Func<T, T, bool> AreEqual { get; }

    public Func<T, int> Hash { get; }

    public static PropertyAccessor<T> Create(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new ArgumentException($"Property {propertyName} not found on {typeof(T).Name}", nameof(propertyName));
        return Create(property);
    }

    public static PropertyAccessor<T> Create(PropertyInfo property) =>
        (PropertyAccessor<T>)typeof(PropertyAccessor<T>)
            .GetMethod(nameof(CreateTyped), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(property.PropertyType)
            .Invoke(null, [property])!;

    private static PropertyAccessor<T> CreateTyped<TProperty>(PropertyInfo property)
    {
        var item = Expression.Parameter(typeof(T), "item");
        var getter = Expression.Lambda<Func<T, TProperty>>(Expression.Property(item, property), item).Compile();
        var comparer = EqualityComparer<TProperty>.Default;
        return new PropertyAccessor<T>(
            property,
            x => getter(x),
            (x, y) => comparer.Equals(getter(x), getter(y)),
            x => getter(x) is { } value ? comparer.GetHashCode(value) : 0);
    }
}
