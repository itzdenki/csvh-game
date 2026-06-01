// Feature: tower-defense-vn, ScriptableObject mirror cho ISpecialSkillTable (3 skill Special).
// Validates: Requirements 6.2, 6.6, 11.2.

using System;
using CSVH.Core.Progression;
using UnityEngine;

namespace CSVH.Game.Data
{
    /// <summary>
    /// ScriptableObject hiện thực <see cref="ISpecialSkillTable"/> cho designer chỉnh tham số
    /// 3 skill Special (Trống Đồng / Mũi Tên / Lưỡi Gươm) trong Inspector mà không cần build lại
    /// code — tương tự <see cref="UpgradeTableSO"/> với nhánh nâng cấp Giáp/Công.
    /// </summary>
    [CreateAssetMenu(fileName = "SpecialSkillTable", menuName = "CSVH/Special Skill Table", order = 1)]
    public sealed class SpecialSkillTableSO : ScriptableObject, ISpecialSkillTable
    {
        /// <summary>
        /// Bộ field designer-tunable cho MỘT skill. Là lớp serializable lồng để hiện gọn gàng
        /// trong Inspector; ánh xạ 1-1 sang <see cref="SpecialSkillParams"/> ở Core.
        /// </summary>
        [Serializable]
        public sealed class SkillEntry
        {
            [Tooltip("Sát thương mỗi lần áp ở cấp 1.")]
            [Min(0f)] public float baseDamage = 20f;

            [Tooltip("Sát thương cộng thêm mỗi cấp.")]
            [Min(0f)] public float damageStep = 8f;

            [Tooltip("Thời gian hồi chiêu (giây) ở cấp 1.")]
            [Min(0.1f)] public float baseCooldown = 12f;

            [Tooltip("Số giây hồi chiêu GIẢM mỗi cấp.")]
            [Min(0f)] public float cooldownStep = 0.6f;

            [Tooltip("Sàn hồi chiêu — không bao giờ thấp hơn giá trị này.")]
            [Min(0.1f)] public float minCooldown = 3f;

            [Tooltip("Bán kính ảnh hưởng (đơn vị world) từ Vị_Trí_Thành.")]
            [Min(0.1f)] public float radius = 5f;

            [Tooltip("Số lần nổ/chém cơ bản (Trống Đồng / Lưỡi Gươm). Mũi Tên bỏ qua (luôn 1).")]
            [Min(0)] public int baseHitCount = 1;

            [Tooltip("Mức '%' cộng dồn mỗi cấp cho hiệu ứng phụ (0.1 = +10%/cấp). " +
                     "Trống Đồng/Lưỡi Gươm: xác suất thêm 1 lần nổ/chém. Mũi Tên: xác suất dính choáng.")]
            [Min(0f)] public float extraEffectChanceStep = 0.12f;

            [Tooltip("Thời gian choáng cơ bản khi trúng (Mũi Tên). 0 cho skill khác.")]
            [Min(0f)] public float baseStunSeconds = 0f;

            [Tooltip("Thời gian choáng cộng thêm mỗi cấp (Mũi Tên).")]
            [Min(0f)] public float stunStep = 0f;

            [Tooltip("Xác suất dính choáng ở cấp 1 (Mũi Tên), trong [0,1]. 0 cho skill khác.")]
            [Range(0f, 1f)] public float baseStunChance = 0f;

            [Tooltip("Giá Vàng để nâng từ cấp 1 lên 2.")]
            [Min(1)] public int baseCost = 80;

            [Tooltip("Hệ số tăng giá theo cấp (≥ 1 để giá không giảm).")]
            [Min(1f)] public float costGrowth = 1.3f;

            [Tooltip("Giá Vàng để 'mua'/mở khoá skill lần đầu.")]
            [Min(1)] public int unlockCost = 120;

            /// <summary>Chuyển sang record bất biến ở Core.</summary>
            public SpecialSkillParams ToParams() => new(
                BaseDamage: baseDamage,
                DamageStep: damageStep,
                BaseCooldown: baseCooldown,
                CooldownStep: cooldownStep,
                MinCooldown: minCooldown,
                Radius: radius,
                BaseHitCount: baseHitCount,
                ExtraEffectChanceStep: extraEffectChanceStep,
                BaseStunSeconds: baseStunSeconds,
                StunStep: stunStep,
                BaseStunChance: baseStunChance,
                BaseCost: baseCost,
                CostGrowth: costGrowth,
                UnlockCost: unlockCost);
        }

        [Header("Trống Đồng Đông Sơn (nổ AoE nhiều chỗ)")]
        [SerializeField]
        private SkillEntry _trongDong = new SkillEntry
        {
            baseDamage = 18f,
            damageStep = 7f,
            baseCooldown = 14f,
            cooldownStep = 0.7f,
            minCooldown = 4f,
            radius = 6f,
            baseHitCount = 2,
            extraEffectChanceStep = 0.15f,
            baseCost = 90,
            costGrowth = 1.3f,
            unlockCost = 140,
        };

        [Header("Mũi Tên An Dương Vương (sát thương + choáng)")]
        [SerializeField]
        private SkillEntry _muiTen = new SkillEntry
        {
            baseDamage = 30f,
            damageStep = 10f,
            baseCooldown = 10f,
            cooldownStep = 0.5f,
            minCooldown = 3f,
            radius = 5f,
            baseHitCount = 1,
            extraEffectChanceStep = 0.1f,
            baseStunSeconds = 1.2f,
            stunStep = 0.15f,
            baseStunChance = 0.4f,
            baseCost = 80,
            costGrowth = 1.3f,
            unlockCost = 120,
        };

        [Header("Lưỡi Gươm Lê Lợi (chém nhiều nhát)")]
        [SerializeField]
        private SkillEntry _luoiGuom = new SkillEntry
        {
            baseDamage = 22f,
            damageStep = 9f,
            baseCooldown = 9f,
            cooldownStep = 0.5f,
            minCooldown = 2.5f,
            radius = 4.5f,
            baseHitCount = 1,
            extraEffectChanceStep = 0.2f,
            baseCost = 85,
            costGrowth = 1.3f,
            unlockCost = 130,
        };

        /// <inheritdoc />
        public SpecialSkillParams ParamsFor(SpecialSkillKind kind) => kind switch
        {
            SpecialSkillKind.TrongDong => _trongDong.ToParams(),
            SpecialSkillKind.MuiTen => _muiTen.ToParams(),
            SpecialSkillKind.LuoiGuom => _luoiGuom.ToParams(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "SpecialSkillKind không hợp lệ."),
        };
    }
}
