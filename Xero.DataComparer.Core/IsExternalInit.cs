// Polyfill: the C# compiler emits a reference to this type for `init`-only setters
// and positional records. It ships in net5.0+ but is missing on net48 / netstandard2.0,
// so we provide it ourselves there.
#if NET48 || NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
