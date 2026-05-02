namespace Xero.DataComparer.Core;

public class DelegatedEqualityComparer<T> : System.Collections.Generic.EqualityComparer<T>
{
    private readonly Func<T, int> _getHashCodeFct;
    private readonly Func<T, T, bool> _equalsFct;

    public DelegatedEqualityComparer(Func<T, int> getHashCodeFct, Func<T, T, bool> equalsFct)
    {
        _getHashCodeFct = getHashCodeFct;
        _equalsFct = equalsFct;
    }


    public override int GetHashCode(T x)
    {
        return _getHashCodeFct(x);
    }

    public override bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;

        if (x.GetType() != y.GetType()) return false;

        return _equalsFct(x, y);
    }
}