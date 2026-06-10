// Feature: in-match-upgrades — hệ 9 nâng cấp TRONG TRẬN (reset mỗi trận).
//
// Bất biến:
//   - Mọi cấp ≥ 0 và chỉ tăng qua TryBuy thành công (mỗi lần +1).
//   - TryBuy thiếu Vàng → trạng thái không đổi (ví Vàng giữ nguyên, cấp giữ nguyên).
//   - Các hệ số suy ra (multiplier, %) là hàm thuần của (cấp, bảng) — không trạng thái ẩn.

using System;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Kết quả chi tiết của một lần <see cref="MatchUpgradeSystem.TryBuy"/>.
    /// </summary>
    /// <param name="Outcome"><see cref="UpgradeOutcome.Bought"/> hoặc <see cref="UpgradeOutcome.NotEnoughGold"/>.</param>
    /// <param name="CostPaid">Vàng đã trừ; 0 khi thiếu Vàng.</param>
    /// <param name="NewLevel">Cấp mới của nâng cấp; bằng cấp cũ khi thiếu Vàng.</param>
    public readonly record struct MatchBuyOutcome(
        UpgradeOutcome Outcome,
        int CostPaid,
        int NewLevel);

    /// <summary>
    /// Hệ thống 9 nâng cấp trong trận (<see cref="MatchUpgradeKind"/>) — thuần C#, không
    /// phụ thuộc Unity. Tiền tệ là Vàng trong trận, dùng chung ví với
    /// <see cref="UpgradeSystem"/> (chi qua <see cref="UpgradeSystem.TrySpend"/>).
    /// Cấp không có trần (mốc 6-inf): giá trị tăng tuyến tính theo bảng
    /// <see cref="IMatchUpgradeTable"/>, riêng Chí Mạng / Nỏ Băng bị kẹp trần.
    /// </summary>
    public sealed class MatchUpgradeSystem
    {
        // Số lượng kind — giữ đồng bộ với enum MatchUpgradeKind.
        private const int KindCount = 10;

        private readonly IMatchUpgradeTable _table;
        private readonly int[] _levels = new int[KindCount];

        /// <exception cref="ArgumentNullException">Khi <paramref name="table"/> là <c>null</c>.</exception>
        public MatchUpgradeSystem(IMatchUpgradeTable table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
        }

        /// <summary>Cấp hiện tại của một nâng cấp (≥ 0; 0 = chưa nâng).</summary>
        public int GetLevel(MatchUpgradeKind kind) => _levels[IndexOf(kind)];

        /// <summary>Giá Vàng cho cấp kế tiếp của <paramref name="kind"/>.</summary>
        public int CostFor(MatchUpgradeKind kind) => _table.CostFor(kind, GetLevel(kind));

        /// <summary>
        /// Cố gắng mua một cấp của <paramref name="kind"/>, trừ Vàng từ ví
        /// <paramref name="wallet"/> (Requirement 6.3: thiếu Vàng → không đổi trạng thái).
        /// </summary>
        /// <exception cref="ArgumentNullException">Khi <paramref name="wallet"/> là <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi bảng giá trả giá trị âm (vi phạm hợp đồng <see cref="IMatchUpgradeTable"/>).
        /// </exception>
        public MatchBuyOutcome TryBuy(MatchUpgradeKind kind, UpgradeSystem wallet)
        {
            if (wallet is null) throw new ArgumentNullException(nameof(wallet));

            int idx = IndexOf(kind);
            int currentLevel = _levels[idx];

            // Nâng cấp có trần (ví dụ Làn Đạn) đã max → không trừ Vàng, không đổi trạng thái.
            if (IsMaxed(kind))
            {
                return new MatchBuyOutcome(UpgradeOutcome.Maxed, 0, currentLevel);
            }
            int cost = _table.CostFor(kind, currentLevel);
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind), cost, "IMatchUpgradeTable.CostFor phải trả giá trị ≥ 0.");
            }

            if (!wallet.TrySpend(cost))
            {
                return new MatchBuyOutcome(UpgradeOutcome.NotEnoughGold, 0, currentLevel);
            }

            _levels[idx] = currentLevel + 1;
            return new MatchBuyOutcome(UpgradeOutcome.Bought, cost, currentLevel + 1);
        }

        // ==== Hệ số suy ra (hàm thuần của cấp + bảng) ====================================

        /// <summary>Hệ số sát thương: <c>1 + cấp × DamagePerLevel</c> (Lv1 = ×1.05).</summary>
        public float DamageMultiplier
            => 1f + GetLevel(MatchUpgradeKind.Damage) * _table.DamagePerLevel;

        /// <summary>Hệ số Fire Rate: <c>1 + cấp × AttackSpeedPerLevel</c> — tăng nó làm giảm cooldown giữa hai phát bắn.</summary>
        public float FireRateMultiplier
            => 1f + GetLevel(MatchUpgradeKind.AttackSpeed) * _table.AttackSpeedPerLevel;

        /// <summary>
        /// <c>true</c> khi <paramref name="kind"/> có trần cấp và đã đạt trần
        /// (xem <see cref="IMatchUpgradeTable.MaxLevelFor"/>).
        /// </summary>
        public bool IsMaxed(MatchUpgradeKind kind)
        {
            int max = _table.MaxLevelFor(kind);
            return max > 0 && GetLevel(kind) >= max;
        }

        /// <summary>Tỷ lệ chí mạng [0, CritChanceCap]: <c>cấp Chí Mạng × CritChancePerLevel</c>.</summary>
        public float CritChance
        {
            get
            {
                float raw = GetLevel(MatchUpgradeKind.Crit) * _table.CritChancePerLevel;
                float cap = _table.CritChanceCap;
                return raw > cap ? cap : (raw < 0f ? 0f : raw);
            }
        }

        /// <summary>
        /// Hệ số sát thương KHI chí mạng: <c>BaseCritMultiplier + cấp Chí Mạng × CritDamagePerLevel</c>
        /// — cùng một cấp với tỷ lệ (nâng cấp Chí Mạng đã gộp tỷ lệ + sát thương).
        /// </summary>
        public float CritMultiplier
            => _table.BaseCritMultiplier
               + GetLevel(MatchUpgradeKind.Crit) * _table.CritDamagePerLevel;

        /// <summary>
        /// Số mũi tên CỘNG THÊM mỗi lần bắn (Làn Đạn): bằng cấp hiện tại, đã kẹp theo
        /// <see cref="IMatchUpgradeTable.MaxLevelFor"/>. Tổng mũi tên = 1 + giá trị này.
        /// </summary>
        public int ExtraProjectiles
        {
            get
            {
                int level = GetLevel(MatchUpgradeKind.Multishot);
                int max = _table.MaxLevelFor(MatchUpgradeKind.Multishot);
                return max > 0 && level > max ? max : level;
            }
        }

        /// <summary>Góc lệch (độ) giữa hai mũi tên kề nhau khi bắn nhiều Làn Đạn.</summary>
        public float MultishotSpreadDegrees => _table.MultishotSpreadDegrees;

        /// <summary>Hệ số vận tốc mũi tên: <c>1 + cấp × ProjectileSpeedPerLevel</c>.</summary>
        public float ProjectileSpeedMultiplier
            => 1f + GetLevel(MatchUpgradeKind.ProjectileSpeed) * _table.ProjectileSpeedPerLevel;

        /// <summary>HP Thành hồi mỗi giây: <c>cấp × RegenHpPerLevel</c> (Lv1 = 5 HP/s với bảng mặc định).</summary>
        public float RegenHpPerSecond
            => GetLevel(MatchUpgradeKind.BaseRegen) * _table.RegenHpPerLevel;

        /// <summary>
        /// Tỷ lệ làm chậm của Nỏ Băng: 0 khi chưa nâng; ngược lại
        /// <c>min(IceSlowCap, IceSlowBase + cấp × IceSlowPerLevel)</c> (Lv1 = 15%… Lv5 = 55%).
        /// </summary>
        public float IceSlowFraction
        {
            get
            {
                int level = GetLevel(MatchUpgradeKind.IceArrow);
                if (level <= 0)
                {
                    return 0f;
                }

                float raw = _table.IceSlowBase + level * _table.IceSlowPerLevel;
                float cap = _table.IceSlowCap;
                return raw > cap ? cap : raw;
            }
        }

        /// <summary>Thời gian làm chậm mỗi phát trúng (giây); 0 khi chưa nâng Nỏ Băng.</summary>
        public float IceSlowDurationSeconds
            => GetLevel(MatchUpgradeKind.IceArrow) > 0 ? _table.IceSlowDurationSeconds : 0f;

        /// <summary>
        /// % sát thương Nỏ gây độc mỗi giây: <c>cấp × PoisonDpsPerLevel</c>
        /// (Lv1 = 5% ATK/s). Tầng combat nhân với sát thương thực của phát bắn.
        /// </summary>
        public float PoisonDpsFraction
            => GetLevel(MatchUpgradeKind.PoisonArrow) * _table.PoisonDpsPerLevel;

        /// <summary>Thời gian độc mỗi phát trúng (giây); 0 khi chưa nâng Nỏ Độc.</summary>
        public float PoisonDurationSeconds
            => GetLevel(MatchUpgradeKind.PoisonArrow) > 0 ? _table.PoisonDurationSeconds : 0f;

        /// <summary>
        /// Tỉ lệ Hoàng Kim (Kinh Tế): 0 khi chưa nâng; ngược lại
        /// <c>min(GoldRushChanceCap, GoldRushChanceBase + cấp × GoldRushChancePerLevel)</c>
        /// (Lv1 = 10% … Lv5 = 20% với bảng mặc định). Khi kích hoạt lúc hạ gục, Vàng rơi
        /// được cộng thêm <see cref="GoldRushBonusFraction"/>.
        /// </summary>
        public float GoldRushChance
        {
            get
            {
                int level = GetLevel(MatchUpgradeKind.GoldRush);
                if (level <= 0)
                {
                    return 0f;
                }

                float raw = _table.GoldRushChanceBase + level * _table.GoldRushChancePerLevel;
                float cap = _table.GoldRushChanceCap;
                return raw > cap ? cap : (raw < 0f ? 0f : raw);
            }
        }

        /// <summary>Phần Vàng cộng thêm khi Hoàng Kim kích hoạt (1.0 = gấp đôi).</summary>
        public float GoldRushBonusFraction => _table.GoldRushBonusFraction;

        /// <summary>
        /// Δ HP tối đa khi mua MỘT cấp Cường Hóa Thành, tính trên HP tối đa BAN ĐẦU
        /// <paramref name="initialMaxHp"/> (mỗi cấp +5% của HP gốc → tuyến tính như bảng GDD,
        /// không lãi kép). Caller (composition root) cộng Δ này qua <c>GameSession.OnArmorUpgraded</c>.
        /// </summary>
        public float FortifyHpDeltaFor(int initialMaxHp)
            => initialMaxHp * _table.FortifyHpPerLevel;

        private static int IndexOf(MatchUpgradeKind kind)
        {
            int idx = (int)kind;
            if (idx < 0 || idx >= KindCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "MatchUpgradeKind không hợp lệ.");
            }
            return idx;
        }
    }
}
