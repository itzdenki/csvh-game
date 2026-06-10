// Feature: tower-defense-vn — GDD Cơ chế 2 (Meta Upgrade). Dữ liệu một dòng trong
// "Cửa Hàng Xu Cổ" mà GameSceneRoot dựng từ MetaProgressionState rồi đẩy cho HUDController.

using CSVH.Core.Progression;

namespace CSVH.Game.UI
{
    /// <summary>
    /// Mô tả bất biến một nhánh nâng cấp META hiển thị trong Cửa Hàng Xu Cổ. HUD chỉ render,
    /// không tự suy diễn — mọi giá trị do <c>GameSceneRoot</c> tính từ
    /// <see cref="MetaProgressionState"/> + <see cref="IMetaUpgradeTable"/>.
    /// </summary>
    /// <param name="Track">Nhánh nâng cấp META.</param>
    /// <param name="Name">Tên hiển thị tiếng Việt (vd "Máu Cổng").</param>
    /// <param name="Level">Cấp hiện tại đã mua.</param>
    /// <param name="MaxLevel">Cấp tối đa của nhánh.</param>
    /// <param name="Cost">Giá Xu cổ cho bậc kế tiếp (bỏ qua khi <paramref name="IsMaxed"/>).</param>
    /// <param name="EffectDesc">Mô tả hiệu ứng hiện tại / sau khi nâng.</param>
    /// <param name="CanAfford">Đủ Xu cổ để mua bậc kế tiếp.</param>
    /// <param name="IsMaxed">Đã đạt cấp tối đa (không mua được nữa).</param>
    public readonly record struct MetaShopRow(
        MetaUpgradeTrack Track,
        string Name,
        int Level,
        int MaxLevel,
        int Cost,
        string EffectDesc,
        bool CanAfford,
        bool IsMaxed);
}
