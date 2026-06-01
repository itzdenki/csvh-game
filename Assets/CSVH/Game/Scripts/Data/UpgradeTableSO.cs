// Feature: tower-defense-vn, Task 10.2 - ScriptableObject triển khai IUpgradeCostTable
// Validates: Requirements 6.2, 6.4, 6.5

using CSVH.Core.Progression;
using UnityEngine;

namespace CSVH.Game.Data
{
    /// <summary>
    /// ScriptableObject mirror cho <see cref="IUpgradeCostTable"/> ở Core. Cho phép designer
    /// chỉnh bảng giá / bậc tăng nâng cấp trong Inspector mà không cần build lại code
    /// (Requirement 6.2 — giá Vàng cho mỗi bậc, 6.4 — Bước_Tăng_Giáp, 6.5 — Bước_Tăng_Công).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Công thức giá hình học (theo design.md "Decision: cost growth"):
    /// <c>cost = max(1, round((BaseCost + trackOffset) × CostGrowth^currentLevel))</c>
    /// với <c>trackOffset</c> dịch theo nhánh để Special đắt hơn Công, Công đắt hơn Giáp.
    /// </para>
    /// <para>
    /// <c>SpecialStep</c> không nằm trong <see cref="IUpgradeCostTable"/> nhưng vẫn phơi
    /// như một field designer-tunable cho task 5.3 (cooldown gating Special) đọc qua
    /// <see cref="SpecialStep"/>.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "UpgradeTable", menuName = "CSVH/Upgrade Table", order = 0)]
    public sealed class UpgradeTableSO : ScriptableObject, IUpgradeCostTable
    {
        [Header("Bậc tăng theo nhánh")]
        [Tooltip("Giáp_Cơ_Bản (Requirement 6.4). Kỳ vọng ≥ 0.")]
        [SerializeField, Min(0f)] private float _baseArmor = 0f;

        [Tooltip("Bước_Tăng_Giáp mỗi bậc Giáp (Requirement 6.4). Đồng thời là Δ Máu_Tối_Đa (Requirement 5.6).")]
        [SerializeField, Min(0f)] private float _armorStep = 5f;

        [Tooltip("Bước_Tăng_Công mỗi bậc Công (Requirement 6.5).")]
        [SerializeField, Min(0f)] private float _attackStep = 0.1f;

        [Tooltip("Bước_Tăng_Special mỗi bậc Special (designer-tunable; consumer ở task 5.3).")]
        [SerializeField, Min(0f)] private float _specialStep = 0.1f;

        [Header("Bảng giá Vàng")]
        [Tooltip("Giá nâng cấp cơ bản tại currentLevel = 0 (Requirement 6.2). Kỳ vọng > 0.")]
        [SerializeField, Min(1)] private int _baseCost = 50;

        [Tooltip("Hệ số tăng giá theo cấp (≥ 1.0 để giá không bao giờ giảm).")]
        [SerializeField, Min(1f)] private float _costGrowth = 1.25f;

        /// <inheritdoc />
        public float BaseArmor => _baseArmor;

        /// <inheritdoc />
        public float ArmorStep => _armorStep;

        /// <inheritdoc />
        public float AttackStep => _attackStep;

        /// <summary>
        /// Bước_Tăng_Special mỗi bậc Special. Không thuộc <see cref="IUpgradeCostTable"/>
        /// nhưng phơi để pipeline Special (task 5.3) đọc khi mở rộng.
        /// </summary>
        public float SpecialStep => _specialStep;

        /// <inheritdoc />
        public int BaseCost => _baseCost;

        /// <inheritdoc />
        public float CostGrowth => _costGrowth;

        /// <inheritdoc />
        /// <remarks>
        /// Công thức giá hình học có dịch theo nhánh: Special đắt nhất, Công ở giữa, Giáp rẻ nhất.
        /// Kết quả luôn ≥ 1 để TryBuy không bao giờ "miễn phí" (hợp đồng IUpgradeCostTable).
        /// </remarks>
        public int CostFor(UpgradeTrack track, int currentLevel)
        {
            if (currentLevel < 0) currentLevel = 0;

            int trackOffset = track switch
            {
                UpgradeTrack.Armor => 0,
                UpgradeTrack.Attack => 5,
                UpgradeTrack.Special => 50,
                _ => 0,
            };

            float raw = (_baseCost + trackOffset) * Mathf.Pow(_costGrowth, currentLevel);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        private void OnValidate()
        {
            // Kẹp các giá trị về miền hợp lệ phòng khi designer nhập âm hoặc 0 cho BaseCost.
            if (_baseArmor < 0f) _baseArmor = 0f;
            if (_armorStep < 0f) _armorStep = 0f;
            if (_attackStep < 0f) _attackStep = 0f;
            if (_specialStep < 0f) _specialStep = 0f;
            if (_baseCost < 1) _baseCost = 1;
            if (_costGrowth < 1f) _costGrowth = 1f;
        }
    }
}
