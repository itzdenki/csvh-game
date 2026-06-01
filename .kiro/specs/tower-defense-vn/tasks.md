# Implementation Plan: Tower Defense Việt Nam (CSVH)

## Overview

Triển khai theo trình tự "Core trước, Unity sau" để tận dụng Property-Based Testing (FsCheck) chạy trên `CSVH.Core` thuần C# trước khi đụng đến scene Unity. Mỗi property bất biến trong design được hiện thực thành đúng một sub-task PBT (đánh dấu tùy chọn `*`) đặt sát module mà nó kiểm chứng. Sau khi Core ổn định, lớp Unity (URP 2D, UI Toolkit, prefab, storage, audio, input) được nối dây trong các đợt tiếp theo, kết thúc bằng GameSceneRoot và một smoke test PlayMode chạy hết một Đợt.

Convert the feature design into a series of prompts for a code-generation LLM that will implement each step with incremental progress. Make sure that each prompt builds on the previous prompts, and ends with wiring things together. There should be no hanging or orphaned code that isn't integrated into a previous step. Focus ONLY on tasks that involve writing, modifying, or testing code.

## Tasks

- [x] 1. Set up project structure, assemblies, and core primitives
  - [x] 1.1 Create folder layout and assembly definitions
    - Tạo cây thư mục `Assets/CSVH/{Core,Game,Tests/EditMode,Tests/PlayMode}` theo design
    - Tạo `CSVH.Core.asmdef` (no Unity refs ngoài `Unity.Nuget.Newtonsoft-Json`)
    - Tạo `CSVH.Game.asmdef` tham chiếu `CSVH.Core` + Unity modules
    - Tạo `CSVH.Tests.Edit.asmdef` (NUnit, FsCheck.NUnit) và `CSVH.Tests.Play.asmdef` (Unity Test Framework)
    - _Requirements: 13.1, 13.2_

  - [x] 1.2 Add Newtonsoft.Json and FsCheck.NUnit packages
    - Cài `com.unity.nuget.newtonsoft-json` qua Package Manager
    - Cài `FsCheck` + `FsCheck.NUnit` (NuGetForUnity hoặc tham chiếu DLL trong `Assets/CSVH/Tests/Plugins/`)
    - Xác nhận asmdef tests reference đúng các plugin
    - _Requirements: 10.1, 10.2_

  - [x] 1.3 Implement Result, FieldPoint, FieldGeometry primitives
    - Cài `Result<T,E>` discriminated union trong `CSVH.Core.Common`
    - Cài `readonly record struct FieldPoint(float X, float Y)` với `IsValidSpawnPoint`, `IsValidTowerPoint`
    - Cài `FieldGeometry(HalfWidth, HalfHeight, TowerPosition, TowerCollisionRadius)`
    - _Requirements: 1.1, 1.3_

- [x] 2. Implement configuration layer (waves and enemies JSON)
  - [x] 2.1 Implement EnemyConfig, SpawnEntry, WaveConfig, ConfigBundle records
    - Định nghĩa record bất biến cho `EnemyConfig` (Id, LocalizedName, MaxHp, Speed, MeleeDamage, Resistance, GoldReward, ExpReward, ScoreReward)
    - Định nghĩa `SpawnEntry`, `WaveConfig`, `ConfigBundle` theo design
    - Trang bị `Equals`/value equality cho round-trip test
    - _Requirements: 10.2, 10.5, 10.6_

  - [x] 2.2 Implement ConfigLoader with parse and schema validation
    - Parse JSON bằng Newtonsoft với line/col tracking
    - Validate ràng buộc: `MaxHp>0`, `Speed>0`, `MeleeDamage≥0`, `Resistance≥0`, rewards `≥0`, `WaveNumber≥1`, `PreparationSeconds≥0`, `Count≥0`, `SpawnIntervalSeconds>0`, `BaseDamage≥0`, `RequiredExp>0`, `LevelScale≥1.0`, `SpawnGate.X≤0 ∨ Y≥0`
    - Trả `Result<ConfigBundle, ConfigError>` với `FieldPath`, `Line`, `Column`, `Message`
    - _Requirements: 1.4, 2.6, 3.5, 4.6, 10.1, 10.2, 10.3_

  - [x] 2.3 Implement ConfigWriter with stable pretty-print
    - Pretty-print UTF-8 ổn định (sắp khóa cố định, indent 2 space, newline `\n`)
    - Đảm bảo `Write(Load(s))` ≡ `s` sau chuẩn hóa whitespace
    - _Requirements: 10.4, 10.5, 10.6_

  - [x] 2.4 Property test - Property 1: Round-trip JSON config
    - **Property 1: Round-trip cấu hình JSON**
    - **Validates: Requirements 10.2, 10.4, 10.5, 10.6**

  - [x] 2.5 Property test - Property 2: ConfigLoader rejects invalid fields
    - **Property 2: Bộ_Nạp_Cấu_Hình từ chối trường vi phạm ràng buộc**
    - **Validates: Requirements 1.4, 2.6, 3.5, 4.6, 10.3**

- [x] 3. Implement combat math and projectile logic
  - [x] 3.1 Implement CombatResolver damage formulas
    - `ProjectileDamage(BaseDamage, AttackMultiplier, TargetResistance) = max(0, base*mult - resist)`
    - `MeleeDamageOnTower(MeleeDamage, Armor) = max(0, melee - armor)`
    - `ClampHp(newValue, max) = clamp(newValue, 0, max)`
    - _Requirements: 2.3, 3.3, 5.2, 5.3_

  - [x] 3.2 Implement ProjectileLogic with hit registry and out-of-field cull
    - `TryRegisterHit(enemyId)` chỉ trả `true` lần đầu cho mỗi `enemyId`
    - `IsOutOfField(point, geometry)` so sánh với `HalfWidth`/`HalfHeight`
    - Hỗ trợ `Cull(world)` xóa Đạn ngoài biên (idempotent)
    - _Requirements: 3.4, 3.6_

  - [x] 3.3 Property test - Property 6: Projectile damage formula non-negative
    - **Property 6: Công thức sát thương Đạn lên Quái**
    - **Validates: Requirements 3.3**

  - [x] 3.4 Property test - Property 7: Melee damage on tower non-negative
    - **Property 7: Công thức sát thương Quái lên Thành**
    - **Validates: Requirements 2.3, 5.2**

  - [x] 3.5 Property test - Property 8: Idempotence of out-of-field cull
    - **Property 8: Idempotence của hành động hủy Đạn ngoài biên**
    - **Validates: Requirements 3.4**

  - [x] 3.6 Property test - Property 9: Each projectile damages each enemy at most once
    - **Property 9: Mỗi Đạn gây sát thương cho mỗi Quái tối đa một lần**
    - **Validates: Requirements 3.6**

- [x] 4. Implement wave scheduling, spawn queue, and enemy paths
  - [x] 4.1 Implement SpawnQueue and SpawnIntent
    - FIFO queue chứa `(EnemyConfig, FieldPoint Gate)`
    - Hỗ trợ enqueue khi vượt cap, dequeue khi `aliveEnemies < spawnCap`
    - _Requirements: 7.2, 13.4_

  - [x] 4.2 Implement WaveScheduler core loop
    - State machine: `Loading → Preparing → Active → Cleared → Preparing → ...`, `GameOver` từ bất kỳ đâu
    - `Tick(dt, aliveEnemies, spawnCap=200)` trả `IReadOnlyList<SpawnIntent>` tôn trọng cap
    - `OnWaveCleared()` tăng `CurrentWave` và set `Countdown = PreparationSeconds`
    - `OnGameOver()` chuyển state, sau đó `Tick` luôn trả empty
    - `IsBossWave => CurrentWave % 5 == 0`
    - _Requirements: 7.1, 7.2, 7.4, 7.5, 7.7, 13.4_

  - [x] 4.3 Implement enemy path generation (gate to tower)
    - Hàm pure: `BuildPath(gate, towerPosition)` trả polyline có `path[0]==gate`, `path[last]==towerPosition`
    - `AdvanceAlongPath(position, speed, dt, path)` di chuyển khoảng `speed*dt` dọc đường
    - _Requirements: 2.1, 2.2_

  - [x] 4.4 Property test - Property 3: Spawn gate and tower position invariant
    - **Property 3: Bất biến vị trí Cổng_Spawn và Vị_Trí_Thành**
    - **Validates: Requirements 1.1, 1.3, 2.1**

  - [x] 4.5 Property test - Property 4: Path endpoints at gate and tower
    - **Property 4: Đường_Đi_Quái có hai đầu mút đúng**
    - **Validates: Requirements 2.1**

  - [x] 4.6 Property test - Property 5: Movement step proportional to speed and dt
    - **Property 5: Bước di chuyển tỉ lệ với Tốc_Độ và thời gian**
    - **Validates: Requirements 2.2**

  - [x] 4.7 Property test - Property 15: Wave kinematics, alive caps, prep reset
    - **Property 15: Vận động học wave (model-based)**
    - **Validates: Requirements 7.2, 13.3, 13.4**

  - [x] 4.8 Property test - Property 16: CurrentWave strictly monotonic
    - **Property 16: Số Đợt đơn điệu tăng nghiêm ngặt**
    - **Validates: Requirements 7.4, 7.5**

  - [x] 4.9 Property test - Property 17: Boss-wave predicate and stats
    - **Property 17: Boss-wave predicate**
    - **Validates: Requirements 7.7**

- [x] 5. Implement progression, upgrades, and HP/damage pipeline
  - [x] 5.1 Implement LevelingSystem
    - Constructor validate `baseRequired>0`, `scale≥1.0`
    - `AddExp(amount≥0)` lặp `while CurrentExp ≥ RequiredExp`: trừ, tăng `Level`, `RequiredExp = ⌈RequiredExp*scale⌉`
    - Bất biến: `0 ≤ CurrentExp < RequiredExp`, `Level` không giảm
    - _Requirements: 4.1, 4.2, 4.3, 4.5_

  - [x] 5.2 Implement UpgradeSystem (Armor, Attack, Special tracks)
    - `TryBuy(track, costs)`: nếu `Gold ≥ cost` trừ vàng, tăng level, trả `Bought`; ngược lại `NotEnoughGold`
    - `CurrentArmor = baseArmor + ArmorLevel*armorStep`
    - `CurrentAttackMultiplier = 1 + AttackLevel*attackStep`
    - Tăng `MaxHp` khi mua Giáp tăng `Δ`: `CurrentHp' = CurrentHp + Δ`, `MaxHp' = MaxHp + Δ`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 5.6_

  - [x] 5.3 Implement Special cooldown gating
    - `TryActivateSpecial(...)`: nếu `CooldownRemaining > 0` trả `false`, không thay đổi
    - Khi `CooldownRemaining == 0`: đặt `CooldownMax`, áp hiệu ứng cho mọi Quái trong `Bán_Kính_Special`
    - Tick giảm `CooldownRemaining` theo `dt`, kẹp `≥ 0`
    - _Requirements: 6.6, 6.7_

  - [x] 5.4 Implement HP and damage pipeline tying GameSession
    - Wrap `GameSession` áp dụng `CombatResolver.MeleeDamageOnTower` khi Quái đến Thành
    - Kẹp `CurrentHp ∈ [0, MaxHp]` sau mỗi cập nhật
    - Khi `CurrentHp == 0` gọi `WaveScheduler.OnGameOver()` để chặn spawn
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 5.5 Property test - Property 10: Leveling invariants
    - **Property 10: Bất biến hệ leveling**
    - **Validates: Requirements 4.2, 4.3, 4.5**

  - [x] 5.6 Property test - Property 11: HP bounds and GameOver halts spawning
    - **Property 11: Bất biến giới hạn Máu và dừng spawn khi Kết_Thúc_Trận**
    - **Validates: Requirements 5.3, 5.4**

  - [x] 5.7 Property test - Property 12: Armor upgrade preserves HP/MaxHp invariant
    - **Property 12: Nâng cấp Giáp tăng Máu_Tối_Đa bảo toàn ràng buộc**
    - **Validates: Requirements 5.6**

  - [x] 5.8 Property test - Property 13: Upgrade arithmetic and confluence
    - **Property 13: Số học mua nâng cấp và confluence**
    - **Validates: Requirements 6.2, 6.3, 6.4, 6.5**

  - [x] 5.9 Property test - Property 14: Special cooldown gating
    - **Property 14: Cooldown gating Special**
    - **Validates: Requirements 6.6, 6.7**

- [x] 6. Implement scoring, storage interface, and cultural catalog
  - [x] 6.1 Implement ScoreTracker
    - `AddEnemyKill(reward)`, `AddWaveCompletion(wave, base) → += base*wave`
    - `LoadHighScore(storage)`, `TryFinalize(storage) → HighScore = max(HighScore, SessionScore)`
    - _Requirements: 8.1, 8.2, 8.3, 8.5, 8.6_

  - [x] 6.2 Implement IStorageService and in-memory test impl
    - Interface: `ReadHighScore`, `WriteHighScore`, `ReadVolume(channel)`, `WriteVolume(channel, value)`
    - `VolumeChannel { Music, Sfx }`, `StorageKeys` constants
    - `InMemoryStorageService` cho EditMode tests, clamp volume ∈ [0,1], default volume = 1.0, default highscore = 0
    - _Requirements: 8.6, 12.1, 12.2, 12.3_

  - [x] 6.3 Implement CulturalCatalog record and lookups
    - Record `CulturalCatalog(EnemyNames, SpecialNames, ProjectileNames)`
    - `ContainsEnemy`, `ContainsSpecial` queries; expose `Count` ≥ 5 enemies, ≥ 3 specials
    - _Requirements: 11.1, 11.2_

  - [x] 6.4 Property test - Property 18: SessionScore monotonic and HighScore = max
    - **Property 18: Tích lũy Điểm_Phiên đơn điệu và Kỷ_Lục bằng max**
    - **Validates: Requirements 8.2, 8.3, 8.5**

  - [x] 6.5 Property test - Property 19: HighScore round-trip via storage
    - **Property 19: Round-trip Kỷ_Lục qua Bộ_Lưu_Trữ**
    - **Validates: Requirements 8.6, 12.2**

  - [x] 6.6 Property test - Property 20: Volume round-trip with clamp and default
    - **Property 20: Round-trip âm lượng có clamp và mặc định**
    - **Validates: Requirements 12.1, 12.2, 12.3**

  - [x] 6.7 Property test - Property 22: CulturalCatalog no-orphan no-dangling
    - **Property 22: Bộ_Văn_Hóa no-orphan / no-dangling**
    - **Validates: Requirements 11.1, 11.2**

- [x] 7. Implement HUD formatters and Vietnamese localization
  - [x] 7.1 Implement Format helpers (Wave, Hp, Level, ExpRatio, Countdown)
    - `Format.Wave(N) = $"Đợt {N}/∞"`, `Format.NextWave(N) = $"Đợt kế tiếp: {N+1}"`
    - `Format.Countdown(sec) = $"Đếm ngược: {sec}"`
    - `Format.Hp(cur,max) = $"{cur}/{max}"`, `Format.Level(lvl) = $"Cấp: {lvl}"`
    - `Format.ExpRatio(cur,req) = clamp(cur/req, 0, 1)` khi `req>0`
    - _Requirements: 4.4, 4.5, 5.5, 7.3, 7.6, 8.4_

  - [x] 7.2 Implement Localizer with Vietnamese string bundle
    - `UiStringKeys` enum/constants cho mọi nhãn UI
    - `Localizer.Get(key, "vi")` tra cứu bundle, trả `[?key]` khi thiếu kèm log warning
    - Mọi khóa nhãn người chơi đều có chuỗi UTF-8 không rỗng và chứa ít nhất một ký tự có dấu
    - _Requirements: 11.3_

  - [x] 7.3 Property test - Property 23: HUD formatter strings
    - **Property 23: HUD formatter strings**
    - **Validates: Requirements 4.4, 4.5, 5.5, 7.3, 7.6, 8.4**

  - [x] 7.4 Property test - Property 25: Vietnamese translation completeness
    - **Property 25: Bản dịch tiếng Việt đầy đủ**
    - **Validates: Requirements 11.3**

- [x] 8. Checkpoint - Ensure Core EditMode tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement Unity storage, logging, and audio adapters
  - [x] 9.1 Implement UnityStorageService
    - Volumes → `PlayerPrefs` keys `csvh.volume.music`, `csvh.volume.sfx`, clamp [0,1], default 1.0
    - HighScore → `Application.persistentDataPath/highscore.json` `{"highScore": <long>}`
    - Khi parse fail: ghi đè bằng default `{"highScore":0}`, return 0, log warning qua `ILogSink`
    - Ghi âm lượng phải hoàn tất trong vòng 1 giây (Req 12.1)
    - _Requirements: 8.6, 12.1, 12.2, 12.3, 12.4_

  - [x] 9.2 Implement ILogSink and Unity log adapter
    - Interface `ILogSink` với `Warn`, `Error`, `Info` để test quan sát được (Property 21)
    - `UnityLogSink` chuyển sang `UnityEngine.Debug`
    - _Requirements: 12.4_

  - [x] 9.3 Implement AudioService driven by IStorageService
    - Đọc volume music/sfx khi khởi động, áp dụng vào `AudioMixer` group
    - Đăng ký event khi volume thay đổi để cập nhật mixer
    - Phát BGM nhạc cụ truyền thống (đàn bầu, sáo trúc, trống) trong các Đợt thường
    - _Requirements: 11.5, 12.1, 12.2, 12.3_

  - [x] 9.4 Property test - Property 21: Corrupt storage falls back to default and logs warning
    - **Property 21: Dữ liệu Bộ_Lưu_Trữ hỏng → fallback mặc định**
    - **Validates: Requirements 12.4**

- [x] 10. Author Unity data assets and configure URP 2D scene
  - [x] 10.1 Configure URP 2D camera and sorting axis
    - Đặt camera orthographic, gán Renderer2D đã có ở `Assets/Settings/Renderer2D.asset`
    - `Project Settings → Graphics → Custom Axis = (0, 1, -0.5)` cho 2.5D sorting
    - Tilemap Isometric Z-As-Y cho nền cỏ; đảm bảo sorting: ground < tower < projectile
    - _Requirements: 1.2, 1.5_

  - [x] 10.2 Author UpgradeTable.asset (ScriptableObject) with cost steps
    - Định nghĩa `UpgradeTableSO : ScriptableObject` triển khai `IUpgradeCostTable` từ Core
    - Điền `BaseArmor`, `ArmorStep`, `AttackStep`, `SpecialStep`, `BaseCost`, `CostGrowth`
    - _Requirements: 6.2, 6.4, 6.5_

  - [x] 10.3 Author CulturalCatalog.asset with VN-themed names
    - ScriptableObject mirror của `CulturalCatalog`
    - ≥ 5 tên Quái (Thuồng_Luồng, Quân_Tống, Quân_Nguyên_Mông, Hồ_Tinh, Mộc_Tinh, ...)
    - ≥ 3 Special (Trống_Đồng_Đông_Sơn, Lưỡi_Gươm_Lê_Lợi, Mũi_Tên_An_Dương_Vương)
    - Hoa văn / palette trống đồng Đông Sơn cho icon
    - _Requirements: 11.1, 11.2, 11.4_

  - [x] 10.4 Author waves.json and enemies.json under StreamingAssets
    - Tạo `Assets/StreamingAssets/waves.json` với ít nhất 5 Đợt mẫu (Đợt 5 là boss)
    - Tạo `Assets/StreamingAssets/enemies.json` với ≥ 5 Loại_Quái khác biệt Tốc_Độ/Máu/Sát_Thương
    - Mọi `SpawnGate` thỏa `X≤0 ∨ Y≥0`; load qua ConfigLoader phải `Result.Ok`
    - _Requirements: 2.5, 7.1, 10.1, 10.2_

- [x] 11. Implement gameplay views (Enemy, Projectile, Spawner, Tower)
  - [x] 11.1 Implement EnemyView
    - MonoBehaviour pathfinding theo waypoint (path từ Core), tốc độ từ `EnemyConfig`
    - Trừ HP khi nhận damage, raise `OnKilled(reward)` khi `Hp ≤ 0`
    - Khi chạm Thành: gửi sự kiện đánh cận chiến rồi tự hủy
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 11.2 Implement ProjectileView
    - Di chuyển theo `Vận_Tốc`, sử dụng `Trigger2D` phát hiện Quái
    - Gọi `ProjectileLogic.TryRegisterHit(enemyId)` rồi áp `CombatResolver.ProjectileDamage`
    - Tự hủy khi `IsOutOfField` hoặc đã chạm Quái (cùng frame)
    - Cap số Đạn đồng thời ≤ 500
    - _Requirements: 3.2, 3.3, 3.4, 3.6, 13.3_

  - [x] 11.3 Implement EnemySpawnerView
    - Subscribe `WaveScheduler.Tick` outputs, instantiate prefab tại `gate.WorldPosition`
    - Gắn `EnemyView` với stats từ `EnemyConfig`, register vào alive list
    - Tôn trọng cap 200 alive (Req 13.4)
    - _Requirements: 2.1, 2.5, 7.1, 13.4_

  - [x] 11.4 Implement TowerView
    - Tự động bắn theo nhịp `Tốc_Độ_Bắn`
    - Chọn Mục_Tiêu gần nhất qua `Physics2D.OverlapCircleNonAlloc`
    - Instantiate `ProjectileView` với hướng từ `TowerPosition` đến `target.position`
    - Layer order: cao hơn ground, thấp hơn projectile fx
    - _Requirements: 1.5, 3.1, 3.2_

- [x] 12. Implement HUD UI Toolkit and input
  - [x] 12.1 Implement HUDController (UIDocument with six anchored regions)
    - UXML/USS với 6 vùng: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight
    - Bind `HudSnapshot` qua callback `Action<HudSnapshot>`
    - TopCenter: `Format.Wave` + `Format.NextWave` + `Format.Countdown`
    - TopRight: `Điểm: {SessionScore}` + `Cao nhất: {HighScore}`
    - BottomLeft: `Format.Level` + vòng tiến trình EXP với `Format.ExpRatio`
    - BottomCenter: `Format.Hp` + 4 icon Công/Giáp/Special/EXP
    - Phản hồi nhấn icon ≤ 100 ms (Req 13.2)
    - _Requirements: 4.4, 4.5, 5.5, 6.8, 7.3, 7.6, 8.4, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 13.2_

  - [x] 12.2 Implement UpgradeIconView for Công/Giáp/Special/EXP
    - VisualElement riêng cho mỗi icon, hiển thị giá hiện tại và Thời_Gian_Hồi (Special)
    - Toast "Không đủ Vàng" khi `TryBuy` trả `NotEnoughGold`
    - Nhấp nháy icon Special khi đang cooldown
    - _Requirements: 6.3, 6.7, 6.8_

  - [x] 12.3 Implement InputService bridging InputSystem_Actions
    - Map các action từ `Assets/InputSystem_Actions.inputactions` đã có sẵn
    - Bấm icon nâng cấp → `UpgradeSystem.TryBuy`
    - Phím tắt kích hoạt Special → `TryActivateSpecial`
    - _Requirements: 6.2, 6.6, 13.2_

  - [x] 12.4 PlayMode property test - Property 24: HUD anchor regions across resolutions
    - **Property 24: HUD giữ vùng anchor khi đổi độ phân giải**
    - **Validates: Requirements 9.7**

- [x] 13. Implement scene bootstrap and integration
  - [x] 13.1 Implement GameSceneRoot bootstrap
    - Load `waves.json` + `enemies.json` qua `ConfigLoader`; nếu lỗi, hiển thị màn "Cấu hình lỗi" với chi tiết
    - Khởi tạo `WaveScheduler`, `LevelingSystem`, `UpgradeSystem`, `ScoreTracker` từ `UpgradeTable.asset`
    - Đăng ký `EnemySpawnerView`, `TowerView`, `HUDController`, `AudioService`, `InputService`
    - Tick logic mỗi frame: forward `Time.deltaTime` cho Core, push `HudSnapshot` cho HUD
    - _Requirements: 5.1, 7.1, 8.1, 9.1, 10.1, 10.3_

  - [x] 13.2 Wire ScoreTracker.Finalize on GameOver into UnityStorageService
    - Khi `WaveState == GameOver`: gọi `ScoreTracker.TryFinalize(unityStorage)`
    - Hiển thị màn hình kết thúc với Điểm_Phiên và Kỷ_Lục mới
    - _Requirements: 5.4, 8.5, 8.6_

  - [x] 13.3 PlayMode integration test - one wave end-to-end
    - Smoke: scene load → Đợt 1 spawn → Quái bị tiêu diệt → Đợt cleared → tăng `CurrentWave`
    - Kiểm tra HUD region presence, sorting orders, BGM tag `traditional`
    - _Requirements: 1.5, 2.4, 7.2, 7.4, 11.5_

- [x] 14. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP. Core implementation tasks (no `*`) MUST be completed.
- Mỗi sub-task PBT bám đúng Property tương ứng (P1-P25) trong design; comment trong code phải có tag `// Feature: tower-defense-vn, Property {n}: {tóm tắt}` và đặt ngưỡng `[Property(MaxTest = 100)]` trở lên.
- Property tests đặt sát module hiện thực để bắt lỗi sớm; unit example tests bổ sung cho trạng thái khởi tạo (4.1, 5.1, 7.1, 8.1) và format edge cases.
- Checkpoint 8 chốt Core trước khi đụng Unity; checkpoint 14 chốt cả EditMode + PlayMode.
- Danh sách yêu cầu được trích chéo theo điều khoản chi tiết (vd. `5.6`, `7.7`) để dễ truy vết.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["2.1", "3.1", "3.2", "5.1", "5.2", "6.1", "6.2", "6.3", "7.1"] },
    { "id": 3, "tasks": ["2.2", "2.3", "4.1", "5.3", "7.2"] },
    { "id": 4, "tasks": ["4.2", "4.3"] },
    { "id": 5, "tasks": ["5.4"] },
    { "id": 6, "tasks": ["2.4", "2.5", "3.3", "3.4", "3.5", "3.6", "4.4", "4.5", "4.6", "4.7", "4.8", "4.9", "5.5", "5.6", "5.7", "5.8", "5.9", "6.4", "6.5", "6.6", "6.7", "7.3", "7.4"] },
    { "id": 7, "tasks": ["9.1", "9.2", "10.1", "10.2", "10.3", "10.4"] },
    { "id": 8, "tasks": ["9.3", "9.4"] },
    { "id": 9, "tasks": ["11.1", "11.2"] },
    { "id": 10, "tasks": ["11.3", "11.4"] },
    { "id": 11, "tasks": ["12.1", "12.2", "12.3"] },
    { "id": 12, "tasks": ["13.1", "12.4"] },
    { "id": 13, "tasks": ["13.2", "13.3"] }
  ]
}
```
