// Feature: tower-defense-vn
// Validates: Requirements 7.2, 13.4 (spawn queue feed cho WaveScheduler).
// Tham chiếu design.md - section "Core - Wave Scheduler".

using CSVH.Core.Common;
using CSVH.Core.Config;

namespace CSVH.Core.Wave
{
    /// <summary>
    /// Yêu cầu spawn một <see cref="EnemyConfig"/> tại <see cref="FieldPoint"/> Cổng_Spawn,
    /// được <see cref="SpawnQueue"/> giữ cho đến khi <see cref="WaveScheduler.Tick"/>
    /// dequeue (tôn trọng <c>spawnCap = 200</c>, Requirement 13.4).
    /// <para/>
    /// Là <c>readonly record struct</c> nên có value equality và allocation-free
    /// — phù hợp với hàng đợi FIFO trong inner loop của WaveScheduler.
    /// </summary>
    /// <param name="Enemy">Loại_Quái cần spawn; không được <c>null</c>.</param>
    /// <param name="Gate">Cổng_Spawn xuất phát; phải thỏa <c>X ≤ 0 ∨ Y ≥ 0</c> (Requirement 1.3, 2.1).</param>
    public readonly record struct SpawnIntent(EnemyConfig Enemy, FieldPoint Gate);
}
