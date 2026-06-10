// Feature: tower-defense-vn
// Validates: Requirements 3.3, 3.6 — hợp đồng tối thiểu giữa ProjectileView và mục tiêu Quái.
// TODO(task 11.1): Khi EnemyView (CSVH.Game.Spawning) được hiện thực, cho EnemyView triển khai
//                  interface này (EnemyId ổn định, Resistance, TakeDamage). Hợp đồng giữ ở đây để
//                  tránh phụ thuộc cứng giữa task 11.1 và 11.2 đang chạy song song.

namespace CSVH.Game.Tower
{
    /// <summary>
    /// Hợp đồng tối thiểu cho một thực thể có thể bị Đạn (<see cref="ProjectileView"/>) gây sát thương.
    ///
    /// <para>
    /// <see cref="EnemyId"/> là định danh ổn định (đời sống bằng đời sống của thực thể) dùng để
    /// <see cref="CSVH.Core.Combat.ProjectileLogic.TryRegisterHit(int)"/> đảm bảo "mỗi Đạn × mỗi Quái
    /// tối đa một lần" (Requirement 3.6).
    /// </para>
    ///
    /// <para>
    /// <see cref="Resistance"/> được nạp vào <see cref="CSVH.Core.Combat.DamageInputs"/> để
    /// tính sát thương hiệu quả (Requirement 3.3).
    /// </para>
    /// </summary>
    public interface IProjectileTarget
    {
        /// <summary>Định danh ổn định của Quái — không đổi trong suốt vòng đời của thực thể.</summary>
        int EnemyId { get; }

        /// <summary>Kháng_Của_Quái áp dụng lên đòn Đạn (Requirement 3.3); kỳ vọng <c>≥ 0</c>.</summary>
        float Resistance { get; }

        /// <summary>Trừ Máu_Hiện_Tại của Quái theo lượng <paramref name="damage"/> đã được kẹp <c>≥ 0</c>.</summary>
        void TakeDamage(float damage);

        /// <summary>
        /// Áp hiệu ứng làm chậm (nâng cấp trong trận "Nỏ Băng"): giảm
        /// <paramref name="fraction"/> (0..1) tốc độ di chuyển trong <paramref name="seconds"/> giây.
        /// Các lần áp chồng nhau lấy max từng thành phần (không cộng dồn).
        /// </summary>
        void ApplySlow(float fraction, float seconds);

        /// <summary>
        /// Áp hiệu ứng độc (nâng cấp trong trận "Nỏ Độc"): gây <paramref name="damagePerSecond"/>
        /// sát thương mỗi giây trong <paramref name="seconds"/> giây. Lần áp mới làm tươi lại
        /// thời gian và lấy max DPS (không cộng dồn).
        /// </summary>
        void ApplyPoison(float damagePerSecond, float seconds);
    }
}
