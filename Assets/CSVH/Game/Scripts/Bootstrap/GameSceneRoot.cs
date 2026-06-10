// Feature: tower-defense-vn, Task 13.1 - GameSceneRoot bootstrap
// Feature: tower-defense-vn, Task 13.2 - Wire ScoreTracker.Finalize on GameOver into UnityStorageService
// Validates: Requirements 5.1, 5.4, 7.1, 8.1, 8.5, 8.6, 9.1, 10.1, 10.3

using System.Collections.Generic;
using System.IO;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Logging;
using CSVH.Core.Progression;
using CSVH.Core.Wave;
using CSVH.Game.Audio;
using CSVH.Game.Data;
using CSVH.Game.Input;
using CSVH.Game.Logging;
using CSVH.Game.Spawning;
using CSVH.Game.Storage;
using CSVH.Game.Tower;
using CSVH.Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace CSVH.Game.Bootstrap
{
    /// <summary>
    /// Composition root cho scene chính: nạp <c>waves.json</c> + <c>enemies.json</c>
    /// qua <see cref="ConfigLoader"/>, dựng các hệ Core (<see cref="WaveScheduler"/>,
    /// <see cref="LevelingSystem"/>, <see cref="UpgradeSystem"/>, <see cref="ScoreTracker"/>,
    /// <see cref="SpecialAbility"/>) bó trong một <see cref="CSVH.Core.Game.GameSession"/>,
    /// rồi nối dây các view MonoBehaviour (<see cref="EnemySpawnerView"/>,
    /// <see cref="TowerView"/>, <see cref="HUDController"/>, <see cref="AudioService"/>,
    /// <see cref="InputService"/>).
    ///
    /// <para>
    /// Mỗi frame trong <see cref="Update"/>: forward <see cref="Time.deltaTime"/> cho
    /// <see cref="CSVH.Core.Game.GameSession.Tick"/> (cập nhật cooldown Special) và
    /// <see cref="WaveScheduler.Tick"/> (sinh <see cref="SpawnIntent"/> theo cap quái sống),
    /// đồng thời đẩy một <see cref="HudSnapshot"/> mới cho HUD (Requirement 9.1).
    /// </para>
    ///
    /// <para>
    /// Khi <see cref="ConfigLoader.Load"/> thất bại, hiển thị màn "Cấu hình lỗi" qua
    /// <see cref="_configErrorScreen"/> hoặc <see cref="HudToast"/> kèm <see cref="ConfigError"/>
    /// (Requirement 10.1, 10.3) và bỏ qua phần khởi tạo gameplay — Update sớm thoát vì
    /// <c>_session</c>/<c>_scheduler</c> còn null.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("CSVH/Bootstrap/Game Scene Root")]
    public sealed class GameSceneRoot : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private UpgradeTableSO _upgradeTable;

        [Tooltip("Bảng 9 nâng cấp TRONG TRẬN (Sát Thương / Tốc Đánh / Chí Mạng / … / Nỏ Độc). " +
                 "Bỏ trống thì icon Công/Giáp mở bảng nâng cấp 3 nhánh cũ.")]
        [SerializeField] private MatchUpgradeTableSO _matchUpgradeTable;

        [Tooltip("Icon cho 10 thẻ nâng cấp trong trận, THEO THỨ TỰ enum MatchUpgradeKind: " +
                 "Damage, AttackSpeed, CritChance, CritDamage, ProjectileSpeed, FortifiedBase, " +
                 "BaseRegen, IceArrow, PoisonArrow, GoldRush. Bỏ trống phần tử nào thì thẻ đó ẩn icon.")]
        [SerializeField] private Sprite[] _matchUpgradeIcons = new Sprite[10];
        [SerializeField] private CulturalCatalogSO _culturalCatalog;
        [SerializeField] private string _wavesFileName = "waves.json";
        [SerializeField] private string _enemiesFileName = "enemies.json";

        [Header("Views")]
        [SerializeField] private HUDController _hud;
        [SerializeField] private InputService _input;
        [SerializeField] private AudioService _audio;
        [SerializeField] private TowerView _tower;
        [SerializeField] private EnemySpawnerView _spawner;
        [SerializeField] private GameObject _configErrorScreen;

        [Header("Game Over Screen")]
        [Tooltip("Root GameObject của màn 'Kết thúc trận đấu'. Bật khi WaveState chuyển sang GameOver (Requirement 5.4).")]
        [SerializeField] private GameObject _gameOverScreen;
        [Tooltip("UIDocument tùy chọn để cập nhật Label trong UI Toolkit. Bỏ trống nếu dùng prefab uGUI/Text bằng tay.")]
        [SerializeField] private UnityEngine.UIElements.UIDocument _gameOverDocument;
        [Tooltip("Tên Label tiêu đề trong UIDocument (mặc định 'GameOverTitle').")]
        [SerializeField] private string _gameOverTitleLabelName = "GameOverTitle";
        [Tooltip("Tên Label hiển thị Điểm phiên (mặc định 'GameOverScore').")]
        [SerializeField] private string _gameOverScoreLabelName = "GameOverScore";
        [Tooltip("Tên Label hiển thị Kỷ lục (mặc định 'GameOverHighScore').")]
        [SerializeField] private string _gameOverHighScoreLabelName = "GameOverHighScore";

        [Header("Field Geometry")]
        [Tooltip("Vị_Trí_Thành (X > 0, Y < 0 — góc Đông Nam, Requirement 1.1).")]
        [SerializeField] private Vector2 _towerPosition = new Vector2(1f, -1f);

        [Tooltip("Vùng spawn Quái (SpriteRenderer của SpawnZone trên nền). Khi gán, MỌI Quái " +
                 "spawn tại điểm ngẫu nhiên trong bounds của sprite này thay vì cổng trong waves.json.")]
        [SerializeField] private SpriteRenderer _spawnZoneArea;
        [SerializeField] private float _halfWidth = 10f;
        [SerializeField] private float _halfHeight = 10f;
        [SerializeField] private float _towerCollisionRadius = 0.5f;
        [SerializeField] private int _initialMaxHp = 100;
        [SerializeField] private int _initialGold = 50;

        [Header("Leveling / Special")]
        [SerializeField] private int _baseRequiredExp = 100;
        [SerializeField] private float _levelScale = 1.5f;
        [Tooltip("Bảng tham số 3 skill Special (Trống Đồng / Mũi Tên / Lưỡi Gươm). Bỏ trống thì Special bị tắt.")]
        [SerializeField] private SpecialSkillTableSO _specialSkillTable;

        [Header("Meta (Xu cổ — GDD Cơ chế 2)")]
        [Tooltip("Bảng nâng cấp VĨNH VIỄN mua bằng Xu cổ (Máu Cổng / Sát thương Nỏ / Giảm hồi chiêu). " +
                 "Bỏ trống thì tắt hệ META: Quái không rớt Xu, không có bonus, không hiện Cửa Hàng.")]
        [SerializeField] private MetaUpgradeTableSO _metaUpgradeTable;

        [Header("Wave Scheduler")]
        [Tooltip("Cap số Quái sống đồng thời (Requirement 13.4).")]
        [SerializeField] private int _spawnCap = 200;
        [Tooltip("Thời gian đếm ngược của mỗi Đợt (giây). Hết giờ thì sang Đợt kế; Quái chưa kịp spawn được tích sang Đợt sau. 0 = tắt (Đợt kết thúc khi sạch Quái).")]
        [SerializeField] private float _waveDurationSeconds = 60f;
        [Tooltip("Khi dọn sạch Quái trước khi hết giờ, chờ thêm số giây này rồi tự skip sang Đợt kế (chỉ áp dụng ở chế độ đếm ngược).")]
        [SerializeField] private float _earlyClearGraceSeconds = 5f;
        [Tooltip("Nối thẳng các Đợt, bỏ Pha_Chuẩn_Bị (cả đầu trận lẫn giữa các Đợt); Quái spawn tức thì, HUD không hiện 'Đợt kế tiếp'/'Đếm ngược'.")]
        [SerializeField] private bool _startWaveImmediately = true;

        private ILogSink _log;
        private UnityStorageService _storage;
        private CSVH.Core.Game.GameSession _session;
        private WaveScheduler _scheduler;
        private FieldGeometry _geometry;
        private bool _configFailed;
        private bool _gameOverHandled;
        private float _savedTimeScale = 1f;
        private IRandom _rng;
        private MetaProgressionState _metaState;
        // Hệ 9 nâng cấp TRONG TRẬN; null khi không gán bảng (_matchUpgradeTable).
        private MatchUpgradeSystem _matchUpgrades;
        // HP tối đa lúc VÀO trận (gồm bonus META) — mốc tính Δ +5%/cấp của Cường Hóa Thành.
        private int _sessionInitialMaxHp;
        // Phần lẻ HP hồi tích lũy của Hồi Phục Thành (GameSession.Heal nhận số nguyên).
        private float _regenCarry;
        // Tên hiển thị của boss (từ enemies.json) cho dòng "BOSS: <tên>" ở Đợt 21.
        private string _bossDisplayName;
        // Đã hỏi "chơi tiếp?" sau Đợt 21 chưa (chỉ hỏi đúng một lần).
        private bool _continuePromptShown;
        // Người chơi chủ động kết thúc sau khi thắng chương → màn kết hiện "Chiến thắng!".
        private bool _victoryEnd;
        // Cạnh lên của trạng thái "đếm ngược sang Đợt kế" để thu Xu đúng một lần mỗi đợt.
        private bool _prevInCountdown;

        private void Start()
        {
            _log = new UnityLogSink();

            var loadResult = LoadConfigs();
            if (loadResult.IsErr)
            {
                ShowConfigError(loadResult.Error);
                return;
            }
            var bundle = loadResult.Value;

            // Requirement 1.1: dựng FieldGeometry với tower trong góc Đông Nam.
            _geometry = new FieldGeometry(
                _halfWidth,
                _halfHeight,
                new FieldPoint(_towerPosition.x, _towerPosition.y),
                _towerCollisionRadius);

            // WaveScheduler cần tra cứu EnemyConfig theo Id.
            var enemiesById = new Dictionary<string, EnemyConfig>(bundle.Enemies.Count);
            for (int i = 0; i < bundle.Enemies.Count; i++)
            {
                enemiesById[bundle.Enemies[i].Id] = bundle.Enemies[i];
            }
            _scheduler = new WaveScheduler(
                bundle.Waves,
                enemiesById,
                defaultGates: null,
                // Tổng số Quái mỗi Đợt do TotalEnemiesForWave quyết định (Đợt boss chỉ 1 con);
                // scheduler scale Σ trọng số của BuildWaveSpawns về đúng giá trị này.
                totalEnemiesForWave: TotalEnemiesForWave,
                // Mỗi Đợt đếm ngược _waveDurationSeconds giây; hết giờ sang Đợt kế và
                // tích Quái chưa kịp spawn sang Đợt sau.
                waveDurationSeconds: _waveDurationSeconds,
                // Dọn sạch Quái sớm thì chờ _earlyClearGraceSeconds giây rồi skip.
                earlyClearGraceSeconds: _earlyClearGraceSeconds,
                // Vào Đợt 1 ngay khi khởi động (bỏ Pha_Chuẩn_Bị đầu trận).
                startActiveImmediately: _startWaveImmediately,
                // Đội hình mở khóa dần theo Chương 1: 1-5 Mọt Gỗ; 6-10 +Bù Nhìn Rơm;
                // 11-15 +Gốc Cây Ma; 16-20 +Quạ Đen; 21 boss Mộc Tinh (xem BuildWaveSpawns).
                spawnsForWave: BuildWaveSpawns);

            var leveling = new LevelingSystem(_baseRequiredExp, _levelScale);
            var upgrades = new UpgradeSystem(initialGold: _initialGold);
            var score = new ScoreTracker();
            _rng = new SystemRandom();

            _storage = new UnityStorageService(_log);
            score.LoadHighScore(_storage);

            // GDD Cơ chế 2: nạp tiến trình META (Xu cổ) đã lưu và quy ra hiệu ứng vĩnh viễn
            // áp cho TRẬN MỚI này. Bỏ trống bảng meta ⇒ tắt hệ META (MetaBonuses.None).
            _metaState = _metaUpgradeTable != null
                ? new MetaProgressionState(_storage.ReadMetaProgress(), _metaUpgradeTable)
                : null;
            var metaBonuses = _metaState != null ? _metaState.Bonuses : MetaBonuses.None;

            // Hồi chiêu Ultimate giảm theo nâng cấp META (cooldownScale ≤ 1).
            var specialSkills = _specialSkillTable != null
                ? new SpecialSkillSystem(_specialSkillTable, metaBonuses.CooldownScale)
                : null;

            // Máu Cổng khởi đầu = cấu hình cơ bản + bonus META "Máu Cổng".
            int initialMaxHp = _initialMaxHp + metaBonuses.GateHpBonus;
            _sessionInitialMaxHp = initialMaxHp;

            // Hệ 9 nâng cấp TRONG TRẬN (reset mỗi trận). Bỏ trống bảng ⇒ tắt, dùng UI cũ.
            _matchUpgrades = _matchUpgradeTable != null
                ? new MatchUpgradeSystem(_matchUpgradeTable)
                : null;

            // Requirement 5.1, 7.1, 8.1: GameSession bó các hệ Core lại cho view layer.
            _session = new CSVH.Core.Game.GameSession(
                initialMaxHp,
                _upgradeTable,
                _scheduler,
                leveling,
                upgrades,
                score,
                specialSkills);

            // Sát thương Nỏ cơ bản tăng theo bonus META "Sát thương Nỏ".
            WireViews(upgrades, specialSkills, metaBonuses.CrossbowDamageBonus);
            ConfigureSpawnerAbilities(enemiesById);
            WireHudInputBridge();

            // Tên hiển thị boss cho dòng "BOSS: <tên>" ở Đợt 21 (lấy từ enemies.json).
            _bossDisplayName = enemiesById.TryGetValue(EnemyBoss, out var bossConfig)
                ? bossConfig.LocalizedName
                : EnemyBoss;

            _scheduler.Start();
        }

        // Chương 1 — Làng Quê Thanh Bình. Id ASCII (tên hiển thị tiếng Việt nằm ở enemies.json):
        //   Runner  = Mọt Gỗ      — chạy nhanh, máu thấp
        //   Fighter = Bù Nhìn Rơm — quái tiêu chuẩn
        //   Tank    = Gốc Cây Ma  — máu cao, chậm
        //   Special = Quạ Đen     — bay, tốc độ cao (còn trồi lên từ xác Fighter/Tank)
        //   Boss    = Mộc Tinh    — boss chương, "hóa khô" khi còn 10% máu
        private const string EnemyRunner = "Mot_Go";
        private const string EnemyFighter = "Bu_Nhin_Rom";
        private const string EnemyTank = "Goc_Cay_Ma";
        private const string EnemySpecial = "Qua_Den";
        private const string EnemyBoss = "Moc_Tinh";

        // Số Đợt của chương 1; Đợt cuối (21) là boss. Sau Đợt 21, nếu người chơi đồng ý
        // "chơi tiếp", trận chuyển sang chế độ VÔ TẬN (Đợt 22+) với đội hình ngẫu nhiên.
        private const int ChapterWaveCount = 21;

        // Đợt Quạ Đen mở khóa theo tiến trình chương — trước Đợt này Quạ Đen không xuất hiện,
        // KỂ CẢ qua cơ chế trồi lên từ xác Bù Nhìn Rơm / Gốc Cây Ma.
        private const int CrowUnlockWave = 16;

        // Chế độ vô tận: số Quái tăng theo Đợt cho tới Đợt này; từ đó trở đi giữ nguyên.
        private const int EndlessEnemyCapWave = 35;

        // Xác suất Quạ Đen trồi lên khi Bù Nhìn Rơm / Gốc Cây Ma bị tiêu diệt.
        private const float CrowDeathSpawnChance = 0.25f;

        // Xác suất boss Mộc Tinh góp mặt trong một Đợt vô tận.
        private const float EndlessBossChance = 0.15f;

        /// <summary>
        /// Tổng số Quái cho Đợt: Đợt 1→20 tăng dần 5..24; Đợt boss (21) chỉ một mình Mộc Tinh;
        /// Đợt vô tận (22+) tiếp tục quy luật <c>5 + (Đợt − 1)</c> nhưng từ Đợt
        /// <see cref="EndlessEnemyCapWave"/> (35) trở đi giữ nguyên (= 39 con).
        /// </summary>
        private int TotalEnemiesForWave(int wave)
        {
            if (wave < 1) wave = 1;

            if (wave == ChapterWaveCount)
            {
                return 1;
            }

            int capped = Mathf.Min(wave, EndlessEnemyCapWave);
            return 5 + (capped - 1);
        }

        /// <summary>
        /// Đội hình Đợt theo tiến trình mở khóa Chương 1:
        /// <list type="bullet">
        ///   <item>Đợt 1-5: chỉ Mọt Gỗ (Runner).</item>
        ///   <item>Đợt 6-10: thêm Bù Nhìn Rơm (Fighter).</item>
        ///   <item>Đợt 11-15: thêm Gốc Cây Ma (Tank).</item>
        ///   <item>Đợt 16-20: thêm Quạ Đen (Special).</item>
        ///   <item>Đợt 21: chỉ Mộc Tinh (Boss).</item>
        ///   <item>Đợt 22+ (Vô Tận): đội hình NGẪU NHIÊN từ cả 5 loại, kể cả boss.</item>
        /// </list>
        /// Trọng số giảm dần theo độ mạnh để Quái yếu đông hơn; Runner đặt CUỐI để nhận phần dư
        /// khi scheduler scale tổng về <see cref="TotalEnemiesForWave"/>.
        /// </summary>
        private IReadOnlyList<SpawnEntry> BuildWaveSpawns(int wave)
        {
            if (wave > ChapterWaveCount)
            {
                return BuildEndlessSpawns();
            }

            if (wave == ChapterWaveCount)
            {
                return new List<SpawnEntry> { new SpawnEntry(EnemyBoss, 1, 3f) };
            }

            var spawns = new List<SpawnEntry>(4);
            if (wave >= CrowUnlockWave) spawns.Add(new SpawnEntry(EnemySpecial, 1, 2.2f));
            if (wave >= 11) spawns.Add(new SpawnEntry(EnemyTank, 2, 2.6f));
            if (wave >= 6) spawns.Add(new SpawnEntry(EnemyFighter, 3, 1.6f));
            spawns.Add(new SpawnEntry(EnemyRunner, 4, 1.0f)); // Runner cuối → nhận phần dư.
            return spawns;
        }

        /// <summary>
        /// Đội hình NGẪU NHIÊN cho chế độ Vô Tận (Đợt 22+): mỗi loại Quái có xác suất góp mặt
        /// riêng, boss Mộc Tinh hiếm (<see cref="EndlessBossChance"/>) và trọng số nhỏ để sau
        /// khi scheduler scale chỉ ra 1-2 con. Runner luôn có mặt và đặt cuối để nhận phần dư.
        /// </summary>
        private IReadOnlyList<SpawnEntry> BuildEndlessSpawns()
        {
            var spawns = new List<SpawnEntry>(5);

            if (_rng.NextDouble() < EndlessBossChance)
            {
                spawns.Add(new SpawnEntry(EnemyBoss, 1, 6f));
            }
            if (_rng.NextDouble() < 0.6)
            {
                spawns.Add(new SpawnEntry(EnemySpecial, EndlessCount(1, 3), 2.2f));
            }
            if (_rng.NextDouble() < 0.7)
            {
                spawns.Add(new SpawnEntry(EnemyTank, EndlessCount(2, 4), 2.6f));
            }
            if (_rng.NextDouble() < 0.85)
            {
                spawns.Add(new SpawnEntry(EnemyFighter, EndlessCount(3, 6), 1.6f));
            }
            spawns.Add(new SpawnEntry(EnemyRunner, 4, 1.0f));
            return spawns;
        }

        /// <summary>Trọng số ngẫu nhiên nguyên trong <c>[min, max]</c> cho đội hình vô tận.</summary>
        private int EndlessCount(int min, int max)
            => min + (int)(_rng.NextDouble() * (max - min + 1));

        /// <summary>
        /// Inject phụ thuộc vào các view MonoBehaviour. View nào chưa gán (null) sẽ bị
        /// bỏ qua nhẹ nhàng để scene tối thiểu vẫn chạy được trong test/headless.
        /// </summary>
        private void WireViews(UpgradeSystem upgrades, SpecialSkillSystem specialSkills, float towerExtraBaseDamage)
        {
            if (_spawner != null)
            {
                // Forward sự kiện Quái → GameSession: chạm Thành trừ máu (Req 2.3),
                // bị tiêu diệt cộng thưởng (Req 2.4). Trước đây thiếu cầu nối này nên
                // Quái tự hủy khi tới Thành mà Máu Thành không đổi.
                _spawner.Initialize(
                    _geometry,
                    onReachedTower: config => _session?.OnEnemyReachedTower(config.MeleeDamage),
                    onEnemyKilled: HandleEnemyKilled);
            }

            if (_tower != null)
            {
                // Thành chỉ bắn khi còn Quái sống của đợt; hết Quái thì ngừng bắn.
                _tower.Initialize(
                    _geometry,
                    upgrades,
                    _upgradeTable,
                    canFire: () => _spawner != null && _spawner.AliveCount > 0,
                    extraBaseDamage: towerExtraBaseDamage,
                    matchUpgrades: _matchUpgrades);

                // Bind thanh máu Thành (nếu Tower GameObject có gắn TowerHealthBarView).
                // Pattern: lookup component cùng GameObject để tránh thêm SerializeField mới.
                var towerHpBar = _tower.GetComponent<TowerHealthBarView>();
                if (towerHpBar != null)
                {
                    towerHpBar.Bind(
                        hpGetter: () => _session.CurrentHp,
                        maxHpGetter: () => _session.MaxHp);
                }
            }

            if (_audio != null)
            {
                _audio.Initialize(_storage);
            }

            if (_input != null)
            {
                _input.Bind(upgrades, _upgradeTable, specialSkills, _rng);
            }
        }

        /// <summary>
        /// Bật kỹ năng đặc biệt Chương 1 cho spawner: Quạ Đen trồi lên từ xác Bù Nhìn Rơm /
        /// Gốc Cây Ma (xác suất <see cref="CrowDeathSpawnChance"/>), và bật "hóa khô" cho boss
        /// Mộc Tinh. No-op nếu thiếu spawner hoặc roster không có Quạ Đen.
        /// </summary>
        private void ConfigureSpawnerAbilities(IReadOnlyDictionary<string, EnemyConfig> enemiesById)
        {
            if (_spawner == null || enemiesById == null)
            {
                return;
            }

            if (enemiesById.TryGetValue(EnemySpecial, out var crowConfig))
            {
                _spawner.ConfigureDeathSpawn(
                    crowConfig,
                    new HashSet<string> { EnemyFighter, EnemyTank },
                    CrowDeathSpawnChance,
                    // Quạ Đen chỉ mở khóa từ Đợt 16: trước đó không trồi lên từ xác —
                    // nếu không, người chơi gặp Quạ ngay Đợt 6 khi Bù Nhìn Rơm đầu tiên chết.
                    eligible: () => _scheduler != null && _scheduler.CurrentWave >= CrowUnlockWave);
            }

            _spawner.SetEnrageEnemy(EnemyBoss);
        }

        /// <summary>
        /// Quái bị tiêu diệt: cộng EXP/Điểm/Xu cổ NGAY, còn Phần_Thưởng_Vàng được "gói" vào một
        /// đồng Xu RƠI TẠI CHỖ Quái chết và nằm yên trên Sân_Đấu. Toàn bộ Xu chỉ chảy về ô Vàng
        /// (và cộng Vàng) khi vào đợt đếm ngược sang Đợt kế — xem <see cref="MaybeCollectCoins"/>.
        /// Thiếu HUD (test/headless) thì cộng Vàng ngay để không mất thưởng.
        /// </summary>
        private void HandleEnemyKilled(EnemyConfig config, Vector3 worldPos)
        {
            if (_session == null || config == null)
            {
                return;
            }

            // EXP/Điểm/Xu cổ tức thì; HOÃN Vàng — Xu rơi tại chỗ, chờ thu cuối Đợt.
            _session.OnEnemyKilled(config, creditGoldNow: false);

            int gold = config.GoldReward;

            // Nâng cấp Kinh Tế "Hoàng Kim": roll mỗi lần hạ gục; trúng thì cộng thêm
            // GoldRushBonusFraction × Vàng rơi (mặc định +100% → gấp đôi).
            if (gold > 0 && _matchUpgrades != null && _rng != null)
            {
                float chance = _matchUpgrades.GoldRushChance;
                if (chance > 0f && _rng.NextDouble() < chance)
                {
                    long boosted = gold + (long)Mathf.Round(gold * _matchUpgrades.GoldRushBonusFraction);
                    gold = boosted > int.MaxValue ? int.MaxValue : (int)boosted;
                }
            }

            if (gold <= 0)
            {
                return;
            }

            if (_hud != null)
            {
                _hud.DropGoldCoin(worldPos, () => _session?.AddGold(gold));
            }
            else
            {
                _session.AddGold(gold);
            }
        }

        /// <summary>
        /// Khi vừa BƯỚC VÀO đợt đếm ngược sang Đợt kế (dọn sạch sớm "Đã dọn sạch! Đợt kế sau…",
        /// hoặc Pha_Chuẩn_Bị), cho toàn bộ Xu đang nằm trên sân chảy về ô Vàng đúng một lần.
        /// Phát hiện bằng cạnh lên của trạng thái đếm ngược để không gọi lặp mỗi frame.
        /// </summary>
        private void MaybeCollectCoins()
        {
            bool inCountdown = _scheduler != null
                && (_scheduler.IsEarlyClearPending || _scheduler.State == WaveState.Preparing);

            if (inCountdown && !_prevInCountdown)
            {
                _hud?.CollectAllCoins();
            }
            _prevInCountdown = inCountdown;
        }

        /// <summary>
        /// Cầu HUD ↔ InputService: nhấp icon HUD chạy cùng đường dẫn như phím tắt
        /// (Requirement 13.2). Khi mua Giáp thành công, cộng <see cref="UpgradeTableSO.ArmorStep"/>
        /// vào <c>(MaxHp, CurrentHp)</c> qua <see cref="CSVH.Core.Game.GameSession.OnArmorUpgraded"/>
        /// (Requirement 5.6, Property 12). Khi thiếu vàng, hiện toast tiếng Việt (Requirement 6.3).
        /// </summary>
        private void WireHudInputBridge()
        {
            if (_hud != null && _input != null)
            {
                _hud.OnIconAttackClicked += HandleAttackIconClicked;
                _hud.OnIconArmorClicked += HandleArmorIconClicked;
                _hud.OnIconSpecialClicked += HandleSpecialIconClicked;
                _hud.OnSkillIconClicked += HandleSkillIconClicked;
                // Icon EXP: bootstrap chưa có hành động; subscriber rỗng giữ chỗ.
                _hud.OnIconExpClicked += HandleExpIconClicked;
                // Nút "Nâng Cấp" duy nhất phía trên thanh máu → bảng Nâng Cấp 2 tab.
                _hud.OnUpgradeHubClicked += HandleUpgradeHubClicked;
            }

            if (_input != null)
            {
                _input.UpgradeRequested += HandleUpgradeRequested;
                _input.SkillUpgradeRequested += HandleSkillUpgradeRequested;
                _input.SkillActivated += HandleSkillActivated;
            }
        }

        // Nút "Nâng Cấp" (thay 4 icon cũ phía trên thanh máu) → bảng Nâng Cấp 2 tab.
        // Không gán bảng nâng cấp trong trận thì rơi về bảng 3 nhánh cũ.
        private void HandleUpgradeHubClicked()
        {
            if (_matchUpgrades != null) OpenUpgradeHub(0);
            else OpenUpgradeModal(UpgradeTrack.Attack);
        }

        // Tương thích ngược: các icon Công/Giáp cũ (nếu UXML còn) cũng mở bảng Nâng Cấp.
        private void HandleAttackIconClicked() => HandleUpgradeHubClicked();
        private void HandleArmorIconClicked() => HandleUpgradeHubClicked();
        // Icon "Đặc biệt" cũ (nếu UXML còn) → mở thẳng tab Đặc Biệt của bảng Nâng Cấp.
        private void HandleSpecialIconClicked()
        {
            if (_matchUpgrades != null) OpenUpgradeHub(1);
            else OpenSpecialHub(null);
        }
        private void HandleSkillIconClicked(SpecialSkillKind kind) => _input?.RequestActivateSkill(kind);
        private void HandleExpIconClicked() => OpenLevelInfoModal();

        /// <summary>
        /// Áp hiệu ứng skill vừa kích hoạt lên Quái quanh Thành. No-op nếu đang hồi chiêu
        /// hoặc thiếu spawner. Đây là mắt xích biến việc bấm skill thành sát thương/choáng thật.
        /// </summary>
        private void HandleSkillActivated(SpecialActivation activation)
        {
            if (!activation.Activated || _spawner == null)
            {
                return;
            }

            _spawner.ApplySpecialEffect(
                origin: _towerPosition,
                radius: activation.Radius,
                hitCount: activation.HitCount,
                damagePerHit: activation.DamagePerHit,
                stunSeconds: activation.StunSeconds);
        }

        private void HandleSkillUpgradeRequested(SpecialSkillKind kind, UpgradeOutcome outcome)
        {
            if (outcome == UpgradeOutcome.NotEnoughGold)
            {
                HudToast.ShowError("Không đủ Vàng");
            }
        }

        /// <summary>
        /// Mở bảng nâng cấp cho <paramref name="track"/> và tạm dừng game cho tới khi
        /// người chơi xác nhận "Nâng cấp" hoặc "Đóng". Trong lúc mở, quái ngừng di chuyển
        /// và Thành ngừng bắn (Time.timeScale = 0). Khi xác nhận, thực hiện mua qua
        /// <see cref="InputService.RequestUpgrade"/> (cùng đường dẫn với phím tắt) rồi tiếp tục.
        /// </summary>
        private void OpenUpgradeModal(UpgradeTrack track)
        {
            if (_session == null || _hud == null || _input == null)
            {
                return;
            }

            // Không mở khi đã kết thúc trận hoặc modal đang mở.
            if (_session.IsGameOver || _hud.IsModalOpen)
            {
                return;
            }

            int level = _session.Upgrades.GetLevel(track);
            int cost = _upgradeTable != null ? _upgradeTable.CostFor(track, level) : 0;
            int gold = _session.Upgrades.Gold;
            bool canAfford = gold >= cost;

            string title = TrackTitle(track);
            string body =
                $"Cấp hiện tại: {level}\n" +
                $"Hiệu ứng khi nâng: {TrackEffect(track)}\n\n" +
                $"Giá: {cost} Vàng (đang có: {gold})" +
                (canAfford ? string.Empty : "\n<b>Không đủ Vàng</b>");

            PauseForModal();
            _hud.ShowUpgradeModal(
                title: title,
                body: body,
                confirmText: $"Nâng cấp ({cost})",
                onConfirm: () =>
                {
                    ResumeFromModal();
                    _input.RequestUpgrade(track);
                },
                onCancel: ResumeFromModal,
                confirmEnabled: canAfford);
        }

        // ==== Bảng 9 nâng cấp TRONG TRẬN (phong cách Subway Surfers) ====================

        /// <summary>
        /// Mở bảng "Nâng Cấp" 2 tab và tạm dừng game. Tab 0 — Nâng Cấp Trong Trận: 9 thẻ
        /// <see cref="MatchUpgradeKind"/> mua bằng Vàng; tab 1 — Nâng Cấp Đặc Biệt: mở khóa /
        /// nâng cấp 3 skill. Gọi lại sau mỗi lần mua để refresh (Vàng / cấp / giá) mà vẫn
        /// giữ pause; nút "X" đóng bảng và tiếp tục trận.
        /// </summary>
        /// <param name="tab">Tab mở sẵn: 0 = Trong Trận, 1 = Đặc Biệt.</param>
        private void OpenUpgradeHub(int tab)
        {
            if (_session == null || _hud == null || _matchUpgrades == null)
            {
                return;
            }

            if (_session.IsGameOver)
            {
                return;
            }

            // Chỉ tạm dừng lần đầu mở; khi refresh sau mỗi lần mua thì giữ nguyên.
            if (!_hud.IsModalOpen)
            {
                PauseForModal();
            }

            // Tab Đặc Biệt chỉ có khi trận cấu hình Special (null ⇒ HUD ẩn tab).
            List<SkillTabInfo> specialRows = null;
            if (_session.SpecialSkills != null)
            {
                specialRows = new List<SkillTabInfo>
                {
                    MakeSkillTab(SpecialSkillKind.TrongDong),
                    MakeSkillTab(SpecialSkillKind.MuiTen),
                    MakeSkillTab(SpecialSkillKind.LuoiGuom),
                };
            }

            _hud.ShowUpgradeHub(
                matchRows: BuildMatchUpgradeRows(),
                specialRows: specialRows,
                gold: _session.Upgrades.Gold,
                selectedTab: tab,
                onBuyMatch: HandleMatchUpgradeBuy,
                onBuySpecial: HandleHubSpecialBuy,
                onClose: () =>
                {
                    _hud.CloseUpgradeHub();
                    ResumeFromModal();
                });
        }

        /// <summary>
        /// Mua từ tab Đặc Biệt: skill còn khóa → mở khóa; đã mở → nâng một cấp. Đi qua
        /// <see cref="InputService"/> để dùng cùng đường dẫn với phím tắt (toast thiếu Vàng
        /// đã xử lý ở <see cref="HandleSkillUpgradeRequested"/>). Refresh bảng giữ tab Đặc Biệt.
        /// </summary>
        private void HandleHubSpecialBuy(SpecialSkillKind kind)
        {
            var skills = _session?.SpecialSkills;
            if (skills == null || _input == null)
            {
                return;
            }

            if (!skills.IsUnlocked(kind))
            {
                _input.RequestUnlockSkill(kind);
            }
            else
            {
                _input.RequestUpgradeSkill(kind);
            }

            OpenUpgradeHub(1); // refresh bảng (giữ mở + giữ pause + giữ tab)
        }

        /// <summary>
        /// Mua một cấp nâng cấp trong trận. Mua Cường Hóa Thành thành công thì cộng Δ HP tối đa
        /// (+5% HP gốc/cấp) qua <see cref="CSVH.Core.Game.GameSession.OnArmorUpgraded"/> để giữ
        /// bất biến <c>0 ≤ CurrentHp ≤ MaxHp</c> (Property 12). Thiếu Vàng → toast tiếng Việt.
        /// </summary>
        private void HandleMatchUpgradeBuy(MatchUpgradeKind kind)
        {
            if (_session == null || _matchUpgrades == null)
            {
                return;
            }

            var outcome = _matchUpgrades.TryBuy(kind, _session.Upgrades);
            if (outcome.Outcome == UpgradeOutcome.Bought)
            {
                if (kind == MatchUpgradeKind.FortifiedBase)
                {
                    _session.OnArmorUpgraded(_matchUpgrades.FortifyHpDeltaFor(_sessionInitialMaxHp));
                }
            }
            else if (outcome.Outcome == UpgradeOutcome.NotEnoughGold)
            {
                HudToast.ShowError("Không đủ Vàng");
            }
            else if (outcome.Outcome == UpgradeOutcome.Maxed)
            {
                HudToast.ShowError("Đã đạt cấp tối đa");
            }

            OpenUpgradeHub(0); // refresh bảng (giữ mở + giữ pause + giữ tab Trong Trận)
        }

        /// <summary>Dựng 9 thẻ cho bảng Nâng Cấp từ <see cref="MatchUpgradeSystem"/> + bảng tham số.</summary>
        private List<MatchUpgradeRow> BuildMatchUpgradeRows()
        {
            var t = _matchUpgradeTable;
            var rows = new List<MatchUpgradeRow>(9);

            rows.Add(MakeMatchRow(MatchUpgradeKind.Damage, "Sát Thương",
                "Tăng sát thương cơ bản của mỗi mũi tên bắn ra từ Nỏ Thần.",
                PercentStepEffect(MatchUpgradeKind.Damage, t.DamagePerLevel, "Sát thương")));

            rows.Add(MakeMatchRow(MatchUpgradeKind.AttackSpeed, "Tốc Đánh",
                "Tăng tốc độ bắn của Nỏ Thần (số mũi tên mỗi giây).",
                PercentStepEffect(MatchUpgradeKind.AttackSpeed, t.AttackSpeedPerLevel, "Tốc đánh")));

            int critLevel = _matchUpgrades.GetLevel(MatchUpgradeKind.Crit);
            rows.Add(MakeMatchRow(MatchUpgradeKind.Crit, "Chí Mạng",
                "Tăng cả tỷ lệ kích hoạt lẫn sát thương của đòn đánh chí mạng.",
                $"Tỷ lệ: {critLevel * t.CritChancePerLevel * 100f:0.#}% → {(critLevel + 1) * t.CritChancePerLevel * 100f:0.#}% • " +
                $"Sát thương: ×{t.BaseCritMultiplier + critLevel * t.CritDamagePerLevel:0.0#} → " +
                $"×{t.BaseCritMultiplier + (critLevel + 1) * t.CritDamagePerLevel:0.0#}"));

            int multiLevel = _matchUpgrades.GetLevel(MatchUpgradeKind.Multishot);
            int multiMax = t.MaxLevelFor(MatchUpgradeKind.Multishot);
            bool multiMaxed = _matchUpgrades.IsMaxed(MatchUpgradeKind.Multishot);
            rows.Add(MakeMatchRow(MatchUpgradeKind.Multishot, "Làn Đạn",
                "Bắn thêm mũi tên mỗi lần bắn, tỏa nhẹ sang hai bên hướng ngắm.",
                multiMaxed
                    ? $"Mũi tên mỗi lần bắn: {1 + multiLevel} (tối đa)"
                    : $"Mũi tên mỗi lần bắn: {1 + multiLevel} → {2 + multiLevel}" +
                      (multiMax > 0 ? $" (tối đa {1 + multiMax})" : string.Empty),
                // Thanh cấp hiển thị SỐ LÀN: 5 vạch (= trần làn), tô theo số làn hiện tại.
                barSegments: multiMax > 0 ? 1 + multiMax : 6,
                barFilled: 1 + multiLevel));

            rows.Add(MakeMatchRow(MatchUpgradeKind.ProjectileSpeed, "Tốc Độ Bay",
                "Tăng vận tốc di chuyển của mũi tên sau khi bắn ra.",
                PercentStepEffect(MatchUpgradeKind.ProjectileSpeed, t.ProjectileSpeedPerLevel, "Tốc độ bay")));

            rows.Add(MakeMatchRow(MatchUpgradeKind.FortifiedBase, "Cường Hóa Thành",
                "Tăng HP tối đa của Thành Lũy.",
                PercentStepEffect(MatchUpgradeKind.FortifiedBase, t.FortifyHpPerLevel, "HP tối đa")));

            int regenLevel = _matchUpgrades.GetLevel(MatchUpgradeKind.BaseRegen);
            rows.Add(MakeMatchRow(MatchUpgradeKind.BaseRegen, "Hồi Phục Thành",
                "Thành Lũy hồi một lượng HP cố định mỗi giây trong suốt màn chơi.",
                $"Hồi máu: {regenLevel * t.RegenHpPerLevel:0.#} HP/s → {(regenLevel + 1) * t.RegenHpPerLevel:0.#} HP/s"));

            int iceLevel = _matchUpgrades.GetLevel(MatchUpgradeKind.IceArrow);
            float iceNow = iceLevel <= 0 ? 0f
                : Mathf.Min(t.IceSlowCap, t.IceSlowBase + iceLevel * t.IceSlowPerLevel);
            float iceNext = Mathf.Min(t.IceSlowCap, t.IceSlowBase + (iceLevel + 1) * t.IceSlowPerLevel);
            rows.Add(MakeMatchRow(MatchUpgradeKind.IceArrow, "Nỏ Băng",
                "Mũi tên trúng mục tiêu làm chậm tốc độ di chuyển của Quái.",
                $"Làm chậm: {iceNow * 100f:0.#}% → {iceNext * 100f:0.#}%"));

            int poisonLevel = _matchUpgrades.GetLevel(MatchUpgradeKind.PoisonArrow);
            rows.Add(MakeMatchRow(MatchUpgradeKind.PoisonArrow, "Nỏ Độc",
                $"Mũi tên trúng mục tiêu gây độc trong {t.PoisonDurationSeconds:0.#} giây, " +
                "tính theo % sát thương hiện tại của Nỏ Thần mỗi giây.",
                $"Độc: {poisonLevel * t.PoisonDpsPerLevel * 100f:0.#}% ATK/s → " +
                $"{(poisonLevel + 1) * t.PoisonDpsPerLevel * 100f:0.#}% ATK/s"));

            int goldLevel = _matchUpgrades.GetLevel(MatchUpgradeKind.GoldRush);
            float goldNow = goldLevel <= 0 ? 0f
                : Mathf.Min(t.GoldRushChanceCap, t.GoldRushChanceBase + goldLevel * t.GoldRushChancePerLevel);
            float goldNext = Mathf.Min(t.GoldRushChanceCap, t.GoldRushChanceBase + (goldLevel + 1) * t.GoldRushChancePerLevel);
            rows.Add(MakeMatchRow(MatchUpgradeKind.GoldRush, "Hoàng Kim",
                $"Mỗi lần hạ gục Quái có tỉ lệ nhận thêm {t.GoldRushBonusFraction * 100f:0}% Vàng rơi.",
                $"Tỉ lệ: {goldNow * 100f:0.#}% → {goldNext * 100f:0.#}%"));

            return rows;
        }

        /// <summary>Dòng hiệu ứng "X: +a% → +b%" cho các nâng cấp tăng tuyến tính theo %.</summary>
        private string PercentStepEffect(MatchUpgradeKind kind, float perLevel, string label)
        {
            int level = _matchUpgrades.GetLevel(kind);
            return $"{label}: +{level * perLevel * 100f:0.#}% → +{(level + 1) * perLevel * 100f:0.#}%";
        }

        private MatchUpgradeRow MakeMatchRow(
            MatchUpgradeKind kind, string name, string desc, string effectText,
            int barSegments = 6, int barFilled = -1)
        {
            int level = _matchUpgrades.GetLevel(kind);
            bool maxed = _matchUpgrades.IsMaxed(kind);
            int cost = maxed ? 0 : _matchUpgrades.CostFor(kind);
            int idx = (int)kind;
            var icon = _matchUpgradeIcons != null && idx < _matchUpgradeIcons.Length
                ? _matchUpgradeIcons[idx]
                : null;

            return new MatchUpgradeRow(
                Kind: kind,
                Name: name,
                Description: desc,
                EffectText: effectText,
                Level: level,
                Cost: cost,
                CanAfford: !maxed && _session.Upgrades.Gold >= cost,
                Icon: icon,
                IsMaxed: maxed,
                BarSegments: barSegments,
                BarFilled: barFilled);
        }

        /// <summary>
        /// Mở bảng "Skill Đặc biệt" dạng 3 tab (mỗi tab một skill, hiện tên). Tab còn khóa →
        /// nút "Mua (giá)" mở khoá ngay tại chỗ; tab đã mở khoá → nút "Nâng cấp" mở một bảng
        /// KHÁC để xác nhận nâng (xem <see cref="OpenSkillUpgradeConfirm"/>). Gọi lại để refresh
        /// sau khi mua/nâng (giữ tab). Tạm dừng game lần đầu mở và tiếp tục khi đóng.
        /// </summary>
        /// <param name="focus">Skill chọn sẵn khi mở; <c>null</c> thì mặc định Trống Đồng.</param>
        private void OpenSpecialHub(SpecialSkillKind? focus)
        {
            if (_session == null || _hud == null || _input == null || _session.SpecialSkills == null)
            {
                return;
            }

            if (_session.IsGameOver)
            {
                return;
            }

            // Chỉ tạm dừng lần đầu mở; khi refresh (hub/bảng xác nhận vẫn đang mở) thì giữ nguyên.
            if (!_hud.IsModalOpen)
            {
                PauseForModal();
            }

            var tabs = new List<SkillTabInfo>
            {
                MakeSkillTab(SpecialSkillKind.TrongDong),
                MakeSkillTab(SpecialSkillKind.MuiTen),
                MakeSkillTab(SpecialSkillKind.LuoiGuom),
            };

            _hud.ShowSkillHubModal(
                tabs: tabs,
                gold: _session.Upgrades.Gold,
                selected: focus ?? SpecialSkillKind.TrongDong,
                onBuy: kind =>
                {
                    _input.RequestUnlockSkill(kind);
                    OpenSpecialHub(kind); // refresh hub (giá/vàng/trạng thái), giữ tab
                },
                onUpgrade: OpenSkillUpgradeConfirm,
                onClose: () =>
                {
                    _hud.CloseSkillHubModal();
                    ResumeFromModal();
                });
        }

        /// <summary>Dựng <see cref="SkillTabInfo"/> cho một tab skill từ <see cref="SpecialSkillSystem"/>.</summary>
        private SkillTabInfo MakeSkillTab(SpecialSkillKind kind)
        {
            var skills = _session.SpecialSkills;
            return new SkillTabInfo(
                Kind: kind,
                Name: kind.DisplayName(),
                IsUnlocked: skills.IsUnlocked(kind),
                Level: skills.GetLevel(kind),
                EffectDesc: SkillEffectDescription(kind),
                UnlockCost: skills.UnlockCostFor(kind),
                UpgradeCost: skills.CostFor(kind));
        }

        /// <summary>
        /// Bảng KHÁC để xác nhận nâng cấp một skill đã mở khoá (hiển cấp + giá). Mở chồng lên
        /// trên hub. Xác nhận → mua nâng cấp rồi quay lại hub; đóng → quay lại hub (không resume).
        /// </summary>
        private void OpenSkillUpgradeConfirm(SpecialSkillKind kind)
        {
            if (_session == null || _session.SpecialSkills == null || _hud == null || _input == null)
            {
                return;
            }

            var skills = _session.SpecialSkills;
            int level = skills.GetLevel(kind);
            int cost = skills.CostFor(kind);
            int gold = _session.Upgrades.Gold;
            bool canAfford = gold >= cost;

            string body =
                $"Cấp hiện tại: {level}\n" +
                $"Hiệu ứng: {SkillEffectDescription(kind)}\n\n" +
                $"Giá: {cost} Vàng (đang có: {gold})" +
                (canAfford ? string.Empty : "\n<b>Không đủ Vàng</b>");

            _hud.ShowUpgradeModal(
                title: "Nâng cấp " + kind.DisplayName(),
                body: body,
                confirmText: $"Nâng cấp ({cost})",
                onConfirm: () =>
                {
                    _input.RequestUpgradeSkill(kind);
                    OpenSpecialHub(kind); // quay lại hub (vẫn pause, hub mở dưới nền)
                },
                onCancel: () => OpenSpecialHub(kind),
                confirmEnabled: canAfford);
        }

        private static string SkillEffectDescription(SpecialSkillKind kind) => kind switch
        {
            SpecialSkillKind.TrongDong => "Nổ sát thương diện rộng quanh Thành; nâng cấp tăng sát thương, số chỗ nổ và giảm hồi chiêu.",
            SpecialSkillKind.MuiTen => "Bắn gây sát thương kèm cơ hội choáng Quái; nâng cấp tăng sát thương, thời gian/khả năng choáng và giảm hồi chiêu.",
            SpecialSkillKind.LuoiGuom => "Chém nhiều nhát trong vùng; nâng cấp tăng sát thương, số nhát chém và giảm hồi chiêu.",
            _ => "—",
        };

        /// <summary>
        /// Icon EXP/Cấp Thành chỉ hiển thị thông tin (Cấp_Thành tăng bằng EXP từ tiêu diệt
        /// Quái, không mua bằng Vàng). Mở bảng thông tin + tạm dừng cho đồng nhất trải nghiệm.
        /// </summary>
        private void OpenLevelInfoModal()
        {
            if (_session == null || _hud == null || _hud.IsModalOpen || _session.IsGameOver)
            {
                return;
            }

            int level = _session.Leveling.Level;
            int exp = _session.Leveling.CurrentExp;
            int req = _session.Leveling.RequiredExp;

            PauseForModal();
            _hud.ShowUpgradeModal(
                title: "Cấp Thành",
                body: $"Cấp hiện tại: {level}\nEXP: {exp}/{req}\n\nTiêu diệt Quái để tích EXP và lên cấp.",
                confirmText: "Đóng",
                onConfirm: ResumeFromModal,
                onCancel: ResumeFromModal,
                confirmEnabled: true);
        }

        /// <summary>
        /// Hỏi người chơi sau khi xong Đợt boss (21): "Chơi tiếp chế độ Vô Tận?".
        /// Đồng ý → tiếp tục với đội hình ngẫu nhiên (<see cref="BuildEndlessSpawns"/>);
        /// Từ chối → kết thúc trận với màn "Chiến thắng!" (vẫn chốt Kỷ_Lục + ký gửi Xu cổ).
        /// </summary>
        private void AskContinueEndless()
        {
            if (_hud == null || _session == null || _session.IsGameOver)
            {
                return;
            }

            PauseForModal();
            _hud.ShowUpgradeModal(
                title: "Chiến thắng Chương 1!",
                body:
                    $"Bạn đã vượt qua {ChapterWaveCount} Đợt và hạ gục {_bossDisplayName}.\n\n" +
                    "Chơi tiếp chế độ Vô Tận? Quái (kể cả boss) sẽ xuất hiện ngẫu nhiên " +
                    $"và đông dần cho tới Đợt {EndlessEnemyCapWave}.",
                confirmText: "Chơi tiếp",
                onConfirm: ResumeFromModal,
                onCancel: () =>
                {
                    ResumeFromModal();
                    EndMatchVictory();
                },
                confirmEnabled: true,
                cancelText: "Kết thúc");
        }

        /// <summary>
        /// Kết thúc trận theo lựa chọn của người chơi sau chiến thắng chương: đẩy scheduler
        /// sang GameOver để nhánh xử lý trong <see cref="Update"/> chốt phiên đúng một lần
        /// (ghi Kỷ_Lục, ký gửi Xu cổ, hiện màn kết — tiêu đề "Chiến thắng!").
        /// </summary>
        private void EndMatchVictory()
        {
            _victoryEnd = true;
            _scheduler?.OnGameOver();
        }

        private static string TrackTitle(UpgradeTrack track) => track switch
        {
            UpgradeTrack.Attack => "Nâng cấp Công",
            UpgradeTrack.Armor => "Nâng cấp Giáp",
            UpgradeTrack.Special => "Nâng cấp Đặc biệt",
            _ => "Nâng cấp",
        };

        private string TrackEffect(UpgradeTrack track)
        {
            if (_upgradeTable == null)
            {
                return "—";
            }

            return track switch
            {
                UpgradeTrack.Attack => $"+{_upgradeTable.AttackStep:0.#} Sát thương",
                UpgradeTrack.Armor => $"+{_upgradeTable.ArmorStep:0.#} Giáp & Máu tối đa",
                UpgradeTrack.Special => $"+{_upgradeTable.SpecialStep:0.#} sức mạnh Đặc biệt",
                _ => "—",
            };
        }

        private void PauseForModal()
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void ResumeFromModal()
        {
            Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
        }

        private void HandleUpgradeRequested(UpgradeTrack track, UpgradeOutcome outcome)
        {
            if (_session == null)
            {
                return;
            }

            if (outcome == UpgradeOutcome.Bought && track == UpgradeTrack.Armor && _upgradeTable != null)
            {
                // Requirement 5.6 / Property 12: Δ Máu_Tối_Đa = ArmorStep cho mỗi bậc Armor mua.
                _session.OnArmorUpgraded(_upgradeTable.ArmorStep);
            }

            if (outcome == UpgradeOutcome.NotEnoughGold)
            {
                // Requirement 6.3: phản hồi tiếng Việt khi thiếu vàng.
                HudToast.ShowError("Không đủ Vàng");
            }
        }

        /// <summary>
        /// Đọc <c>waves.json</c> + <c>enemies.json</c> từ <see cref="Application.streamingAssetsPath"/>
        /// và uỷ thác phần parse/validate cho <see cref="ConfigLoader"/> (Requirement 10.1).
        /// Trả <see cref="Result{ConfigBundle, ConfigError}"/> để caller chuyển trực tiếp sang
        /// nhánh hiển thị "Cấu hình lỗi" (Requirement 10.3) mà không cần try/catch.
        /// </summary>
        private Result<ConfigBundle, ConfigError> LoadConfigs()
        {
            var streaming = Application.streamingAssetsPath;
            var wavesPath = Path.Combine(streaming, _wavesFileName);
            var enemiesPath = Path.Combine(streaming, _enemiesFileName);

            if (!File.Exists(wavesPath) || !File.Exists(enemiesPath))
            {
                return Result<ConfigBundle, ConfigError>.Err(new ConfigError(
                    FieldPath: "$",
                    Line: 0,
                    Column: 0,
                    Message: $"Missing config file(s) in StreamingAssets: '{_wavesFileName}' or '{_enemiesFileName}'"));
            }

            string wavesJson;
            string enemiesJson;
            try
            {
                wavesJson = File.ReadAllText(wavesPath);
                enemiesJson = File.ReadAllText(enemiesPath);
            }
            catch (System.Exception ex)
            {
                // I/O lỗi không thuộc đường dẫn bình thường → vẫn quy về ConfigError để
                // GameSceneRoot xử lý đồng nhất (Requirement 10.3).
                return Result<ConfigBundle, ConfigError>.Err(new ConfigError(
                    FieldPath: "$",
                    Line: 0,
                    Column: 0,
                    Message: $"Failed to read config files: {ex.Message}"));
            }

            var loader = new ConfigLoader();
            return loader.Load(wavesJson, enemiesJson);
        }

        private void ShowConfigError(ConfigError err)
        {
            _configFailed = true;
            _log?.Error($"Cấu hình lỗi tại {err.FieldPath} (line {err.Line}, col {err.Column}): {err.Message}");

            if (_configErrorScreen != null)
            {
                _configErrorScreen.SetActive(true);
            }

            if (_hud != null)
            {
                HudToast.ShowError($"Cấu hình lỗi: {err.FieldPath} — {err.Message}");
            }
        }

        private void Update()
        {
            // Khi bootstrap chưa hoàn tất hoặc đã rơi vào trạng thái lỗi cấu hình, không tick.
            if (_configFailed || _session == null || _scheduler == null)
            {
                return;
            }

            var dt = Time.deltaTime;

            // Requirement 5.1 / 7.1: forward dt cho Core mỗi frame.
            _session.Tick(dt);

            // Nâng cấp trong trận "Hồi Phục Thành": hồi HP/s liên tục; tích phần lẻ vì
            // GameSession.Heal nhận số nguyên (Heal tự kẹp tại MaxHp và no-op khi GameOver).
            if (_matchUpgrades != null && !_session.IsGameOver)
            {
                float regen = _matchUpgrades.RegenHpPerSecond;
                if (regen > 0f)
                {
                    _regenCarry += regen * dt;
                    int whole = (int)_regenCarry;
                    if (whole > 0)
                    {
                        _regenCarry -= whole;
                        _session.Heal(whole);
                    }
                }
            }

            int alive = _spawner != null ? _spawner.AliveCount : 0;
            var intents = _scheduler.Tick(dt, alive, _spawnCap);
            if (intents.Count > 0 && _spawner != null)
            {
                _spawner.ApplyIntents(RemapIntentsToSpawnZone(intents));
            }

            // Đợt vừa kết thúc (hết đồng hồ ở chế độ time-based, hoặc sạch Quái ở chế độ
            // cũ): tiến sang Pha_Chuẩn_Bị của Đợt kế. OnWaveCleared tăng CurrentWave +1
            // và nạp lại Countdown chuẩn bị (Req 7.4, 7.5).
            if (_scheduler.State == WaveState.Cleared)
            {
                int finishedWave = _scheduler.CurrentWave;
                _scheduler.OnWaveCleared();

                // Vừa xong Đợt boss (21) → hỏi "chơi tiếp chế độ Vô Tận?" đúng MỘT lần.
                if (finishedWave == ChapterWaveCount && !_continuePromptShown)
                {
                    _continuePromptShown = true;
                    AskContinueEndless();
                }
            }

            // Requirement 5.4 / 8.5 / 8.6: khi WaveScheduler vừa chuyển sang GameOver,
            // chốt phiên đúng MỘT lần — TryFinalize ghi Kỷ_Lục mới nếu Điểm_Phiên > Kỷ_Lục
            // (Property 18) — rồi hiển thị màn "Kết thúc trận đấu".
            if (!_gameOverHandled && _scheduler.State == WaveState.GameOver)
            {
                _gameOverHandled = true;
                if (_storage != null)
                {
                    _session.Score.TryFinalize(_storage);
                }
                // GDD Cơ chế 2: ký gửi Xu cổ kiếm được trong trận vào kho META bền vững.
                BankMetaCoins();
                ShowGameOverScreen();
            }

            // Vào đợt đếm ngược sang Đợt kế → cho toàn bộ Xu trên sân chảy về ô Vàng.
            MaybeCollectCoins();

            PushHudSnapshot();
        }

        /// <summary>
        /// Ghi đè Cổng_Spawn của các <see cref="SpawnIntent"/> về điểm ngẫu nhiên trong vùng
        /// <see cref="_spawnZoneArea"/> (sprite SpawnZone trên nền) — Quái "trồi ra" từ khu
        /// trại địch rồi đi vào Thành. Không gán vùng ⇒ giữ nguyên cổng từ waves.json.
        /// Điểm spawn được kẹp trong hộp Sân_Đấu (±HalfWidth/Height) trừ lề 0.5 để
        /// polyline/cull biên luôn hợp lệ.
        /// </summary>
        private IReadOnlyList<SpawnIntent> RemapIntentsToSpawnZone(IReadOnlyList<SpawnIntent> intents)
        {
            if (_spawnZoneArea == null || _rng == null || intents == null || intents.Count == 0)
            {
                return intents;
            }

            var b = _spawnZoneArea.bounds;
            float maxX = _halfWidth - 0.5f;
            float maxY = _halfHeight - 0.5f;

            var remapped = new List<SpawnIntent>(intents.Count);
            for (int i = 0; i < intents.Count; i++)
            {
                float x = Mathf.Clamp(
                    Mathf.Lerp(b.min.x + 0.5f, b.max.x - 0.5f, (float)_rng.NextDouble()),
                    -maxX, maxX);
                float y = Mathf.Clamp(
                    Mathf.Lerp(b.min.y + 0.3f, b.max.y - 0.3f, (float)_rng.NextDouble()),
                    -maxY, maxY);
                remapped.Add(new SpawnIntent(intents[i].Enemy, new FieldPoint(x, y)));
            }
            return remapped;
        }

        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                // Feature: Auto adjust camera position to anchor the tower's relative screen position
                // based on the 1920x1080 (16:9) reference aspect ratio.
                float refAspect = 1920f / 1080f;
                float currentAspect = (float)Screen.width / Screen.height;
                float aspectMultiplier = currentAspect / refAspect;

                var pos = Camera.main.transform.position;
                // Dời camera theo trục X sao cho tỉ lệ khoảng cách từ Thành tới tâm Camera 
                // thay đổi thuận với tỉ lệ Aspect Ratio, giúp Thành luôn giữ nguyên vị trí tương đối trên màn hình.
                // Giả định vị trí gốc của Camera là X = 0 ở tỉ lệ 16:9.
                float originalCamX = 0f; 
                pos.x = _towerPosition.x - (_towerPosition.x - originalCamX) * aspectMultiplier;
                Camera.main.transform.position = pos;
            }
        }

        private void PushHudSnapshot()
        {
            if (_hud == null)
            {
                return;
            }

            // Requirement 9.1: HUD chỉ cập nhật qua snapshot bất biến trong cùng frame.
            var snap = new HudSnapshot(
                WaveNumber: _scheduler.CurrentWave,
                CountdownSeconds: Mathf.CeilToInt(Mathf.Max(0f, _scheduler.Countdown)),
                Level: _session.Leveling.Level,
                CurrentExp: _session.Leveling.CurrentExp,
                RequiredExp: _session.Leveling.RequiredExp,
                Hp: _session.CurrentHp,
                MaxHp: _session.MaxHp,
                SessionScore: _session.Score.SessionScore,
                HighScore: _session.Score.HighScore,
                Gold: _session.Upgrades.Gold,
                ArmorLvl: _session.Upgrades.ArmorLevel,
                AttackLvl: _session.Upgrades.AttackLevel,
                TrongDong: SkillInfo(SpecialSkillKind.TrongDong),
                MuiTen: SkillInfo(SpecialSkillKind.MuiTen),
                LuoiGuom: SkillInfo(SpecialSkillKind.LuoiGuom),
                ShowNextWave: _scheduler.State == WaveState.Preparing,
                WaveElapsedSeconds: _scheduler.WaveElapsed,
                WaveTimeRemainingSeconds: _scheduler.WaveTimeRemaining,
                IsEarlyClearPending: _scheduler.IsEarlyClearPending,
                EarlyClearCountdownSeconds: _scheduler.EarlyClearCountdown,
                // Đợt boss (21): HUD hiện "BOSS: <tên>" thay cho "Đợt 21/∞".
                BossName: _scheduler.CurrentWave == ChapterWaveCount ? _bossDisplayName : null);

            _hud.ApplySnapshot(snap);
        }

        /// <summary>
        /// Dựng <see cref="SkillHudInfo"/> cho một skill từ <see cref="SpecialSkillSystem"/>;
        /// trả giá trị rỗng nếu trận đấu không cấu hình Special.
        /// </summary>
        private SkillHudInfo SkillInfo(SpecialSkillKind kind)
        {
            var skills = _session.SpecialSkills;
            if (skills == null)
            {
                return new SkillHudInfo(0, 0f, 0f, false);
            }

            return new SkillHudInfo(
                skills.GetLevel(kind),
                skills.GetCooldownRemaining(kind),
                skills.GetCooldownMax(kind),
                skills.IsUnlocked(kind));
        }

        /// <summary>
        /// Hiển thị màn "Kết thúc trận đấu" (Requirement 5.4) và điền Điểm_Phiên + Kỷ_Lục
        /// vào các Label đã cấu hình trên <see cref="_gameOverDocument"/> nếu có.
        /// Nội dung là tiếng Việt theo Requirement 6.x.
        /// </summary>
        private void ShowGameOverScreen()
        {
            // Tương thích ngược: nếu scene có gán prefab màn thua riêng thì vẫn bật.
            if (_gameOverScreen != null)
            {
                _gameOverScreen.SetActive(true);
            }

            long sessionScore = _session.Score.SessionScore;
            long highScore = _session.Score.HighScore;
            bool isNewHighScore = sessionScore >= highScore && sessionScore > 0;
            // Kết thúc chủ động sau khi thắng chương → "Chiến thắng!"; sập Thành → thua.
            string titleText = _victoryEnd ? "Chiến thắng!" : "Bạn đã thua cuộc";
            string scoreText = $"Điểm phiên: {sessionScore}";
            string highScoreText = $"Kỷ lục: {highScore}";

            if (_gameOverDocument != null && _gameOverDocument.rootVisualElement != null)
            {
                var root = _gameOverDocument.rootVisualElement;
                SetLabelText(root, _gameOverTitleLabelName, titleText);
                SetLabelText(root, _gameOverScoreLabelName, scoreText);
                SetLabelText(root, _gameOverHighScoreLabelName, highScoreText);
            }

            // Bảng "Bạn đã thua cuộc" dựng bằng code trong HUD — luôn hiển thị, không phụ
            // thuộc tham chiếu scene dễ vỡ (Requirement 5.4). Tạm dừng game khi hiện bảng.
            if (_hud != null)
            {
                Time.timeScale = 0f;

                // GDD Cơ chế 2: dòng Xu cổ kiếm được + nút Cửa Hàng (chỉ khi bật hệ META).
                string coinsText = null;
                System.Action onOpenShop = null;
                if (_metaState != null)
                {
                    coinsText = $"Xu cổ: +{_session.MetaCoinsEarned} (tổng {_metaState.Coins})";
                    onOpenShop = OpenMetaShop;
                }

                _hud.ShowGameOverScreen(
                    titleText, scoreText, highScoreText, isNewHighScore, RestartMatch, coinsText, onOpenShop);
            }

            _log?.Info($"GameOver: {scoreText}; {highScoreText}");
        }

        /// <summary>
        /// GDD Cơ chế 2: cộng Xu cổ kiếm được trong trận (<see cref="CSVH.Core.Game.GameSession.MetaCoinsEarned"/>)
        /// vào kho META bền vững rồi ghi xuống Bộ_Lưu_Trữ. Gọi đúng MỘT lần khi trận kết thúc
        /// (bảo vệ bởi <c>_gameOverHandled</c>). No-op nếu hệ META bị tắt (không gán bảng meta).
        /// </summary>
        private void BankMetaCoins()
        {
            if (_metaState == null || _storage == null)
            {
                return;
            }

            if (_session.MetaCoinsEarned > 0)
            {
                _metaState.AddCoins(_session.MetaCoinsEarned);
            }
            _storage.WriteMetaProgress(_metaState.ToSnapshot());
        }

        /// <summary>
        /// GDD Cơ chế 2: mở "Cửa Hàng Xu Cổ" trên màn Game Over. Dựng 3 dòng nâng cấp vĩnh viễn
        /// từ <see cref="_metaState"/> + <see cref="_metaUpgradeTable"/> và đẩy cho HUD render.
        /// </summary>
        private void OpenMetaShop()
        {
            if (_metaState == null || _metaUpgradeTable == null || _hud == null)
            {
                return;
            }

            _hud.ShowMetaShopModal(
                coins: _metaState.Coins,
                rows: BuildShopRows(),
                onBuy: HandleMetaBuy,
                onClose: () => _hud.CloseMetaShopModal());
        }

        /// <summary>
        /// Mua một bậc nâng cấp META; ghi lưu ngay khi thành công rồi mở lại Cửa Hàng để refresh
        /// (số dư Xu cổ / cấp / nút). Thiếu Xu cổ → toast tiếng Việt.
        /// </summary>
        private void HandleMetaBuy(MetaUpgradeTrack track)
        {
            if (_metaState == null)
            {
                return;
            }

            var outcome = _metaState.TryBuy(track);
            if (outcome.Outcome == MetaUpgradeOutcome.Bought)
            {
                _storage?.WriteMetaProgress(_metaState.ToSnapshot());
            }
            else if (outcome.Outcome == MetaUpgradeOutcome.NotEnoughCoins)
            {
                HudToast.ShowError("Không đủ Xu cổ");
            }

            OpenMetaShop(); // refresh bảng (giữ mở)
        }

        private System.Collections.Generic.List<MetaShopRow> BuildShopRows()
        {
            var rows = new List<MetaShopRow>(3);
            rows.Add(MakeShopRow(MetaUpgradeTrack.GateHp, "Máu Cổng",
                $"+{_metaUpgradeTable.GateHpPerLevel} Máu Tối Đa mỗi bậc"));
            rows.Add(MakeShopRow(MetaUpgradeTrack.CrossbowDamage, "Sát thương Nỏ",
                $"+{_metaUpgradeTable.CrossbowDamagePerLevel:0.#} Sát thương Nỏ mỗi bậc"));
            rows.Add(MakeShopRow(MetaUpgradeTrack.UltimateCooldown, "Giảm hồi chiêu Ultimate",
                $"−{_metaUpgradeTable.CooldownReductionPerLevel * 100f:0}% hồi chiêu/bậc " +
                $"(tối đa −{_metaUpgradeTable.MaxCooldownReduction * 100f:0}%)"));
            return rows;
        }

        private MetaShopRow MakeShopRow(MetaUpgradeTrack track, string name, string effectDesc)
        {
            int level = _metaState.GetLevel(track);
            int maxLevel = _metaUpgradeTable.MaxLevelFor(track);
            bool maxed = !_metaState.CanUpgrade(track);
            int cost = maxed ? 0 : _metaState.CostFor(track);
            bool canAfford = !maxed && _metaState.Coins >= cost;
            return new MetaShopRow(track, name, level, maxLevel, cost, effectDesc, canAfford, maxed);
        }

        /// <summary>Tải lại scene hiện tại để chơi lại từ đầu (nút "Chơi lại" trên màn thua).</summary>
        private void RestartMatch()
        {
            Time.timeScale = 1f;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        private static void SetLabelText(UnityEngine.UIElements.VisualElement root, string name, string text)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            var label = root.Q<UnityEngine.UIElements.Label>(name);
            if (label != null)
            {
                label.text = text;
            }
        }

        private void OnDestroy()
        {
            // Khôi phục timeScale phòng khi thoát play mode lúc modal đang mở.
            Time.timeScale = 1f;

            // Hủy đăng ký để tránh leak khi scene unload.
            if (_hud != null)
            {
                _hud.OnIconAttackClicked -= HandleAttackIconClicked;
                _hud.OnIconArmorClicked -= HandleArmorIconClicked;
                _hud.OnIconSpecialClicked -= HandleSpecialIconClicked;
                _hud.OnSkillIconClicked -= HandleSkillIconClicked;
                _hud.OnIconExpClicked -= HandleExpIconClicked;
                _hud.OnUpgradeHubClicked -= HandleUpgradeHubClicked;
            }

            if (_input != null)
            {
                _input.UpgradeRequested -= HandleUpgradeRequested;
                _input.SkillUpgradeRequested -= HandleSkillUpgradeRequested;
                _input.SkillActivated -= HandleSkillActivated;
            }
        }
    }
}
