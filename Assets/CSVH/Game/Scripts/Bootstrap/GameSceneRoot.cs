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
                // Số quái mỗi Đợt theo công thức: 5 + (Đợt - 1).
                // Áp dụng vào mọi Đợt; cấu hình JSON giữ nguyên phân bổ giữa các loại
                // Quái, scheduler sẽ scale Σ count về đúng giá trị này.
                totalEnemiesForWave: wave => 5 + Mathf.Max(0, wave - 1),
                // Mỗi Đợt đếm ngược _waveDurationSeconds giây; hết giờ sang Đợt kế và
                // tích Quái chưa kịp spawn sang Đợt sau.
                waveDurationSeconds: _waveDurationSeconds,
                // Dọn sạch Quái sớm thì chờ _earlyClearGraceSeconds giây rồi skip.
                earlyClearGraceSeconds: _earlyClearGraceSeconds,
                // Vào Đợt 1 ngay khi khởi động (bỏ Pha_Chuẩn_Bị đầu trận).
                startActiveImmediately: _startWaveImmediately,
                // Mở khóa Loại_Quái dần theo mốc 5 Đợt: 1-5 chỉ Hồ Tinh; 6-10 thêm
                // Quân Tống; 11-15 thêm Quân Nguyên Mông; ... (xem BuildWaveSpawns).
                spawnsForWave: BuildWaveSpawns);

            var leveling = new LevelingSystem(_baseRequiredExp, _levelScale);
            var upgrades = new UpgradeSystem(initialGold: _initialGold);
            var score = new ScoreTracker();
            _rng = new SystemRandom();
            var specialSkills = _specialSkillTable != null
                ? new SpecialSkillSystem(_specialSkillTable)
                : null;

            _storage = new UnityStorageService(_log);
            score.LoadHighScore(_storage);

            // Requirement 5.1, 7.1, 8.1: GameSession bó các hệ Core lại cho view layer.
            _session = new CSVH.Core.Game.GameSession(
                _initialMaxHp,
                _upgradeTable,
                _scheduler,
                leveling,
                upgrades,
                score,
                specialSkills);

            WireViews(upgrades, specialSkills);
            WireHudInputBridge();

            _scheduler.Start();
        }

        // Danh sách Loại_Quái theo độ khó tăng dần (index 0 = yếu nhất). Cứ mỗi 5 Đợt
        // mở khóa thêm một Loại: Đợt 1-5 chỉ phần tử [0] (Hồ Tinh); 6-10 thêm [1]
        // (Quân Tống); 11-15 thêm [2] (Quân Nguyên Mông); ... cho tới hết danh sách.
        private static readonly string[] EnemyTierOrder =
        {
            "Hồ_Tinh",
            "Quân_Tống",
            "Quân_Nguyên_Mông",
            "Mộc_Tinh",
            "Thuồng_Luồng",
            "Quỷ_Một_Giò",
        };

        // Nhịp spawn (giây) cho từng tier — Quái càng mạnh spawn càng thưa để giữ nhịp
        // độ khó hợp lý. Cùng chỉ số với EnemyTierOrder.
        private static readonly float[] EnemyTierInterval =
        {
            1.2f, 1.5f, 1.8f, 2.2f, 2.6f, 5.0f,
        };

        /// <summary>
        /// Sinh đội hình <see cref="SpawnEntry"/> cho Đợt thứ <paramref name="wave"/> theo
        /// tiến trình mở khóa: số Loại_Quái = <c>ceil(wave / 5)</c> (kẹp theo số tier có
        /// sẵn). Loại yếu nhất đông nhất, Loại vừa mở khóa hiếm nhất (trọng số giảm dần)
        /// để độ khó tăng từ tốn. Tổng số Quái thực tế do <c>totalEnemiesForWave</c> của
        /// scheduler quyết định (scale theo tỉ lệ này), nên đây chỉ là phân bổ tương đối.
        /// </summary>
        private IReadOnlyList<SpawnEntry> BuildWaveSpawns(int wave)
        {
            if (wave < 1) wave = 1;

            int unlocked = (wave + 4) / 5; // ceil(wave / 5)
            if (unlocked > EnemyTierOrder.Length) unlocked = EnemyTierOrder.Length;
            if (unlocked < 1) unlocked = 1;

            // Dựng danh sách theo thứ tự Loại mới → Loại cũ, để entry CUỐI (Loại yếu nhất)
            // nhận phần dư khi scheduler scale tổng — tránh dồn phần dư vào Quái mạnh.
            var spawns = new List<SpawnEntry>(unlocked);
            for (int i = unlocked - 1; i >= 0; i--)
            {
                int weight = unlocked - i; // i=0 (yếu nhất) có trọng số lớn nhất.
                spawns.Add(new SpawnEntry(EnemyTierOrder[i], weight, EnemyTierInterval[i]));
            }
            return spawns;
        }

        /// <summary>
        /// Inject phụ thuộc vào các view MonoBehaviour. View nào chưa gán (null) sẽ bị
        /// bỏ qua nhẹ nhàng để scene tối thiểu vẫn chạy được trong test/headless.
        /// </summary>
        private void WireViews(UpgradeSystem upgrades, SpecialSkillSystem specialSkills)
        {
            if (_spawner != null)
            {
                // Forward sự kiện Quái → GameSession: chạm Thành trừ máu (Req 2.3),
                // bị tiêu diệt cộng thưởng (Req 2.4). Trước đây thiếu cầu nối này nên
                // Quái tự hủy khi tới Thành mà Máu Thành không đổi.
                _spawner.Initialize(
                    _geometry,
                    onReachedTower: config => _session?.OnEnemyReachedTower(config.MeleeDamage),
                    onEnemyKilled: config => _session?.OnEnemyKilled(config));
            }

            if (_tower != null)
            {
                // Thành chỉ bắn khi còn Quái sống của đợt; hết Quái thì ngừng bắn.
                _tower.Initialize(
                    _geometry,
                    upgrades,
                    _upgradeTable,
                    canFire: () => _spawner != null && _spawner.AliveCount > 0);

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
            }

            if (_input != null)
            {
                _input.UpgradeRequested += HandleUpgradeRequested;
                _input.SkillUpgradeRequested += HandleSkillUpgradeRequested;
                _input.SkillActivated += HandleSkillActivated;
            }
        }

        private void HandleAttackIconClicked() => OpenUpgradeModal(UpgradeTrack.Attack);
        private void HandleArmorIconClicked() => OpenUpgradeModal(UpgradeTrack.Armor);
        private void HandleSpecialIconClicked() => OpenSpecialHub(null);
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

            int alive = _spawner != null ? _spawner.AliveCount : 0;
            var intents = _scheduler.Tick(dt, alive, _spawnCap);
            if (intents.Count > 0 && _spawner != null)
            {
                _spawner.ApplyIntents(intents);
            }

            // Đợt vừa kết thúc (hết đồng hồ ở chế độ time-based, hoặc sạch Quái ở chế độ
            // cũ): tiến sang Pha_Chuẩn_Bị của Đợt kế. OnWaveCleared tăng CurrentWave +1
            // và nạp lại Countdown chuẩn bị (Req 7.4, 7.5).
            if (_scheduler.State == WaveState.Cleared)
            {
                _scheduler.OnWaveCleared();
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
                ShowGameOverScreen();
            }

            PushHudSnapshot();
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
                EarlyClearCountdownSeconds: _scheduler.EarlyClearCountdown);

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
            string titleText = "Bạn đã thua cuộc";
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
                _hud.ShowGameOverScreen(titleText, scoreText, highScoreText, isNewHighScore, RestartMatch);
            }

            _log?.Info($"GameOver: {scoreText}; {highScoreText}");
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
