// Feature: tower-defense-vn — Vòng lặp Nâng cấp (Upgrade Loop), tầng META "Xu cổ".
// Kết quả của một lần thử mua nâng cấp vĩnh viễn bằng Xu cổ. Core không ném exception
// cho đường dẫn bình thường → trả enum/struct result.

namespace CSVH.Core.Progression
{
    /// <summary>Kết quả enum của một lần thử mua nâng cấp META (Xu cổ).</summary>
    public enum MetaUpgradeOutcome
    {
        /// <summary>Đủ Xu cổ và chưa chạm trần cấp → trừ Xu cổ và tăng cấp thành công.</summary>
        Bought = 0,

        /// <summary>Thiếu Xu cổ → trạng thái không đổi.</summary>
        NotEnoughCoins = 1,

        /// <summary>Nhánh đã đạt cấp tối đa (<see cref="IMetaUpgradeTable.MaxLevelFor"/>) → không mua được nữa.</summary>
        MaxLevelReached = 2,
    }

    /// <summary>
    /// Kết quả chi tiết của <see cref="MetaProgressionState.TryBuy"/>.
    /// </summary>
    /// <param name="Outcome">Kết quả enum.</param>
    /// <param name="CostPaid">Số Xu cổ đã trừ. <c>0</c> khi không mua được.</param>
    /// <param name="NewLevel">Cấp mới của nhánh sau giao dịch. Bằng cấp cũ khi không mua được.</param>
    public readonly record struct MetaBuyOutcome(
        MetaUpgradeOutcome Outcome,
        int CostPaid,
        int NewLevel);
}
