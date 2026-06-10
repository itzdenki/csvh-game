// Feature: in-match-upgrades — dữ liệu một thẻ trong bảng "Nâng Cấp" trong trận
// (phong cách Subway Surfers) mà GameSceneRoot dựng từ MatchUpgradeSystem rồi đẩy cho HUDController.

using CSVH.Core.Progression;
using UnityEngine;

namespace CSVH.Game.UI
{
    /// <summary>
    /// Mô tả bất biến một thẻ nâng cấp trong trận. HUD chỉ render, không tự suy diễn —
    /// mọi giá trị do <c>GameSceneRoot</c> tính từ <see cref="MatchUpgradeSystem"/> +
    /// <see cref="IMatchUpgradeTable"/>.
    /// </summary>
    /// <param name="Kind">Loại nâng cấp.</param>
    /// <param name="Name">Tên hiển thị tiếng Việt (vd "Sát Thương").</param>
    /// <param name="Description">Mô tả ngắn hiệu ứng của nâng cấp.</param>
    /// <param name="EffectText">Dòng hiệu ứng số: giá trị hiện tại → giá trị sau khi nâng.</param>
    /// <param name="Level">Cấp hiện tại (0 = chưa nâng).</param>
    /// <param name="Cost">Giá Vàng cho cấp kế tiếp (bỏ qua khi <paramref name="IsMaxed"/>).</param>
    /// <param name="CanAfford">Đủ Vàng để mua cấp kế tiếp.</param>
    /// <param name="Icon">Sprite icon của thẻ; <c>null</c> để ẩn ô icon.</param>
    /// <param name="IsMaxed">Đã đạt cấp tối đa (nút giá đổi thành "Tối đa" và vô hiệu).</param>
    /// <param name="BarSegments">Số vạch của thanh cấp (mặc định 6).</param>
    /// <param name="BarFilled">
    /// Số vạch tô đầy; <c>-1</c> = dùng <paramref name="Level"/>. Cho phép thẻ như Làn Đạn
    /// hiển thị SỐ LÀN hiện tại (1 + cấp) trên thanh đúng bằng trần làn.
    /// </param>
    public readonly record struct MatchUpgradeRow(
        MatchUpgradeKind Kind,
        string Name,
        string Description,
        string EffectText,
        int Level,
        int Cost,
        bool CanAfford,
        Sprite Icon,
        bool IsMaxed = false,
        int BarSegments = 6,
        int BarFilled = -1);
}
