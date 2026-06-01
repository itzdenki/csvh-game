// Feature: tower-defense-vn
// Validates: Requirements 10.2, 10.5, 10.6 (value equality cho round-trip Load/Save cấu hình).
// Helper nội bộ cho structural equality/hashing trên IReadOnlyList<T> dùng bởi
// WaveConfig và ConfigBundle.

using System.Collections.Generic;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Tiện ích nội bộ so sánh và băm danh sách bất biến theo nội dung.
    /// Records mặc định so sánh <see cref="IReadOnlyList{T}"/> theo tham chiếu, không
    /// đáp ứng yêu cầu round-trip (Requirement 10.5/10.6) - các record cấu hình sẽ
    /// override <c>Equals</c>/<c>GetHashCode</c> qua các helper này.
    /// </summary>
    internal static class ConfigEqualityHelpers
    {
        /// <summary>
        /// So sánh nội dung hai danh sách: cùng <c>null</c>, hoặc cùng độ dài và
        /// các phần tử bằng nhau theo <see cref="EqualityComparer{T}.Default"/>.
        /// </summary>
        public static bool ListEquals<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;

            var cmp = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!cmp.Equals(a[i], b[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Băm danh sách theo nội dung (kết hợp tuần tự). Trả 0 cho <c>null</c>;
        /// cùng nội dung sinh cùng hash, đảm bảo hợp đồng với <see cref="ListEquals{T}"/>.
        /// </summary>
        public static int ListHashCode<T>(IReadOnlyList<T> list)
        {
            if (list is null) return 0;

            // FNV-1a-style fold để tránh phụ thuộc System.HashCode khi list quá dài.
            unchecked
            {
                int hash = 17;
                var cmp = EqualityComparer<T>.Default;
                for (int i = 0; i < list.Count; i++)
                {
                    int item = list[i] is null ? 0 : cmp.GetHashCode(list[i]);
                    hash = hash * 31 + item;
                }
                return hash;
            }
        }
    }
}
