using System.Text.Json;

namespace Xero.DataComparer
{
    public class CoupleItem<T>
    {
        public CoupleItem(T referenceItem, T targetItem)
        {
            ReferenceItem = referenceItem;
            TargetItem = targetItem;
        }

        public T ReferenceItem { get; set; }

        public T TargetItem { get; set; }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
