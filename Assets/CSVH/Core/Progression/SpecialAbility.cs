// Feature: tower-defense-vn
// Property 14: Cooldown gating Special
//   - Khi CooldownRemaining > 0: TryActivate trả false và giữ nguyên CooldownRemaining.
//   - Khi CooldownRemaining == 0: TryActivate đặt CooldownRemaining = CooldownMax và trả về true.
//   - Việc áp hiệu ứng cho mọi Quái có khoảng cách Euclid ≤ Bán_Kính_Special được callsite
//     (GameSession - task 5.4) thực thi qua helper EnemiesInRadius để giữ SpecialAbility
//     thuần gating, dễ test thuộc tính.
//   - Tick(dt) giảm CooldownRemaining theo dt, kẹp ≥ 0.
// Validates: Requirements 6.6, 6.7

using System;
using System.Collections.Generic;
using CSVH.Core.Common;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Cơ chế Cooldown của Special (Requirement 6.6, 6.7). Lớp pure C#, không phụ thuộc
    /// UnityEngine để có thể test bằng FsCheck trong EditMode/CI.
    ///
    /// <para>
    /// Bất biến trong vòng đời một instance:
    /// <list type="bullet">
    ///   <item><description><see cref="CooldownMax"/> &gt; 0 và <see cref="Radius"/> &gt; 0 (kiểm tại constructor).</description></item>
    ///   <item><description><c>0 ≤ <see cref="CooldownRemaining"/> ≤ <see cref="CooldownMax"/></c> sau mọi lời gọi <see cref="Tick"/> hoặc <see cref="TryActivate"/>.</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// SpecialAbility chỉ quản lý gating cooldown (Property 14). Việc tìm các Quái trong
    /// <see cref="Radius"/> được tách thành helper thuần
    /// <see cref="EnemiesInRadius(FieldPoint, float, IEnumerable{ValueTuple{int, FieldPoint}})"/>
    /// và được callsite (GameSession ở task 5.4) gọi sau khi <see cref="TryActivate"/>
    /// trả <c>true</c>.
    /// </para>
    ///
    /// Tham chiếu thiết kế: section "Property 14: Cooldown gating Special" trong design.md.
    /// </summary>
    public sealed class SpecialAbility
    {
        // Kích hoạt còn lại (giây). Bất biến: 0 ≤ _cooldownRemaining ≤ _cooldownMax.
        private float _cooldownRemaining;

        // Hằng số trong vòng đời instance, validate > 0 tại constructor.
        private readonly float _cooldownMax;
        private readonly float _radius;

        /// <summary>
        /// Thời_Gian_Hồi_Còn_Lại (giây). Khởi tạo <c>0</c> nên Special có thể kích hoạt ngay
        /// trong frame đầu (Requirement 6.6). Sau mỗi <see cref="Tick"/> hoặc kích hoạt thành
        /// công, được kẹp về khoảng <c>[0, <see cref="CooldownMax"/>]</c>.
        /// </summary>
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Thời_Gian_Hồi_Tối_Đa (giây). Bất biến trong vòng đời instance, &gt; 0.</summary>
        public float CooldownMax => _cooldownMax;

        /// <summary>Bán_Kính_Special — bán kính ảnh hưởng theo khoảng cách Euclid (Requirement 6.6).</summary>
        public float Radius => _radius;

        /// <summary>
        /// <c>true</c> khi Special sẵn sàng kích hoạt (cooldown đã hồi xong). Tương đương
        /// <c><see cref="CooldownRemaining"/> &lt;= 0</c>.
        /// </summary>
        public bool IsReady => _cooldownRemaining <= 0f;

        /// <summary>
        /// Tạo một <see cref="SpecialAbility"/> với <see cref="CooldownMax"/> và
        /// <see cref="Radius"/> cố định. <see cref="CooldownRemaining"/> khởi tạo <c>0</c> để
        /// lần kích hoạt đầu tiên không bị gating (theo design.md).
        /// </summary>
        /// <param name="cooldownMax">Thời_Gian_Hồi_Tối_Đa (giây). Phải &gt; 0.</param>
        /// <param name="radius">Bán_Kính_Special. Phải &gt; 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <paramref name="cooldownMax"/> hoặc <paramref name="radius"/> &lt;= 0,
        /// hoặc là <see cref="float.NaN"/>.
        /// </exception>
        public SpecialAbility(float cooldownMax, float radius)
        {
            // So sánh dạng `!(x > 0)` cũng loại trừ NaN, vì mọi so sánh với NaN đều cho false.
            if (!(cooldownMax > 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cooldownMax), cooldownMax, "cooldownMax phải > 0.");
            }

            if (!(radius > 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius), radius, "radius phải > 0.");
            }

            _cooldownMax = cooldownMax;
            _radius = radius;
            _cooldownRemaining = 0f;
        }

        /// <summary>
        /// Cố gắng kích hoạt Special. Đây là phần gating thuần (Property 14):
        /// <list type="bullet">
        ///   <item>
        ///     Nếu <see cref="CooldownRemaining"/> &gt; 0 → trả <c>false</c> và không thay
        ///     đổi trạng thái nào (Requirement 6.7).
        ///   </item>
        ///   <item>
        ///     Nếu <see cref="CooldownRemaining"/> == 0 → đặt
        ///     <see cref="CooldownRemaining"/> = <see cref="CooldownMax"/> và trả <c>true</c>
        ///     (Requirement 6.6). Callsite chịu trách nhiệm áp hiệu ứng cho mọi Quái có
        ///     khoảng cách Euclid ≤ <see cref="Radius"/>; xem
        ///     <see cref="EnemiesInRadius(FieldPoint, float, IEnumerable{ValueTuple{int, FieldPoint}})"/>.
        ///   </item>
        /// </list>
        /// </summary>
        public bool TryActivate()
        {
            // Requirement 6.7: đang hồi → từ chối, giữ nguyên CooldownRemaining.
            if (_cooldownRemaining > 0f)
            {
                return false;
            }

            // Requirement 6.6: kích hoạt — đặt cooldown về tối đa.
            _cooldownRemaining = _cooldownMax;
            return true;
        }

        /// <summary>
        /// Tick một bước thời gian; giảm <see cref="CooldownRemaining"/> theo <paramref name="dt"/>
        /// và kẹp <c>≥ 0</c>. Khi <see cref="CooldownRemaining"/> đã bằng 0, lời gọi này không
        /// thay đổi gì.
        /// </summary>
        /// <param name="dt">Khoảng thời gian (giây). Phải <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="dt"/> &lt; 0.</exception>
        public void Tick(float dt)
        {
            if (!(dt >= 0f))
            {
                // Bao gồm cả NaN.
                throw new ArgumentOutOfRangeException(
                    nameof(dt), dt, "dt phải ≥ 0.");
            }

            _cooldownRemaining = MathF.Max(0f, _cooldownRemaining - dt);
        }

        /// <summary>
        /// Đặt lại <see cref="CooldownRemaining"/> về <c>0</c>. Dành cho test (helper) hoặc
        /// các tình huống setup lại trận đấu; không dùng trong gameplay thông thường.
        /// </summary>
        public void Reset()
        {
            _cooldownRemaining = 0f;
        }

        /// <summary>
        /// Helper thuần: trả về Id của các Quái có khoảng cách Euclid &lt;= <paramref name="radius"/>
        /// tính từ <paramref name="origin"/>. Tách khỏi <see cref="TryActivate"/> để giữ
        /// SpecialAbility chỉ tập trung gating; callsite (GameSession - task 5.4) gọi helper
        /// này sau khi kích hoạt thành công và áp hiệu ứng (Property 14).
        /// </summary>
        /// <param name="origin">
        /// Tâm áp dụng hiệu ứng — thường là <see cref="FieldGeometry.TowerPosition"/>.
        /// </param>
        /// <param name="radius">Bán_Kính_Special. Yêu cầu &gt; 0.</param>
        /// <param name="enemies">
        /// Snapshot Quái còn sống cùng tọa độ; mỗi phần tử là cặp <c>(Id, Position)</c>.
        /// Không được <c>null</c>; có thể rỗng.
        /// </param>
        /// <returns>Iterator các Id của Quái nằm trong <paramref name="radius"/>.</returns>
        /// <exception cref="ArgumentNullException">Khi <paramref name="enemies"/> là <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="radius"/> &lt;= 0.</exception>
        public static IEnumerable<int> EnemiesInRadius(
            FieldPoint origin,
            float radius,
            IEnumerable<(int Id, FieldPoint Position)> enemies)
        {
            if (enemies is null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (!(radius > 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius), radius, "radius phải > 0.");
            }

            // So sánh bình phương để tránh sqrt; đại số đơn điệu nên kết quả không đổi.
            float radiusSq = radius * radius;
            return EnumerateInRadius(origin, radiusSq, enemies);
        }

        private static IEnumerable<int> EnumerateInRadius(
            FieldPoint origin,
            float radiusSq,
            IEnumerable<(int Id, FieldPoint Position)> enemies)
        {
            foreach (var (id, position) in enemies)
            {
                float dx = position.X - origin.X;
                float dy = position.Y - origin.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq <= radiusSq)
                {
                    yield return id;
                }
            }
        }
    }
}
