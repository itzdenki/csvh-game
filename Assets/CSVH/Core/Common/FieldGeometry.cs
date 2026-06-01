// Feature: tower-defense-vn
// Validates: Requirements 1.1, 1.3 (kích thước Sân_Đấu, vị trí Thành, bán kính va chạm Thành).

namespace CSVH.Core.Common
{
    /// <summary>
    /// Thông số hình học của Sân_Đấu: nửa-chiều-rộng và nửa-chiều-cao quanh gốc tọa độ,
    /// vị trí cố định của Thành (góc Đông Nam) và bán kính va chạm dùng cho phán đoán
    /// "Quái chạm Thành" (Requirement 2.3) cũng như cho cull Đạn ngoài biên (Requirement 3.4).
    /// Tham chiếu thiết kế: section "Core - Cấu hình và geometry" trong design.md.
    /// </summary>
    /// <param name="HalfWidth">Nửa chiều rộng Sân_Đấu (theo trục X). Kỳ vọng &gt; 0; ràng buộc được kiểm tại ConfigLoader.</param>
    /// <param name="HalfHeight">Nửa chiều cao Sân_Đấu (theo trục Y). Kỳ vọng &gt; 0; ràng buộc được kiểm tại ConfigLoader.</param>
    /// <param name="TowerPosition">Vị_Trí_Thành; kỳ vọng <see cref="FieldPoint.IsValidTowerPoint"/> trả <c>true</c> (Requirement 1.1).</param>
    /// <param name="TowerCollisionRadius">Bán kính va chạm của Thành; kỳ vọng &gt; 0.</param>
    public sealed record FieldGeometry(
        float HalfWidth,
        float HalfHeight,
        FieldPoint TowerPosition,
        float TowerCollisionRadius);
}
