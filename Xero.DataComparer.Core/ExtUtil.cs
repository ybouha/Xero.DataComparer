namespace Xero.DataComparer
{
    public static  class ExtUtil
    {
        public static IEnumerable<TSource> ExceptEx<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            IEqualityComparer<TSource> comparer)
        {
            HashSet<TSource> bannedElements = new HashSet<TSource>(second, comparer);
            foreach (TSource item in first)
            {
                if (bannedElements.Add(item))
                {
                    yield return item;
                }
            }
        }
    }
}
