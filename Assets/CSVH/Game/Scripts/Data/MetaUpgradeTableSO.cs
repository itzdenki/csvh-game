// Feature: tower-defense-vn — GDD Cơ chế 2 (Meta Upgrade). ScriptableObject mirror cho
// IMetaUpgradeTable: cho designer chỉnh bảng giá / hiệu ứng nâng cấp vĩnh viễn (Xu cổ)
// trong Inspector mà không cần build lại code.

using CSVH.Core.Progression;
using UnityEngine;

namespace CSVH.Game.Data
{
    /// <summary>
    /// Bảng tham số cho hệ nâng cấp META (Xu cổ) — hiện thực <see cref="IMetaUpgradeTable"/>.
    /// Giá mỗi bậc theo công thức hình học: <c>cost = max(1, round(baseCost × growth^level))</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "MetaUpgradeTable", menuName = "CSVH/Meta Upgrade Table", order = 1)]
    public sealed class MetaUpgradeTableSO : ScriptableObject, IMetaUpgradeTable
    {
        [Header("Hiệu ứng mỗi bậc")]
        [Tooltip("Máu Cổng (Máu_Tối_Đa) cộng thêm mỗi bậc nhánh Máu Cổng.")]
        [SerializeField, Min(0)] private int _gateHpPerLevel = 25;

        [Tooltip("Sát thương cơ bản của Nỏ (Đạn) cộng thêm mỗi bậc nhánh Sát thương Nỏ.")]
        [SerializeField, Min(0f)] private float _crossbowDamagePerLevel = 1f;

        [Tooltip("Tỉ lệ GIẢM hồi chiêu Ultimate mỗi bậc (0.05 = −5%/bậc).")]
        [SerializeField, Min(0f)] private float _cooldownReductionPerLevel = 0.05f;

        [Tooltip("Tỉ lệ giảm hồi chiêu tối đa, ∈ [0, 1). 0.6 = giảm tối đa 60%.")]
        [SerializeField, Range(0f, 0.99f)] private float _maxCooldownReduction = 0.6f;

        [Header("Cấp tối đa mỗi nhánh")]
        [SerializeField, Min(0)] private int _gateHpMaxLevel = 20;
        [SerializeField, Min(0)] private int _crossbowDamageMaxLevel = 20;
        [SerializeField, Min(0)] private int _ultimateCooldownMaxLevel = 10;

        [Header("Bảng giá Xu cổ (cost = round(baseCost × growth^level))")]
        [Tooltip("Giá Xu cổ cho bậc đầu tiên của nhánh Máu Cổng.")]
        [SerializeField, Min(1)] private int _gateHpBaseCost = 10;

        [Tooltip("Giá Xu cổ cho bậc đầu tiên của nhánh Sát thương Nỏ.")]
        [SerializeField, Min(1)] private int _crossbowDamageBaseCost = 12;

        [Tooltip("Giá Xu cổ cho bậc đầu tiên của nhánh Giảm hồi chiêu Ultimate.")]
        [SerializeField, Min(1)] private int _ultimateCooldownBaseCost = 15;

        [Tooltip("Hệ số tăng giá theo cấp (≥ 1.0 để giá không bao giờ giảm).")]
        [SerializeField, Min(1f)] private float _costGrowth = 1.4f;

        /// <inheritdoc />
        public int GateHpPerLevel => _gateHpPerLevel;

        /// <inheritdoc />
        public float CrossbowDamagePerLevel => _crossbowDamagePerLevel;

        /// <inheritdoc />
        public float CooldownReductionPerLevel => _cooldownReductionPerLevel;

        /// <inheritdoc />
        public float MaxCooldownReduction => _maxCooldownReduction;

        /// <inheritdoc />
        public int MaxLevelFor(MetaUpgradeTrack track) => track switch
        {
            MetaUpgradeTrack.GateHp => _gateHpMaxLevel,
            MetaUpgradeTrack.CrossbowDamage => _crossbowDamageMaxLevel,
            MetaUpgradeTrack.UltimateCooldown => _ultimateCooldownMaxLevel,
            _ => 0,
        };

        /// <inheritdoc />
        public int CostFor(MetaUpgradeTrack track, int currentLevel)
        {
            if (currentLevel < 0) currentLevel = 0;

            int baseCost = track switch
            {
                MetaUpgradeTrack.GateHp => _gateHpBaseCost,
                MetaUpgradeTrack.CrossbowDamage => _crossbowDamageBaseCost,
                MetaUpgradeTrack.UltimateCooldown => _ultimateCooldownBaseCost,
                _ => 1,
            };

            float raw = baseCost * Mathf.Pow(_costGrowth, currentLevel);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        private void OnValidate()
        {
            if (_gateHpPerLevel < 0) _gateHpPerLevel = 0;
            if (_crossbowDamagePerLevel < 0f) _crossbowDamagePerLevel = 0f;
            if (_cooldownReductionPerLevel < 0f) _cooldownReductionPerLevel = 0f;
            _maxCooldownReduction = Mathf.Clamp(_maxCooldownReduction, 0f, 0.99f);
            if (_gateHpMaxLevel < 0) _gateHpMaxLevel = 0;
            if (_crossbowDamageMaxLevel < 0) _crossbowDamageMaxLevel = 0;
            if (_ultimateCooldownMaxLevel < 0) _ultimateCooldownMaxLevel = 0;
            if (_gateHpBaseCost < 1) _gateHpBaseCost = 1;
            if (_crossbowDamageBaseCost < 1) _crossbowDamageBaseCost = 1;
            if (_ultimateCooldownBaseCost < 1) _ultimateCooldownBaseCost = 1;
            if (_costGrowth < 1f) _costGrowth = 1f;
        }
    }
}
