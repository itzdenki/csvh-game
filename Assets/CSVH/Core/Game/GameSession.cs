// Feature: tower-defense-vn
// Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.6
//
// Property hỗ trợ:
//   - Property 11 (Bất biến giới hạn Máu và dừng spawn khi Kết_Thúc_Trận): sau mỗi
//     bước cập nhật, 0 ≤ CurrentHp ≤ MaxHp; khi CurrentHp đạt 0, gọi
//     WaveScheduler.OnGameOver() để mọi Tick kế tiếp trả danh sách rỗng.
//   - Property 12 (Nâng cấp Giáp tăng Máu_Tối_Đa bảo toàn ràng buộc): khi
//     OnArmorUpgraded(Δ), CurrentHp' = CurrentHp + Δ và MaxHp' = MaxHp + Δ với Δ ≥ 0.
//
// Tham chiếu thiết kế: section "Trạng thái runtime (in-memory)" và "Property 11/12"
// trong design.md. GameSession là wrapper pure C# (không tham chiếu UnityEngine), bó
// các module Core lại để view layer Unity (EnemyView, TowerView, HUDController) chỉ
// gọi qua API hành vi.

using System;
using CSVH.Core.Combat;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Progression;
using CSVH.Core.Storage;
using CSVH.Core.Wave;

namespace CSVH.Core.Game
{
    /// <summary>
    /// Trạng thái runtime tổng hợp của một trận đấu — thuần C#, không phụ thuộc Unity.
    /// Bó các module Core (<see cref="WaveScheduler"/>, <see cref="LevelingSystem"/>,
    /// <see cref="UpgradeSystem"/>, <see cref="ScoreTracker"/>, <see cref="SpecialAbility"/>)
    /// thành một đối tượng duy nhất để view layer chỉ gọi qua API hành vi như
    /// <see cref="OnEnemyReachedTower"/>, <see cref="OnEnemyKilled"/>,
    /// <see cref="OnArmorUpgraded"/>, <see cref="Tick"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bất biến trong vòng đời một instance:
    /// <list type="bullet">
    ///   <item><description><c>0 ≤ <see cref="CurrentHp"/> ≤ <see cref="MaxHp"/></c> sau mỗi cập nhật (Requirement 5.3, Property 11).</description></item>
    ///   <item><description>Khi <see cref="CurrentHp"/> đạt 0, <see cref="WaveScheduler.OnGameOver"/> được gọi và mọi <see cref="OnEnemyReachedTower"/> kế tiếp là no-op (Requirement 5.4, Property 11).</description></item>
    ///   <item><description><see cref="MaxHp"/> &gt; 0 (kiểm tại constructor, Requirement 5.1).</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class GameSession
    {
        private readonly IUpgradeCostTable _costs;

        /// <summary>Máu_Hiện_Tại của Thành. Khởi tạo bằng <see cref="MaxHp"/>; kẹp <c>[0, MaxHp]</c> sau mỗi cập nhật (Requirement 5.1, 5.3).</summary>
        public int CurrentHp { get; private set; }

        /// <summary>Máu_Tối_Đa của Thành. Tăng khi mua nhánh <see cref="UpgradeTrack.Armor"/> (Requirement 5.6, Property 12).</summary>
        public int MaxHp { get; private set; }

        /// <summary>Bộ điều phối đợt sóng; nhận lệnh <see cref="WaveScheduler.OnGameOver"/> khi Thành sụp đổ.</summary>
        public WaveScheduler WaveScheduler { get; }

        /// <summary>Hệ thống cấp/EXP của Thành.</summary>
        public LevelingSystem Leveling { get; }

        /// <summary>Hệ thống nâng cấp Giáp/Công/Special.</summary>
        public UpgradeSystem Upgrades { get; }

        /// <summary>Theo dõi Điểm_Phiên và Kỷ_Lục.</summary>
        public ScoreTracker Score { get; }

        /// <summary>
        /// Hệ thống 3 skill Special (Trống Đồng / Mũi Tên / Lưỡi Gươm). Có thể <c>null</c> nếu
        /// trận đấu không cấu hình Special — <see cref="Tick"/> bỏ qua khi <c>null</c>.
        /// </summary>
        public SpecialSkillSystem SpecialSkills { get; }

        /// <summary><c>true</c> khi <see cref="CurrentHp"/> đã đạt 0; sau đó mọi sát thương cận chiến tới Thành là no-op.</summary>
        public bool IsGameOver => CurrentHp <= 0;

        /// <summary>
        /// Tạo một phiên trận đấu mới với <paramref name="initialMaxHp"/> là Máu_Tối_Đa và
        /// Máu_Hiện_Tại khởi đầu (Requirement 5.1).
        /// </summary>
        /// <param name="initialMaxHp">Máu_Tối_Đa ban đầu; phải <c>&gt; 0</c>.</param>
        /// <param name="costs">Bảng chi phí nâng cấp; cần để tính <see cref="UpgradeSystem.CurrentArmor"/>.</param>
        /// <param name="wave">Bộ điều phối đợt sóng.</param>
        /// <param name="leveling">Hệ thống cấp/EXP.</param>
        /// <param name="upgrades">Hệ thống nâng cấp.</param>
        /// <param name="score">Theo dõi điểm.</param>
        /// <param name="specialSkills">
        /// Hệ 3 skill Special; có thể <c>null</c> nếu trận đấu không có Special.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="initialMaxHp"/> &lt;= 0.</exception>
        /// <exception cref="ArgumentNullException">Khi <paramref name="costs"/>, <paramref name="wave"/>, <paramref name="leveling"/>, <paramref name="upgrades"/>, hoặc <paramref name="score"/> là <c>null</c>.</exception>
        public GameSession(
            int initialMaxHp,
            IUpgradeCostTable costs,
            WaveScheduler wave,
            LevelingSystem leveling,
            UpgradeSystem upgrades,
            ScoreTracker score,
            SpecialSkillSystem specialSkills)
        {
            if (initialMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialMaxHp), initialMaxHp, "initialMaxHp phải > 0.");
            }

            _costs = costs ?? throw new ArgumentNullException(nameof(costs));
            WaveScheduler = wave ?? throw new ArgumentNullException(nameof(wave));
            Leveling = leveling ?? throw new ArgumentNullException(nameof(leveling));
            Upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            Score = score ?? throw new ArgumentNullException(nameof(score));
            SpecialSkills = specialSkills; // có thể null

            MaxHp = initialMaxHp;
            CurrentHp = initialMaxHp;
        }

        /// <summary>
        /// Áp sát thương cận chiến của một Quái lên Thành (Requirement 2.3, 5.2, 5.3, 5.4).
        /// <list type="number">
        ///   <item>Nếu <see cref="IsGameOver"/> → no-op (Property 11: GameOver giữ nguyên trạng thái).</item>
        ///   <item>Tính Sát_Thương_Hiệu_Quả qua <see cref="CombatResolver.MeleeDamageOnTower"/> với Giáp hiện tại.</item>
        ///   <item>Quy về số nguyên (ceil để tránh "free hit" do làm tròn xuống) rồi trừ vào <see cref="CurrentHp"/>.</item>
        ///   <item>Kẹp <see cref="CurrentHp"/> ∈ <c>[0, <see cref="MaxHp"/>]</c> qua <see cref="CombatResolver.ClampHp"/>.</item>
        ///   <item>Nếu <see cref="CurrentHp"/> chạm 0 → gọi <see cref="WaveScheduler.OnGameOver"/> để chặn spawn (Requirement 5.4).</item>
        /// </list>
        /// </summary>
        /// <param name="meleeDamage">Sát_Thương_Cận_Chiến của Quái; phải <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="meleeDamage"/> &lt; 0 hoặc là <see cref="float.NaN"/>.</exception>
        public void OnEnemyReachedTower(float meleeDamage)
        {
            // !(>= 0) bao gồm cả NaN — caller bug đáng phải throw chứ không silently no-op.
            if (!(meleeDamage >= 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(meleeDamage), meleeDamage, "meleeDamage phải ≥ 0.");
            }

            // Property 11 (phần halt): khi đã GameOver, mọi sự kiện cận chiến tiếp theo là no-op.
            if (IsGameOver)
            {
                return;
            }

            float armor = Upgrades.CurrentArmor(_costs);
            float effective = CombatResolver.MeleeDamageOnTower(meleeDamage, armor);

            // Ceil về int: tránh "0 damage" cho các phân số nhỏ; bám sát kỳ vọng "đòn thực sự gây
            // sát thương" trong Property 11. Effective ≥ 0 theo Property 7.
            int damageInt = (int)MathF.Ceiling(effective);
            CurrentHp = CombatResolver.ClampHp(CurrentHp - damageInt, MaxHp);

            if (CurrentHp == 0)
            {
                WaveScheduler.OnGameOver();
            }
        }

        /// <summary>
        /// Cộng Δ Máu_Tối_Đa khi mua nhánh <see cref="UpgradeTrack.Armor"/> thành công
        /// (Requirement 5.6, Property 12). Giá trị <paramref name="maxHpDelta"/> đến từ
        /// <see cref="BuyOutcome.MaxHpDelta"/>.
        ///
        /// <para>
        /// Sau khi gọi: <c>MaxHp' = MaxHp + Δ</c> và <c>CurrentHp' = CurrentHp + Δ</c>
        /// (kẹp tại <c>MaxHp'</c> để giữ bất biến <c>0 ≤ CurrentHp ≤ MaxHp</c>). Khi <c>Δ = 0</c>,
        /// trạng thái không đổi (lời gọi an toàn cho cả nhánh Attack/Special).
        /// </para>
        /// </summary>
        /// <param name="maxHpDelta">Lượng tăng Máu_Tối_Đa; phải <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="maxHpDelta"/> &lt; 0 hoặc là <see cref="float.NaN"/>.</exception>
        public void OnArmorUpgraded(float maxHpDelta)
        {
            if (!(maxHpDelta >= 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHpDelta), maxHpDelta, "maxHpDelta phải ≥ 0.");
            }

            // Round (chứ không floor) để Δ phân số gần một số nguyên không bị mất hẳn.
            int delta = (int)MathF.Round(maxHpDelta);
            if (delta == 0)
            {
                return;
            }

            // Cộng kẹp để chống tràn int khi Δ rất lớn (PBT có thể sinh số gần int.MaxValue).
            long newMax = (long)MaxHp + delta;
            MaxHp = newMax > int.MaxValue ? int.MaxValue : (int)newMax;

            long newCur = (long)CurrentHp + delta;
            int proposed = newCur > int.MaxValue ? int.MaxValue : (int)newCur;
            CurrentHp = CombatResolver.ClampHp(proposed, MaxHp);
        }

        /// <summary>
        /// Áp dụng phần thưởng từ một Quái vừa bị tiêu diệt: cộng vàng vào <see cref="Upgrades"/>,
        /// EXP vào <see cref="Leveling"/>, và điểm vào <see cref="Score"/> (Requirement 2.4, 4.2, 8.2).
        /// </summary>
        /// <param name="enemy">Cấu hình Quái đã chết; không được <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Khi <paramref name="enemy"/> là <c>null</c>.</exception>
        public void OnEnemyKilled(EnemyConfig enemy)
        {
            if (enemy is null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            Upgrades.AddGold(enemy.GoldReward);
            Leveling.AddExp(enemy.ExpReward);
            Score.AddEnemyKill(enemy.ScoreReward);
        }

        /// <summary>
        /// Tick một bước thời gian. Hiện tại chỉ forward <paramref name="dt"/> cho
        /// <see cref="Special"/> (nếu có) để hồi cooldown (Requirement 6.7, Property 14);
        /// các module khác (<see cref="WaveScheduler"/>) được caller tick trực tiếp với
        /// <c>aliveEnemies</c> riêng.
        /// </summary>
        /// <param name="dt">Khoảng thời gian (giây); phải <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="dt"/> &lt; 0 hoặc là <see cref="float.NaN"/>.</exception>
        public void Tick(float dt)
        {
            if (!(dt >= 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dt), dt, "dt phải ≥ 0.");
            }

            SpecialSkills?.Tick(dt);
        }
    }
}
