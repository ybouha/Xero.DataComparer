namespace Xero.DataComparer
{
    using System.Collections.Generic;

    public class HashSetComparer<T>
    {
        private const int Lower31BitMask = 0x7FFFFFFF;

        //left 
        private int[] _leftBuckets;
        private Slot[] _leftSlots;
        private int _leftSize;

        private int _leftLastIndex;

        //right
        private int[] _rightBuckets;
        private Slot[] _rightSlots;
        private int _rightSize;

        private int _rightLastIndex;


        private readonly IEqualityComparer<T> _comparer;

        public HashSetComparer(int leftCount, int rightCount, IEqualityComparer<T> comparer)
        {
            _comparer = comparer;

            InitializeLeft(leftCount);
            InitializeRight(rightCount);
        }

        public bool AddLeftItem(T value) =>
            AddIfNotPresent(_leftSize, ref _leftLastIndex, _leftBuckets, _leftSlots, value);

        public bool AddRightItem(T value) =>
            AddIfNotPresent(_rightSize, ref _rightLastIndex, _rightBuckets, _rightSlots, value);

        private int InternalGetHashCode(T item)
        {
            if (item == null) return 0;
            return _comparer.GetHashCode(item) & Lower31BitMask;
        }


        private bool AddIfNotPresent(int size, ref int lastIndex, int[] buckets, Slot[] slots, T value)
        {
            var hashCode = InternalGetHashCode(value);
            var bucket = hashCode % size;

            for (var i = buckets[bucket] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && _comparer.Equals(slots[i].value, value))
                    return false;
            }

            int index = lastIndex;
            lastIndex++;

            slots[index].hashCode = hashCode;
            slots[index].value = value;
            slots[index].next = buckets[bucket] - 1;
            buckets[bucket] = index + 1;

            return true;
        }

        private void InitializeLeft(int capacity)
        {
            _leftSize = HashHelpers.GetPrime(capacity);
            _leftBuckets = new int[_leftSize];
            _leftSlots = new Slot[_leftSize];
            _leftLastIndex = 0;
        }

        private void InitializeRight(int capacity)
        {
            _rightSize = HashHelpers.GetPrime(capacity);
            _rightBuckets = new int[_rightSize];
            _rightSlots = new Slot[_rightSize];
            _rightLastIndex = 0;
        }


        public IEnumerable<T> OnlyInLeft()
        {
            for (int i = 0; i < _leftLastIndex; i++)
            {
                if (!Contains(_rightBuckets, _rightSlots, _leftSlots[i].value, out _))
                    yield return _leftSlots[i].value;
            }
        }

        public IEnumerable<T> OnlyInRight()
        {
            for (int i = 0; i < _rightLastIndex; i++)
            {
                if (!Contains(_leftBuckets, _leftSlots, _rightSlots[i].value, out _))
                    yield return _rightSlots[i].value;
            }
        }

        public IEnumerable<(T, T)> InBoth()
        {
            for (int i = 0; i < _leftLastIndex; i++)
            {
                if (Contains(_rightBuckets, _rightSlots, _leftSlots[i].value, out var rightItem))
                    yield return (_leftSlots[i].value, rightItem);
            }
        }


        private bool Contains(int[] buckets, Slot[] slots, T value, out T otherValue)
        {
            var hashCode = InternalGetHashCode(value);
            var bucket = hashCode % buckets.Length;

            for (var i = buckets[bucket] - 1; i >= 0; i = slots[i].next)
            {
                var slot = slots[i];
                if (slot.hashCode == hashCode)
                {
                    if (_comparer.Equals(slot.value, value))
                    {
                        otherValue = slot.value;
                        return true;
                    }
                }
            }

            otherValue = default;
            return false;
        }

        internal struct Slot
        {
            internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal int next;          // Index of next entry, -1 if last
            internal T value;
        }
    }

}
