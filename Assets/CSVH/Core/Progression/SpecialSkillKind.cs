// Feature: tower-defense-vn — 3 skill Special riêng biệt (Trống Đồng / Mũi Tên / Lưỡi Gươm).
// Validates: Requirements 6.1, 6.6, 11.2.

using System;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Ba chiêu Special lấy cảm hứng văn hóa Việt Nam (Requirement 11.2). Mỗi giá trị là
    /// "nguồn sự thật" để map sang tên trong <c>CulturalCatalog</c>, sprite HUD và hiệu ứng
    /// gameplay tương ứng:
    /// <list type="bullet">
    ///   <item><see cref="TrongDong"/> — Trống Đồng Đông Sơn: nổ AoE nhiều chỗ quanh Thành.</item>
    ///   <item><see cref="MuiTen"/> — Mũi Tên An Dương Vương: gây sát thương kèm choáng (stun).</item>
    ///   <item><see cref="LuoiGuom"/> — Lưỡi Gươm Lê Lợi: chém nhiều nhát trong vùng.</item>
    /// </list>
    /// </summary>
    public enum SpecialSkillKind
    {
        TrongDong = 0,
        MuiTen = 1,
        LuoiGuom = 2,
    }

    /// <summary>
    /// Helper map <see cref="SpecialSkillKind"/> ↔ định danh tên trong <c>CulturalCatalog</c>
    /// (khớp với <c>CulturalCatalog.asset</c> đã cấu hình sẵn).
    /// </summary>
    public static class SpecialSkillKinds
    {
        /// <summary>Tất cả skill theo thứ tự hiển thị cố định trên HUD.</summary>
        public static readonly SpecialSkillKind[] All =
        {
            SpecialSkillKind.TrongDong,
            SpecialSkillKind.MuiTen,
            SpecialSkillKind.LuoiGuom,
        };

        /// <summary>
        /// Định danh tên skill khớp <c>CulturalCatalog.SpecialNames</c>
        /// (vd <c>"Trống_Đồng_Đông_Sơn"</c>).
        /// </summary>
        public static string CatalogName(this SpecialSkillKind kind) => kind switch
        {
            SpecialSkillKind.TrongDong => "Trống_Đồng_Đông_Sơn",
            SpecialSkillKind.MuiTen => "Mũi_Tên_An_Dương_Vương",
            SpecialSkillKind.LuoiGuom => "Lưỡi_Gươm_Lê_Lợi",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "SpecialSkillKind không hợp lệ."),
        };

        /// <summary>Tên hiển thị tiếng Việt (có dấu cách) cho UI.</summary>
        public static string DisplayName(this SpecialSkillKind kind) => kind switch
        {
            SpecialSkillKind.TrongDong => "Trống Đồng Đông Sơn",
            SpecialSkillKind.MuiTen => "Mũi Tên An Dương Vương",
            SpecialSkillKind.LuoiGuom => "Lưỡi Gươm Lê Lợi",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "SpecialSkillKind không hợp lệ."),
        };
    }
}
