// Feature: tower-defense-vn
// Validates: Requirements 6.2, 6.3, 5.6 (kết quả của một lần mua nâng cấp).

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Kết quả enum đơn giản của một lần thử mua nâng cấp.
    /// Theo nguyên tắc Core không ném exception cho đường dẫn bình thường, ta trả về
    /// một enum result thay vì throw.
    /// </summary>
    public enum UpgradeOutcome
    {
        /// <summary>Vàng đủ; trừ vàng và tăng cấp nhánh thành công (Requirement 6.2).</summary>
        Bought = 0,

        /// <summary>Vàng không đủ; trạng thái không đổi (Requirement 6.3).</summary>
        NotEnoughGold = 1,
    }

    /// <summary>
    /// Kết quả chi tiết của <see cref="UpgradeSystem.TryBuy"/>.
    /// Cho phép GameSession ở task 5.4 áp <see cref="MaxHpDelta"/> vào cặp
    /// <c>(CurrentHp, MaxHp)</c> bảo toàn ràng buộc <c>0 ≤ CurrentHp ≤ MaxHp</c>
    /// (Property 12, Requirement 5.6).
    /// </summary>
    /// <param name="Outcome">Kết quả enum (<see cref="UpgradeOutcome.Bought"/> hoặc <see cref="UpgradeOutcome.NotEnoughGold"/>).</param>
    /// <param name="CostPaid">Số vàng đã trừ. <c>0</c> khi <see cref="UpgradeOutcome.NotEnoughGold"/>.</param>
    /// <param name="NewLevel">Cấp mới của nhánh sau giao dịch. Bằng cấp cũ khi <see cref="UpgradeOutcome.NotEnoughGold"/>.</param>
    /// <param name="MaxHpDelta">
    /// Lượng <c>Δ</c> nên cộng vào <c>(CurrentHp, MaxHp)</c> sau khi mua. Khác 0 chỉ khi mua
    /// nhánh <see cref="UpgradeTrack.Armor"/> thành công; bằng <c>ArmorStep</c> theo Requirement 5.6.
    /// </param>
    public readonly record struct BuyOutcome(
        UpgradeOutcome Outcome,
        int CostPaid,
        int NewLevel,
        float MaxHpDelta);
}
