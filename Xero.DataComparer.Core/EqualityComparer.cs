namespace Xero.DataComparer
{
    public class EqualityComparerEx<T> : IEqualityComparer<T>
    {
        private readonly List<Func<T, object>> _keyGetters;

        public EqualityComparerEx(List<Func<T, object>> keyGetters)
        {
            _keyGetters = keyGetters;
        }

        public bool Equals(T? x, T? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            for (int i = 0; i < _keyGetters.Count; i++)
            {
                var val1 = _keyGetters[i](x);
                var val2 = _keyGetters[i](y);
                if (!Equals(val1, val2)) return false;
            }
            return true;
        }

        public int GetHashCode(T obj)
        {
            if (obj == null) return 0;
            unchecked
            {
                int hashCode = 17;
                for (int i = 0; i < _keyGetters.Count; i++)
                {
                    var val = _keyGetters[i](obj);
                    hashCode = hashCode * 31 + (val?.GetHashCode() ?? 0);
                }
                return hashCode;
            }
        }

    }

}
