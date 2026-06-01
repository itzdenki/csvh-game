// Feature: tower-defense-vn
// Validates: Requirements 6.1 (cung cấp đúng ba nhánh nâng cấp).

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Ba nhánh nâng cấp duy nhất của Thành theo Requirement 6.1:
    /// <list type="bullet">
    ///   <item><see cref="Armor"/> — Giáp (giảm sát thương Quái cận chiến lên Thành; cũng tăng <c>Máu_Tối_Đa</c> theo Requirement 5.6).</item>
    ///   <item><see cref="Attack"/> — Công (tăng hệ_số_Công cho mọi Đạn).</item>
    ///   <item><see cref="Special"/> — Chiêu đặc biệt (gating bởi cooldown ở Property 14, xử lý trong task 5.3).</item>
    /// </list>
    /// </summary>
    public enum UpgradeTrack
    {
        Armor = 0,
        Attack = 1,
        Special = 2,
    }
}
