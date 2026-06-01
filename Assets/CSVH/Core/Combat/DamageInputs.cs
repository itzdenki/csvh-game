// Feature: tower-defense-vn
// Validates: Requirements 3.3 (sát thương Đạn lên Quái) — input bag cho CombatResolver.ProjectileDamage.
// Property 6: Công thức sát thương Đạn lên Quái.

namespace CSVH.Core.Combat
{
    /// <summary>
    /// Bộ tham số bất biến cho công thức sát thương Đạn → Quái
    /// (Requirement 3.3, Property 6 trong design.md).
    /// Là <c>readonly record struct</c> nên có value equality và allocation-free,
    /// phù hợp dùng trong hot path của <see cref="CombatResolver"/>.
    /// </summary>
    /// <param name="BaseDamage">
    /// Sát_Thương_Cơ_Bản của Đạn (kỳ vọng <c>≥ 0</c>; ràng buộc được kiểm tại Bộ_Nạp_Cấu_Hình
    /// theo Requirement 3.5, không phải tại đây).
    /// </param>
    /// <param name="AttackMultiplier">
    /// Hệ_Số_Công của Thành = <c>1 + Cấp_Nâng_Cấp_Công × Bước_Tăng_Công</c>
    /// (Requirement 6.5). Kỳ vọng <c>≥ 0</c>.
    /// </param>
    /// <param name="TargetResistance">
    /// Kháng_Của_Quái áp dụng lên đòn Đạn (Requirement 3.3). Kỳ vọng <c>≥ 0</c>;
    /// nếu lớn hơn <c>BaseDamage * AttackMultiplier</c> thì sát thương kết quả bị kẹp về 0.
    /// </param>
    public readonly record struct DamageInputs(
        float BaseDamage,
        float AttackMultiplier,
        float TargetResistance);
}
