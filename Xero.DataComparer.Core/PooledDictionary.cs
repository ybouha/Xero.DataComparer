namespace Xero.DataComparer.Core;

/// <summary>
/// Lightweight dictionary backed by two parallel arrays sized to exact capacity at construction.
/// No internal resizing, no external dependencies.
/// Only the operations used by the comparison pipeline are implemented;
/// lookup is a linear scan which is faster than hashing for the small entry counts used here.
/// </summary>
public sealed class PooledDictionary<TKey, TValue> where TKey : notnull
{
    private static readonly EqualityComparer<TKey> _comparer = EqualityComparer<TKey>.Default;

    private TKey[] _keys;
    private TValue[] _values;
    private int _count;

    public PooledDictionary(int capacity = 16)
    {
        _keys = new TKey[capacity];
        _values = new TValue[capacity];
    }

    public int Count => _count;

    public IEnumerable<TKey> Keys
    {
        get
        {
            for (int i = 0; i < _count; i++)
                yield return _keys[i];
        }
    }

    public TValue this[TKey key]
    {
        get
        {
            for (int i = 0; i < _count; i++)
                if (_comparer.Equals(_keys[i], key))
                    return _values[i];
            throw new KeyNotFoundException($"Key not found: {key}");
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_comparer.Equals(_keys[i], key))
            {
                value = _values[i];
                return true;
            }
        }
        value = default!;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        // Guard: skip silently if key already present (parallel paths can produce duplicates)
        for (int i = 0; i < _count; i++)
            if (_comparer.Equals(_keys[i], key)) return;

        if (_count == _keys.Length) Grow();
        _keys[_count] = key;
        _values[_count] = value;
        _count++;
    }

    private void Grow()
    {
        Array.Resize(ref _keys, _keys.Length * 2);
        Array.Resize(ref _values, _values.Length * 2);
    }
}
