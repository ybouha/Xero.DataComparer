namespace Xero.DataComparer.Core;

/// <summary>
/// Two hash sets (left and right) sized once from the known counts and sharing one comparer.
/// The hash code of each item is computed once, when it is added, and reused to look it up in the other set.
/// </summary>
public class HashSetComparer<T>
{
    private const int Lower31BitMask = 0x7FFFFFFF;

    //left
    private readonly int[] _leftBuckets;
    private readonly Slot[] _leftSlots;
    private int _leftLastIndex;

    //right
    private readonly int[] _rightBuckets;
    private readonly Slot[] _rightSlots;
    private int _rightLastIndex;

    private readonly IEqualityComparer<T> _comparer;

    public HashSetComparer(int leftCount, int rightCount, IEqualityComparer<T> comparer)
    {
        _comparer = comparer;

        int leftSize = HashHelpers.GetPrime(leftCount);
        _leftBuckets = new int[leftSize];
        _leftSlots = new Slot[leftSize];

        int rightSize = HashHelpers.GetPrime(rightCount);
        _rightBuckets = new int[rightSize];
        _rightSlots = new Slot[rightSize];
    }

    /// <summary>Adds an item to the left set; false when an equal item is already there (the item is not added).</summary>
    public bool AddLeftItem(T value) =>
        AddIfNotPresent(ref _leftLastIndex, _leftBuckets, _leftSlots, value);

    /// <summary>Adds an item to the right set; false when an equal item is already there (the item is not added).</summary>
    public bool AddRightItem(T value) =>
        AddIfNotPresent(ref _rightLastIndex, _rightBuckets, _rightSlots, value);

    private int InternalGetHashCode(T item)
    {
        if (item == null) return 0;
        return _comparer.GetHashCode(item) & Lower31BitMask;
    }

    private bool AddIfNotPresent(ref int lastIndex, int[] buckets, Slot[] slots, T value)
    {
        var hashCode = InternalGetHashCode(value);
        if (Contains(buckets, slots, hashCode, value, out _)) return false;
        if (lastIndex == slots.Length) throw new InvalidOperationException("More items added than the capacity given to the constructor.");

        var bucket = hashCode % buckets.Length;
        int index = lastIndex;
        lastIndex++;

        slots[index].hashCode = hashCode;
        slots[index].value = value;
        slots[index].next = buckets[bucket] - 1;
        buckets[bucket] = index + 1;

        return true;
    }

    public IEnumerable<T> OnlyInLeft()
    {
        for (int i = 0; i < _leftLastIndex; i++)
        {
            if (!Contains(_rightBuckets, _rightSlots, _leftSlots[i].hashCode, _leftSlots[i].value, out _))
                yield return _leftSlots[i].value;
        }
    }

    public IEnumerable<T> OnlyInRight()
    {
        for (int i = 0; i < _rightLastIndex; i++)
        {
            if (!Contains(_leftBuckets, _leftSlots, _rightSlots[i].hashCode, _rightSlots[i].value, out _))
                yield return _rightSlots[i].value;
        }
    }

    public IEnumerable<(T, T)> InBoth()
    {
        for (int i = 0; i < _leftLastIndex; i++)
        {
            if (Contains(_rightBuckets, _rightSlots, _leftSlots[i].hashCode, _leftSlots[i].value, out var rightItem))
                yield return (_leftSlots[i].value, rightItem);
        }
    }

    private bool Contains(int[] buckets, Slot[] slots, int hashCode, T value, out T otherValue)
    {
        var bucket = hashCode % buckets.Length;

        for (var i = buckets[bucket] - 1; i >= 0; i = slots[i].next)
        {
            ref var slot = ref slots[i];
            if (slot.hashCode == hashCode && _comparer.Equals(slot.value, value))
            {
                otherValue = slot.value;
                return true;
            }
        }

        otherValue = default!;
        return false;
    }

    internal struct Slot
    {
        internal int hashCode;      // Lower 31 bits of hash code
        internal int next;          // Index of next entry, -1 if last
        internal T value;
    }
}
