// Feature: tower-defense-vn
// Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 5.6.
// Properties supported by this module:
//   - Property 12: Nâng cấp Giáp tăng Máu_Tối_Đa bảo toàn ràng buộc
//                  (delta phơi qua BuyOutcome.MaxHpDelta khi mua nhánh Armor).
//   - Property 13: Số học mua nâng cấp và confluence (TryBuy là phép cộng/trừ giao hoán
//                  trên các trường độc lập).
//   - Property 14: Cooldown gating Special — cooldown logic được xử lý ở module Special
//                  riêng tại task 5.3; ở đây chỉ tăng SpecialLevel khi mua thành công.

using System;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Hệ thống mua nâng cấp ba nhánh: Giáp, Công, Special (Requirement 6.1).
    /// Lớp này thuần C#, không phụ thuộc Unity, để có thể test bằng Property-Based Testing
    /// (FsCheck) trên CI mà không cần Editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Thiết kế bám theo design.md mục "Core - Upgrade System": <see cref="ArmorLevel"/>,
    /// <see cref="AttackLevel"/>, <see cref="SpecialLevel"/>, <see cref="Gold"/> đều khởi tạo 0
    /// (hàm dựng có thể nhận <paramref name="initialGold"/> tùy chọn).
    /// </para>
    /// <para>
    /// <b>Property 13 (confluence)</b>: Vì <see cref="TryBuy"/> chỉ thực hiện trừ vàng và tăng
    /// một biến cấp tương ứng — đều là phép cộng/trừ giao hoán trên các trường độc lập —
    /// hai hoán vị bất kỳ của cùng một tập nâng cấp có thể chi trả sẽ cho ra cùng trạng thái
    /// cuối <c>(ArmorLevel, AttackLevel, SpecialLevel, Gold)</c>. Không cần code thêm để bảo đảm
    /// tính chất này; nó suy ra trực tiếp từ tính chất số học của các phép gán.
    /// </para>
    /// <para>
    /// <b>Tích hợp Máu_Tối_Đa (Property 12, Requirement 5.6)</b>: việc cộng <c>Δ</c> vào cặp
    /// <c>(CurrentHp, MaxHp)</c> được pipeline HP/damage thực hiện ở task 5.4. Lớp này phơi
    /// <c>Δ</c> thông qua <see cref="BuyOutcome.MaxHpDelta"/>: khi mua nhánh
    /// <see cref="UpgradeTrack.Armor"/> thành công, delta bằng <c>costs.ArmorStep</c>; ngược lại
    /// (Attack/Special hoặc <see cref="UpgradeOutcome.NotEnoughGold"/>) bằng 0.
    /// </para>
    /// </remarks>
    public sealed class UpgradeSystem
    {
        /// <summary>Cấp_Nâng_Cấp_Giáp hiện tại (≥ 0). Khởi tạo 0.</summary>
        public int ArmorLevel { get; private set; }

        /// <summary>Cấp_Nâng_Cấp_Công hiện tại (≥ 0). Khởi tạo 0.</summary>
        public int AttackLevel { get; private set; }

        /// <summary>Cấp_Nâng_Cấp_Special hiện tại (≥ 0). Khởi tạo 0.</summary>
        public int SpecialLevel { get; private set; }

        /// <summary>Vàng hiện có. Khởi tạo theo <c>initialGold</c> hoặc 0; Yêu cầu 6 đảm bảo luôn <c>≥ 0</c> sau mỗi <see cref="TryBuy"/>.</summary>
        public int Gold { get; private set; }

        /// <summary>
        /// Tạo một hệ thống nâng cấp mới với số vàng ban đầu tùy chọn.
        /// </summary>
        /// <param name="initialGold">Vàng khởi đầu, kỳ vọng <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="initialGold"/> &lt; 0.</exception>
        public UpgradeSystem(int initialGold = 0)
        {
            if (initialGold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialGold), initialGold, "initialGold phải ≥ 0.");
            }

            Gold = initialGold;
        }

        /// <summary>
        /// Cộng vàng vào kho do Quái rớt ra hoặc do thưởng đợt.
        /// </summary>
        /// <param name="amount">Lượng vàng cộng thêm; kỳ vọng <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="amount"/> &lt; 0.</exception>
        public void AddGold(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "amount phải ≥ 0.");
            }

            // Phòng ngừa overflow: nếu vượt int.MaxValue thì kẹp lại.
            long sum = (long)Gold + amount;
            Gold = sum > int.MaxValue ? int.MaxValue : (int)sum;
        }

        /// <summary>
        /// Cố gắng trừ <paramref name="amount"/> Vàng khỏi ví. Đây là cổng chi tiêu chung cho
        /// các hệ ngoài <see cref="TryBuy"/> (vd <see cref="SpecialSkillSystem.TryBuyUpgrade"/>):
        /// chỉ trừ khi đủ Vàng, đảm bảo bất biến <c>Gold ≥ 0</c> (Requirement 6.3).
        /// </summary>
        /// <param name="amount">Lượng Vàng cần trừ; kỳ vọng <c>≥ 0</c>.</param>
        /// <returns>
        /// <c>true</c> nếu đủ Vàng và đã trừ; <c>false</c> nếu thiếu Vàng (không thay đổi trạng thái).
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="amount"/> &lt; 0.</exception>
        public bool TrySpend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "amount phải ≥ 0.");
            }

            if (Gold < amount)
            {
                return false;
            }

            Gold -= amount;
            return true;
        }

        /// <summary>
        /// Cố gắng mua một bậc của <paramref name="track"/> bằng vàng hiện có.
        /// </summary>
        /// <param name="track">Nhánh nâng cấp cần mua.</param>
        /// <param name="costs">Bảng giá (không được null).</param>
        /// <returns>
        /// <see cref="BuyOutcome"/> mô tả kết quả:
        /// <list type="bullet">
        ///   <item>
        ///     <see cref="UpgradeOutcome.Bought"/> nếu <c>Gold ≥ cost</c>: trừ vàng, tăng cấp,
        ///     <c>CostPaid = cost</c>, <c>NewLevel = currentLevel + 1</c>,
        ///     <c>MaxHpDelta = costs.ArmorStep</c> nếu <c>track == Armor</c>, ngược lại 0
        ///     (Requirement 5.6).
        ///   </item>
        ///   <item>
        ///     <see cref="UpgradeOutcome.NotEnoughGold"/> nếu thiếu vàng: <b>không thay đổi
        ///     trạng thái</b> (Requirement 6.3); <c>CostPaid = 0</c>, <c>NewLevel = currentLevel</c>,
        ///     <c>MaxHpDelta = 0</c>.
        ///   </item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">Khi <paramref name="costs"/> null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <paramref name="track"/> không phải giá trị enum hợp lệ, hoặc khi
        /// <c>costs.CostFor(track, currentLevel)</c> trả về giá trị âm (vi phạm hợp đồng <see cref="IUpgradeCostTable"/>).
        /// </exception>
        public BuyOutcome TryBuy(UpgradeTrack track, IUpgradeCostTable costs)
        {
            if (costs is null) throw new ArgumentNullException(nameof(costs));

            int currentLevel = GetLevel(track);
            int cost = costs.CostFor(track, currentLevel);

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(costs), cost, "IUpgradeCostTable.CostFor phải trả giá trị ≥ 0.");
            }

            // Requirement 6.3: thiếu vàng → giữ nguyên trạng thái, trả NotEnoughGold.
            if (Gold < cost)
            {
                return new BuyOutcome(
                    Outcome: UpgradeOutcome.NotEnoughGold,
                    CostPaid: 0,
                    NewLevel: currentLevel,
                    MaxHpDelta: 0f);
            }

            // Requirement 6.2: trừ vàng và tăng cấp nhánh. Phép gán độc lập trên các trường
            // → giao hoán theo Property 13.
            Gold -= cost;
            int newLevel = currentLevel + 1;
            SetLevel(track, newLevel);

            // Requirement 5.6: chỉ Armor mới phát sinh Δ Máu_Tối_Đa.
            float maxHpDelta = track == UpgradeTrack.Armor ? costs.ArmorStep : 0f;

            return new BuyOutcome(
                Outcome: UpgradeOutcome.Bought,
                CostPaid: cost,
                NewLevel: newLevel,
                MaxHpDelta: maxHpDelta);
        }

        /// <summary>
        /// Giáp hiện tại (Requirement 6.4): <c>BaseArmor + ArmorLevel × ArmorStep</c>.
        /// </summary>
        public float CurrentArmor(IUpgradeCostTable costs)
        {
            if (costs is null) throw new ArgumentNullException(nameof(costs));
            return costs.BaseArmor + ArmorLevel * costs.ArmorStep;
        }

        /// <summary>
        /// Hệ_số_Công hiện tại (Requirement 6.5): <c>1 + AttackLevel × AttackStep</c>.
        /// </summary>
        public float CurrentAttackMultiplier(IUpgradeCostTable costs)
        {
            if (costs is null) throw new ArgumentNullException(nameof(costs));
            return 1f + AttackLevel * costs.AttackStep;
        }

        /// <summary>Lấy cấp hiện tại của một nhánh.</summary>
        public int GetLevel(UpgradeTrack track) => track switch
        {
            UpgradeTrack.Armor => ArmorLevel,
            UpgradeTrack.Attack => AttackLevel,
            UpgradeTrack.Special => SpecialLevel,
            _ => throw new ArgumentOutOfRangeException(
                nameof(track), track, "UpgradeTrack không hợp lệ."),
        };

        private void SetLevel(UpgradeTrack track, int newLevel)
        {
            switch (track)
            {
                case UpgradeTrack.Armor:
                    ArmorLevel = newLevel;
                    break;
                case UpgradeTrack.Attack:
                    AttackLevel = newLevel;
                    break;
                case UpgradeTrack.Special:
                    SpecialLevel = newLevel;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(track), track, "UpgradeTrack không hợp lệ.");
            }
        }
    }
}
