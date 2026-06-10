// Feature: tower-defense-vn — Vòng lặp Nâng cấp (Upgrade Loop), tầng META "Xu cổ".
// Validates: GDD Cơ chế 2 — Meta Upgrade (nâng cấp vĩnh viễn ngoài trận).
//
// Bất biến (mirror phong cách UpgradeSystem/ScoreTracker, kiểm bằng PBT):
//   - Coins ≥ 0 và CHỈ giảm qua TryBuy thành công (AddCoins đơn điệu tăng, kẹp long.MaxValue).
//   - Mỗi Level ∈ [0, table.MaxLevelFor(track)] và không bao giờ giảm.
//   - TryBuy thiếu Xu / chạm trần cấp ⇒ KHÔNG đổi trạng thái (CostPaid = 0).
//   - Bonuses đơn điệu theo từng cấp: cấp cao hơn ⇒ GateHp/Crossbow cao hơn (hoặc bằng),
//     CooldownScale thấp hơn (hoặc bằng), kẹp sàn 1 − MaxCooldownReduction.

using System;
using CSVH.Core.Storage;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Trạng thái runtime của tiến trình META (Xu cổ): số dư Xu cổ + cấp ba nhánh nâng cấp
    /// vĩnh viễn (<see cref="MetaUpgradeTrack"/>). Thuần C#, không phụ thuộc Unity, để test
    /// bằng Property-Based Testing trên CI.
    ///
    /// <para>
    /// Vòng đời: composition root nạp một <see cref="MetaProgressSnapshot"/> từ Bộ_Lưu_Trữ →
    /// dựng instance này → áp <see cref="Bonuses"/> vào trận mới → trong/giữa trận cộng
    /// Xu cổ kiếm được (<see cref="AddCoins"/>) và mua nâng cấp (<see cref="TryBuy"/>) →
    /// ghi <see cref="ToSnapshot"/> trở lại Bộ_Lưu_Trữ.
    /// </para>
    /// </summary>
    public sealed class MetaProgressionState
    {
        private readonly IMetaUpgradeTable _table;

        /// <summary>Số dư Xu cổ hiện có (≥ 0).</summary>
        public long Coins { get; private set; }

        /// <summary>Cấp nhánh Máu Cổng đã mua (≥ 0).</summary>
        public int GateHpLevel { get; private set; }

        /// <summary>Cấp nhánh Sát thương Nỏ đã mua (≥ 0).</summary>
        public int CrossbowDamageLevel { get; private set; }

        /// <summary>Cấp nhánh Giảm hồi chiêu Ultimate đã mua (≥ 0).</summary>
        public int UltimateCooldownLevel { get; private set; }

        /// <summary>
        /// Dựng trạng thái từ <paramref name="snapshot"/> đã lưu và bảng tham số
        /// <paramref name="table"/>. Mọi trường của snapshot được kẹp về <c>≥ 0</c> và
        /// không vượt <see cref="IMetaUpgradeTable.MaxLevelFor"/> để chống dữ liệu hỏng.
        /// </summary>
        /// <exception cref="ArgumentNullException">Khi <paramref name="snapshot"/> hoặc <paramref name="table"/> null.</exception>
        public MetaProgressionState(MetaProgressSnapshot snapshot, IMetaUpgradeTable table)
        {
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
            _table = table ?? throw new ArgumentNullException(nameof(table));

            Coins = snapshot.Coins < 0L ? 0L : snapshot.Coins;
            GateHpLevel = ClampLevel(MetaUpgradeTrack.GateHp, snapshot.GateHpLevel);
            CrossbowDamageLevel = ClampLevel(MetaUpgradeTrack.CrossbowDamage, snapshot.CrossbowDamageLevel);
            UltimateCooldownLevel = ClampLevel(MetaUpgradeTrack.UltimateCooldown, snapshot.UltimateCooldownLevel);
        }

        /// <summary>Hiệu ứng vĩnh viễn (đã quy ra số) để áp vào một trận mới.</summary>
        public MetaBonuses Bonuses
        {
            get
            {
                int gateHp = (int)Math.Min(int.MaxValue, (long)GateHpLevel * _table.GateHpPerLevel);
                float crossbow = CrossbowDamageLevel * _table.CrossbowDamagePerLevel;

                // Hệ số hồi chiêu = 1 − tổng%giảm, kẹp sàn (1 − MaxCooldownReduction) để luôn > 0.
                float reduction = UltimateCooldownLevel * _table.CooldownReductionPerLevel;
                float maxReduction = Math.Min(0.99f, Math.Max(0f, _table.MaxCooldownReduction));
                if (reduction > maxReduction) reduction = maxReduction;
                float scale = 1f - reduction;

                return new MetaBonuses(gateHp, crossbow, scale);
            }
        }

        /// <summary>Cấp hiện tại của một nhánh.</summary>
        public int GetLevel(MetaUpgradeTrack track) => track switch
        {
            MetaUpgradeTrack.GateHp => GateHpLevel,
            MetaUpgradeTrack.CrossbowDamage => CrossbowDamageLevel,
            MetaUpgradeTrack.UltimateCooldown => UltimateCooldownLevel,
            _ => throw new ArgumentOutOfRangeException(nameof(track), track, "MetaUpgradeTrack không hợp lệ."),
        };

        /// <summary>
        /// Cộng Xu cổ kiếm được (Quái rớt ra / thưởng cuối trận). Kẹp tại
        /// <see cref="long.MaxValue"/> để đơn điệu kể cả với chuỗi sự kiện cực đoan.
        /// </summary>
        /// <param name="amount">Lượng Xu cổ cộng thêm; phải <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="amount"/> &lt; 0.</exception>
        public void AddCoins(long amount)
        {
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "amount phải ≥ 0.");
            }

            Coins = amount > long.MaxValue - Coins ? long.MaxValue : Coins + amount;
        }

        /// <summary>
        /// Cố mua một bậc của <paramref name="track"/> bằng Xu cổ hiện có.
        /// <list type="bullet">
        ///   <item><see cref="MetaUpgradeOutcome.MaxLevelReached"/> nếu nhánh đã đạt cấp tối đa — không đổi trạng thái.</item>
        ///   <item><see cref="MetaUpgradeOutcome.NotEnoughCoins"/> nếu thiếu Xu cổ — không đổi trạng thái.</item>
        ///   <item><see cref="MetaUpgradeOutcome.Bought"/> nếu đủ: trừ Xu cổ, tăng cấp nhánh.</item>
        /// </list>
        /// </summary>
        public MetaBuyOutcome TryBuy(MetaUpgradeTrack track)
        {
            int currentLevel = GetLevel(track);
            int maxLevel = Math.Max(0, _table.MaxLevelFor(track));

            if (currentLevel >= maxLevel)
            {
                return new MetaBuyOutcome(MetaUpgradeOutcome.MaxLevelReached, 0, currentLevel);
            }

            int cost = _table.CostFor(track, currentLevel);
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(track), cost, "IMetaUpgradeTable.CostFor phải trả giá trị ≥ 0.");
            }

            if (Coins < cost)
            {
                return new MetaBuyOutcome(MetaUpgradeOutcome.NotEnoughCoins, 0, currentLevel);
            }

            Coins -= cost;
            int newLevel = currentLevel + 1;
            SetLevel(track, newLevel);
            return new MetaBuyOutcome(MetaUpgradeOutcome.Bought, cost, newLevel);
        }

        /// <summary>Giá Xu cổ để mua bậc kế tiếp của <paramref name="track"/> (theo cấp hiện tại).</summary>
        public int CostFor(MetaUpgradeTrack track) => _table.CostFor(track, GetLevel(track));

        /// <summary><c>true</c> nếu <paramref name="track"/> chưa đạt cấp tối đa (còn mua được).</summary>
        public bool CanUpgrade(MetaUpgradeTrack track) => GetLevel(track) < Math.Max(0, _table.MaxLevelFor(track));

        /// <summary>Chụp trạng thái hiện tại để ghi xuống Bộ_Lưu_Trữ.</summary>
        public MetaProgressSnapshot ToSnapshot() =>
            new MetaProgressSnapshot(Coins, GateHpLevel, CrossbowDamageLevel, UltimateCooldownLevel);

        private int ClampLevel(MetaUpgradeTrack track, int level)
        {
            if (level < 0) return 0;
            int max = Math.Max(0, _table.MaxLevelFor(track));
            return level > max ? max : level;
        }

        private void SetLevel(MetaUpgradeTrack track, int newLevel)
        {
            switch (track)
            {
                case MetaUpgradeTrack.GateHp: GateHpLevel = newLevel; break;
                case MetaUpgradeTrack.CrossbowDamage: CrossbowDamageLevel = newLevel; break;
                case MetaUpgradeTrack.UltimateCooldown: UltimateCooldownLevel = newLevel; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(track), track, "MetaUpgradeTrack không hợp lệ.");
            }
        }
    }
}
