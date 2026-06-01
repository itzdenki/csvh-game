// Feature: tower-defense-vn — hệ thống quản lý 3 skill Special (kích hoạt + nâng cấp).
// Validates: Requirements 6.1, 6.2, 6.3, 6.6, 6.7.
// Property: cooldown gating per-skill độc lập; mua nâng cấp giữ Gold ≥ 0 và tăng cấp đúng.

using System;
using System.Collections.Generic;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Quản lý ba <see cref="SpecialSkillState"/> (Trống Đồng, Mũi Tên, Lưỡi Gươm). Mỗi skill
    /// có cấp và cooldown độc lập (Requirement 6.1). Lớp thuần C#, không phụ thuộc Unity, để
    /// test bằng FsCheck/NUnit.
    /// <para>
    /// Việc trừ Vàng khi nâng cấp được uỷ thác cho <see cref="UpgradeSystem.TrySpend"/> nên ví
    /// vàng vẫn là một nguồn sự thật duy nhất (giữ bất biến <c>Gold ≥ 0</c>, Requirement 6.3).
    /// </para>
    /// </summary>
    public sealed class SpecialSkillSystem
    {
        private readonly Dictionary<SpecialSkillKind, SpecialSkillState> _states;

        /// <summary>
        /// Tạo hệ thống với bảng tham số <paramref name="table"/>. Khởi tạo cả 3 skill ở cấp 1
        /// (dùng được ngay từ đầu trận).
        /// </summary>
        /// <exception cref="ArgumentNullException">Khi <paramref name="table"/> null.</exception>
        public SpecialSkillSystem(ISpecialSkillTable table)
        {
            if (table is null) throw new ArgumentNullException(nameof(table));

            _states = new Dictionary<SpecialSkillKind, SpecialSkillState>(SpecialSkillKinds.All.Length);
            foreach (var kind in SpecialSkillKinds.All)
            {
                _states[kind] = new SpecialSkillState(kind, table.ParamsFor(kind));
            }
        }

        /// <summary>Lấy state của một skill (read-only access cho HUD/view).</summary>
        public SpecialSkillState State(SpecialSkillKind kind) => _states[kind];

        /// <summary>Cấp hiện tại của <paramref name="kind"/> (≥ 1).</summary>
        public int GetLevel(SpecialSkillKind kind) => _states[kind].Level;

        /// <summary>Thời_Gian_Hồi_Còn_Lại của <paramref name="kind"/> (giây).</summary>
        public float GetCooldownRemaining(SpecialSkillKind kind) => _states[kind].CooldownRemaining;

        /// <summary>Thời_Gian_Hồi tối đa hiện tại của <paramref name="kind"/> (giây).</summary>
        public float GetCooldownMax(SpecialSkillKind kind) => _states[kind].CurrentCooldownMax;

        /// <summary>Giá Vàng để nâng cấp tiếp theo cho <paramref name="kind"/>.</summary>
        public int CostFor(SpecialSkillKind kind) => _states[kind].NextUpgradeCost();

        /// <summary><c>true</c> khi skill <paramref name="kind"/> đã được mua/mở khoá.</summary>
        public bool IsUnlocked(SpecialSkillKind kind) => _states[kind].IsUnlocked;

        /// <summary>Giá Vàng để mở khoá <paramref name="kind"/> lần đầu.</summary>
        public int UnlockCostFor(SpecialSkillKind kind) => _states[kind].UnlockCost;

        /// <summary>
        /// Cố kích hoạt skill <paramref name="kind"/> (Requirement 6.6, 6.7). Trả
        /// <see cref="SpecialActivation"/> mô tả hiệu ứng cần áp; nếu đang hồi chiêu thì
        /// <see cref="SpecialActivation.Activated"/> = <c>false</c>.
        /// </summary>
        public SpecialActivation TryActivate(SpecialSkillKind kind, IRandom rng) =>
            _states[kind].TryActivate(rng);

        /// <summary>Hồi cooldown cho cả 3 skill theo <paramref name="dt"/> (giây ≥ 0).</summary>
        public void Tick(float dt)
        {
            foreach (var state in _states.Values)
            {
                state.Tick(dt);
            }
        }

        /// <summary>
        /// Cố mua/mở khoá skill <paramref name="kind"/> bằng Vàng trong <paramref name="wallet"/>.
        /// Đủ Vàng → trừ Vàng, mở khoá, trả <see cref="UpgradeOutcome.Bought"/>; thiếu → giữ
        /// nguyên, trả <see cref="UpgradeOutcome.NotEnoughGold"/>. Nếu đã mở khoá rồi thì coi như
        /// thành công mà không trừ thêm Vàng.
        /// </summary>
        /// <exception cref="ArgumentNullException">Khi <paramref name="wallet"/> null.</exception>
        public UpgradeOutcome TryUnlock(SpecialSkillKind kind, UpgradeSystem wallet)
        {
            if (wallet is null) throw new ArgumentNullException(nameof(wallet));

            var state = _states[kind];
            if (state.IsUnlocked)
            {
                return UpgradeOutcome.Bought;
            }

            if (!wallet.TrySpend(state.UnlockCost))
            {
                return UpgradeOutcome.NotEnoughGold;
            }

            state.Unlock();
            return UpgradeOutcome.Bought;
        }

        /// <summary>
        /// Cố mua một bậc nâng cấp cho <paramref name="kind"/> bằng Vàng trong
        /// <paramref name="wallet"/>. Skill phải đã mở khoá; nếu chưa thì trả
        /// <see cref="UpgradeOutcome.NotEnoughGold"/> (không có nhánh nâng khi còn khóa). Đủ Vàng
        /// → trừ Vàng, tăng cấp, trả <see cref="UpgradeOutcome.Bought"/>; thiếu → giữ nguyên
        /// (Requirement 6.3).
        /// </summary>
        /// <exception cref="ArgumentNullException">Khi <paramref name="wallet"/> null.</exception>
        public UpgradeOutcome TryBuyUpgrade(SpecialSkillKind kind, UpgradeSystem wallet)
        {
            if (wallet is null) throw new ArgumentNullException(nameof(wallet));

            var state = _states[kind];
            if (!state.IsUnlocked)
            {
                return UpgradeOutcome.NotEnoughGold;
            }

            int cost = state.NextUpgradeCost();

            if (!wallet.TrySpend(cost))
            {
                return UpgradeOutcome.NotEnoughGold;
            }

            state.Upgrade();
            return UpgradeOutcome.Bought;
        }
    }
}
