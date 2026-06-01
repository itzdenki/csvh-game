// Feature: tower-defense-vn — RNG seam để giữ logic skill Special deterministic, test được.
// Validates: Requirements 6.6 (hiệu ứng %), hỗ trợ Property-Based Testing.

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Trừu tượng nguồn số ngẫu nhiên để tách logic xác suất của skill Special khỏi
    /// <c>System.Random</c> hay <c>UnityEngine.Random</c>. Nhờ vậy Core vẫn thuần C#,
    /// không phụ thuộc Unity, và test có thể tiêm một nguồn deterministic
    /// (<c>FakeRandom</c>) để tái lập kết quả.
    /// </summary>
    public interface IRandom
    {
        /// <summary>
        /// Trả một số thực trong khoảng nửa mở <c>[0.0, 1.0)</c>, dùng để roll xác suất
        /// hiệu ứng phụ (số chỗ nổ / số nhát chém / thời gian choáng).
        /// </summary>
        double NextDouble();
    }
}
