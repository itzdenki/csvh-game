// Feature: tower-defense-vn — Vòng lặp Nâng cấp (Upgrade Loop), tầng META "Xu cổ".
// Bảng tham số designer-tunable cho nâng cấp vĩnh viễn (mirror trong MetaUpgradeTableSO).

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Bảng tham số cho hệ nâng cấp META (Xu cổ). Tách thành interface để
    /// <see cref="MetaProgressionState"/> ở Core thuần C# không phụ thuộc Unity; lớp ở Game
    /// (<c>MetaUpgradeTableSO</c>) hiện thực và cho designer chỉnh trong Inspector.
    ///
    /// <para>Hợp đồng (contract) với mọi triển khai:</para>
    /// <list type="number">
    ///   <item><see cref="CostFor"/> trả <c>≥ 0</c>, là hàm thuần (deterministic, không side effect).</item>
    ///   <item>Các getter "PerLevel" trả <c>≥ 0</c>.</item>
    ///   <item><see cref="MaxCooldownReduction"/> ∈ <c>[0, 1)</c> để hệ số hồi chiêu luôn <c>&gt; 0</c>.</item>
    ///   <item><see cref="MaxLevelFor"/> trả <c>≥ 0</c>; <c>0</c> nghĩa là nhánh bị khóa hoàn toàn.</item>
    /// </list>
    /// </summary>
    public interface IMetaUpgradeTable
    {
        /// <summary>Máu_Tối_Đa cộng thêm cho Thành mỗi bậc nhánh <see cref="MetaUpgradeTrack.GateHp"/> (≥ 0).</summary>
        int GateHpPerLevel { get; }

        /// <summary>Sát_Thương_Cơ_Bản (Nỏ) cộng thêm mỗi bậc nhánh <see cref="MetaUpgradeTrack.CrossbowDamage"/> (≥ 0).</summary>
        float CrossbowDamagePerLevel { get; }

        /// <summary>
        /// Tỉ lệ GIẢM Thời_Gian_Hồi mỗi bậc nhánh <see cref="MetaUpgradeTrack.UltimateCooldown"/>
        /// (vd <c>0.05</c> = −5%/bậc). ≥ 0; tổng giảm bị kẹp bởi <see cref="MaxCooldownReduction"/>.
        /// </summary>
        float CooldownReductionPerLevel { get; }

        /// <summary>Tỉ lệ giảm hồi chiêu tối đa, ∈ <c>[0, 1)</c> (vd <c>0.6</c> = giảm tối đa 60%).</summary>
        float MaxCooldownReduction { get; }

        /// <summary>Cấp tối đa của <paramref name="track"/> (≥ 0). Vượt cấp này không mua được nữa.</summary>
        int MaxLevelFor(MetaUpgradeTrack track);

        /// <summary>
        /// Giá Xu cổ để mua bậc kế tiếp của <paramref name="track"/> khi đang ở cấp
        /// <paramref name="currentLevel"/> (≥ 0). Kết quả <c>≥ 0</c>, hàm thuần.
        /// </summary>
        int CostFor(MetaUpgradeTrack track, int currentLevel);
    }
}
