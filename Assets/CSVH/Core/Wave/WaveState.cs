// Feature: tower-defense-vn
// Validates: Requirements 7.1, 7.2, 7.4, 7.5, 7.7, 13.4
// Tham chiếu design.md - section "Core - Wave Scheduler" và state diagram:
//   Loading -> Preparing -> Active -> Cleared -> Preparing -> ...
//   Bất kỳ trạng thái nào -> GameOver khi Máu_Hiện_Tại = 0 (Requirement 5.4).

namespace CSVH.Core.Wave
{
    /// <summary>
    /// Trạng thái của <see cref="WaveScheduler"/> trong vòng đời một trận đấu.
    /// <para/>
    /// Chuyển trạng thái hợp lệ:
    /// <list type="bullet">
    ///   <item><see cref="Loading"/> → <see cref="Preparing"/> qua <c>Start()</c>.</item>
    ///   <item><see cref="Preparing"/> → <see cref="Active"/> khi <c>Countdown</c> về <c>0</c> (Requirement 7.2).</item>
    ///   <item><see cref="Active"/> → <see cref="Cleared"/> khi toàn bộ Quái đã spawn, hàng đợi rỗng và <c>aliveEnemies = 0</c>.</item>
    ///   <item><see cref="Cleared"/> → <see cref="Preparing"/> qua <c>OnWaveCleared()</c>; <c>CurrentWave</c> tăng đúng <c>1</c> (Requirements 7.4, 7.5).</item>
    ///   <item>Bất kỳ trạng thái nào → <see cref="GameOver"/> qua <c>OnGameOver()</c>; sau đó <c>Tick</c> luôn trả empty (Requirement 5.4 / Property 11).</item>
    /// </list>
    /// </summary>
    public enum WaveState
    {
        /// <summary>Đang nạp cấu hình; chưa kích hoạt vòng lặp Đợt. <c>Tick</c> không phát ra <c>SpawnIntent</c>.</summary>
        Loading = 0,

        /// <summary>Pha_Chuẩn_Bị giữa các Đợt; <c>Countdown</c> giảm dần về 0 trước khi vào <see cref="Active"/> (Requirement 7.2).</summary>
        Preparing = 1,

        /// <summary>Đợt đang chạy: <see cref="WaveScheduler"/> phát <see cref="SpawnIntent"/> theo nhịp <c>SpawnIntervalSeconds</c>, tôn trọng cap 200 quái sống (Requirement 13.4).</summary>
        Active = 2,

        /// <summary>Đợt đã xong: mọi <see cref="SpawnIntent"/> đã phát hết, hàng đợi rỗng và không còn Quái sống. Chờ caller gọi <c>OnWaveCleared()</c>.</summary>
        Cleared = 3,

        /// <summary>Trận đã Kết_Thúc_Trận; mọi <c>Tick</c> kế tiếp trả danh sách rỗng (Requirement 5.4 / Property 11).</summary>
        GameOver = 4,

        /// <summary>Trạng thái lỗi không hồi phục (ví dụ cấu hình hỏng phát hiện runtime); semantics tương tự <see cref="GameOver"/> đối với <c>Tick</c> nhưng tách biệt cho HUD/telemetry.</summary>
        Failed = 5,
    }
}
