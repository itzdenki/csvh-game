// Feature: tower-defense-vn
// Validates: Requirements 2.3, 3.3, 5.2, 5.3.
// Property 6: Công thức sát thương Đạn lên Quái — luôn ≥ 0.
// Property 7: Công thức sát thương Quái lên Thành — luôn ≥ 0.

using System;

namespace CSVH.Core.Combat
{
    /// <summary>
    /// Tập hợp các công thức sát thương thuần (pure) dùng cho cả Đạn → Quái
    /// và Quái → Thành, cùng một helper kẹp Máu_Hiện_Tại trong <c>[0, max]</c>
    /// (Requirement 5.3).
    ///
    /// <para>
    /// Tất cả phương thức đều không phân bổ, không có side effect, không phụ thuộc
    /// <c>UnityEngine</c> — phù hợp cho Property-Based Testing chạy trên .NET console
    /// (xem <c>Properties 6, 7</c> trong design.md).
    /// </para>
    /// </summary>
    public static class CombatResolver
    {
        // Feature: tower-defense-vn — Requirement 3.3, Property 6.
        /// <summary>
        /// Tính sát thương hiệu quả của một Đạn lên một Quái:
        /// <c>max(0, BaseDamage × AttackMultiplier − TargetResistance)</c>
        /// (Requirement 3.3).
        /// </summary>
        /// <param name="i">Bộ tham số bất biến mô tả Đạn và Quái mục tiêu.</param>
        /// <returns>
        /// Sát_Thương_Hiệu_Quả không âm. Nếu <c>kháng</c> &gt; <c>base × mult</c>,
        /// trả về <c>0</c> thay vì giá trị âm (Property 6: kết quả luôn <c>≥ 0</c>).
        /// </returns>
        public static float ProjectileDamage(DamageInputs i)
            => MathF.Max(0f, i.BaseDamage * i.AttackMultiplier - i.TargetResistance);

        // Feature: tower-defense-vn — Requirements 2.3, 5.2, Property 7.
        /// <summary>
        /// Tính sát thương Quái cận chiến gây cho Thành sau khi áp dụng Giáp:
        /// <c>max(0, MeleeDamage − Armor)</c> (Requirements 2.3, 5.2).
        /// </summary>
        /// <param name="meleeDamage">Sát_Thương_Cận_Chiến của Quái; kỳ vọng <c>≥ 0</c>.</param>
        /// <param name="armor">Giáp hiện tại của Thành; kỳ vọng <c>≥ 0</c>.</param>
        /// <returns>
        /// Sát_Thương_Nhận không âm. Nếu <paramref name="armor"/> &gt; <paramref name="meleeDamage"/>,
        /// trả về <c>0</c> (Property 7: kết quả luôn <c>≥ 0</c>).
        /// </returns>
        public static float MeleeDamageOnTower(float meleeDamage, float armor)
            => MathF.Max(0f, meleeDamage - armor);

        // Feature: tower-defense-vn — Requirement 5.3.
        /// <summary>
        /// Kẹp Máu_Hiện_Tại sau cập nhật trong khoảng <c>[0, max]</c>
        /// (Requirement 5.3: <c>0 ≤ Máu_Hiện_Tại ≤ Máu_Tối_Đa</c>).
        /// </summary>
        /// <param name="newValue">Giá trị Máu_Hiện_Tại đề xuất sau một bước cập nhật.</param>
        /// <param name="max">Máu_Tối_Đa hiện tại; kỳ vọng <c>≥ 0</c>.</param>
        /// <returns>Giá trị đã kẹp, đảm bảo nằm trong <c>[0, max]</c>.</returns>
        public static int ClampHp(int newValue, int max)
            => Math.Clamp(newValue, 0, max);
    }
}
