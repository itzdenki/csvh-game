// Feature: tower-defense-vn
// Validates: Requirements 7.1, 7.2, 7.4, 7.5, 7.7, 13.4
// Property tests scheduled (4.7-4.9):
//   - Property 15: Vận động học wave (model-based) - tổng spawn, cap, transitions
//   - Property 16: CurrentWave đơn điệu tăng nghiêm ngặt
//   - Property 17: IsBossWave == (CurrentWave % 5 == 0)
// Tham chiếu design.md - section "Core - Wave Scheduler" và state diagram.

using System;
using System.Collections.Generic;
using CSVH.Core.Common;
using CSVH.Core.Config;

namespace CSVH.Core.Wave
{
    /// <summary>
    /// Vòng lặp lập lịch Đợt và Pha_Chuẩn_Bị, thuần C# (không phụ thuộc Unity, không
    /// truy cập <c>UnityEngine.Time</c>): caller bơm <c>deltaSeconds</c> qua
    /// <see cref="Tick"/> mỗi frame và nhận lại các <see cref="SpawnIntent"/> để view
    /// layer (<c>EnemySpawnerView</c>) hiện thực hóa Prefab.
    /// <para/>
    /// State machine (xem <see cref="WaveState"/>):
    /// <c>Loading → Preparing → Active → Cleared → Preparing → ...</c>;
    /// <see cref="OnGameOver"/> chuyển từ bất kỳ đâu sang <see cref="WaveState.GameOver"/>
    /// và <see cref="Tick"/> kế tiếp luôn trả danh sách rỗng (Requirement 5.4).
    /// <para/>
    /// Bất biến cốt lõi:
    /// <list type="bullet">
    ///   <item><c>CurrentWave ≥ 1</c> luôn đúng và chỉ tăng (Requirement 7.4 / Property 16).</item>
    ///   <item><see cref="IsBossWave"/> ≡ <c>CurrentWave % 5 == 0</c> (Requirement 7.7 / Property 17).</item>
    ///   <item>Tổng số <see cref="SpawnIntent"/> phát ra cho một Đợt bằng <c>Σ count</c> trong <see cref="WaveConfig.Spawns"/> (Property 15).</item>
    ///   <item><see cref="Tick"/> tôn trọng cap qua <see cref="SpawnQueue.Drain"/> — không bao giờ phát quá <c>spawnCap - aliveEnemies</c> trong một lần gọi (Requirement 13.4).</item>
    ///   <item>Số đợt là vô hạn (Requirement 7.5): khi <c>CurrentWave</c> vượt số bản cấu hình, scheduler dùng cấu hình theo chu kỳ <c>(CurrentWave - 1) % waves.Count</c>.</item>
    /// </list>
    /// </summary>
    public sealed class WaveScheduler
    {
        private readonly IReadOnlyList<WaveConfig> _waves;
        private readonly IReadOnlyDictionary<string, EnemyConfig> _enemiesById;
        private readonly IReadOnlyList<FieldPoint> _defaultGates;
        private readonly Func<int, int> _totalEnemiesForWaveFn;
        private readonly Func<int, IReadOnlyList<SpawnEntry>> _spawnsForWaveFn;
        private readonly float _waveDurationSeconds;
        private readonly float _earlyClearGraceSeconds;
        private readonly bool _startActiveImmediately;
        private readonly SpawnQueue _queue = new SpawnQueue();

        // Trạng thái spawn hiện tại của Đợt đang Active. Mỗi entry mirror một SpawnEntry
        // trong WaveConfig: EnemyId + số còn lại cần phát + đếm ngược tới spawn kế (giây).
        private readonly List<ActiveSpawnEntry> _activeEntries = new List<ActiveSpawnEntry>();

        // Các entry "chưa kịp spawn" của Đợt trước, được tích sang Đợt sau khi Đợt
        // hết thời gian (time-based mode). Áp dụng vào _activeEntries ở EnterActive kế.
        private readonly List<ActiveSpawnEntry> _carryOver = new List<ActiveSpawnEntry>();

        // Round-robin index trên danh sách Cổng_Spawn của Đợt; reset mỗi khi vào Active.
        private int _gateIndex;

        // Đồng hồ ân hạn "dọn sạch sớm" (giây) ở chế độ time-based: bắt đầu đếm từ
        // _earlyClearGraceSeconds khi Đợt đã sạch Quái trước khi hết giờ; về 0 thì skip
        // sang Đợt kế. Giá trị âm = chưa kích hoạt.
        private float _earlyClearTimer = -1f;

        /// <summary>Trạng thái hiện tại của scheduler. Bắt đầu là <see cref="WaveState.Loading"/>.</summary>
        public WaveState State { get; private set; } = WaveState.Loading;

        /// <summary>
        /// Số thứ tự Đợt hiện tại; bắt đầu bằng <c>1</c> và tăng đúng <c>1</c> mỗi lần
        /// <see cref="OnWaveCleared"/> được gọi (Requirement 7.4 / Property 16).
        /// </summary>
        public int CurrentWave { get; private set; } = 1;

        /// <summary>
        /// Số giây còn lại của Pha_Chuẩn_Bị hiện tại. Trong <see cref="WaveState.Preparing"/>
        /// giảm dần theo <c>dt</c> và kẹp <c>≥ 0</c>; ở các state khác là giá trị
        /// "đã đặt cho Đợt kế tiếp" (Requirement 7.2).
        /// </summary>
        public float Countdown { get; private set; }

        /// <summary>
        /// Thời gian chạy của Đợt hiện tại (giây), tính từ thời điểm vào
        /// <see cref="WaveState.Active"/>. Reset về <c>0</c> mỗi khi scheduler chuyển
        /// vào Active. Ở <see cref="WaveState.Preparing"/>/<see cref="WaveState.Cleared"/>
        /// giữ giá trị cuối của Đợt vừa diễn ra (hoặc <c>0</c> nếu chưa từng vào Active).
        /// </summary>
        public float WaveElapsed { get; private set; }

        /// <summary>
        /// Tổng thời lượng (giây) của một Đợt khi chạy ở chế độ đếm-ngược-theo-thời-gian
        /// (cấu hình qua constructor, mặc định <c>0</c> = tắt). Khi <c>&gt; 0</c>, Đợt kết
        /// thúc lúc <see cref="WaveElapsed"/> đạt giá trị này bất kể còn Quái hay không;
        /// các Quái chưa kịp spawn được tích sang Đợt sau.
        /// </summary>
        public float WaveDurationSeconds => _waveDurationSeconds;

        /// <summary>
        /// <c>true</c> nếu scheduler chạy ở chế độ đếm ngược theo thời gian
        /// (<see cref="WaveDurationSeconds"/> &gt; 0).
        /// </summary>
        public bool IsTimedMode => _waveDurationSeconds > 0f;

        /// <summary>
        /// Số giây còn lại của Đợt hiện tại ở chế độ đếm ngược (<c>WaveDurationSeconds - WaveElapsed</c>,
        /// kẹp <c>≥ 0</c>). Khi chế độ time-based tắt, trả <c>0</c>.
        /// </summary>
        public float WaveTimeRemaining =>
            _waveDurationSeconds > 0f ? Math.Max(0f, _waveDurationSeconds - WaveElapsed) : 0f;

        /// <summary>
        /// <c>true</c> khi Đợt đã được dọn sạch Quái sớm và đang trong khoảng ân hạn
        /// trước khi tự skip sang Đợt kế (chỉ ở chế độ time-based).
        /// </summary>
        public bool IsEarlyClearPending => _earlyClearTimer >= 0f;

        /// <summary>
        /// Số giây còn lại của khoảng ân hạn "dọn sạch sớm" trước khi skip sang Đợt kế.
        /// Trả <c>0</c> khi chưa kích hoạt (kẹp <c>≥ 0</c>).
        /// </summary>
        public float EarlyClearCountdown => _earlyClearTimer > 0f ? _earlyClearTimer : 0f;

        /// <summary>
        /// <c>true</c> khi Đợt hiện tại là boss (mỗi 5 Đợt) (Requirement 7.7 / Property 17).
        /// </summary>
        public bool IsBossWave => CurrentWave % 5 == 0;

        /// <summary>
        /// Tạo scheduler. Thiết lập <see cref="State"/>=<see cref="WaveState.Loading"/>,
        /// <see cref="CurrentWave"/>=1 và <see cref="Countdown"/>=<c>waves[0].PreparationSeconds</c>.
        /// </summary>
        /// <param name="waves">Cấu hình các Đợt. Không được <c>null</c> hoặc rỗng.</param>
        /// <param name="enemiesById">Bản đồ <see cref="EnemyConfig.Id"/> → <see cref="EnemyConfig"/>. Không được <c>null</c>.</param>
        /// <param name="defaultGates">Cổng_Spawn dự phòng dùng khi <see cref="WaveConfig.SpawnGates"/> rỗng. Có thể <c>null</c>.</param>
        /// <param name="totalEnemiesForWave">
        /// Hàm tùy chọn xác định tổng số Quái cho Đợt thứ <c>N</c>. Khi cung cấp,
        /// scheduler sẽ scale tổng <see cref="SpawnEntry.Count"/> trong cấu hình về đúng
        /// giá trị hàm trả (giữ nguyên tỉ lệ phân bổ giữa các loại Quái). Khi bỏ trống,
        /// dùng nguyên Σ count cấu hình. Hàm phải trả <c>≥ 0</c>.
        /// </param>
        /// <param name="waveDurationSeconds">
        /// Khi <c>&gt; 0</c>, bật chế độ đếm ngược theo thời gian: mỗi Đợt Active kéo dài
        /// đúng <paramref name="waveDurationSeconds"/> giây rồi tự kết thúc (chuyển sang
        /// <see cref="WaveState.Cleared"/>) bất kể còn Quái sống hay chưa. Các Quái chưa
        /// kịp spawn được tích sang Đợt sau. Mặc định <c>0</c> = giữ hành vi cũ (Đợt kết
        /// thúc khi mọi Quái đã spawn, queue rỗng và <c>aliveEnemies == 0</c>).
        /// </param>
        /// <param name="earlyClearGraceSeconds">
        /// Chỉ có tác dụng ở chế độ time-based (<paramref name="waveDurationSeconds"/> &gt; 0).
        /// Khi Đợt được dọn sạch Quái <em>trước</em> khi hết giờ (mọi Quái đã spawn, queue
        /// rỗng, <c>aliveEnemies == 0</c>), scheduler chờ thêm <paramref name="earlyClearGraceSeconds"/>
        /// giây rồi tự skip sang Đợt kế. Mặc định <c>0</c> = skip ngay khi sạch Quái.
        /// </param>
        /// <param name="startActiveImmediately">
        /// Khi <c>true</c>, <see cref="Start"/> đưa Đợt 1 vào <see cref="WaveState.Active"/>
        /// ngay (bỏ Pha_Chuẩn_Bị đầu trận) để Quái spawn tức thì khi vào trận — HUD không
        /// hiển thị "Đợt kế tiếp"/"Đếm ngược" lúc khởi động. Các Pha_Chuẩn_Bị giữa các Đợt
        /// sau vẫn theo <c>PreparationSeconds</c> như thường. Mặc định <c>false</c> = giữ
        /// hành vi cũ (Đợt 1 cũng có Pha_Chuẩn_Bị).
        /// </param>
        /// <param name="spawnsForWave">
        /// Hàm tùy chọn trả về đội hình <see cref="SpawnEntry"/> cho Đợt thứ <c>N</c>.
        /// Khi cung cấp, scheduler dùng kết quả hàm thay cho <see cref="WaveConfig.Spawns"/>
        /// tĩnh — cho phép mở khóa Loại_Quái dần theo tiến trình (vd Đợt 1-5 chỉ Hồ Tinh,
        /// 6-10 thêm Quân Tống, ...). Trả danh sách rỗng ⇒ Đợt không phát Quái nào. Khi
        /// bỏ trống, giữ hành vi cũ (dùng cấu hình tĩnh theo chu kỳ). Cổng_Spawn và
        /// <see cref="WaveConfig.PreparationSeconds"/> vẫn lấy từ cấu hình Đợt.
        /// </param>
        public WaveScheduler(
            IReadOnlyList<WaveConfig> waves,
            IReadOnlyDictionary<string, EnemyConfig> enemiesById,
            IReadOnlyList<FieldPoint> defaultGates = null,
            Func<int, int> totalEnemiesForWave = null,
            float waveDurationSeconds = 0f,
            float earlyClearGraceSeconds = 0f,
            bool startActiveImmediately = false,
            Func<int, IReadOnlyList<SpawnEntry>> spawnsForWave = null)
        {
            if (waves is null) throw new ArgumentNullException(nameof(waves));
            if (waves.Count == 0)
                throw new ArgumentException("waves must contain at least one WaveConfig.", nameof(waves));
            if (enemiesById is null) throw new ArgumentNullException(nameof(enemiesById));
            if (waveDurationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(waveDurationSeconds), "waveDurationSeconds must be ≥ 0.");
            if (earlyClearGraceSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(earlyClearGraceSeconds), "earlyClearGraceSeconds must be ≥ 0.");

            _waves = waves;
            _enemiesById = enemiesById;
            _defaultGates = defaultGates ?? Array.Empty<FieldPoint>();
            _totalEnemiesForWaveFn = totalEnemiesForWave;
            _spawnsForWaveFn = spawnsForWave;
            _waveDurationSeconds = waveDurationSeconds;
            _earlyClearGraceSeconds = earlyClearGraceSeconds;
            _startActiveImmediately = startActiveImmediately;

            // Pre-load Countdown cho Đợt 1 ngay từ Loading để HUD có thể đọc số đếm
            // ngược trước khi caller gọi Start().
            Countdown = _waves[0].PreparationSeconds;
        }

        /// <summary>
        /// Chuyển <see cref="WaveState.Loading"/> → <see cref="WaveState.Preparing"/>.
        /// Đặt lại <see cref="Countdown"/> về <c>PreparationSeconds</c> của Đợt hiện tại
        /// (Requirement 7.2). Chỉ được gọi đúng một lần ngay sau khi nạp cấu hình xong.
        /// <para/>
        /// Khi cấu hình <c>startActiveImmediately = true</c>, bỏ Pha_Chuẩn_Bị của Đợt 1
        /// và vào thẳng <see cref="WaveState.Active"/> để Quái spawn ngay khi vào trận.
        /// </summary>
        public void Start()
        {
            if (State != WaveState.Loading)
                throw new InvalidOperationException(
                    $"Start() requires State == Loading, but current state is {State}.");

            if (_startActiveImmediately)
            {
                // Vào thẳng Active: không Pha_Chuẩn_Bị, không "Đợt kế tiếp"/"Đếm ngược".
                Countdown = 0f;
                EnterActive();
                return;
            }

            Countdown = GetCurrentWaveConfig().PreparationSeconds;
            State = WaveState.Preparing;
        }

        /// <summary>
        /// Bước thời gian một frame: tiêu thụ <paramref name="dt"/> giây, sinh
        /// <see cref="SpawnIntent"/> theo nhịp <c>SpawnIntervalSeconds</c> của các
        /// <see cref="SpawnEntry"/> đang hoạt động, và rút khỏi <see cref="SpawnQueue"/>
        /// tối đa <c>max(0, spawnCap - aliveEnemies)</c> phần tử (Requirement 13.4).
        /// <para/>
        /// Trong <see cref="WaveState.Preparing"/>, nếu <paramref name="dt"/> &gt;
        /// <see cref="Countdown"/>, scheduler tiêu thụ phần Countdown trước, chuyển sang
        /// <see cref="WaveState.Active"/> rồi dùng phần thời gian dôi để tiến các spawn
        /// entry — tránh stall một frame và bám sát mô hình kinematic của Property 15.
        /// <para/>
        /// Trong <see cref="WaveState.Active"/>, khi mọi entry đã phát hết, hàng đợi rỗng
        /// và <paramref name="aliveEnemies"/> = 0, scheduler tự động chuyển sang
        /// <see cref="WaveState.Cleared"/>; caller gọi <see cref="OnWaveCleared"/> để bước
        /// vào Pha_Chuẩn_Bị của Đợt kế.
        /// </summary>
        /// <param name="dt">Thời lượng frame (giây); phải <c>≥ 0</c>.</param>
        /// <param name="aliveEnemies">Số Quái đang sống trên Sân_Đấu; <c>≥ 0</c>.</param>
        /// <param name="spawnCap">Cap quái sống đồng thời (mặc định 200, Requirement 13.4).</param>
        /// <returns>Danh sách read-only các <see cref="SpawnIntent"/> được rút trong tick này.</returns>
        public IReadOnlyList<SpawnIntent> Tick(float dt, int aliveEnemies, int spawnCap = 200)
        {
            if (dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt), "dt must be ≥ 0.");
            if (aliveEnemies < 0)
                throw new ArgumentOutOfRangeException(nameof(aliveEnemies), "aliveEnemies must be ≥ 0.");
            if (spawnCap < 0)
                throw new ArgumentOutOfRangeException(nameof(spawnCap), "spawnCap must be ≥ 0.");

            // Trạng thái không sinh spawn: GameOver (Req 5.4 / Property 11), Loading
            // (caller chưa Start), Cleared (chờ OnWaveCleared).
            if (State == WaveState.GameOver
                || State == WaveState.Loading
                || State == WaveState.Cleared)
            {
                return Array.Empty<SpawnIntent>();
            }

            float remainingDt = dt;

            if (State == WaveState.Preparing)
            {
                if (remainingDt < Countdown)
                {
                    // Vẫn còn trong Pha_Chuẩn_Bị; chỉ giảm Countdown.
                    Countdown -= remainingDt;
                    return Array.Empty<SpawnIntent>();
                }

                // Tiêu thụ trọn Countdown rồi chuyển vào Active với phần thời gian dôi.
                remainingDt -= Countdown;
                Countdown = 0f;
                EnterActive();
            }

            // State == Active từ đây.
            // Ở chế độ đếm ngược theo thời gian, chỉ tiêu thụ tối đa phần thời gian còn
            // lại của Đợt (roomLeft) để WaveElapsed không vượt quá WaveDurationSeconds.
            float activeDt = remainingDt;
            if (_waveDurationSeconds > 0f)
            {
                float roomLeft = Math.Max(0f, _waveDurationSeconds - WaveElapsed);
                if (activeDt > roomLeft) activeDt = roomLeft;
            }

            WaveElapsed += activeDt;
            AdvanceActive(activeDt);
            var drained = _queue.Drain(aliveEnemies, spawnCap);

            if (_waveDurationSeconds > 0f)
            {
                // Đợt kết thúc theo đồng hồ: khi hết thời gian, tích các Quái chưa kịp
                // spawn sang Đợt sau (carry-over) rồi chuyển sang Cleared — bất kể còn
                // Quái sống hay chưa.
                if (WaveElapsed >= _waveDurationSeconds)
                {
                    CaptureCarryOver();
                    _earlyClearTimer = -1f;
                    State = WaveState.Cleared;
                }
                else if (IsCurrentWaveExhausted(aliveEnemies))
                {
                    // Dọn sạch sớm: mọi Quái đã spawn, queue rỗng và không còn Quái sống.
                    // Chờ thêm _earlyClearGraceSeconds giây rồi skip sang Đợt kế.
                    if (_earlyClearTimer < 0f)
                    {
                        _earlyClearTimer = _earlyClearGraceSeconds;
                    }

                    _earlyClearTimer -= activeDt;
                    if (_earlyClearTimer <= 0f)
                    {
                        _earlyClearTimer = -1f;
                        State = WaveState.Cleared;
                    }
                }
                else
                {
                    // Lại có Quái (carry-over spawn tiếp / chưa sạch): hủy đồng hồ ân hạn.
                    _earlyClearTimer = -1f;
                }
            }
            else
            {
                // Hành vi cũ: Auto-transition Active → Cleared khi không còn gì để phát
                // và không còn Quái sống — Property 15 yêu cầu State == Cleared khi đợt kết thúc.
                if (IsCurrentWaveExhausted(aliveEnemies))
                    State = WaveState.Cleared;
            }

            return drained;
        }

        /// <summary>
        /// Tăng <see cref="CurrentWave"/> đúng 1 và đặt <see cref="Countdown"/> bằng
        /// <c>PreparationSeconds</c> của Đợt mới, rồi chuyển sang <see cref="WaveState.Preparing"/>
        /// (Requirements 7.4, 7.5; Property 16). Chấp nhận gọi từ <see cref="WaveState.Active"/>
        /// (caller tự phát hiện điều kiện) hoặc <see cref="WaveState.Cleared"/> (auto-detected).
        /// <para/>
        /// Khi cấu hình <c>startActiveImmediately = true</c>, bỏ luôn Pha_Chuẩn_Bị giữa
        /// các Đợt và vào thẳng <see cref="WaveState.Active"/> của Đợt kế — các Đợt nối
        /// tiếp nhau, HUD không hiển thị "Đợt kế tiếp"/"Đếm ngược".
        /// </summary>
        public void OnWaveCleared()
        {
            if (State != WaveState.Active && State != WaveState.Cleared)
                throw new InvalidOperationException(
                    $"OnWaveCleared() requires State == Active or Cleared, but current state is {State}.");

            CurrentWave++;
            _activeEntries.Clear();
            _queue.Clear();
            _gateIndex = 0;
            WaveElapsed = 0f;
            _earlyClearTimer = -1f;

            if (_startActiveImmediately)
            {
                // Nối thẳng sang Đợt kế: không Pha_Chuẩn_Bị, không "Đợt kế tiếp"/"Đếm ngược".
                Countdown = 0f;
                EnterActive();
                return;
            }

            Countdown = GetCurrentWaveConfig().PreparationSeconds;
            State = WaveState.Preparing;
        }

        /// <summary>
        /// Chuyển sang <see cref="WaveState.GameOver"/> và xóa toàn bộ trạng thái spawn
        /// đang chờ (Requirement 5.4 / Property 11). Sau khi gọi, mọi <see cref="Tick"/>
        /// trả danh sách rỗng — kể cả khi caller tiếp tục gọi.
        /// </summary>
        public void OnGameOver()
        {
            State = WaveState.GameOver;
            _activeEntries.Clear();
            _queue.Clear();
            _carryOver.Clear();
        }

        // ---- Helpers ----

        /// <summary>
        /// Thu các Quái "chưa kịp spawn" của Đợt vừa hết giờ để tích sang Đợt sau
        /// (chỉ dùng ở chế độ time-based). Gồm: (1) phần <c>Remaining</c> còn lại trong
        /// các <see cref="ActiveSpawnEntry"/>, và (2) các <see cref="SpawnIntent"/> còn
        /// kẹt trong <see cref="SpawnQueue"/> do vướng cap. Kết quả nạp vào
        /// <see cref="_carryOver"/> và được <see cref="EnterActive"/> của Đợt kế hợp nhất.
        /// </summary>
        private void CaptureCarryOver()
        {
            // (1) Remaining chưa spawn trong các entry đang hoạt động.
            for (int i = 0; i < _activeEntries.Count; i++)
            {
                var e = _activeEntries[i];
                if (e.Remaining > 0)
                {
                    _carryOver.Add(new ActiveSpawnEntry
                    {
                        EnemyId = e.EnemyId,
                        Remaining = e.Remaining,
                        NextSpawnAt = e.Interval,
                        Interval = e.Interval,
                    });
                }
            }

            // (2) Intent còn kẹt trong hàng đợi (vướng cap). Gom theo EnemyId.
            if (_queue.Count > 0)
            {
                var pending = _queue.Drain(0, int.MaxValue); // rút sạch
                for (int i = 0; i < pending.Count; i++)
                {
                    string id = pending[i].Enemy.Id;
                    bool merged = false;
                    for (int j = 0; j < _carryOver.Count; j++)
                    {
                        if (_carryOver[j].EnemyId == id)
                        {
                            var c = _carryOver[j];
                            c.Remaining += 1;
                            _carryOver[j] = c;
                            merged = true;
                            break;
                        }
                    }
                    if (!merged)
                    {
                        _carryOver.Add(new ActiveSpawnEntry
                        {
                            EnemyId = id,
                            Remaining = 1,
                            NextSpawnAt = 1.0f,
                            Interval = 1.0f,
                        });
                    }
                }
            }
        }

        private WaveConfig GetCurrentWaveConfig()
        {
            // Cyclic indexing để hỗ trợ "vô hạn đợt" (Requirement 7.5) khi cấu hình hữu hạn:
            // Đợt N với N > waves.Count tái sử dụng waves[(N-1) % waves.Count].
            int idx = (CurrentWave - 1) % _waves.Count;
            return _waves[idx];
        }

        private void EnterActive()
        {
            var cfg = GetCurrentWaveConfig();
            _activeEntries.Clear();
            _gateIndex = 0;
            WaveElapsed = 0f;

            // Đội hình Quái cho Đợt: ưu tiên hàm spawnsForWave (mở khóa Loại_Quái dần
            // theo số Đợt, Requirement 7.5/7.6) nếu caller cung cấp; ngược lại dùng
            // cấu hình tĩnh của Đợt (cyclic). Cho phép trả rỗng → Đợt không có entry.
            IReadOnlyList<SpawnEntry> spawns = _spawnsForWaveFn != null
                ? (_spawnsForWaveFn(CurrentWave) ?? Array.Empty<SpawnEntry>())
                : cfg.Spawns;

            // Tổng count cuối cùng cho Đợt: nếu caller cung cấp totalEnemiesForWave,
            // scale tỉ lệ Σ count cấu hình về đúng giá trị đó (giữ nguyên phân bổ
            // giữa các loại Quái). Khi không có hàm, dùng nguyên cấu hình.
            int configuredTotal = 0;
            for (int i = 0; i < spawns.Count; i++) configuredTotal += spawns[i].Count;

            int targetTotal = configuredTotal;
            if (_totalEnemiesForWaveFn != null)
            {
                targetTotal = _totalEnemiesForWaveFn(CurrentWave);
                if (targetTotal < 0) targetTotal = 0;
            }

            // Tính count theo tỉ lệ và bù phần dư cho entry cuối để Σ scaled == targetTotal
            // chính xác. Khi configuredTotal == 0, scheduler không có gì để phát — chấp
            // nhận targetTotal = 0 trong trường hợp này.
            for (int i = 0; i < spawns.Count; i++)
            {
                var entry = spawns[i];
                int scaledCount;
                if (configuredTotal == 0 || targetTotal == configuredTotal)
                {
                    scaledCount = entry.Count;
                }
                else if (i == spawns.Count - 1)
                {
                    // Entry cuối: lấy phần dư để tổng khớp đúng targetTotal.
                    int already = 0;
                    for (int j = 0; j < _activeEntries.Count; j++) already += _activeEntries[j].Remaining;
                    scaledCount = targetTotal - already;
                    if (scaledCount < 0) scaledCount = 0;
                }
                else
                {
                    // (long) để tránh tràn khi count/total lớn.
                    scaledCount = (int)((long)entry.Count * targetTotal / configuredTotal);
                }

                _activeEntries.Add(new ActiveSpawnEntry
                {
                    EnemyId = entry.EnemyId,
                    Remaining = scaledCount,
                    // Spawn đầu tiên fire sau đúng một SpawnIntervalSeconds — nhịp đều
                    // giúp Property 15 đếm tổng spawn = Σ count một cách xác định.
                    NextSpawnAt = entry.SpawnIntervalSeconds,
                    Interval = entry.SpawnIntervalSeconds,
                });
            }

            // Hợp nhất Quái tích lũy từ Đợt trước (chỉ phát sinh ở chế độ time-based khi
            // Đợt trước hết giờ mà chưa spawn hết). Gộp vào entry cùng EnemyId nếu có,
            // ngược lại thêm entry mới — để Đợt này phải xử lý cả phần dồn lại.
            for (int c = 0; c < _carryOver.Count; c++)
            {
                var carry = _carryOver[c];
                if (carry.Remaining <= 0) continue;

                int idx = -1;
                for (int j = 0; j < _activeEntries.Count; j++)
                {
                    if (_activeEntries[j].EnemyId == carry.EnemyId) { idx = j; break; }
                }

                if (idx >= 0)
                {
                    var e = _activeEntries[idx];
                    e.Remaining += carry.Remaining;
                    _activeEntries[idx] = e;
                }
                else
                {
                    _activeEntries.Add(carry);
                }
            }
            _carryOver.Clear();

            State = WaveState.Active;
        }

        private void AdvanceActive(float dt)
        {
            if (dt <= 0f || _activeEntries.Count == 0)
                return;

            var cfg = GetCurrentWaveConfig();
            var gates = cfg.SpawnGates is { Count: > 0 } ? cfg.SpawnGates : _defaultGates;

            for (int i = 0; i < _activeEntries.Count; i++)
            {
                var entry = _activeEntries[i];
                if (entry.Remaining <= 0)
                    continue;

                entry.NextSpawnAt -= dt;
                while (entry.NextSpawnAt <= 0f && entry.Remaining > 0)
                {
                    if (gates.Count == 0)
                        throw new InvalidOperationException(
                            $"Wave {CurrentWave} has no spawn gates configured (WaveConfig.SpawnGates and defaultGates are both empty).");

                    if (!_enemiesById.TryGetValue(entry.EnemyId, out var enemyCfg))
                        throw new KeyNotFoundException(
                            $"Enemy id '{entry.EnemyId}' referenced by Wave {CurrentWave} is not present in enemiesById dictionary.");

                    var gate = gates[_gateIndex % gates.Count];
                    _gateIndex++;

                    _queue.Enqueue(new SpawnIntent(enemyCfg, gate));
                    entry.Remaining--;
                    entry.NextSpawnAt += entry.Interval;
                }

                _activeEntries[i] = entry;
            }
        }

        private bool IsCurrentWaveExhausted(int aliveEnemies)
        {
            if (aliveEnemies > 0) return false;
            if (_queue.Count > 0) return false;
            for (int i = 0; i < _activeEntries.Count; i++)
            {
                if (_activeEntries[i].Remaining > 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Trạng thái mutable cho một <see cref="SpawnEntry"/> trong Đợt đang Active:
        /// đếm ngược tới spawn kế (giây) và số Quái còn cần phát.
        /// </summary>
        private struct ActiveSpawnEntry
        {
            public string EnemyId;
            public int Remaining;
            public float NextSpawnAt;
            public float Interval;
        }
    }
}
