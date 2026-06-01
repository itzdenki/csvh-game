// Feature: tower-defense-vn
// Validates: Requirements 6.2, 6.4, 6.5, 5.6.

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Bảng tham số nâng cấp do designer cấu hình (mirror trong
    /// <c>UpgradeTable.asset</c> ScriptableObject ở task 10.2).
    /// Tách thành interface để <see cref="UpgradeSystem"/> ở Core thuần C# không phụ thuộc Unity.
    /// </summary>
    /// <remarks>
    /// Quy ước tăng theo bậc tuyến tính (Requirement 6.4, 6.5):
    /// <list type="bullet">
    ///   <item><c>CurrentArmor = BaseArmor + ArmorLevel × ArmorStep</c></item>
    ///   <item><c>CurrentAttackMultiplier = 1 + AttackLevel × AttackStep</c></item>
    /// </list>
    /// Khi mua một bậc Giáp, <c>Máu_Tối_Đa</c> tăng đúng <c>ArmorStep</c> (cùng đại lượng dùng
    /// cho công thức Giáp) và <c>Máu_Hiện_Tại</c> tăng cùng lượng (Requirement 5.6, Property 12).
    /// </remarks>
    public interface IUpgradeCostTable
    {
        /// <summary>Giáp_Cơ_Bản, hằng số nền (Requirement 6.4). Kỳ vọng <c>≥ 0</c>.</summary>
        float BaseArmor { get; }

        /// <summary>
        /// Bước_Tăng_Giáp cho mỗi bậc Giáp (Requirement 6.4) và đồng thời là lượng
        /// <c>Δ</c> tăng <c>Máu_Tối_Đa</c> mỗi bậc Giáp (Requirement 5.6). Kỳ vọng <c>≥ 0</c>.
        /// </summary>
        float ArmorStep { get; }

        /// <summary>Bước_Tăng_Công cho mỗi bậc Công (Requirement 6.5). Kỳ vọng <c>≥ 0</c>.</summary>
        float AttackStep { get; }

        /// <summary>Giá nâng cấp cơ bản tại <c>currentLevel = 0</c>. Kỳ vọng <c>&gt; 0</c>.</summary>
        int BaseCost { get; }

        /// <summary>
        /// Hệ số tăng giá theo cấp. Kỳ vọng <c>≥ 1.0</c> để giá không bao giờ giảm.
        /// Cách dùng cụ thể là tùy <see cref="CostFor"/> hiện thực (ví dụ giá hình học
        /// <c>BaseCost × CostGrowth^currentLevel</c>).
        /// </summary>
        float CostGrowth { get; }

        /// <summary>
        /// Trả giá vàng để mua nâng cấp tiếp theo của <paramref name="track"/> khi nhánh
        /// đó đang ở cấp <paramref name="currentLevel"/> (≥ 0).
        /// Hợp đồng: kết quả <c>≥ 0</c> và là hàm thuần (deterministic, không tác dụng phụ).
        /// </summary>
        int CostFor(UpgradeTrack track, int currentLevel);
    }
}
