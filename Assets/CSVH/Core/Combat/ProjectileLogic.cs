// Feature: tower-defense-vn
// Validates: Requirements 3.4, 3.6
// Properties: P8 - Idempotence của hành động hủy Đạn ngoài biên (qua IsOutOfField + ProjectileWorld.Cull).
//             P9 - Mỗi Đạn gây sát thương cho mỗi Quái tối đa một lần (qua TryRegisterHit).

using System;
using System.Collections.Generic;
using CSVH.Core.Common;

namespace CSVH.Core.Combat
{
    /// <summary>
    /// Logic thuần (không phụ thuộc Unity) gắn với MỘT viên Đạn.
    ///
    /// <para>
    /// Validates: Requirements 3.4, 3.6.
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item>Theo dõi tập <c>enemyId</c> đã bị Đạn này gây sát thương để đảm bảo
    ///   "mỗi Đạn × mỗi Quái tối đa một lần" (Requirement 3.6, Property 9).</item>
    ///   <item>Cung cấp predicate <see cref="IsOutOfField(FieldPoint, FieldGeometry)"/> dùng
    ///   để cull Đạn rời biên Sân_Đấu (Requirement 3.4, Property 8).</item>
    /// </list>
    ///
    /// Một instance được tạo khi Đạn được phóng và loại bỏ khi Đạn bị hủy. Class không nắm
    /// vị trí/vận tốc — phần đó thuộc layer view (<c>ProjectileView</c>) hoặc snapshot
    /// <see cref="ProjectileSnapshot"/> dùng cho mô phỏng và Property-Based Testing.
    /// </summary>
    public sealed class ProjectileLogic
    {
        // Property 9 (Requirement 3.6): mỗi enemyId chỉ thêm vào HashSet một lần;
        // HashSet<T>.Add trả false ở các lần gọi tiếp theo cùng id.
        private readonly HashSet<int> _hitTargetIds = new HashSet<int>();

        /// <summary>
        /// Thử ghi nhận Đạn này đã trúng <paramref name="enemyId"/>.
        /// Trả <c>true</c> đúng một lần cho mỗi <paramref name="enemyId"/> phân biệt;
        /// các lần gọi sau cùng <paramref name="enemyId"/> trả <c>false</c> để chặn double-hit.
        ///
        /// <para>Validates: Requirement 3.6 (Property 9).</para>
        /// </summary>
        public bool TryRegisterHit(int enemyId) => _hitTargetIds.Add(enemyId);

        /// <summary>
        /// Số <c>enemyId</c> phân biệt đã được Đạn này ghi nhận. Hữu ích cho debug/test.
        /// </summary>
        public int HitCount => _hitTargetIds.Count;

        /// <summary>
        /// Kiểm tra <paramref name="point"/> đã rời khỏi vùng Sân_Đấu định bởi <paramref name="geometry"/> hay chưa.
        /// Quy ước biên: nửa-chiều-rộng theo trục X và nửa-chiều-cao theo trục Y quanh gốc tọa độ; điểm
        /// nằm NGOÀI biên khi <c>|X| &gt; HalfWidth ∨ |Y| &gt; HalfHeight</c>.
        ///
        /// <para>Validates: Requirement 3.4 (Property 8).</para>
        /// </summary>
        /// <remarks>
        /// Là method <c>static</c> để có thể dùng trên nhiều Đạn cùng lúc (xem
        /// <see cref="ProjectileWorld.Cull"/>) mà không cần instance. Predicate chỉ phụ thuộc
        /// đầu vào nên ổn định (stable) — nền tảng cho tính idempotent của <see cref="ProjectileWorld.Cull"/>.
        /// </remarks>
        public static bool IsOutOfField(FieldPoint point, FieldGeometry geometry)
        {
            if (geometry is null) throw new ArgumentNullException(nameof(geometry));
            return MathF.Abs(point.X) > geometry.HalfWidth
                || MathF.Abs(point.Y) > geometry.HalfHeight;
        }
    }
}
