// Feature: tower-defense-vn — bảng tham số designer-tunable cho 3 skill Special.
// Validates: Requirements 6.2, 6.6, 11.2.

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Bộ tham số bất biến cho MỘT skill Special, do designer cấu hình (mirror trong
    /// <c>SpecialSkillTableSO</c>). Mọi giá trị "current" được suy ra từ
    /// <see cref="SpecialSkillState.Level"/> theo công thức tuyến tính/hình học, nên record
    /// này chỉ giữ hằng số nền + bước tăng.
    /// </summary>
    /// <param name="BaseDamage">Sát thương mỗi lần áp ở cấp 1. ≥ 0.</param>
    /// <param name="DamageStep">Sát thương cộng thêm mỗi cấp. ≥ 0.</param>
    /// <param name="BaseCooldown">Thời_Gian_Hồi (giây) ở cấp 1. &gt; 0.</param>
    /// <param name="CooldownStep">Số giây hồi chiêu GIẢM mỗi cấp. ≥ 0.</param>
    /// <param name="MinCooldown">Sàn hồi chiêu — cooldown không bao giờ thấp hơn giá trị này. &gt; 0.</param>
    /// <param name="Radius">Bán_Kính ảnh hưởng (Euclid) từ Vị_Trí_Thành. &gt; 0.</param>
    /// <param name="BaseHitCount">Số lần nổ/chém cơ bản (Trống Đồng, Lưỡi Gươm). Với Mũi Tên bỏ qua (luôn 1).</param>
    /// <param name="ExtraEffectChanceStep">
    /// Mức "%" cộng dồn mỗi cấp cho hiệu ứng phụ:
    /// với Trống Đồng/Lưỡi Gươm là xác suất thêm 1 lần nổ/chém; với Mũi Tên là xác suất dính choáng.
    /// (vd 0.1 = +10%/cấp). ≥ 0; có thể &gt; 1 để đảm bảo thêm chắc chắn.
    /// </param>
    /// <param name="BaseStunSeconds">Thời gian choáng cơ bản khi trúng (Mũi Tên). 0 cho skill khác.</param>
    /// <param name="StunStep">Thời gian choáng cộng thêm mỗi cấp (Mũi Tên). ≥ 0.</param>
    /// <param name="BaseStunChance">Xác suất dính choáng ở cấp 1 (Mũi Tên), trong [0,1]. 0 cho skill khác.</param>
    /// <param name="BaseCost">Giá Vàng để nâng từ cấp 1 lên 2. ≥ 1.</param>
    /// <param name="CostGrowth">Hệ số tăng giá theo cấp (≥ 1 để giá không giảm).</param>
    /// <param name="UnlockCost">Giá Vàng để "mua"/mở khoá skill lần đầu. ≥ 1.</param>
    public readonly record struct SpecialSkillParams(
        float BaseDamage,
        float DamageStep,
        float BaseCooldown,
        float CooldownStep,
        float MinCooldown,
        float Radius,
        int BaseHitCount,
        float ExtraEffectChanceStep,
        float BaseStunSeconds,
        float StunStep,
        float BaseStunChance,
        int BaseCost,
        float CostGrowth,
        int UnlockCost);

    /// <summary>
    /// Bảng tham số cho cả 3 skill Special. Tách thành interface để
    /// <see cref="SpecialSkillSystem"/> ở Core thuần C# không phụ thuộc Unity; lớp ở Game
    /// (<c>SpecialSkillTableSO</c>) hiện thực và cho designer chỉnh trong Inspector.
    /// </summary>
    public interface ISpecialSkillTable
    {
        /// <summary>Trả bộ tham số cho <paramref name="kind"/>. Hàm thuần, không side effect.</summary>
        SpecialSkillParams ParamsFor(SpecialSkillKind kind);
    }
}
