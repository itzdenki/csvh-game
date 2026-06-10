// Feature: tower-defense-vn — Vòng lặp Nâng cấp (Upgrade Loop), tầng META "Xu cổ".
// DTO bất biến cho dữ liệu meta lưu bền vững giữa các trận (số dư Xu cổ + cấp 3 nhánh).

namespace CSVH.Core.Storage
{
    /// <summary>
    /// Ảnh chụp bất biến của tiến trình META lưu bền vững giữa các phiên chơi:
    /// số dư <c>Xu cổ</c> và cấp của ba nhánh nâng cấp vĩnh viễn. Là <c>sealed record</c>
    /// với toàn trường nguyên thủy nên có value-equality (hỗ trợ round-trip Property meta).
    ///
    /// <para>
    /// Bất biến: mọi trường <c>≥ 0</c>. Triển khai <see cref="IStorageService.ReadMetaProgress"/>
    /// phải kẹp về <see cref="Empty"/> khi chưa có dữ liệu / dữ liệu hỏng (Requirement 12.4).
    /// </para>
    /// </summary>
    /// <param name="Coins">Số dư Xu cổ hiện có (≥ 0).</param>
    /// <param name="GateHpLevel">Cấp nhánh Máu Cổng đã mua (≥ 0).</param>
    /// <param name="CrossbowDamageLevel">Cấp nhánh Sát thương Nỏ đã mua (≥ 0).</param>
    /// <param name="UltimateCooldownLevel">Cấp nhánh Giảm hồi chiêu Ultimate đã mua (≥ 0).</param>
    public sealed record MetaProgressSnapshot(
        long Coins,
        int GateHpLevel,
        int CrossbowDamageLevel,
        int UltimateCooldownLevel)
    {
        /// <summary>Trạng thái mặc định khi chưa từng chơi: không Xu cổ, mọi nhánh cấp 0.</summary>
        public static MetaProgressSnapshot Empty { get; } = new MetaProgressSnapshot(0L, 0, 0, 0);
    }
}
