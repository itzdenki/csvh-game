// Feature: tower-defense-vn — Vòng lặp Nâng cấp (Upgrade Loop), tầng META "Xu cổ".
// Ba nhánh nâng cấp VĨNH VIỄN mua bằng Xu cổ ngoài trận (GDD Cơ chế 2 — Meta Upgrade).

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Ba nhánh nâng cấp vĩnh viễn (meta) của người chơi, mua bằng <c>Xu cổ</c> giữa các
    /// trận (khác với <see cref="UpgradeTrack"/> là nâng cấp tạm thời trong trận bằng Vàng):
    /// <list type="bullet">
    ///   <item><see cref="GateHp"/> — Tăng Máu_Tối_Đa khởi đầu của Cổng/Thành mỗi trận.</item>
    ///   <item><see cref="CrossbowDamage"/> — Tăng Sát_Thương_Cơ_Bản của Nỏ (Đạn) mỗi trận.</item>
    ///   <item><see cref="UltimateCooldown"/> — Giảm Thời_Gian_Hồi của các skill Ultimate.</item>
    /// </list>
    /// </summary>
    public enum MetaUpgradeTrack
    {
        GateHp = 0,
        CrossbowDamage = 1,
        UltimateCooldown = 2,
    }
}
