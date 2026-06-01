// Feature: tower-defense-vn — trạng thái runtime của MỘT skill Special.
// Validates: Requirements 6.2, 6.6, 6.7.

using System;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Trạng thái runtime một skill Special: trạng thái mở khoá + cấp hiện tại + cooldown riêng.
    /// Skill bắt đầu ở trạng thái KHÓA (<see cref="IsUnlocked"/> = <c>false</c>) — phải "mua"
    /// (mở khoá) bằng Vàng mới dùng được. Sau khi mở khoá, mọi chỉ số được suy ra từ
    /// <see cref="Level"/> và <see cref="SpecialSkillParams"/>:
    /// <list type="bullet">
    ///   <item><see cref="CurrentDamage"/> = Base + (Level-1) × DamageStep.</item>
    ///   <item><see cref="CurrentCooldownMax"/> = max(MinCooldown, Base − (Level-1) × CooldownStep).</item>
    /// </list>
    /// Lớp thuần C#, không phụ thuộc Unity — test bằng FsCheck/NUnit.
    /// </summary>
    public sealed class SpecialSkillState
    {
        private readonly SpecialSkillParams _p;
        private float _cooldownRemaining;

        /// <summary>Skill mà state này đại diện.</summary>
        public SpecialSkillKind Kind { get; }

        /// <summary>Cấp hiện tại (≥ 1).</summary>
        public int Level { get; private set; }

        /// <summary><c>true</c> khi skill đã được mua/mở khoá. Mặc định <c>false</c>.</summary>
        public bool IsUnlocked { get; private set; }

        /// <summary>Giá Vàng để mở khoá skill lần đầu (≥ 1).</summary>
        public int UnlockCost => Math.Max(1, _p.UnlockCost);

        /// <summary>Tạo state với tham số <paramref name="p"/> và cấp khởi đầu (mặc định 1).</summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <c>BaseCooldown</c>/<c>MinCooldown</c>/<c>Radius</c> ≤ 0, hoặc <paramref name="initialLevel"/> &lt; 1.
        /// </exception>
        public SpecialSkillState(SpecialSkillKind kind, SpecialSkillParams p, int initialLevel = 1)
        {
            if (!(p.BaseCooldown > 0f))
                throw new ArgumentOutOfRangeException(nameof(p), p.BaseCooldown, "BaseCooldown phải > 0.");
            if (!(p.MinCooldown > 0f))
                throw new ArgumentOutOfRangeException(nameof(p), p.MinCooldown, "MinCooldown phải > 0.");
            if (!(p.Radius > 0f))
                throw new ArgumentOutOfRangeException(nameof(p), p.Radius, "Radius phải > 0.");
            if (initialLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(initialLevel), initialLevel, "initialLevel phải ≥ 1.");

            Kind = kind;
            _p = p;
            Level = initialLevel;
            _cooldownRemaining = 0f;
        }

        /// <summary>Sát thương mỗi lần áp lên một Quái ở cấp hiện tại.</summary>
        public float CurrentDamage => _p.BaseDamage + (Level - 1) * _p.DamageStep;

        /// <summary>Thời_Gian_Hồi tối đa ở cấp hiện tại (đã kẹp sàn <c>MinCooldown</c>).</summary>
        public float CurrentCooldownMax =>
            MathF.Max(_p.MinCooldown, _p.BaseCooldown - (Level - 1) * _p.CooldownStep);

        /// <summary>Bán_Kính ảnh hưởng (không đổi theo cấp trong v1).</summary>
        public float CurrentRadius => _p.Radius;

        /// <summary>Thời_Gian_Hồi_Còn_Lại (giây), trong <c>[0, CurrentCooldownMax]</c>.</summary>
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary><c>true</c> khi skill sẵn sàng (hết hồi chiêu).</summary>
        public bool IsReady => _cooldownRemaining <= 0f;

        /// <summary>Giá Vàng để nâng từ cấp hiện tại lên cấp kế tiếp.</summary>
        public int NextUpgradeCost()
        {
            float raw = _p.BaseCost * MathF.Pow(_p.CostGrowth, Level - 1);
            return Math.Max(1, (int)MathF.Round(raw));
        }

        /// <summary>Tăng một cấp (gọi sau khi đã trừ Vàng thành công).</summary>
        public void Upgrade() => Level += 1;

        /// <summary>Mở khoá skill (gọi sau khi đã trừ Vàng thành công). Idempotent.</summary>
        public void Unlock() => IsUnlocked = true;

        /// <summary>Hồi cooldown theo <paramref name="dt"/> (giây ≥ 0), kẹp về ≥ 0.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="dt"/> &lt; 0 hoặc NaN.</exception>
        public void Tick(float dt)
        {
            if (!(dt >= 0f))
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt phải ≥ 0.");
            _cooldownRemaining = MathF.Max(0f, _cooldownRemaining - dt);
        }

        /// <summary>
        /// Cố gắng kích hoạt skill. Nếu đang hồi chiêu → trả <see cref="SpecialActivation.NotReady"/>
        /// và không đổi trạng thái (Requirement 6.7). Nếu sẵn sàng → đặt cooldown về tối đa, roll
        /// hiệu ứng phụ qua <paramref name="rng"/> và trả <see cref="SpecialActivation"/> mô tả hiệu
        /// ứng để view layer áp lên Quái (Requirement 6.6).
        /// </summary>
        /// <param name="rng">
        /// Nguồn ngẫu nhiên cho hiệu ứng "%". Có thể <c>null</c> → coi như không có hiệu ứng phụ
        /// (số lần = base, không choáng) để callsite test/headless vẫn an toàn.
        /// </param>
        public SpecialActivation TryActivate(IRandom rng)
        {
            if (!IsUnlocked || _cooldownRemaining > 0f)
            {
                return SpecialActivation.NotReady(Kind);
            }

            _cooldownRemaining = CurrentCooldownMax;

            float damage = CurrentDamage;
            int hitCount;
            float stun;

            if (Kind == SpecialSkillKind.MuiTen)
            {
                // Mũi Tên: một phát, "%" quyết định có dính choáng hay không.
                hitCount = 1;
                float stunChance = MathF.Min(1f, _p.BaseStunChance + (Level - 1) * _p.ExtraEffectChanceStep);
                bool stunned = rng != null && rng.NextDouble() < stunChance;
                stun = stunned ? _p.BaseStunSeconds + (Level - 1) * _p.StunStep : 0f;
            }
            else
            {
                // Trống Đồng / Lưỡi Gươm: "%" quyết định số lần nổ/chém thêm.
                hitCount = _p.BaseHitCount + RollExtra((Level - 1) * _p.ExtraEffectChanceStep, rng);
                stun = 0f;
            }

            return new SpecialActivation(Kind, true, CurrentRadius, hitCount, damage, stun);
        }

        // Quy đổi "tổng %" thành số lần thêm: phần nguyên là số lần CHẮC CHẮN, phần lẻ là
        // xác suất roll thêm 1 lần. Nhờ vậy %/cấp lớn hơn 100% vẫn tăng đều và deterministic
        // với FakeRandom (cùng seed → cùng kết quả).
        private static int RollExtra(float chance, IRandom rng)
        {
            if (chance <= 0f) return 0;
            int guaranteed = (int)MathF.Floor(chance);
            float remainder = chance - guaranteed;
            if (remainder > 0f && rng != null && rng.NextDouble() < remainder)
            {
                guaranteed += 1;
            }
            return guaranteed;
        }
    }
}
