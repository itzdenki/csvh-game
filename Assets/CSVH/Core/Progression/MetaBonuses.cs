// Feature: tower-defense-vn — Vòng lặp Nâng cấp (Upgrade Loop), tầng META "Xu cổ".
// Gói các hiệu ứng vĩnh viễn (đã quy ra số) mà các nâng cấp Xu cổ áp lên một trận mới.

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Tổng hợp hiệu ứng của các nâng cấp META (Xu cổ) áp vào lúc BẮT ĐẦU một trận
    /// (GDD Cơ chế 2 — Meta Upgrade). Là <c>readonly record struct</c> bất biến để
    /// composition root đọc một lần rồi phân phối tới các hệ:
    /// <list type="bullet">
    ///   <item><see cref="GateHpBonus"/> cộng vào Máu_Tối_Đa khởi đầu của Thành.</item>
    ///   <item><see cref="CrossbowDamageBonus"/> cộng vào Sát_Thương_Cơ_Bản của Đạn (Nỏ).</item>
    ///   <item><see cref="CooldownScale"/> nhân vào Thời_Gian_Hồi của skill Ultimate
    ///   (≤ 1.0 ⇒ giảm hồi chiêu; sàn <c>MinCooldown</c> vẫn được tôn trọng ở
    ///   <see cref="SpecialSkillState"/>).</item>
    /// </list>
    /// </summary>
    /// <param name="GateHpBonus">Lượng Máu_Tối_Đa cộng thêm (≥ 0).</param>
    /// <param name="CrossbowDamageBonus">Lượng Sát_Thương_Cơ_Bản cộng thêm cho Đạn (≥ 0).</param>
    /// <param name="CooldownScale">
    /// Hệ số nhân Thời_Gian_Hồi skill, trong <c>(0, 1]</c>. <c>1.0</c> = không đổi.
    /// </param>
    public readonly record struct MetaBonuses(
        int GateHpBonus,
        float CrossbowDamageBonus,
        float CooldownScale)
    {
        /// <summary>Không có hiệu ứng meta nào (dùng khi chưa nạp / không cấu hình bảng meta).</summary>
        public static MetaBonuses None => new MetaBonuses(0, 0f, 1f);
    }
}
