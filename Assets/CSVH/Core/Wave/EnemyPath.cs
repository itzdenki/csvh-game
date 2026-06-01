// Feature: tower-defense-vn
// Validates: Requirements 2.1, 2.2 (Đường_Đi_Quái có hai đầu mút đúng;
//                                   bước di chuyển tỉ lệ với Tốc_Độ × dt).
// Tham chiếu design.md - section "Core - Wave Scheduler" và Property 4, 5.

using System;
using System.Collections.Generic;
using CSVH.Core.Common;

namespace CSVH.Core.Wave
{
    /// <summary>
    /// Tiến trình hiện tại của một Quái dọc theo polyline <see cref="EnemyPath"/>.
    /// <list type="bullet">
    ///   <item><see cref="Position"/>: tọa độ tuyệt đối trên Sân_Đấu.</item>
    ///   <item><see cref="SegmentIndex"/>: chỉ số đoạn hiện hành (đoạn nối
    ///   <c>path[SegmentIndex]</c> với <c>path[SegmentIndex+1]</c>);
    ///   nằm trong <c>[0, path.Count - 2]</c>.</item>
    ///   <item><see cref="DistanceAlongSegment"/>: quãng đường đã đi trong đoạn
    ///   hiện hành; nằm trong <c>[0, SegmentLength]</c>.</item>
    /// </list>
    /// Là <c>readonly record struct</c> nên có value equality và allocation-free —
    /// phù hợp cho cập nhật mỗi frame.
    /// </summary>
    public readonly record struct PathProgress(
        FieldPoint Position,
        int SegmentIndex,
        float DistanceAlongSegment);

    /// <summary>
    /// Sinh và tiến hóa <see cref="PathProgress"/> dọc theo một
    /// <see cref="IReadOnlyList{FieldPoint}"/> mô tả Đường_Đi_Quái từ Cổng_Spawn
    /// đến Vị_Trí_Thành. Tất cả phương thức đều thuần (không phụ thuộc Unity,
    /// không trạng thái nội bộ); phù hợp với Property-Based Testing.
    /// <para/>
    /// Bất biến cốt lõi (Requirements 2.1, 2.2):
    /// <list type="bullet">
    ///   <item>Sau <see cref="BuildPath"/>: <c>path[0] == gate</c> và
    ///   <c>path[^1] == towerPosition</c> (Property 4).</item>
    ///   <item>Sau <see cref="AdvanceAlongPath"/>: nếu chưa chạm cuối,
    ///   khoảng cách di chuyển tích lũy bằng <c>speed × dt</c> (Property 5);
    ///   khi đã chạm cuối, vị trí được kẹp về <c>path[^1]</c>.</item>
    /// </list>
    /// </summary>
    public static class EnemyPath
    {
        /// <summary>
        /// Sinh polyline đơn giản nhất nối <paramref name="gate"/> đến
        /// <paramref name="towerPosition"/>: hai điểm — đường thẳng. Đáp ứng
        /// đầy đủ Requirement 2.1 (điểm đầu là Cổng_Spawn, điểm cuối là
        /// Vị_Trí_Thành). Các task sau có thể thay thế bằng polyline nhiều khúc
        /// (ví dụ chèn điểm uốn) mà không phá vỡ giao diện.
        /// </summary>
        /// <param name="gate">Cổng_Spawn xuất phát; kỳ vọng
        /// <see cref="FieldPoint.IsValidSpawnPoint"/> trả <c>true</c>.</param>
        /// <param name="towerPosition">Vị_Trí_Thành; kỳ vọng
        /// <see cref="FieldPoint.IsValidTowerPoint"/> trả <c>true</c>.</param>
        /// <returns>Polyline có ít nhất 2 điểm với <c>path[0] == gate</c>
        /// và <c>path[^1] == towerPosition</c>.</returns>
        public static IReadOnlyList<FieldPoint> BuildPath(FieldPoint gate, FieldPoint towerPosition)
        {
            // Mảng cố định 2 phần tử: nhỏ gọn, không cấp phát List<>;
            // value equality của FieldPoint giữ Property 4 luôn đúng.
            return new[] { gate, towerPosition };
        }

        /// <summary>
        /// Khởi tạo <see cref="PathProgress"/> ở điểm đầu của <paramref name="path"/>:
        /// <c>SegmentIndex = 0</c>, <c>DistanceAlongSegment = 0</c>,
        /// <c>Position = path[0]</c>. Đây là trạng thái đầu tiên của một Quái
        /// vừa được spawn tại Cổng_Spawn (Requirement 2.1).
        /// </summary>
        /// <param name="path">Polyline có ít nhất 2 điểm; <c>null</c> hoặc &lt; 2
        /// phần tử sẽ ném exception.</param>
        public static PathProgress StartProgress(IReadOnlyList<FieldPoint> path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (path.Count < 2)
            {
                throw new ArgumentException(
                    "Path must contain at least 2 points (gate and tower).", nameof(path));
            }

            return new PathProgress(path[0], 0, 0f);
        }

        /// <summary>
        /// Tiến <see cref="PathProgress"/> hiện tại thêm <c>speed × dt</c> đơn vị
        /// dọc theo polyline <paramref name="path"/>. Khi vượt qua các đoạn,
        /// hàm tự động chuyển <see cref="PathProgress.SegmentIndex"/> về đoạn
        /// kế tiếp và đặt <see cref="PathProgress.DistanceAlongSegment"/> = 0.
        /// Khi đã đến (hoặc vượt) điểm cuối, vị trí được kẹp về <c>path[^1]</c>
        /// và <c>SegmentIndex = path.Count - 2</c>,
        /// <c>DistanceAlongSegment = SegmentLength(lastSegment)</c> — đây là
        /// trạng thái "đã chạm Thành" theo Requirement 2.3.
        /// </summary>
        /// <param name="current">Trạng thái tiến trình hiện tại.</param>
        /// <param name="speed">Tốc_Độ của Quái; phải <c>≥ 0</c>.</param>
        /// <param name="dt">Khoảng thời gian cập nhật (giây); phải <c>≥ 0</c>.</param>
        /// <param name="path">Polyline có ít nhất 2 điểm.</param>
        /// <returns><see cref="PathProgress"/> mới sau khi di chuyển.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> là <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> có ít hơn 2 điểm.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="speed"/>
        /// hoặc <paramref name="dt"/> &lt; 0.</exception>
        public static PathProgress AdvanceAlongPath(
            PathProgress current, float speed, float dt, IReadOnlyList<FieldPoint> path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (path.Count < 2)
            {
                throw new ArgumentException(
                    "Path must contain at least 2 points (gate and tower).", nameof(path));
            }
            if (speed < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speed), speed, "Speed must be non-negative.");
            }
            if (dt < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dt), dt, "Delta time must be non-negative.");
            }

            int segmentIndex = current.SegmentIndex;
            float distanceAlongSegment = current.DistanceAlongSegment;
            float remaining = speed * dt;
            int lastSegment = path.Count - 2;

            // Bảo vệ: nếu input có SegmentIndex âm, kẹp về 0 (đầu polyline).
            if (segmentIndex < 0)
            {
                segmentIndex = 0;
                distanceAlongSegment = 0f;
            }

            // Tiêu thụ remaining qua từng đoạn theo thứ tự FIFO của polyline.
            while (remaining > 0f && segmentIndex <= lastSegment)
            {
                FieldPoint a = path[segmentIndex];
                FieldPoint b = path[segmentIndex + 1];
                float segLen = SegmentLength(a, b);
                float distanceLeftInSegment = segLen - distanceAlongSegment;

                // Đoạn suy biến (segLen == 0) hoặc đã chạm cuối đoạn:
                // chuyển sang đoạn kế mà không tiêu remaining.
                if (distanceLeftInSegment <= 0f)
                {
                    segmentIndex++;
                    distanceAlongSegment = 0f;
                    continue;
                }

                if (remaining < distanceLeftInSegment)
                {
                    distanceAlongSegment += remaining;
                    remaining = 0f;
                }
                else
                {
                    remaining -= distanceLeftInSegment;
                    segmentIndex++;
                    distanceAlongSegment = 0f;
                }
            }

            // Đã vượt đoạn cuối — kẹp về điểm cuối polyline (Requirement 2.3 setup).
            if (segmentIndex > lastSegment)
            {
                FieldPoint endA = path[lastSegment];
                FieldPoint endB = path[lastSegment + 1];
                return new PathProgress(endB, lastSegment, SegmentLength(endA, endB));
            }

            // Nội suy tuyến tính vị trí hiện tại trên đoạn đang đứng.
            FieldPoint pa = path[segmentIndex];
            FieldPoint pb = path[segmentIndex + 1];
            float currentSegLen = SegmentLength(pa, pb);
            float t = currentSegLen > 0f ? distanceAlongSegment / currentSegLen : 0f;
            float x = pa.X + (pb.X - pa.X) * t;
            float y = pa.Y + (pb.Y - pa.Y) * t;
            return new PathProgress(new FieldPoint(x, y), segmentIndex, distanceAlongSegment);
        }

        /// <summary>
        /// Khoảng cách Euclid giữa hai <see cref="FieldPoint"/>. Tách thành
        /// helper để các test PBT dùng lại cùng công thức tham chiếu.
        /// </summary>
        public static float SegmentLength(FieldPoint a, FieldPoint b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}
