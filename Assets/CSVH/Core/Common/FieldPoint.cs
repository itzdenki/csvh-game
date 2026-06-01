// Feature: tower-defense-vn
// Validates: Requirements 1.1, 1.3 (vị trí Thành và Cổng_Spawn).

namespace CSVH.Core.Common
{
    /// <summary>
    /// Tọa độ 2D bất biến trên Sân_Đấu (gốc giữa, X+ Đông, Y+ Bắc).
    /// Là <c>readonly record struct</c> nên có value equality và allocation-free.
    /// </summary>
    /// <param name="X">Hoành độ; X+ về phía Đông.</param>
    /// <param name="Y">Tung độ; Y+ về phía Bắc.</param>
    public readonly record struct FieldPoint(float X, float Y)
    {
        /// <summary>
        /// Cổng_Spawn hợp lệ nằm trên biên Bắc, biên Tây hoặc góc Tây Bắc:
        /// thỏa <c>X ≤ 0 ∨ Y ≥ 0</c> (Requirement 1.3, 2.1).
        /// </summary>
        public bool IsValidSpawnPoint() => X <= 0f || Y >= 0f;

        /// <summary>
        /// Vị_Trí_Thành hợp lệ nằm trong góc Đông Nam:
        /// thỏa <c>X &gt; 0 ∧ Y &lt; 0</c> (Requirement 1.1).
        /// </summary>
        public bool IsValidTowerPoint() => X > 0f && Y < 0f;
    }
}
