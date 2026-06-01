// Feature: tower-defense-vn
// Polyfill cho System.Runtime.CompilerServices.IsExternalInit cần thiết để dùng init-setters
// trên .NET Standard 2.1 (Unity 6 default) trong assembly test. Core có polyfill internal
// nhưng nó không lan sang Tests; ta khai báo riêng tại đây.

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
