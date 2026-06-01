// Feature: tower-defense-vn
// Validates: Requirements 7.2, 13.4 (FIFO buffer cho yêu cầu spawn vượt cap 200).
// Tham chiếu design.md - section "Core - Wave Scheduler":
//   "SpawnIntent chứa EnemyConfig, FieldPoint Gate ... Tick tôn trọng spawnCap = 200:
//    nếu tổng (alive + về-spawn) > 200, các SpawnIntent dôi giữ trong hàng đợi."

using System;
using System.Collections.Generic;

namespace CSVH.Core.Wave
{
    /// <summary>
    /// Hàng đợi FIFO chứa các <see cref="SpawnIntent"/> chờ phát ra. Class thuần
    /// (không phụ thuộc Unity, không theo dõi thời gian); chính sách phát ra là
    /// trách nhiệm của <c>WaveScheduler</c> thông qua <see cref="Drain"/>.
    /// <para/>
    /// Bất biến cốt lõi (Requirement 7.2, 13.4):
    /// <list type="bullet">
    ///   <item>Thứ tự FIFO: phần tử <see cref="Enqueue"/> trước được <see cref="Drain"/>
    ///   trước; <see cref="EnqueueRange"/> giữ nguyên thứ tự của
    ///   <see cref="IEnumerable{T}"/> đầu vào.</item>
    ///   <item><see cref="Drain"/> phát tối đa <c>max(0, spawnCap - aliveEnemies)</c>
    ///   phần tử mỗi lần; phần dôi ở lại trong hàng đợi cho lượt kế tiếp,
    ///   thực thi cap 200 quái sống đồng thời (Requirement 13.4).</item>
    ///   <item>Nếu <c>aliveEnemies ≥ spawnCap</c>, không phần tử nào bị rút —
    ///   spawn được hoãn nguyên vẹn (Requirement 13.4).</item>
    /// </list>
    /// </summary>
    public sealed class SpawnQueue
    {
        // FIFO buffer: head ở đầu hàng đợi sẽ được Drain trước (Requirement 7.2).
        private readonly Queue<SpawnIntent> _pending = new();

        /// <summary>
        /// Số <see cref="SpawnIntent"/> đang chờ trong hàng đợi.
        /// </summary>
        public int Count => _pending.Count;

        /// <summary>
        /// Xếp một yêu cầu spawn vào cuối hàng đợi. Luôn thành công; chính sách
        /// cap (200 quái sống) áp dụng tại <see cref="Drain"/> chứ không ở đây
        /// (Requirement 13.4).
        /// </summary>
        /// <param name="intent">Yêu cầu spawn cần xếp hàng.</param>
        public void Enqueue(SpawnIntent intent) => _pending.Enqueue(intent);

        /// <summary>
        /// Xếp một dãy yêu cầu spawn vào cuối hàng đợi theo đúng thứ tự liệt kê.
        /// Tiện cho <c>WaveScheduler</c> đẩy vào toàn bộ <see cref="SpawnIntent"/>
        /// sinh trong tick (theo nhịp <c>SpawnIntervalSeconds</c>) trước khi gọi
        /// <see cref="Drain"/>.
        /// </summary>
        /// <param name="intents">Dãy yêu cầu spawn; <c>null</c> sẽ ném <see cref="ArgumentNullException"/>.</param>
        public void EnqueueRange(IEnumerable<SpawnIntent> intents)
        {
            if (intents is null) throw new ArgumentNullException(nameof(intents));
            foreach (var intent in intents)
            {
                _pending.Enqueue(intent);
            }
        }

        /// <summary>
        /// Rút tối đa <c>max(0, <paramref name="spawnCap"/> - <paramref name="aliveEnemies"/>)</c>
        /// phần tử khỏi đầu hàng đợi theo thứ tự FIFO; phần còn lại giữ nguyên cho
        /// lượt tick kế tiếp. Đây là điểm thực thi cap quái sống đồng thời
        /// (Requirement 13.4): khi <c>aliveEnemies ≥ spawnCap</c>, hàm trả danh sách
        /// rỗng và không thay đổi trạng thái nội bộ.
        /// </summary>
        /// <param name="aliveEnemies">Số Quái đang sống trên Sân_Đấu (≥ 0).</param>
        /// <param name="spawnCap">Cap quái sống tối đa (Requirement 13.4: 200).</param>
        /// <returns>Danh sách read-only các <see cref="SpawnIntent"/> đã rút theo thứ tự FIFO.</returns>
        public IReadOnlyList<SpawnIntent> Drain(int aliveEnemies, int spawnCap)
        {
            // available = max(0, spawnCap - aliveEnemies); robust với input âm hoặc cap nhỏ.
            int available = spawnCap - aliveEnemies;
            if (available <= 0 || _pending.Count == 0)
            {
                return Array.Empty<SpawnIntent>();
            }

            int take = available < _pending.Count ? available : _pending.Count;
            var drained = new List<SpawnIntent>(take);
            for (int i = 0; i < take; i++)
            {
                drained.Add(_pending.Dequeue());
            }
            return drained;
        }

        /// <summary>
        /// Xóa toàn bộ phần tử đang chờ. Dùng khi reset trận hoặc chuyển sang
        /// trạng thái <c>GameOver</c> để chặn mọi spawn còn lại
        /// (Requirements 5.4, 7.5).
        /// </summary>
        public void Clear() => _pending.Clear();
    }
}
