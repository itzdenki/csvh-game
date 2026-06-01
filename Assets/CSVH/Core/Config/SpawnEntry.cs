// Feature: tower-defense-vn
// Validates: Requirements 10.2, 10.5, 10.6 (round-trip cấu hình JSON dựa trên value equality).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

namespace CSVH.Core.Config
{
    /// <summary>
    /// Một mục trong danh sách spawn của <see cref="WaveConfig"/>: yêu cầu spawn
    /// <paramref name="Count"/> đơn vị Loại_Quái <paramref name="EnemyId"/> với nhịp
    /// <paramref name="SpawnIntervalSeconds"/> giữa hai lần spawn liên tiếp.
    /// Là <c>sealed record</c> chỉ chứa trường nguyên thủy/string nên value equality
    /// auto-generated đã đủ cho round-trip test (Property 1).
    /// </summary>
    /// <param name="EnemyId">Khóa tham chiếu đến <see cref="EnemyConfig.Id"/>. Ràng buộc no-orphan kiểm tại ConfigLoader.</param>
    /// <param name="Count">Số lượng Quái cần spawn. Ràng buộc <c>≥ 0</c> (Requirement 7.2).</param>
    /// <param name="SpawnIntervalSeconds">Khoảng cách thời gian giữa hai lần spawn. Ràng buộc <c>&gt; 0</c>.</param>
    public sealed record SpawnEntry(
        string EnemyId,
        int Count,
        float SpawnIntervalSeconds);
}
