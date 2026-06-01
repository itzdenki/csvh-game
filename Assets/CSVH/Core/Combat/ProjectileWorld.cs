// Feature: tower-defense-vn
// Validates: Requirements 3.4
// Property 8: Idempotence của hành động hủy Đạn ngoài biên — Cull(Cull(world)) == Cull(world).

using System;
using System.Collections.Generic;
using CSVH.Core.Common;

namespace CSVH.Core.Combat
{
    /// <summary>
    /// Snapshot bất biến mô tả MỘT viên Đạn tại một thời điểm: định danh ổn định
    /// <paramref name="Id"/> và vị trí <paramref name="Position"/> trên Sân_Đấu.
    /// Dùng cho mô phỏng pure-C# và Property-Based Testing (Property 8) — tách bạch khỏi
    /// MonoBehaviour <c>ProjectileView</c> ở layer Unity.
    ///
    /// <para>Validates: Requirement 3.4 (Property 8).</para>
    /// </summary>
    public readonly record struct ProjectileSnapshot(int Id, FieldPoint Position);

    /// <summary>
    /// Tập các <see cref="ProjectileSnapshot"/> thể hiện toàn bộ Đạn đang sống trong Sân_Đấu.
    /// Cung cấp <see cref="Cull"/> — phép loại bỏ Đạn đã rời biên — là một phép
    /// idempotent: áp dụng nhiều lần cho cùng kết quả.
    ///
    /// <para>Validates: Requirement 3.4 (Property 8).</para>
    /// </summary>
    public static class ProjectileWorld
    {
        /// <summary>
        /// Trả về danh sách MỚI gồm các Đạn còn nằm trong Sân_Đấu, theo thứ tự xuất hiện
        /// trong <paramref name="world"/>. Đạn ngoài biên (theo
        /// <see cref="ProjectileLogic.IsOutOfField"/>) được lọc bỏ.
        ///
        /// <para>Validates: Requirement 3.4 (Property 8).</para>
        /// </summary>
        /// <remarks>
        /// Hàm có hai tính chất quan trọng:
        /// <list type="bullet">
        ///   <item><b>Deterministic</b>: với cùng đầu vào sẽ cho cùng đầu ra; không phụ thuộc
        ///   thời gian, ngẫu nhiên hay trạng thái toàn cục.</item>
        ///   <item><b>Idempotent</b> (Property 8): vì predicate
        ///   <see cref="ProjectileLogic.IsOutOfField"/> chỉ phụ thuộc <see cref="ProjectileSnapshot.Position"/>
        ///   (không thay đổi giữa các lần gọi) và phép lọc bảo toàn các phần tử thỏa predicate,
        ///   nên <c>Cull(Cull(world, g), g)</c> bằng <c>Cull(world, g)</c>.</item>
        /// </list>
        /// Hàm không mutate đối số — caller giữ nguyên reference cũ, danh sách trả về độc lập.
        /// </remarks>
        /// <param name="world">Tập Đạn hiện tại (chỉ đọc).</param>
        /// <param name="geometry">Hình học Sân_Đấu để xác định biên.</param>
        public static IReadOnlyList<ProjectileSnapshot> Cull(
            IReadOnlyList<ProjectileSnapshot> world,
            FieldGeometry geometry)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (geometry is null) throw new ArgumentNullException(nameof(geometry));

            var kept = new List<ProjectileSnapshot>(world.Count);
            for (int i = 0; i < world.Count; i++)
            {
                var snapshot = world[i];
                if (!ProjectileLogic.IsOutOfField(snapshot.Position, geometry))
                {
                    kept.Add(snapshot);
                }
            }
            return kept;
        }
    }
}
