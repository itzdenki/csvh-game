// Feature: in-match-upgrades — ScriptableObject mirror cho IMatchUpgradeTable ở Core.

using CSVH.Core.Progression;
using UnityEngine;

namespace CSVH.Game.Data
{
    /// <summary>
    /// ScriptableObject triển khai <see cref="IMatchUpgradeTable"/> — cho designer chỉnh
    /// 9 nâng cấp trong trận (bậc tăng + bảng giá theo mốc) trong Inspector mà không cần
    /// build lại code. Giá theo mốc: phần tử i của <see cref="_stepCosts"/> là giá mua
    /// từ cấp i lên cấp i+1 (mốc 1-2, 2-3, … 5-6); từ mốc 6-inf giá nhân
    /// <see cref="_infiniteCostGrowth"/> mỗi cấp.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchUpgradeTable", menuName = "CSVH/Match Upgrade Table", order = 2)]
    public sealed class MatchUpgradeTableSO : ScriptableObject, IMatchUpgradeTable
    {
        [Header("Bậc tăng mỗi cấp (bảng GDD Lv1..Lv5, ngoại suy vô hạn)")]
        [Tooltip("+% Sát Thương mỗi cấp. 0.05 = +5%/cấp (Lv5 = +25%).")]
        [SerializeField, Min(0f)] private float _damagePerLevel = 0.05f;

        [Tooltip("+% Tốc Đánh (Fire Rate) mỗi cấp. 0.05 = +5%/cấp.")]
        [SerializeField, Min(0f)] private float _attackSpeedPerLevel = 0.05f;

        [Tooltip("+ tỷ lệ Chí Mạng mỗi cấp. 0.05 = +5%/cấp.")]
        [SerializeField, Min(0f)] private float _critChancePerLevel = 0.05f;

        [Tooltip("Trần tỷ lệ Chí Mạng (1 = có thể đạt 100%).")]
        [SerializeField, Range(0f, 1f)] private float _critChanceCap = 1f;

        [Tooltip("Hệ số sát thương chí mạng NỀN (1.5 = đòn chí mạng gây 150% khi chưa nâng Chí Mạng).")]
        [SerializeField, Min(1f)] private float _baseCritMultiplier = 1.5f;

        [Tooltip("+ hệ số chí mạng mỗi cấp Chí Mạng (cùng cấp với tỷ lệ — nâng cấp đã gộp). " +
                 "0.25 = +25%/cấp (Lv5 = +125%).")]
        [SerializeField, Min(0f)] private float _critDamagePerLevel = 0.25f;

        [Header("Làn Đạn (bắn nhiều mũi tên)")]
        [Tooltip("Cấp tối đa của Làn Đạn = số mũi tên cộng thêm tối đa (4 → tối đa 5 làn đạn). ≤ 0 = không trần.")]
        [SerializeField] private int _multishotMaxLevel = 4;

        [Tooltip("Góc lệch (độ) giữa hai mũi tên kề nhau khi bắn nhiều làn.")]
        [SerializeField, Min(0f)] private float _multishotSpreadDegrees = 7f;

        [Tooltip("+% Tốc Độ Bay của mũi tên mỗi cấp. 0.05 = +5%/cấp.")]
        [SerializeField, Min(0f)] private float _projectileSpeedPerLevel = 0.05f;

        [Tooltip("+% HP tối đa BAN ĐẦU của Thành mỗi cấp Cường Hóa Thành. 0.05 = +5% HP/cấp.")]
        [SerializeField, Min(0f)] private float _fortifyHpPerLevel = 0.05f;

        [Tooltip("HP hồi mỗi giây cho MỖI cấp Hồi Phục Thành (5 → Lv1 = 5 HP/s, Lv5 = 25 HP/s).")]
        [SerializeField, Min(0f)] private float _regenHpPerLevel = 5f;

        [Header("Nỏ Băng (làm chậm)")]
        [Tooltip("Phần nền của tỷ lệ làm chậm. 0.05 để Lv1 = 5% + 10% = 15% (khớp bảng GDD).")]
        [SerializeField, Range(0f, 1f)] private float _iceSlowBase = 0.05f;

        [Tooltip("+ tỷ lệ làm chậm mỗi cấp. 0.10 = +10%/cấp (Lv5 = 55%).")]
        [SerializeField, Range(0f, 1f)] private float _iceSlowPerLevel = 0.10f;

        [Tooltip("Trần tỷ lệ làm chậm để Quái không bao giờ đứng im hẳn.")]
        [SerializeField, Range(0f, 1f)] private float _iceSlowCap = 0.8f;

        [Tooltip("Thời gian làm chậm sau mỗi phát trúng (giây).")]
        [SerializeField, Min(0f)] private float _iceSlowDurationSeconds = 2f;

        [Header("Nỏ Độc (sát thương theo thời gian)")]
        [Tooltip("% sát thương Nỏ gây độc MỖI GIÂY cho mỗi cấp. 0.05 = 5% ATK/s/cấp (Lv5 = 25% ATK/s).")]
        [SerializeField, Min(0f)] private float _poisonDpsPerLevel = 0.05f;

        [Tooltip("Thời gian độc sau mỗi phát trúng (giây).")]
        [SerializeField, Min(0f)] private float _poisonDurationSeconds = 3f;

        [Header("Hoàng Kim (Kinh Tế — tỉ lệ nhận thêm Vàng khi hạ gục)")]
        [Tooltip("Phần nền của tỉ lệ Hoàng Kim. 0.075 để Lv1 = 7.5% + 2.5% = 10%.")]
        [SerializeField, Range(0f, 1f)] private float _goldRushChanceBase = 0.075f;

        [Tooltip("+ tỉ lệ Hoàng Kim mỗi cấp. 0.025 = +2.5%/cấp (Lv5 = 20%).")]
        [SerializeField, Range(0f, 1f)] private float _goldRushChancePerLevel = 0.025f;

        [Tooltip("Trần tỉ lệ Hoàng Kim.")]
        [SerializeField, Range(0f, 1f)] private float _goldRushChanceCap = 0.5f;

        [Tooltip("Phần Vàng cộng THÊM khi kích hoạt, tính trên Vàng rơi của Quái (1 = +100% → gấp đôi).")]
        [SerializeField, Min(0f)] private float _goldRushBonusFraction = 1f;

        [Header("Bảng giá Vàng theo mốc (giá nền × trọng số từng nâng cấp)")]
        [Tooltip("Phần tử i = giá NỀN mua từ cấp i lên cấp i+1 (mốc 0→1, 1→2, … 5→6). " +
                 "Giá thật = giá nền × trọng số của nâng cấp (làm tròn).")]
        [SerializeField] private int[] _stepCosts = { 40, 70, 120, 200, 330, 520 };

        [Tooltip("Từ mốc 6-inf: giá nền mỗi cấp tiếp theo = giá trước × hệ số này (≥ 1).")]
        [SerializeField, Min(1f)] private float _infiniteCostGrowth = 1.3f;

        [Tooltip("Trọng số giá theo từng nâng cấp, THEO THỨ TỰ enum MatchUpgradeKind: " +
                 "Damage, AttackSpeed, CritChance, CritDamage, ProjectileSpeed, FortifiedBase, " +
                 "BaseRegen, IceArrow, PoisonArrow, GoldRush. Nâng cấp tiện ích rẻ hơn (< 1), " +
                 "nâng cấp khống chế/DoT mạnh đắt hơn (> 1).")]
        [SerializeField] private float[] _costWeights =
        {
            1.0f,  // Damage      — chuẩn DPS
            1.0f,  // AttackSpeed — chuẩn DPS (kèm tăng nhịp áp băng/độc)
            1.0f,  // Crit        — gộp tỷ lệ + sát thương chí mạng trong một cấp
            3.0f,  // Multishot   — +1 mũi tên/cấp (~+100% DPS) → đắt nhất, có trần cấp
            0.5f,  // ProjectileSpeed — tiện ích, không tăng DPS trực tiếp
            0.6f,  // FortifiedBase   — phòng thủ nhỏ giọt (+5% HP gốc)
            1.0f,  // BaseRegen   — phòng thủ mạnh về cuối trận
            1.2f,  // IceArrow    — khống chế diện rộng rất mạnh
            1.1f,  // PoisonArrow — DoT duy trì cả khi đổi mục tiêu
            1.0f,  // GoldRush    — kinh tế, tự hoàn vốn theo thời gian
        };

        /// <inheritdoc />
        public float DamagePerLevel => _damagePerLevel;
        /// <inheritdoc />
        public float AttackSpeedPerLevel => _attackSpeedPerLevel;
        /// <inheritdoc />
        public float CritChancePerLevel => _critChancePerLevel;
        /// <inheritdoc />
        public float CritChanceCap => _critChanceCap;
        /// <inheritdoc />
        public float BaseCritMultiplier => _baseCritMultiplier;
        /// <inheritdoc />
        public float CritDamagePerLevel => _critDamagePerLevel;
        /// <inheritdoc />
        public float ProjectileSpeedPerLevel => _projectileSpeedPerLevel;
        /// <inheritdoc />
        public float FortifyHpPerLevel => _fortifyHpPerLevel;
        /// <inheritdoc />
        public float RegenHpPerLevel => _regenHpPerLevel;
        /// <inheritdoc />
        public float IceSlowBase => _iceSlowBase;
        /// <inheritdoc />
        public float IceSlowPerLevel => _iceSlowPerLevel;
        /// <inheritdoc />
        public float IceSlowCap => _iceSlowCap;
        /// <inheritdoc />
        public float IceSlowDurationSeconds => _iceSlowDurationSeconds;
        /// <inheritdoc />
        public float PoisonDpsPerLevel => _poisonDpsPerLevel;
        /// <inheritdoc />
        public float PoisonDurationSeconds => _poisonDurationSeconds;
        /// <inheritdoc />
        public float GoldRushChanceBase => _goldRushChanceBase;
        /// <inheritdoc />
        public float GoldRushChancePerLevel => _goldRushChancePerLevel;
        /// <inheritdoc />
        public float GoldRushChanceCap => _goldRushChanceCap;
        /// <inheritdoc />
        public float GoldRushBonusFraction => _goldRushBonusFraction;
        /// <inheritdoc />
        public float MultishotSpreadDegrees => _multishotSpreadDegrees;

        /// <inheritdoc />
        public int MaxLevelFor(MatchUpgradeKind kind)
            => kind == MatchUpgradeKind.Multishot ? _multishotMaxLevel : 0;

        /// <inheritdoc />
        /// <remarks>Giá thật = giá nền theo mốc × trọng số của <paramref name="kind"/>, ≥ 1.</remarks>
        public int CostFor(MatchUpgradeKind kind, int currentLevel)
        {
            if (currentLevel < 0) currentLevel = 0;

            // Bảng trống (cấu hình sai) → giá tối thiểu 1 để không bao giờ "miễn phí".
            if (_stepCosts == null || _stepCosts.Length == 0)
            {
                return 1;
            }

            float baseCost;
            if (currentLevel < _stepCosts.Length)
            {
                baseCost = Mathf.Max(1, _stepCosts[currentLevel]);
            }
            else
            {
                // Mốc 6-inf: ngoại suy hình học từ mốc cuối. Kẹp tránh tràn float
                // ở cấp cực cao (giá đụng trần int.MaxValue là dừng).
                int beyond = currentLevel - (_stepCosts.Length - 1);
                baseCost = Mathf.Max(1, _stepCosts[_stepCosts.Length - 1])
                           * Mathf.Pow(_infiniteCostGrowth, beyond);
            }

            float weighted = baseCost * CostWeightFor(kind);
            if (weighted >= int.MaxValue)
            {
                return int.MaxValue;
            }
            return Mathf.Max(1, Mathf.RoundToInt(weighted));
        }

        /// <summary>Trọng số giá của một nâng cấp; 1 khi mảng thiếu phần tử/cấu hình sai.</summary>
        private float CostWeightFor(MatchUpgradeKind kind)
        {
            int idx = (int)kind;
            if (_costWeights == null || idx < 0 || idx >= _costWeights.Length)
            {
                return 1f;
            }
            return _costWeights[idx] > 0f ? _costWeights[idx] : 1f;
        }

        private void OnValidate()
        {
            if (_iceSlowCap < _iceSlowBase) _iceSlowCap = _iceSlowBase;
            if (_goldRushChanceCap < _goldRushChanceBase) _goldRushChanceCap = _goldRushChanceBase;
            if (_infiniteCostGrowth < 1f) _infiniteCostGrowth = 1f;
            if (_stepCosts != null)
            {
                for (int i = 0; i < _stepCosts.Length; i++)
                {
                    if (_stepCosts[i] < 1) _stepCosts[i] = 1;
                }
            }
            if (_costWeights != null)
            {
                for (int i = 0; i < _costWeights.Length; i++)
                {
                    if (_costWeights[i] <= 0f) _costWeights[i] = 1f;
                }
            }
        }
    }
}
