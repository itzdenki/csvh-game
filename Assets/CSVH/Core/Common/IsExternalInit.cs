// Feature: tower-defense-vn
// Polyfill cho System.Runtime.CompilerServices.IsExternalInit cần thiết để dùng init-setters
// và record/readonly record struct trên .NET Standard 2.1 (Unity 6 default).
// Sử dụng ConditionalAttribute để chỉ xuất hiện khi compiler không có sẵn type.

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    /// <summary>
    /// Reserved type bắt buộc bởi compiler để hỗ trợ <c>init</c> setters.
    /// Chỉ là placeholder; không có member.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
