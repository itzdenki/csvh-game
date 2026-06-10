// Feature: in-match-upgrades — bảng tham số do designer cấu hình
// (mirror trong MatchUpgradeTable.asset ScriptableObject ở tầng Game).

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Bảng tham số cho 9 nâng cấp trong trận (<see cref="MatchUpgradeKind"/>).
    /// Tách thành interface để <see cref="MatchUpgradeSystem"/> ở Core thuần C#
    /// không phụ thuộc Unity (test được bằng EditMode/PBT).
    /// </summary>
    /// <remarks>
    /// Quy ước tăng tuyến tính theo cấp (bảng GDD Lv1..Lv5, ngoại suy vô hạn từ Lv6):
    /// <list type="bullet">
    ///   <item>Sát Thương / Tốc Đánh / Chí Mạng / Tốc Độ Bay: <c>cấp × 5%</c>.</item>
    ///   <item>Sát Thương Chí Mạng: <c>cấp × 25%</c> cộng vào hệ số chí mạng nền.</item>
    ///   <item>Cường Hóa Thành: <c>cấp × 5%</c> HP tối đa ban đầu.</item>
    ///   <item>Hồi Phục Thành: <c>cấp × 5</c> HP/giây.</item>
    ///   <item>Nỏ Băng: <c>5% + cấp × 10%</c> (Lv1=15%, Lv5=55%), kẹp tại <see cref="IceSlowCap"/>.</item>
    ///   <item>Nỏ Độc: <c>cấp × 5%</c> sát thương Nỏ mỗi giây trong <see cref="PoisonDurationSeconds"/> giây.</item>
    /// </list>
    /// </remarks>
    public interface IMatchUpgradeTable
    {
        /// <summary>+% sát thương cơ bản mỗi cấp Sát Thương (0.05 = +5%/cấp).</summary>
        float DamagePerLevel { get; }

        /// <summary>+% Fire Rate mỗi cấp Tốc Đánh (0.05 = +5%/cấp).</summary>
        float AttackSpeedPerLevel { get; }

        /// <summary>+ tỷ lệ chí mạng mỗi cấp Chí Mạng (0.05 = +5%/cấp).</summary>
        float CritChancePerLevel { get; }

        /// <summary>Trần tỷ lệ chí mạng (1.0 = có thể đạt 100%).</summary>
        float CritChanceCap { get; }

        /// <summary>Hệ số sát thương chí mạng NỀN khi chưa nâng Chí Mạng (ví dụ 1.5 = 150%).</summary>
        float BaseCritMultiplier { get; }

        /// <summary>+ hệ số chí mạng mỗi cấp Chí Mạng (0.25 = +25%/cấp; cùng cấp với tỷ lệ).</summary>
        float CritDamagePerLevel { get; }

        /// <summary>Góc lệch (độ) giữa hai mũi tên kề nhau khi bắn nhiều Làn Đạn.</summary>
        float MultishotSpreadDegrees { get; }

        /// <summary>+% vận tốc mũi tên mỗi cấp Tốc Độ Bay (0.05 = +5%/cấp).</summary>
        float ProjectileSpeedPerLevel { get; }

        /// <summary>+% HP tối đa BAN ĐẦU của Thành mỗi cấp Cường Hóa Thành (0.05 = +5%/cấp).</summary>
        float FortifyHpPerLevel { get; }

        /// <summary>HP hồi mỗi giây cho MỖI cấp Hồi Phục Thành (5 = Lv1 hồi 5 HP/s, Lv2 hồi 10 HP/s…).</summary>
        float RegenHpPerLevel { get; }

        /// <summary>Phần nền của tỷ lệ làm chậm Nỏ Băng (0.05 để Lv1 = 5% + 10% = 15%).</summary>
        float IceSlowBase { get; }

        /// <summary>+ tỷ lệ làm chậm mỗi cấp Nỏ Băng (0.10 = +10%/cấp).</summary>
        float IceSlowPerLevel { get; }

        /// <summary>Trần tỷ lệ làm chậm (ví dụ 0.8 — không bao giờ đóng băng hoàn toàn).</summary>
        float IceSlowCap { get; }

        /// <summary>Thời gian hiệu lực làm chậm sau mỗi phát trúng (giây).</summary>
        float IceSlowDurationSeconds { get; }

        /// <summary>% sát thương Nỏ gây độc MỖI GIÂY cho mỗi cấp Nỏ Độc (0.05 = 5% ATK/s/cấp).</summary>
        float PoisonDpsPerLevel { get; }

        /// <summary>Thời gian hiệu lực độc sau mỗi phát trúng (giây).</summary>
        float PoisonDurationSeconds { get; }

        /// <summary>Phần nền của tỉ lệ Hoàng Kim (0.075 để Lv1 = 7.5% + 2.5% = 10%).</summary>
        float GoldRushChanceBase { get; }

        /// <summary>+ tỉ lệ Hoàng Kim mỗi cấp (0.025 = +2.5%/cấp → Lv5 = 20%).</summary>
        float GoldRushChancePerLevel { get; }

        /// <summary>Trần tỉ lệ Hoàng Kim (ví dụ 0.5 = tối đa 50%).</summary>
        float GoldRushChanceCap { get; }

        /// <summary>
        /// Phần Vàng cộng THÊM khi Hoàng Kim kích hoạt, tính trên Vàng rơi của Quái
        /// (1.0 = +100% → nhận gấp đôi).
        /// </summary>
        float GoldRushBonusFraction { get; }

        /// <summary>
        /// Giá Vàng để mua cấp kế tiếp của <paramref name="kind"/> khi đang ở cấp
        /// <paramref name="currentLevel"/> (≥ 0; cấp 0 = chưa nâng). Theo các mốc giá
        /// 1-2, 2-3, 3-4, 4-5, 5-6, rồi 6-inf tăng dần vô hạn.
        /// Hợp đồng: kết quả ≥ 0, hàm thuần (deterministic, không tác dụng phụ).
        /// </summary>
        int CostFor(MatchUpgradeKind kind, int currentLevel);

        /// <summary>
        /// Cấp tối đa của <paramref name="kind"/>; <c>≤ 0</c> = không trần (mốc 6-inf).
        /// Dùng cho các nâng cấp quá mạnh nếu vô hạn (ví dụ Làn Đạn).
        /// </summary>
        int MaxLevelFor(MatchUpgradeKind kind);
    }
}
