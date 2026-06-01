// Feature: tower-defense-vn — kết quả một lần kích hoạt skill Special.
// Validates: Requirements 6.6, 6.7.

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Mô tả bất biến kết quả một lần gọi <see cref="SpecialSkillState.TryActivate"/> /
    /// <see cref="SpecialSkillSystem.TryActivate"/>. View layer (GameSceneRoot) đọc record
    /// này để áp hiệu ứng lên các Quái trong <see cref="Radius"/>:
    /// <list type="bullet">
    ///   <item><b>Trống Đồng / Lưỡi Gươm</b>: lặp <see cref="HitCount"/> lần, mỗi lần trừ
    ///   <see cref="DamagePerHit"/> Máu cho mọi Quái trong vùng (nổ / chém nhiều nhát).</item>
    ///   <item><b>Mũi Tên</b>: trừ <see cref="DamagePerHit"/> một lần và áp choáng
    ///   <see cref="StunSeconds"/> giây.</item>
    /// </list>
    /// </summary>
    /// <param name="Kind">Skill đã kích hoạt.</param>
    /// <param name="Activated"><c>true</c> nếu kích hoạt thành công; <c>false</c> nếu đang hồi chiêu (no-op).</param>
    /// <param name="Radius">Bán_Kính ảnh hưởng (Euclid) tính từ Vị_Trí_Thành.</param>
    /// <param name="HitCount">Số lần áp sát thương (nổ/chém). ≥ 0. Với skill choáng thường = 1.</param>
    /// <param name="DamagePerHit">Sát thương mỗi lần áp lên một Quái. ≥ 0.</param>
    /// <param name="StunSeconds">Thời gian choáng áp cho Quái trúng (giây). 0 nếu skill không gây choáng.</param>
    public readonly record struct SpecialActivation(
        SpecialSkillKind Kind,
        bool Activated,
        float Radius,
        int HitCount,
        float DamagePerHit,
        float StunSeconds)
    {
        /// <summary>Kết quả "đang hồi chiêu" — không áp hiệu ứng nào.</summary>
        public static SpecialActivation NotReady(SpecialSkillKind kind) =>
            new(kind, false, 0f, 0, 0f, 0f);
    }
}
