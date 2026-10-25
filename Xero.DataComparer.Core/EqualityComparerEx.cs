namespace Xero.DataComparer.Core;

/// <summary>Equality on a composite key: two items are equal when all their key properties are equal.</summary>
public class EqualityComparerEx<T> : IEqualityComparer<T>
{
    private readonly PropertyAccessor<T>[] _keys;

    public EqualityComparerEx(IEnumerable<string> keyProperties)
        : this(keyProperties.Select(PropertyAccessor<T>.Create).ToArray())
    {
    }

    internal EqualityComparerEx(PropertyAccessor<T>[] keys)
    {
        if (keys.Length == 0) throw new ArgumentException("At least one key property is required.", nameof(keys));
        _keys = keys;
    }

    public bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;

        for (int i = 0; i < _keys.Length; i++)
        {
            if (!_keys[i].AreEqual(x, y)) return false;
        }
        return true;
    }

    public int GetHashCode(T obj)
    {
        if (obj == null) return 0;
        unchecked
        {
            int hashCode = 17;
            for (int i = 0; i < _keys.Length; i++)
            {
                hashCode = hashCode * 31 + _keys[i].Hash(obj);
            }
            return hashCode;
        }
    }
}
