// Feature: tower-defense-vn, Task 12.1 - HUDController (UIDocument with six anchored regions)
// Validates: Requirements 4.4, 4.5, 5.5, 6.8, 7.3, 7.6, 8.4, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 13.2

using System;
using System.Collections.Generic;
using CSVH.Core.Hud;
using CSVH.Core.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace CSVH.Game.UI
{
    /// <summary>
    /// MonoBehaviour vận hành HUD chính dựa trên UI Toolkit (<see cref="UIDocument"/>).
    /// Cây UXML <c>HUD.uxml</c> khai báo sáu vùng anchored cố định:
    /// <c>TopLeft</c>, <c>TopCenter</c>, <c>TopRight</c>, <c>BottomLeft</c>, <c>BottomCenter</c>,
    /// <c>BottomRight</c>; stylesheet <c>HUD.uss</c> giữ vùng theo tỉ lệ 33% mỗi chiều
    /// nên bố cục bảo toàn ở mọi độ phân giải (Requirement 9.7).
    /// <para/>
    /// Controller không sở hữu trạng thái trận đấu — chỉ tiêu thụ <see cref="HudSnapshot"/>
    /// qua <see cref="ApplySnapshot"/> (callback <c>Action&lt;HudSnapshot&gt;</c>) và
    /// phát event khi người chơi nhấn icon. Phản hồi nhấn icon xảy ra ngay trong cùng
    /// frame của sự kiện click, đảm bảo độ trễ &lt;&lt; 100 ms (Requirement 13.2).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    [AddComponentMenu("CSVH/UI/HUD Controller")]
    public sealed class HUDController : MonoBehaviour
    {
        // Region cache — sáu vùng anchored theo Requirement 9.1-9.7.
        private VisualElement _topLeft;
        private VisualElement _topCenter;
        private VisualElement _topRight;
        private VisualElement _bottomLeft;
        private VisualElement _bottomCenter;
        private VisualElement _bottomRight;

        // TopCenter labels (Wave / NextWave / Countdown).
        private Label _waveLabel;
        private Label _waveElapsedLabel;
        private Label _nextWaveLabel;
        private Label _countdownLabel;

        // TopRight labels (Score / HighScore).
        private Label _scoreLabel;
        private Label _highScoreLabel;

        // BottomLeft (Level + Exp progress ring).
        private Label _levelLabel;
        private Label _goldLabel;
        private VisualElement _expProgress;

        // BottomCenter (Hp + 4 upgrade icons).
        private Label _hpLabel;
        private VisualElement _hpBarFill;
        private Button _iconAttack;
        private Button _iconArmor;
        private Button _iconSpecial;
        private Button _iconSkillTrongDong;
        private Button _iconSkillMuiTen;
        private Button _iconSkillLuoiGuom;
        private Button _iconExp;
        // Nút "Nâng Cấp" duy nhất phía trên thanh máu — mở bảng Nâng Cấp 2 tab.
        private Button _upgradeHubButton;

        // Nhãn hồi chiêu chồng lên mỗi icon skill (tạo động khi bind).
        private Label _cooldownTrongDong;
        private Label _cooldownMuiTen;
        private Label _cooldownLuoiGuom;

        [Header("Icon Sprites (nhánh nâng cấp)")]
        [Tooltip("Sprite cho icon Công (Attack). Gán Hud_Icon_Cong.")]
        [SerializeField] private Sprite _iconAttackSprite;
        [Tooltip("Sprite cho icon Giáp (Armor). Gán Hud_Icon_Giap.")]
        [SerializeField] private Sprite _iconArmorSprite;
        [Tooltip("Sprite cho icon Đặc biệt (Special) bên trái. Gán Hud_Icon_Special.")]
        [SerializeField] private Sprite _iconSpecialSprite;
        [Tooltip("Sprite skill Trống Đồng Đông Sơn. Gán Special_Trong_Dong_Dong_Son.")]
        [SerializeField] private Sprite _iconSkillTrongDongSprite;
        [Tooltip("Sprite skill Mũi Tên An Dương Vương. Gán Special_Mui_Ten_An_Duong_Vuong.")]
        [SerializeField] private Sprite _iconSkillMuiTenSprite;
        [Tooltip("Sprite skill Lưỡi Gươm Lê Lợi. Gán Special_Luoi_Guom_Le_Loi.")]
        [SerializeField] private Sprite _iconSkillLuoiGuomSprite;
        [Tooltip("Sprite cho icon EXP / Cấp Thành. Gán Hud_Icon_Exp.")]
        [SerializeField] private Sprite _iconExpSprite;

        [Tooltip("Ẩn chữ trên icon khi đã có sprite (để chỉ hiện hình).")]
        [SerializeField] private bool _hideIconTextWhenSprite = true;

        [Header("Xu trong trận (Match_Coin)")]
        [Tooltip("Sprite đồng Xu (Match_Coin): dùng làm icon tiền tệ cạnh số Vàng và cho Xu rơi khi diệt Quái.")]
        [SerializeField] private Sprite _coinSprite;
        [Tooltip("Thời gian mỗi đồng Xu bay từ chỗ rơi về ô Vàng khi thu (giây).")]
        [SerializeField] private float _coinFlyDurationSeconds = 0.55f;
        [Tooltip("Độ trễ nối tiếp giữa các đồng Xu khi thu hàng loạt (giây) — tạo dòng chảy.")]
        [SerializeField] private float _coinCollectStaggerSeconds = 0.04f;
        [Tooltip("Kích thước đồng Xu rơi/bay (pixel theo panel HUD).")]
        [SerializeField] private float _coinFlySizePx = 40f;
        [Tooltip("Kích thước icon Xu cạnh số Vàng trên HUD (pixel).")]
        [SerializeField] private float _coinIconSizePx = 28f;

        // Icon Xu đặt cạnh số Vàng (tạo động khi bind nếu có _coinSprite).
        private VisualElement _goldIcon;
        private bool _goldIconApplied;

        // Các đồng Xu: rơi tại chỗ Quái chết và NẰM yên cho tới khi thu (CollectAllCoins),
        // lúc đó lần lượt bay về ô Vàng. Advance trong Update bằng unscaled time.
        private readonly List<Coin> _coins = new List<Coin>();

        // Một đồng Xu: phần tử UI tại RestPos (panel coords). Khi Flying=true thì bay về ô Vàng;
        // tới nơi gọi OnCredited để cộng Vàng.
        private sealed class Coin
        {
            public VisualElement Element;
            public Vector2 RestPos;   // vị trí nằm yên (panel coords)
            public float Age;         // thời gian từ lúc rơi (cho pop xuất hiện + bob nhẹ)
            public float BobPhase;    // lệch pha bob để các Xu không nhấp nhô đồng loạt
            public bool Flying;       // đã bắt đầu bay về ô Vàng chưa
            public float Delay;       // chờ trước khi bắt đầu bay (stagger khi thu hàng loạt)
            public float Elapsed;     // thời gian đã bay
            public float Duration;    // tổng thời gian bay
            public Action OnCredited; // gọi khi tới ô Vàng (cộng Vàng)
        }

        private bool _bound;
        // Click handler của icon đã gắn cho cây UI hiện tại chưa (để gắn/gỡ đúng một lần mỗi cây).
        private bool _iconsBound;

        // Modal nâng cấp — overlay phủ toàn màn hình, dựng động khi cần.
        private VisualElement _modalOverlay;
        private Label _modalTitle;
        private Label _modalBody;
        private Button _modalConfirm;
        private Button _modalCancel;
        private Action _modalOnConfirm;
        private Action _modalOnCancel;

        // Bảng "Skill Đặc biệt" dạng 3 tab — overlay riêng, dựng động khi cần.
        private VisualElement _hubOverlay;
        private VisualElement _hubTabsRow;
        private Label _hubContent;
        private Button _hubActionButton;
        private readonly List<Button> _hubTabButtons = new List<Button>();
        private IReadOnlyList<SkillTabInfo> _hubTabs;
        private int _hubGold;
        private SpecialSkillKind _hubSelected;
        private Action<SpecialSkillKind> _hubOnBuy;
        private Action<SpecialSkillKind> _hubOnUpgrade;
        private Action _hubOnClose;

        // Màn "Kết thúc trận đấu" (Game Over) — dựng bằng code, độc lập tham chiếu scene
        // (tránh lỗi missing reference của _gameOverScreen/_gameOverDocument).
        private VisualElement _gameOverOverlay;
        private Label _gameOverTitleLabel;
        private Label _gameOverScoreLabel;
        private Label _gameOverHighScoreLabel;
        private Label _gameOverRecordBadge;
        private Label _gameOverCoinsLabel;
        private Button _gameOverShopButton;
        private Button _gameOverRestartButton;
        private Action _gameOverOnRestart;
        private Action _gameOverOnOpenShop;

        // "Cửa Hàng Xu Cổ" (META) — overlay riêng, dựng động, mở chồng lên màn Game Over.
        private VisualElement _shopOverlay;
        private VisualElement _shopRowsContainer;
        private Label _shopCoinsLabel;
        private Action<MetaUpgradeTrack> _shopOnBuy;
        private Action _shopOnClose;

        // Bảng "Nâng Cấp" 2 tab (phong cách Subway Surfers) — overlay riêng, dựng động.
        // Tab 0 = Nâng Cấp Trong Trận (9 thẻ); tab 1 = Nâng Cấp Đặc Biệt (3 skill).
        private VisualElement _matchOverlay;
        private ScrollView _matchCardList;
        private Label _matchGoldLabel;
        private Button _matchTabButton;
        private Button _specialTabButton;
        private int _matchSelectedTab;
        private IReadOnlyList<MatchUpgradeRow> _matchRows;
        private IReadOnlyList<SkillTabInfo> _matchSpecialRows;
        private int _matchGold;
        private Action<MatchUpgradeKind> _matchOnBuy;
        private Action<SpecialSkillKind> _matchOnBuySpecial;
        private Action _matchOnClose;

        /// <summary><c>true</c> khi bảng nâng cấp, bảng Skill Đặc biệt hoặc bảng Nâng Cấp trong trận đang mở (game nên tạm dừng).</summary>
        public bool IsModalOpen =>
            (_modalOverlay != null && _modalOverlay.parent != null)
            || (_hubOverlay != null && _hubOverlay.parent != null)
            || (_matchOverlay != null && _matchOverlay.parent != null);

        /// <summary><c>true</c> khi bảng "Kết thúc trận đấu" đang hiển thị.</summary>
        public bool IsGameOverShown => _gameOverOverlay != null && _gameOverOverlay.parent != null;

        /// <summary>
        /// Phát ra khi người chơi nhấn icon Công ở BottomCenter.
        /// Subscriber thường là <c>InputService</c> hoặc <c>GameSceneRoot</c> để gọi
        /// <c>UpgradeSystem.TryBuy(UpgradeTrack.Attack, ...)</c> (Requirement 13.2).
        /// </summary>
        public event Action OnIconAttackClicked;

        /// <summary>
        /// Phát ra khi người chơi nhấn icon Giáp ở BottomCenter.
        /// </summary>
        public event Action OnIconArmorClicked;

        /// <summary>
        /// Phát ra khi người chơi nhấn icon Đặc biệt (Special) ở BottomLeft — nhánh nâng cấp
        /// gốc UpgradeTrack.Special. Subscriber mở bảng nâng cấp track đó.
        /// </summary>
        public event Action OnIconSpecialClicked;

        /// <summary>
        /// Phát ra khi người chơi nhấn một trong ba icon skill Special ở BottomRight.
        /// Tham số là skill được nhấn; subscriber (GameSceneRoot) mở bảng mua/nâng cấp skill đó.
        /// </summary>
        public event Action<SpecialSkillKind> OnSkillIconClicked;

        /// <summary>
        /// Phát ra khi người chơi nhấn icon EXP (slot thứ tư của BottomCenter
        /// theo thứ tự cố định Công/Giáp/Special/EXP, Requirement 6.8).
        /// </summary>
        public event Action OnIconExpClicked;

        /// <summary>
        /// Phát ra khi người chơi nhấn nút "Nâng Cấp" phía trên thanh máu.
        /// Subscriber (GameSceneRoot) mở bảng Nâng Cấp 2 tab (Trong Trận / Đặc Biệt).
        /// </summary>
        public event Action OnUpgradeHubClicked;

        /// <summary>Truy cập vùng TopLeft cho avatar Quái (Requirement 9.3).</summary>
        public VisualElement TopLeftRegion => _topLeft;

        /// <summary>Truy cập vùng BottomRight cho art Thành (Requirement 9.6).</summary>
        public VisualElement BottomRightRegion => _bottomRight;

        /// <summary>Truy cập vòng tiến trình EXP để các view phụ (gradient, sprite) gắn vào.</summary>
        public VisualElement ExpProgressElement => _expProgress;

        /// <summary>
        /// Kiểm tra xem toạ độ chuột/touch có đang nằm trên một phần tử UI có thể tương tác không
        /// (tránh bắn đạn khi click nhầm UI).
        /// </summary>
        public bool IsPointerOverUI(Vector2 screenPos)
        {
            if (Root == null || Root.panel == null) return false;

            // Chuyển screenPos từ Bottom-Left (Input System) sang Top-Left (UI Toolkit)
            Vector2 topDownScreenPos = new Vector2(screenPos.x, Screen.height - screenPos.y);

            // Sử dụng RuntimePanelUtils để convert chính xác toạ độ màn hình sang toạ độ Panel ảo
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(Root.panel, topDownScreenPos);
            var picked = Root.panel.Pick(panelPos);

            if (picked == null || picked == Root)
            {
                return false;
            }

            // Bỏ qua các container rỗng trải khắp màn hình
            if (picked.ClassListContains("hud-region") || picked.ClassListContains("hud-root"))
            {
                return false;
            }

            return true;
        }

        /// <summary>Truy cập <see cref="UIDocument.rootVisualElement"/> chuẩn hóa cho các view khác.</summary>
        public VisualElement Root { get; private set; }

        private void Awake()
        {
            BindRoot();
        }

        private void OnEnable()
        {
            // BindRoot tự gắn click handler (BindIcons) trên cây hiện tại.
            BindRoot();
        }

        private void OnDisable()
        {
            if (_iconsBound)
            {
                UnbindIcons();
                _iconsBound = false;
            }
        }

        private void BindRoot()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
                return;
            }

            var root = doc.rootVisualElement;
            if (root == null)
            {
                // Tài liệu chưa build VisualTree (xảy ra trong Awake nếu visualTreeAsset
                // chưa gán); thử lại ở OnEnable.
                return;
            }

            // Đã bind và cây UI hiện tại vẫn còn sống (region chưa bị tách khỏi panel) → khỏi bind lại.
            // UIDocument có thể DỰNG LẠI VisualTree (đổi PanelSettings/độ phân giải — game scale UI 4K,
            // hoặc reimport UXML); khi đó tham chiếu cache cũ bị mồ côi nên phải bind lại trên cây mới,
            // nếu không HUD (số Vàng, icon Xu, click icon) sẽ "chết".
            if (_bound && _iconsBound && ReferenceEquals(root, Root) && _bottomLeft != null && _bottomLeft.panel != null)
            {
                return;
            }

            // Cây mới: gỡ click handler cũ + reset trạng thái phụ thuộc cây trước khi truy vấn lại.
            if (_iconsBound)
            {
                UnbindIcons();
                _iconsBound = false;
            }
            _goldIconApplied = false;
            _goldIcon = null;

            Root = root;

            _topLeft = root.Q<VisualElement>("TopLeft");
            _topCenter = root.Q<VisualElement>("TopCenter");
            _topRight = root.Q<VisualElement>("TopRight");
            _bottomLeft = root.Q<VisualElement>("BottomLeft");
            _bottomCenter = root.Q<VisualElement>("BottomCenter");
            _bottomRight = root.Q<VisualElement>("BottomRight");

            // TopCenter labels (Wave / NextWave / Countdown).
            _waveLabel = root.Q<Label>("WaveLabel");
            _waveElapsedLabel = root.Q<Label>("WaveElapsedLabel");
            _nextWaveLabel = root.Q<Label>("NextWaveLabel");
            _countdownLabel = root.Q<Label>("CountdownLabel");

            _scoreLabel = root.Q<Label>("ScoreLabel");
            _highScoreLabel = root.Q<Label>("HighScoreLabel");

            _levelLabel = root.Q<Label>("LevelLabel");
            _goldLabel = root.Q<Label>("GoldLabel");
            _expProgress = root.Q<VisualElement>("ExpProgress");

            _hpLabel = root.Q<Label>("HpLabel");
            _hpBarFill = root.Q<VisualElement>("HpBarFill");
            _iconAttack = root.Q<Button>("IconAttack");
            _iconArmor = root.Q<Button>("IconArmor");
            _iconSpecial = root.Q<Button>("IconSpecial");
            _iconSkillTrongDong = root.Q<Button>("IconSkillTrongDong");
            _iconSkillMuiTen = root.Q<Button>("IconSkillMuiTen");
            _iconSkillLuoiGuom = root.Q<Button>("IconSkillLuoiGuom");
            _iconExp = root.Q<Button>("IconExp");
            _upgradeHubButton = root.Q<Button>("ButtonUpgradeHub");

            _cooldownTrongDong = EnsureCooldownLabel(_iconSkillTrongDong);
            _cooldownMuiTen = EnsureCooldownLabel(_iconSkillMuiTen);
            _cooldownLuoiGuom = EnsureCooldownLabel(_iconSkillLuoiGuom);

            ApplyIconSprites();
            ApplyGoldCoinIcon();

            _bound = _topLeft != null && _topCenter != null && _topRight != null
                && _bottomLeft != null && _bottomCenter != null && _bottomRight != null;

            // Gắn (lại) click handler cho icon trên cây hiện tại.
            BindIcons();
            _iconsBound = true;
        }

        /// <summary>
        /// Đặt icon đồng Xu (Match_Coin) ngay bên trái số Vàng trên HUD. Bọc
        /// <see cref="_goldLabel"/> trong một hàng ngang [icon][số] để hai phần nằm cạnh nhau.
        /// Chỉ chạy một lần và chỉ khi đã gán <see cref="_coinSprite"/>; thiếu sprite thì giữ
        /// nguyên nhãn "Vàng: N" như cũ.
        /// </summary>
        private void ApplyGoldCoinIcon()
        {
            if (_coinSprite == null || _goldLabel == null)
            {
                return;
            }

            var parent = _goldLabel.parent;
            if (parent == null)
            {
                return;
            }

            // Đã bọc trong GoldRow từ lần bind trước (bind lại trên cùng cây) → tái dùng,
            // tránh bọc lồng nhau khi ApplyGoldCoinIcon chạy lại.
            if (parent.name == "GoldRow")
            {
                _goldIcon = parent.Q<VisualElement>("GoldIcon");
                _goldIconApplied = _goldIcon != null;
                return;
            }

            int idx = parent.IndexOf(_goldLabel);

            var row = new VisualElement { name = "GoldRow" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.pickingMode = PickingMode.Ignore;

            _goldIcon = new VisualElement { name = "GoldIcon" };
            _goldIcon.style.width = _coinIconSizePx;
            _goldIcon.style.height = _coinIconSizePx;
            _goldIcon.style.marginRight = 6;
            _goldIcon.style.backgroundImage = new StyleBackground(_coinSprite);
            _goldIcon.pickingMode = PickingMode.Ignore;

            parent.Remove(_goldLabel);
            row.Add(_goldIcon);
            row.Add(_goldLabel);
            parent.Insert(idx, row);

            _goldIconApplied = true;
        }

        /// <summary>
        /// Gán sprite (nếu được cấu hình trong Inspector) làm <c>backgroundImage</c> cho
        /// từng nút icon nâng cấp. Khi có sprite và <see cref="_hideIconTextWhenSprite"/> bật,
        /// xóa chữ để chỉ hiển thị hình. Nút nào chưa gán sprite giữ nguyên màu nền + chữ cũ.
        /// </summary>
        private void ApplyIconSprites()
        {
            ApplyOneIconSprite(_iconAttack, _iconAttackSprite);
            ApplyOneIconSprite(_iconArmor, _iconArmorSprite);
            ApplyOneIconSprite(_iconSpecial, _iconSpecialSprite);
            ApplyOneIconSprite(_iconSkillTrongDong, _iconSkillTrongDongSprite);
            ApplyOneIconSprite(_iconSkillMuiTen, _iconSkillMuiTenSprite);
            ApplyOneIconSprite(_iconSkillLuoiGuom, _iconSkillLuoiGuomSprite);
            ApplyOneIconSprite(_iconExp, _iconExpSprite);
        }

        /// <summary>
        /// Tạo (một lần) nhãn hồi chiêu chồng lên một icon skill, dùng class
        /// <c>hud-icon-cooldown</c> đã có trong USS. Trả <c>null</c> nếu icon chưa tồn tại.
        /// </summary>
        private static Label EnsureCooldownLabel(Button icon)
        {
            if (icon == null)
            {
                return null;
            }

            var existing = icon.Q<Label>(className: "hud-icon-cooldown");
            if (existing != null)
            {
                return existing;
            }

            var label = new Label { name = icon.name + "_cd" };
            label.AddToClassList("hud-icon-cooldown");
            label.pickingMode = PickingMode.Ignore;
            icon.Add(label);
            return label;
        }

        /// <summary>
        /// Cập nhật hiển thị một icon skill theo <see cref="SkillHudInfo"/>:
        /// <list type="bullet">
        ///   <item>Chưa mở khoá → làm TỐI hẳn (class <c>hud-icon-locked</c>), ẩn số hồi chiêu.</item>
        ///   <item>Đã mở khoá + còn hồi chiêu → làm mờ nhẹ + hiện số giây còn lại.</item>
        ///   <item>Đã mở khoá + sẵn sàng → sáng rõ.</item>
        /// </list>
        /// Icon luôn bấm được để mở bảng mua/nâng cấp.
        /// </summary>
        private static void UpdateSkillIcon(Button icon, Label cooldownLabel, SkillHudInfo info)
        {
            if (icon == null)
            {
                return;
            }

            if (!info.IsUnlocked)
            {
                if (!icon.ClassListContains("hud-icon-locked"))
                {
                    icon.AddToClassList("hud-icon-locked");
                }
                icon.style.opacity = 0.35f;
                if (cooldownLabel != null)
                {
                    cooldownLabel.text = string.Empty;
                }
                return;
            }

            if (icon.ClassListContains("hud-icon-locked"))
            {
                icon.RemoveFromClassList("hud-icon-locked");
            }

            bool ready = info.CooldownRemaining <= 0.0001f;
            icon.style.opacity = ready ? 1f : 0.55f;
            if (cooldownLabel != null)
            {
                cooldownLabel.text = ready
                    ? string.Empty
                    : Mathf.CeilToInt(info.CooldownRemaining).ToString();
            }
        }

        private void ApplyOneIconSprite(Button icon, Sprite sprite)
        {
            if (icon == null || sprite == null)
            {
                return;
            }

            icon.style.backgroundImage = new StyleBackground(sprite);
            // Nền màu trong suốt để sprite hiện rõ thay vì đè màu nhánh.
            icon.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
            if (_hideIconTextWhenSprite)
            {
                icon.text = string.Empty;
            }
        }

        private void BindIcons()
        {
            if (_iconAttack != null)
            {
                _iconAttack.clicked += HandleAttackClicked;
            }

            if (_iconArmor != null)
            {
                _iconArmor.clicked += HandleArmorClicked;
            }

            if (_iconSpecial != null)
            {
                _iconSpecial.clicked += HandleSpecialClicked;
            }

            if (_iconSkillTrongDong != null)
            {
                _iconSkillTrongDong.clicked += HandleSkillTrongDongClicked;
            }

            if (_iconSkillMuiTen != null)
            {
                _iconSkillMuiTen.clicked += HandleSkillMuiTenClicked;
            }

            if (_iconSkillLuoiGuom != null)
            {
                _iconSkillLuoiGuom.clicked += HandleSkillLuoiGuomClicked;
            }

            if (_iconExp != null)
            {
                _iconExp.clicked += HandleExpClicked;
            }

            if (_upgradeHubButton != null)
            {
                _upgradeHubButton.clicked += HandleUpgradeHubClicked;
            }
        }

        private void UnbindIcons()
        {
            if (_iconAttack != null)
            {
                _iconAttack.clicked -= HandleAttackClicked;
            }

            if (_iconArmor != null)
            {
                _iconArmor.clicked -= HandleArmorClicked;
            }

            if (_iconSpecial != null)
            {
                _iconSpecial.clicked -= HandleSpecialClicked;
            }

            if (_iconSkillTrongDong != null)
            {
                _iconSkillTrongDong.clicked -= HandleSkillTrongDongClicked;
            }

            if (_iconSkillMuiTen != null)
            {
                _iconSkillMuiTen.clicked -= HandleSkillMuiTenClicked;
            }

            if (_iconSkillLuoiGuom != null)
            {
                _iconSkillLuoiGuom.clicked -= HandleSkillLuoiGuomClicked;
            }

            if (_iconExp != null)
            {
                _iconExp.clicked -= HandleExpClicked;
            }

            if (_upgradeHubButton != null)
            {
                _upgradeHubButton.clicked -= HandleUpgradeHubClicked;
            }
        }

        private void HandleAttackClicked() => OnIconAttackClicked?.Invoke();
        private void HandleArmorClicked() => OnIconArmorClicked?.Invoke();
        private void HandleSpecialClicked() => OnIconSpecialClicked?.Invoke();
        private void HandleSkillTrongDongClicked() => OnSkillIconClicked?.Invoke(SpecialSkillKind.TrongDong);
        private void HandleSkillMuiTenClicked() => OnSkillIconClicked?.Invoke(SpecialSkillKind.MuiTen);
        private void HandleSkillLuoiGuomClicked() => OnSkillIconClicked?.Invoke(SpecialSkillKind.LuoiGuom);
        private void HandleExpClicked() => OnIconExpClicked?.Invoke();
        private void HandleUpgradeHubClicked() => OnUpgradeHubClicked?.Invoke();

        /// <summary>
        /// Mở bảng nâng cấp dạng modal phủ toàn màn hình. Overlay chặn tương tác với
        /// các icon/phần tử phía dưới và làm mờ nền. Người gọi (thường là
        /// <c>GameSceneRoot</c>) chịu trách nhiệm tạm dừng game (Time.timeScale = 0)
        /// trước khi gọi và khôi phục trong callback.
        /// </summary>
        /// <param name="title">Tiêu đề (ví dụ tên nhánh nâng cấp).</param>
        /// <param name="body">Mô tả: cấp hiện tại, giá, hiệu ứng sau khi nâng.</param>
        /// <param name="confirmText">Nhãn nút xác nhận (ví dụ "Nâng cấp").</param>
        /// <param name="onConfirm">Gọi khi người chơi xác nhận. Modal tự đóng sau đó.</param>
        /// <param name="onCancel">Gọi khi người chơi đóng/hủy. Modal tự đóng sau đó.</param>
        /// <param name="confirmEnabled">Khi <c>false</c>, nút xác nhận bị vô hiệu (ví dụ thiếu Vàng).</param>
        /// <param name="cancelText">Nhãn nút hủy; <c>null</c> = "Đóng".</param>
        public void ShowUpgradeModal(
            string title,
            string body,
            string confirmText,
            Action onConfirm,
            Action onCancel = null,
            bool confirmEnabled = true,
            string cancelText = null)
        {
            BindRoot();
            if (Root == null)
            {
                return;
            }

            EnsureModalBuilt();

            _modalTitle.text = title ?? string.Empty;
            _modalBody.text = body ?? string.Empty;
            _modalConfirm.text = confirmText ?? "Nâng cấp";
            _modalCancel.text = cancelText ?? "Đóng";
            _modalConfirm.SetEnabled(confirmEnabled);
            _modalOnConfirm = onConfirm;
            _modalOnCancel = onCancel;

            // Đưa lên trên cùng để chắc chắn nằm trên mọi phần tử HUD khác.
            if (_modalOverlay.parent != null)
            {
                _modalOverlay.RemoveFromHierarchy();
            }
            Root.Add(_modalOverlay);
            _modalOverlay.BringToFront();
            _modalConfirm.Focus();
        }

        /// <summary>Đóng bảng nâng cấp nếu đang mở (không gọi callback).</summary>
        public void CloseModal()
        {
            if (_modalOverlay != null && _modalOverlay.parent != null)
            {
                _modalOverlay.RemoveFromHierarchy();
            }
            _modalOnConfirm = null;
            _modalOnCancel = null;
        }

        private void EnsureModalBuilt()
        {
            if (_modalOverlay != null)
            {
                return;
            }

            _modalOverlay = new VisualElement { name = "UpgradeModalOverlay" };
            _modalOverlay.AddToClassList("hud-modal-overlay");
            // Inline layout (không phụ thuộc USS reload): phủ toàn màn hình, làm mờ, canh giữa.
            _modalOverlay.style.position = Position.Absolute;
            _modalOverlay.style.left = 0;
            _modalOverlay.style.top = 0;
            _modalOverlay.style.right = 0;
            _modalOverlay.style.bottom = 0;
            _modalOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.6f));
            _modalOverlay.style.alignItems = Align.Center;
            _modalOverlay.style.justifyContent = Justify.Center;
            // Chặn click "xuyên" xuống icon phía dưới khi bấm vào vùng tối.
            _modalOverlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var panel = new VisualElement { name = "UpgradeModalPanel" };
            panel.AddToClassList("hud-modal-panel");
            panel.style.minWidth = 360;
            panel.style.maxWidth = 520;
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 24;
            panel.style.paddingBottom = 24;
            panel.style.backgroundColor = new StyleColor(new Color(38f / 255f, 24f / 255f, 14f / 255f, 0.98f));
            panel.style.borderTopWidth = 3;
            panel.style.borderBottomWidth = 3;
            panel.style.borderLeftWidth = 3;
            panel.style.borderRightWidth = 3;
            var borderCol = new StyleColor(new Color(180f / 255f, 120f / 255f, 50f / 255f, 1f));
            panel.style.borderTopColor = borderCol;
            panel.style.borderBottomColor = borderCol;
            panel.style.borderLeftColor = borderCol;
            panel.style.borderRightColor = borderCol;
            panel.style.borderTopLeftRadius = 12;
            panel.style.borderTopRightRadius = 12;
            panel.style.borderBottomLeftRadius = 12;
            panel.style.borderBottomRightRadius = 12;
            panel.style.alignItems = Align.Center;
            _modalOverlay.Add(panel);

            _modalTitle = new Label { name = "UpgradeModalTitle" };
            _modalTitle.AddToClassList("hud-modal-title");
            _modalTitle.style.fontSize = 26;
            _modalTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _modalTitle.style.color = new StyleColor(new Color(1f, 220f / 255f, 150f / 255f, 1f));
            _modalTitle.style.marginBottom = 12;
            _modalTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_modalTitle);

            _modalBody = new Label { name = "UpgradeModalBody" };
            _modalBody.AddToClassList("hud-modal-body");
            _modalBody.style.fontSize = 18;
            _modalBody.style.color = new StyleColor(new Color(235f / 255f, 225f / 255f, 210f / 255f, 1f));
            _modalBody.style.marginBottom = 20;
            _modalBody.style.whiteSpace = WhiteSpace.Normal;
            _modalBody.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_modalBody);

            var buttons = new VisualElement { name = "UpgradeModalButtons" };
            buttons.AddToClassList("hud-modal-buttons");
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.Center;
            panel.Add(buttons);

            _modalConfirm = new Button { name = "UpgradeModalConfirm" };
            _modalConfirm.AddToClassList("hud-modal-button");
            _modalConfirm.AddToClassList("hud-modal-confirm");
            StyleModalButton(_modalConfirm, new Color(60f / 255f, 120f / 255f, 70f / 255f, 0.95f), new Color(120f / 255f, 200f / 255f, 130f / 255f, 1f));
            _modalConfirm.clicked += HandleModalConfirm;
            buttons.Add(_modalConfirm);

            _modalCancel = new Button { name = "UpgradeModalCancel", text = "Đóng" };
            _modalCancel.AddToClassList("hud-modal-button");
            _modalCancel.AddToClassList("hud-modal-cancel");
            StyleModalButton(_modalCancel, new Color(120f / 255f, 60f / 255f, 50f / 255f, 0.95f), new Color(200f / 255f, 110f / 255f, 90f / 255f, 1f));
            _modalCancel.clicked += HandleModalCancel;
            buttons.Add(_modalCancel);
        }

        private static void StyleModalButton(Button btn, Color bg, Color border)
        {
            btn.style.minWidth = 120;
            btn.style.height = 44;
            btn.style.marginLeft = 8;
            btn.style.marginRight = 8;
            btn.style.paddingLeft = 16;
            btn.style.paddingRight = 16;
            btn.style.fontSize = 18;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.backgroundColor = new StyleColor(bg);
            btn.style.color = new StyleColor(new Color(0.96f, 1f, 0.96f, 1f));
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;
            var bc = new StyleColor(border);
            btn.style.borderTopColor = bc;
            btn.style.borderBottomColor = bc;
            btn.style.borderLeftColor = bc;
            btn.style.borderRightColor = bc;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
        }

        private void HandleModalConfirm()
        {
            var cb = _modalOnConfirm;
            CloseModal();
            cb?.Invoke();
        }

        private void HandleModalCancel()
        {
            var cb = _modalOnCancel;
            CloseModal();
            cb?.Invoke();
        }

        /// <summary>
        /// Mở bảng "Skill Đặc biệt" dạng 3 tab. Mỗi tab là một skill (hiện tên). Tab còn
        /// khóa → nút dưới là "Mua (giá)" gọi <paramref name="onBuy"/>; tab đã mở khoá → nút
        /// "Nâng cấp" gọi <paramref name="onUpgrade"/> (subscriber mở một bảng khác để xác nhận).
        /// Gọi lại để refresh sau khi mua/nâng. Người gọi chịu trách nhiệm pause/resume game.
        /// </summary>
        public void ShowSkillHubModal(
            IReadOnlyList<SkillTabInfo> tabs,
            int gold,
            SpecialSkillKind selected,
            Action<SpecialSkillKind> onBuy,
            Action<SpecialSkillKind> onUpgrade,
            Action onClose)
        {
            BindRoot();
            if (Root == null || tabs == null || tabs.Count == 0)
            {
                return;
            }

            _hubTabs = tabs;
            _hubGold = gold;
            _hubSelected = selected;
            _hubOnBuy = onBuy;
            _hubOnUpgrade = onUpgrade;
            _hubOnClose = onClose;

            EnsureHubBuilt();
            RebuildHubTabs();
            RenderHub();

            if (_hubOverlay.parent != null)
            {
                _hubOverlay.RemoveFromHierarchy();
            }
            Root.Add(_hubOverlay);
            _hubOverlay.BringToFront();
        }

        /// <summary>Đóng bảng Skill Đặc biệt nếu đang mở (không gọi callback).</summary>
        public void CloseSkillHubModal()
        {
            if (_hubOverlay != null && _hubOverlay.parent != null)
            {
                _hubOverlay.RemoveFromHierarchy();
            }
        }

        private SkillTabInfo GetSelectedHubTab()
        {
            for (int i = 0; i < _hubTabs.Count; i++)
            {
                if (_hubTabs[i].Kind == _hubSelected)
                {
                    return _hubTabs[i];
                }
            }
            return _hubTabs[0];
        }

        private void EnsureHubBuilt()
        {
            if (_hubOverlay != null)
            {
                return;
            }

            _hubOverlay = new VisualElement { name = "SkillHubOverlay" };
            _hubOverlay.style.position = Position.Absolute;
            _hubOverlay.style.left = 0;
            _hubOverlay.style.top = 0;
            _hubOverlay.style.right = 0;
            _hubOverlay.style.bottom = 0;
            _hubOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.6f));
            _hubOverlay.style.alignItems = Align.Center;
            _hubOverlay.style.justifyContent = Justify.Center;
            _hubOverlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var panel = new VisualElement { name = "SkillHubPanel" };
            panel.style.minWidth = 420;
            panel.style.maxWidth = 560;
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.backgroundColor = new StyleColor(new Color(38f / 255f, 24f / 255f, 14f / 255f, 0.98f));
            panel.style.borderTopWidth = 3;
            panel.style.borderBottomWidth = 3;
            panel.style.borderLeftWidth = 3;
            panel.style.borderRightWidth = 3;
            var borderCol = new StyleColor(new Color(180f / 255f, 120f / 255f, 50f / 255f, 1f));
            panel.style.borderTopColor = borderCol;
            panel.style.borderBottomColor = borderCol;
            panel.style.borderLeftColor = borderCol;
            panel.style.borderRightColor = borderCol;
            panel.style.borderTopLeftRadius = 12;
            panel.style.borderTopRightRadius = 12;
            panel.style.borderBottomLeftRadius = 12;
            panel.style.borderBottomRightRadius = 12;
            panel.style.alignItems = Align.Center;
            _hubOverlay.Add(panel);

            var title = new Label { name = "SkillHubTitle", text = "Skill Đặc biệt" };
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new StyleColor(new Color(1f, 220f / 255f, 150f / 255f, 1f));
            title.style.marginBottom = 12;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(title);

            _hubTabsRow = new VisualElement { name = "SkillHubTabs" };
            _hubTabsRow.style.flexDirection = FlexDirection.Row;
            _hubTabsRow.style.justifyContent = Justify.Center;
            _hubTabsRow.style.marginBottom = 14;
            panel.Add(_hubTabsRow);

            _hubContent = new Label { name = "SkillHubContent" };
            _hubContent.style.fontSize = 17;
            _hubContent.style.color = new StyleColor(new Color(235f / 255f, 225f / 255f, 210f / 255f, 1f));
            _hubContent.style.marginBottom = 18;
            _hubContent.style.whiteSpace = WhiteSpace.Normal;
            _hubContent.style.unityTextAlign = TextAnchor.MiddleCenter;
            _hubContent.style.minHeight = 96;
            panel.Add(_hubContent);

            var buttons = new VisualElement { name = "SkillHubButtons" };
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.Center;
            panel.Add(buttons);

            _hubActionButton = new Button { name = "SkillHubAction" };
            StyleModalButton(_hubActionButton, new Color(60f / 255f, 120f / 255f, 70f / 255f, 0.95f), new Color(120f / 255f, 200f / 255f, 130f / 255f, 1f));
            _hubActionButton.clicked += HandleHubAction;
            buttons.Add(_hubActionButton);

            var closeButton = new Button { name = "SkillHubClose", text = "Đóng" };
            StyleModalButton(closeButton, new Color(120f / 255f, 60f / 255f, 50f / 255f, 0.95f), new Color(200f / 255f, 110f / 255f, 90f / 255f, 1f));
            closeButton.clicked += HandleHubClose;
            buttons.Add(closeButton);
        }

        private void RebuildHubTabs()
        {
            _hubTabsRow.Clear();
            _hubTabButtons.Clear();

            foreach (var tab in _hubTabs)
            {
                var kind = tab.Kind;
                var b = new Button { text = tab.Name };
                StyleHubTab(b, kind == _hubSelected);
                b.clicked += () =>
                {
                    _hubSelected = kind;
                    RebuildHubTabs();
                    RenderHub();
                };
                _hubTabsRow.Add(b);
                _hubTabButtons.Add(b);
            }
        }

        private void RenderHub()
        {
            var tab = GetSelectedHubTab();

            if (tab.IsUnlocked)
            {
                _hubContent.text =
                    $"{tab.Name}\n\n" +
                    $"Cấp hiện tại: {tab.Level}\n" +
                    $"{tab.EffectDesc}";
                _hubActionButton.text = "Nâng cấp";
                _hubActionButton.SetEnabled(true);
            }
            else
            {
                _hubContent.text =
                    $"{tab.Name}\n\n" +
                    $"<b>Chưa mở khoá</b>\n" +
                    $"{tab.EffectDesc}\n\n" +
                    $"Giá mở khoá: {tab.UnlockCost} Vàng (đang có: {_hubGold})";
                _hubActionButton.text = $"Mua ({tab.UnlockCost})";
                _hubActionButton.SetEnabled(_hubGold >= tab.UnlockCost);
            }
        }

        private void HandleHubAction()
        {
            var tab = GetSelectedHubTab();
            if (tab.IsUnlocked)
            {
                _hubOnUpgrade?.Invoke(tab.Kind);
            }
            else
            {
                _hubOnBuy?.Invoke(tab.Kind);
            }
        }

        private void HandleHubClose() => _hubOnClose?.Invoke();

        private static void StyleHubTab(Button btn, bool selected)
        {
            btn.style.height = 38;
            btn.style.minWidth = 96;
            btn.style.marginLeft = 4;
            btn.style.marginRight = 4;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.fontSize = 14;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.whiteSpace = WhiteSpace.Normal;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;

            var activeBg = new Color(180f / 255f, 120f / 255f, 50f / 255f, 1f);
            var idleBg = new Color(60f / 255f, 40f / 255f, 20f / 255f, 0.9f);
            var activeBorder = new Color(1f, 220f / 255f, 150f / 255f, 1f);
            var idleBorder = new Color(120f / 255f, 80f / 255f, 30f / 255f, 1f);
            var bg = selected ? activeBg : idleBg;
            var bc = new StyleColor(selected ? activeBorder : idleBorder);
            btn.style.backgroundColor = new StyleColor(bg);
            btn.style.color = new StyleColor(selected ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 220f / 255f, 150f / 255f, 1f));
            btn.style.borderTopColor = bc;
            btn.style.borderBottomColor = bc;
            btn.style.borderLeftColor = bc;
            btn.style.borderRightColor = bc;
        }

        /// <summary>
        /// Hiển thị bảng "Kết thúc trận đấu" phủ toàn màn hình (Requirement 5.4). Dựng động
        /// bằng code nên không phụ thuộc tham chiếu scene (tránh lỗi missing reference của
        /// <c>_gameOverScreen</c>/<c>_gameOverDocument</c> trong <c>GameSceneRoot</c>).
        /// </summary>
        /// <param name="title">Tiêu đề lớn, ví dụ "Bạn đã thua cuộc".</param>
        /// <param name="scoreText">Dòng Điểm phiên.</param>
        /// <param name="highScoreText">Dòng Kỷ lục.</param>
        /// <param name="isNewHighScore">Khi <c>true</c>, hiện huy hiệu "Lập kỷ lục mới!".</param>
        /// <param name="onRestart">Callback khi nhấn "Chơi lại"; <c>null</c> để ẩn nút.</param>
        /// <param name="coinsText">
        /// Dòng Xu cổ (META) — vd "Xu cổ: +5 (tổng 42)". <c>null</c>/rỗng để ẩn (khi tắt hệ META).
        /// </param>
        /// <param name="onOpenShop">Callback nút "Cửa Hàng Xu Cổ"; <c>null</c> để ẩn nút (GDD Cơ chế 2).</param>
        public void ShowGameOverScreen(
            string title,
            string scoreText,
            string highScoreText,
            bool isNewHighScore,
            Action onRestart,
            string coinsText = null,
            Action onOpenShop = null)
        {
            BindRoot();
            if (Root == null)
            {
                return;
            }

            EnsureGameOverBuilt();

            _gameOverTitleLabel.text = string.IsNullOrEmpty(title) ? "Bạn đã thua cuộc" : title;
            _gameOverScoreLabel.text = scoreText ?? string.Empty;
            _gameOverHighScoreLabel.text = highScoreText ?? string.Empty;
            _gameOverRecordBadge.style.display = isNewHighScore ? DisplayStyle.Flex : DisplayStyle.None;
            _gameOverOnRestart = onRestart;
            _gameOverRestartButton.style.display = onRestart != null ? DisplayStyle.Flex : DisplayStyle.None;

            // GDD Cơ chế 2: dòng Xu cổ kiếm được + nút mở Cửa Hàng (nâng cấp vĩnh viễn).
            _gameOverCoinsLabel.text = coinsText ?? string.Empty;
            _gameOverCoinsLabel.style.display = string.IsNullOrEmpty(coinsText) ? DisplayStyle.None : DisplayStyle.Flex;
            _gameOverOnOpenShop = onOpenShop;
            _gameOverShopButton.style.display = onOpenShop != null ? DisplayStyle.Flex : DisplayStyle.None;

            if (_gameOverOverlay.parent != null)
            {
                _gameOverOverlay.RemoveFromHierarchy();
            }
            Root.Add(_gameOverOverlay);
            _gameOverOverlay.BringToFront();
            // Không auto-focus nút "Chơi lại": phím Space (chiêu Special) có thể tạo
            // NavigationSubmit và vô tình kích hoạt nút khiến game tự restart. Người chơi
            // phải bấm chuột vào nút để chơi lại.
        }

        /// <summary>Ẩn bảng "Kết thúc trận đấu" nếu đang hiển thị (không gọi callback).</summary>
        public void HideGameOverScreen()
        {
            if (_gameOverOverlay != null && _gameOverOverlay.parent != null)
            {
                _gameOverOverlay.RemoveFromHierarchy();
            }
            _gameOverOnRestart = null;
        }

        private void HandleGameOverRestart()
        {
            var cb = _gameOverOnRestart;
            HideGameOverScreen();
            cb?.Invoke();
        }

        private void HandleGameOverOpenShop() => _gameOverOnOpenShop?.Invoke();

        // 2px viền bo góc 8 cho nút (dùng chung cho các nút dựng inline).
        private static void ApplyButtonBorder(Button btn, Color border)
        {
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;
            var bc = new StyleColor(border);
            btn.style.borderTopColor = bc;
            btn.style.borderBottomColor = bc;
            btn.style.borderLeftColor = bc;
            btn.style.borderRightColor = bc;
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
        }

        // ==== Bảng "Nâng Cấp" 2 tab (phong cách Subway Surfers) ========================

        /// <summary>
        /// Mở bảng "Nâng Cấp" 2 tab: <b>Nâng Cấp Trong Trận</b> (9 thẻ cuộn dọc kiểu Subway
        /// Surfers — icon, tên, mô tả, thanh cấp, nút giá xanh) và <b>Nâng Cấp Đặc Biệt</b>
        /// (3 skill: mở khóa / nâng cấp). HUD chỉ render dữ liệu đã tính sẵn; bấm nút giá gọi
        /// callback tương ứng (subscriber mua rồi gọi lại hàm này để refresh). Nút "X" góc
        /// phải trên gọi <paramref name="onClose"/>. Người gọi chịu trách nhiệm pause/resume.
        /// </summary>
        /// <param name="matchRows">9 thẻ nâng cấp trong trận đã tính sẵn.</param>
        /// <param name="specialRows">3 skill Đặc Biệt; <c>null</c>/rỗng để ẩn tab Đặc Biệt.</param>
        /// <param name="gold">Số Vàng hiện có (hiện trên đầu bảng).</param>
        /// <param name="selectedTab">Tab mở sẵn: 0 = Trong Trận, 1 = Đặc Biệt.</param>
        /// <param name="onBuyMatch">Gọi khi bấm nút giá của một thẻ trong trận.</param>
        /// <param name="onBuySpecial">Gọi khi bấm nút mở khóa/nâng cấp của một skill Đặc Biệt.</param>
        /// <param name="onClose">Gọi khi bấm nút "X".</param>
        public void ShowUpgradeHub(
            IReadOnlyList<MatchUpgradeRow> matchRows,
            IReadOnlyList<SkillTabInfo> specialRows,
            int gold,
            int selectedTab,
            Action<MatchUpgradeKind> onBuyMatch,
            Action<SpecialSkillKind> onBuySpecial,
            Action onClose)
        {
            BindRoot();
            if (Root == null || matchRows == null)
            {
                return;
            }

            _matchRows = matchRows;
            _matchSpecialRows = specialRows;
            _matchGold = gold;
            _matchOnBuy = onBuyMatch;
            _matchOnBuySpecial = onBuySpecial;
            _matchOnClose = onClose;

            bool hasSpecial = specialRows != null && specialRows.Count > 0;
            _matchSelectedTab = hasSpecial ? Mathf.Clamp(selectedTab, 0, 1) : 0;

            EnsureMatchPanelBuilt();
            _matchGoldLabel.text = gold.ToString("N0");
            _specialTabButton.style.display = hasSpecial ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshHubTabStyles();
            RenderHubContent();

            if (_matchOverlay.parent != null)
            {
                _matchOverlay.RemoveFromHierarchy();
            }
            Root.Add(_matchOverlay);
            _matchOverlay.BringToFront();
        }

        /// <summary>Đóng bảng Nâng Cấp nếu đang mở (không gọi callback).</summary>
        public void CloseUpgradeHub()
        {
            if (_matchOverlay != null && _matchOverlay.parent != null)
            {
                _matchOverlay.RemoveFromHierarchy();
            }
            _matchRows = null;
            _matchSpecialRows = null;
            _matchOnBuy = null;
            _matchOnBuySpecial = null;
            _matchOnClose = null;
        }

        /// <summary>
        /// Dựng overlay bảng Nâng Cấp một lần (lazy). Tông sáng xanh da trời kiểu Subway
        /// Surfers: panel xanh nhạt, thẻ trắng viền xanh, nút giá xanh lá đậm.
        /// </summary>
        private void EnsureMatchPanelBuilt()
        {
            if (_matchOverlay != null)
            {
                return;
            }

            _matchOverlay = new VisualElement { name = "MatchUpgradeOverlay" };
            _matchOverlay.style.position = Position.Absolute;
            _matchOverlay.style.left = 0;
            _matchOverlay.style.top = 0;
            _matchOverlay.style.right = 0;
            _matchOverlay.style.bottom = 0;
            _matchOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.6f));
            _matchOverlay.style.alignItems = Align.Center;
            _matchOverlay.style.justifyContent = Justify.Center;
            _matchOverlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var panel = new VisualElement { name = "MatchUpgradePanel" };
            panel.style.width = 520;
            panel.style.maxHeight = Length.Percent(86f);
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 14;
            panel.style.backgroundColor = new StyleColor(new Color(168f / 255f, 214f / 255f, 240f / 255f, 1f));
            SetBorder(panel, 3, new Color(110f / 255f, 168f / 255f, 205f / 255f, 1f), 16);
            _matchOverlay.Add(panel);

            // Hàng đầu: tiêu đề + ô Vàng hiện có.
            var header = new VisualElement { name = "MatchUpgradeHeader" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 10;
            panel.Add(header);

            var title = new Label { text = "Nâng Cấp" };
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new StyleColor(new Color(20f / 255f, 90f / 255f, 150f / 255f, 1f));
            header.Add(title);

            // Cụm phải của header: [icon Xu + số Vàng] [nút X].
            var headerRight = new VisualElement();
            headerRight.style.flexDirection = FlexDirection.Row;
            headerRight.style.alignItems = Align.Center;
            header.Add(headerRight);

            if (_coinSprite != null)
            {
                var coin = new VisualElement();
                coin.style.width = 26;
                coin.style.height = 26;
                coin.style.marginRight = 6;
                coin.style.backgroundImage = new StyleBackground(_coinSprite);
                headerRight.Add(coin);
            }

            _matchGoldLabel = new Label();
            _matchGoldLabel.style.fontSize = 20;
            _matchGoldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _matchGoldLabel.style.color = new StyleColor(new Color(140f / 255f, 90f / 255f, 10f / 255f, 1f));
            _matchGoldLabel.style.marginRight = 12;
            headerRight.Add(_matchGoldLabel);

            // Nút "X" đóng bảng (thay cho nút "Tiếp tục" cũ).
            var closeX = new Button { name = "UpgradeHubClose", text = "X" };
            closeX.style.width = 34;
            closeX.style.height = 34;
            closeX.style.fontSize = 18;
            closeX.style.unityFontStyleAndWeight = FontStyle.Bold;
            closeX.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeX.style.backgroundColor = new StyleColor(new Color(214f / 255f, 76f / 255f, 60f / 255f, 1f));
            closeX.style.color = new StyleColor(Color.white);
            SetBorder(closeX, 2, new Color(160f / 255f, 45f / 255f, 35f / 255f, 1f), 10);
            closeX.clicked += () => _matchOnClose?.Invoke();
            headerRight.Add(closeX);

            // Hàng 2 tab: Nâng Cấp Trong Trận | Nâng Cấp Đặc Biệt.
            var tabsRow = new VisualElement { name = "UpgradeHubTabs" };
            tabsRow.style.flexDirection = FlexDirection.Row;
            tabsRow.style.marginBottom = 10;
            panel.Add(tabsRow);

            _matchTabButton = BuildHubTab("Nâng Cấp Trong Trận", 0);
            tabsRow.Add(_matchTabButton);
            _specialTabButton = BuildHubTab("Nâng Cấp Đặc Biệt", 1);
            tabsRow.Add(_specialTabButton);

            // Danh sách thẻ cuộn dọc (nội dung đổi theo tab).
            _matchCardList = new ScrollView(ScrollViewMode.Vertical) { name = "MatchUpgradeCards" };
            _matchCardList.style.flexGrow = 1;
            _matchCardList.style.flexShrink = 1;
            panel.Add(_matchCardList);
        }

        /// <summary>Một nút tab của bảng Nâng Cấp; bấm chuyển <see cref="_matchSelectedTab"/> và render lại.</summary>
        private Button BuildHubTab(string text, int tabIndex)
        {
            var btn = new Button { text = text };
            btn.style.flexGrow = 1;
            btn.style.height = 38;
            btn.style.marginLeft = tabIndex == 0 ? 0 : 6;
            btn.style.fontSize = 15;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.clicked += () =>
            {
                if (_matchSelectedTab == tabIndex)
                {
                    return;
                }
                _matchSelectedTab = tabIndex;
                RefreshHubTabStyles();
                RenderHubContent();
            };
            return btn;
        }

        /// <summary>Tab đang chọn: nền trắng chữ xanh đậm; tab nghỉ: nền xanh đậm chữ trắng.</summary>
        private void RefreshHubTabStyles()
        {
            StyleHubTabButton(_matchTabButton, _matchSelectedTab == 0);
            StyleHubTabButton(_specialTabButton, _matchSelectedTab == 1);
        }

        private static void StyleHubTabButton(Button btn, bool selected)
        {
            if (btn == null)
            {
                return;
            }

            if (selected)
            {
                btn.style.backgroundColor = new StyleColor(Color.white);
                btn.style.color = new StyleColor(new Color(20f / 255f, 90f / 255f, 150f / 255f, 1f));
                SetBorder(btn, 2, new Color(110f / 255f, 168f / 255f, 205f / 255f, 1f), 10);
            }
            else
            {
                btn.style.backgroundColor = new StyleColor(new Color(58f / 255f, 125f / 255f, 178f / 255f, 1f));
                btn.style.color = new StyleColor(Color.white);
                SetBorder(btn, 2, new Color(36f / 255f, 92f / 255f, 138f / 255f, 1f), 10);
            }
        }

        /// <summary>Render nội dung tab đang chọn vào danh sách cuộn (luôn cuộn về đầu).</summary>
        private void RenderHubContent()
        {
            _matchCardList.Clear();
            _matchCardList.scrollOffset = Vector2.zero;

            if (_matchSelectedTab == 1 && _matchSpecialRows != null)
            {
                for (int i = 0; i < _matchSpecialRows.Count; i++)
                {
                    _matchCardList.Add(BuildSpecialCard(_matchSpecialRows[i]));
                }
                return;
            }

            if (_matchRows == null)
            {
                return;
            }

            for (int i = 0; i < _matchRows.Count; i++)
            {
                _matchCardList.Add(BuildMatchCard(_matchRows[i]));
            }
        }

        /// <summary>
        /// Một thẻ skill Đặc Biệt cùng phong cách với thẻ nâng cấp trong trận: tên, mô tả,
        /// chip "Cấp N" + thanh vạch; nút dưới là "Mở khóa (giá)" khi còn khóa, ngược lại
        /// là nút giá nâng cấp. Hết Vàng → nút xám và vô hiệu.
        /// </summary>
        private VisualElement BuildSpecialCard(SkillTabInfo info)
        {
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingLeft = 6;
            card.style.paddingRight = 6;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.backgroundColor = new StyleColor(new Color(214f / 255f, 236f / 255f, 248f / 255f, 1f));
            SetBorder(card, 2, new Color(150f / 255f, 195f / 255f, 222f / 255f, 1f), 12);

            var inner = new VisualElement();
            inner.style.paddingLeft = 10;
            inner.style.paddingRight = 10;
            inner.style.paddingTop = 8;
            inner.style.paddingBottom = 10;
            inner.style.backgroundColor = new StyleColor(Color.white);
            SetBorder(inner, 0, Color.clear, 8);
            card.Add(inner);

            var nameLabel = new Label { text = info.Name + (info.IsUnlocked ? string.Empty : "  (chưa mở khóa)") };
            nameLabel.style.fontSize = 19;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = new StyleColor(info.IsUnlocked
                ? new Color(22f / 255f, 105f / 255f, 180f / 255f, 1f)
                : new Color(110f / 255f, 120f / 255f, 130f / 255f, 1f));
            nameLabel.style.marginBottom = 4;
            inner.Add(nameLabel);

            var descLabel = new Label { text = info.EffectDesc };
            descLabel.style.fontSize = 13;
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            descLabel.style.color = new StyleColor(new Color(60f / 255f, 88f / 255f, 110f / 255f, 1f));
            descLabel.style.marginBottom = 6;
            inner.Add(descLabel);

            int specialLevel = info.IsUnlocked ? info.Level : 0;
            inner.Add(BuildLevelBarRow(specialLevel, 6, specialLevel));

            int cost = info.IsUnlocked ? info.UpgradeCost : info.UnlockCost;
            var kind = info.Kind;
            inner.Add(BuildCostButton(
                $"SpecialBuy_{kind}",
                info.IsUnlocked ? null : "Mở khóa",
                cost,
                _matchGold >= cost,
                () => _matchOnBuySpecial?.Invoke(kind)));

            return card;
        }

        /// <summary>
        /// Một thẻ nâng cấp kiểu Subway Surfers: khung xanh nhạt → lòng trắng; trong lòng:
        /// [icon | tên + mô tả], thanh cấp (chip "Cấp N" + 6 vạch), nút giá xanh lá to bản
        /// với icon Xu. Hết Vàng → nút xám và vô hiệu.
        /// </summary>
        private VisualElement BuildMatchCard(MatchUpgradeRow row)
        {
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingLeft = 6;
            card.style.paddingRight = 6;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.backgroundColor = new StyleColor(new Color(214f / 255f, 236f / 255f, 248f / 255f, 1f));
            SetBorder(card, 2, new Color(150f / 255f, 195f / 255f, 222f / 255f, 1f), 12);

            var inner = new VisualElement();
            inner.style.paddingLeft = 10;
            inner.style.paddingRight = 10;
            inner.style.paddingTop = 8;
            inner.style.paddingBottom = 10;
            inner.style.backgroundColor = new StyleColor(Color.white);
            SetBorder(inner, 0, Color.clear, 8);
            card.Add(inner);

            // [icon | tên + mô tả]
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            topRow.style.marginBottom = 6;
            inner.Add(topRow);

            if (row.Icon != null)
            {
                var iconBox = new VisualElement();
                iconBox.style.width = 64;
                iconBox.style.height = 64;
                iconBox.style.marginRight = 10;
                iconBox.style.flexShrink = 0;
                iconBox.style.backgroundColor = new StyleColor(new Color(74f / 255f, 144f / 255f, 217f / 255f, 1f));
                SetBorder(iconBox, 2, new Color(40f / 255f, 100f / 255f, 170f / 255f, 1f), 10);
                iconBox.style.alignItems = Align.Center;
                iconBox.style.justifyContent = Justify.Center;

                var icon = new VisualElement();
                icon.style.width = 52;
                icon.style.height = 52;
                icon.style.backgroundImage = new StyleBackground(row.Icon);
                iconBox.Add(icon);
                topRow.Add(iconBox);
            }

            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            textCol.style.flexShrink = 1;
            topRow.Add(textCol);

            var nameLabel = new Label { text = row.Name };
            nameLabel.style.fontSize = 19;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = new StyleColor(new Color(22f / 255f, 105f / 255f, 180f / 255f, 1f));
            textCol.Add(nameLabel);

            var descLabel = new Label { text = row.Description };
            descLabel.style.fontSize = 13;
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            descLabel.style.color = new StyleColor(new Color(60f / 255f, 88f / 255f, 110f / 255f, 1f));
            textCol.Add(descLabel);

            if (!string.IsNullOrEmpty(row.EffectText))
            {
                var effectLabel = new Label { text = row.EffectText };
                effectLabel.style.fontSize = 13;
                effectLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                effectLabel.style.whiteSpace = WhiteSpace.Normal;
                effectLabel.style.color = new StyleColor(new Color(30f / 255f, 140f / 255f, 60f / 255f, 1f));
                textCol.Add(effectLabel);
            }

            inner.Add(BuildLevelBarRow(
                row.Level,
                row.BarSegments,
                row.BarFilled < 0 ? row.Level : row.BarFilled));

            var kind = row.Kind;
            inner.Add(BuildCostButton(
                $"MatchBuy_{kind}", null, row.Cost, row.CanAfford,
                () => _matchOnBuy?.Invoke(kind), row.IsMaxed));

            return card;
        }

        /// <summary>
        /// Thanh cấp dùng chung: chip "Cấp N" + <paramref name="segments"/> vạch, tô đầy
        /// <paramref name="filled"/> vạch (vượt số vạch vẫn hiện đúng số ở chip).
        /// </summary>
        private static VisualElement BuildLevelBarRow(int level, int segments, int filled)
        {
            var levelRow = new VisualElement();
            levelRow.style.flexDirection = FlexDirection.Row;
            levelRow.style.alignItems = Align.Center;
            levelRow.style.marginBottom = 8;

            var levelChip = new Label { text = $"Cấp {level}" };
            levelChip.style.fontSize = 12;
            levelChip.style.unityFontStyleAndWeight = FontStyle.Bold;
            levelChip.style.color = new StyleColor(Color.white);
            levelChip.style.backgroundColor = new StyleColor(new Color(46f / 255f, 74f / 255f, 94f / 255f, 1f));
            levelChip.style.paddingLeft = 8;
            levelChip.style.paddingRight = 8;
            levelChip.style.paddingTop = 2;
            levelChip.style.paddingBottom = 2;
            levelChip.style.marginRight = 8;
            SetBorder(levelChip, 0, Color.clear, 8);
            levelRow.Add(levelChip);

            if (segments < 1) segments = 1;
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.flexGrow = 1;
            bar.style.height = 14;
            bar.style.backgroundColor = new StyleColor(new Color(46f / 255f, 74f / 255f, 94f / 255f, 1f));
            bar.style.paddingLeft = 2;
            bar.style.paddingRight = 2;
            bar.style.paddingTop = 2;
            bar.style.paddingBottom = 2;
            SetBorder(bar, 0, Color.clear, 7);
            for (int s = 0; s < segments; s++)
            {
                var seg = new VisualElement();
                seg.style.flexGrow = 1;
                seg.style.marginLeft = s == 0 ? 0 : 2;
                bool isFilled = filled > s;
                seg.style.backgroundColor = new StyleColor(isFilled
                    ? new Color(110f / 255f, 205f / 255f, 60f / 255f, 1f)
                    : new Color(255f / 255f, 255f / 255f, 255f / 255f, 0.18f));
                SetBorder(seg, 0, Color.clear, 4);
                bar.Add(seg);
            }
            levelRow.Add(bar);

            return levelRow;
        }

        /// <summary>
        /// Nút giá xanh lá to bản dùng chung: [tiền tố tùy chọn + số Vàng + icon Xu].
        /// Hết Vàng → xám + vô hiệu; <paramref name="maxed"/> → "Tối đa" xám + vô hiệu.
        /// </summary>
        private Button BuildCostButton(
            string name, string prefix, int cost, bool canAfford, Action onClick, bool maxed = false)
        {
            var buyButton = new Button { name = name };
            buyButton.style.height = 44;
            buyButton.style.flexDirection = FlexDirection.Row;
            buyButton.style.alignItems = Align.Center;
            buyButton.style.justifyContent = Justify.Center;

            if (maxed)
            {
                buyButton.text = "Tối đa";
                buyButton.style.fontSize = 18;
                buyButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                buyButton.style.color = new StyleColor(Color.white);
                buyButton.style.backgroundColor = new StyleColor(new Color(150f / 255f, 160f / 255f, 165f / 255f, 1f));
                SetBorder(buyButton, 2, new Color(110f / 255f, 120f / 255f, 125f / 255f, 1f), 10);
                buyButton.SetEnabled(false);
                return buyButton;
            }

            if (canAfford)
            {
                buyButton.style.backgroundColor = new StyleColor(new Color(88f / 255f, 195f / 255f, 34f / 255f, 1f));
                SetBorder(buyButton, 2, new Color(62f / 255f, 154f / 255f, 18f / 255f, 1f), 10);
            }
            else
            {
                buyButton.style.backgroundColor = new StyleColor(new Color(150f / 255f, 160f / 255f, 165f / 255f, 1f));
                SetBorder(buyButton, 2, new Color(110f / 255f, 120f / 255f, 125f / 255f, 1f), 10);
                buyButton.SetEnabled(false);
            }
            buyButton.clicked += () => onClick?.Invoke();

            var costLabel = new Label
            {
                text = string.IsNullOrEmpty(prefix)
                    ? cost.ToString("N0")
                    : $"{prefix}  {cost:N0}",
            };
            costLabel.style.fontSize = 20;
            costLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            costLabel.style.color = new StyleColor(Color.white);
            costLabel.pickingMode = PickingMode.Ignore;
            buyButton.Add(costLabel);

            if (_coinSprite != null)
            {
                var coin = new VisualElement();
                coin.style.width = 24;
                coin.style.height = 24;
                coin.style.marginLeft = 8;
                coin.style.backgroundImage = new StyleBackground(_coinSprite);
                coin.pickingMode = PickingMode.Ignore;
                buyButton.Add(coin);
            }

            return buyButton;
        }

        /// <summary>Viền + bo góc đồng nhất bốn cạnh cho phần tử dựng inline.</summary>
        private static void SetBorder(VisualElement el, int width, Color color, int radius)
        {
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            var c = new StyleColor(color);
            el.style.borderTopColor = c;
            el.style.borderBottomColor = c;
            el.style.borderLeftColor = c;
            el.style.borderRightColor = c;
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }

        // ==== Cửa Hàng Xu Cổ (META — GDD Cơ chế 2) =====================================

        /// <summary>
        /// Mở "Cửa Hàng Xu Cổ" — bảng nâng cấp VĨNH VIỄN, mở chồng lên màn Game Over.
        /// HUD chỉ render <paramref name="rows"/> + số dư <paramref name="coins"/>; bấm "Mua"
        /// gọi <paramref name="onBuy"/> (subscriber mua + ghi lưu rồi gọi lại hàm này để refresh).
        /// </summary>
        public void ShowMetaShopModal(
            long coins,
            IReadOnlyList<MetaShopRow> rows,
            Action<MetaUpgradeTrack> onBuy,
            Action onClose)
        {
            BindRoot();
            if (Root == null || rows == null)
            {
                return;
            }

            _shopOnBuy = onBuy;
            _shopOnClose = onClose;

            EnsureShopBuilt();
            _shopCoinsLabel.text = "Xu cổ: " + coins;
            RenderShopRows(rows);

            if (_shopOverlay.parent != null)
            {
                _shopOverlay.RemoveFromHierarchy();
            }
            Root.Add(_shopOverlay);
            _shopOverlay.BringToFront();
        }

        /// <summary>Đóng Cửa Hàng Xu Cổ nếu đang mở (không gọi callback).</summary>
        public void CloseMetaShopModal()
        {
            if (_shopOverlay != null && _shopOverlay.parent != null)
            {
                _shopOverlay.RemoveFromHierarchy();
            }
        }

        private void EnsureShopBuilt()
        {
            if (_shopOverlay != null)
            {
                return;
            }

            _shopOverlay = new VisualElement { name = "MetaShopOverlay" };
            _shopOverlay.style.position = Position.Absolute;
            _shopOverlay.style.left = 0;
            _shopOverlay.style.top = 0;
            _shopOverlay.style.right = 0;
            _shopOverlay.style.bottom = 0;
            _shopOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.7f));
            _shopOverlay.style.alignItems = Align.Center;
            _shopOverlay.style.justifyContent = Justify.Center;
            _shopOverlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var panel = new VisualElement { name = "MetaShopPanel" };
            panel.style.minWidth = 460;
            panel.style.maxWidth = 620;
            panel.style.paddingLeft = 26;
            panel.style.paddingRight = 26;
            panel.style.paddingTop = 22;
            panel.style.paddingBottom = 22;
            panel.style.backgroundColor = new StyleColor(new Color(30f / 255f, 22f / 255f, 12f / 255f, 0.99f));
            panel.style.borderTopWidth = 3;
            panel.style.borderBottomWidth = 3;
            panel.style.borderLeftWidth = 3;
            panel.style.borderRightWidth = 3;
            var borderCol = new StyleColor(new Color(200f / 255f, 150f / 255f, 60f / 255f, 1f));
            panel.style.borderTopColor = borderCol;
            panel.style.borderBottomColor = borderCol;
            panel.style.borderLeftColor = borderCol;
            panel.style.borderRightColor = borderCol;
            panel.style.borderTopLeftRadius = 14;
            panel.style.borderTopRightRadius = 14;
            panel.style.borderBottomLeftRadius = 14;
            panel.style.borderBottomRightRadius = 14;
            panel.style.alignItems = Align.Stretch;
            _shopOverlay.Add(panel);

            var title = new Label { name = "MetaShopTitle", text = "Cửa Hàng Xu Cổ" };
            title.style.fontSize = 26;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new StyleColor(new Color(1f, 220f / 255f, 140f / 255f, 1f));
            title.style.marginBottom = 4;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(title);

            var subtitle = new Label { text = "Nâng cấp vĩnh viễn — giữ qua mọi trận" };
            subtitle.style.fontSize = 14;
            subtitle.style.color = new StyleColor(new Color(190f / 255f, 175f / 255f, 150f / 255f, 1f));
            subtitle.style.marginBottom = 10;
            subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(subtitle);

            _shopCoinsLabel = new Label { name = "MetaShopCoins" };
            _shopCoinsLabel.style.fontSize = 20;
            _shopCoinsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _shopCoinsLabel.style.color = new StyleColor(new Color(1f, 215f / 255f, 110f / 255f, 1f));
            _shopCoinsLabel.style.marginBottom = 14;
            _shopCoinsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_shopCoinsLabel);

            _shopRowsContainer = new VisualElement { name = "MetaShopRows" };
            panel.Add(_shopRowsContainer);

            var closeButton = new Button { name = "MetaShopClose", text = "Đóng" };
            closeButton.style.height = 44;
            closeButton.style.marginTop = 10;
            closeButton.style.fontSize = 18;
            closeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeButton.style.backgroundColor = new StyleColor(new Color(120f / 255f, 60f / 255f, 50f / 255f, 0.95f));
            closeButton.style.color = new StyleColor(new Color(0.96f, 1f, 0.96f, 1f));
            ApplyButtonBorder(closeButton, new Color(200f / 255f, 110f / 255f, 90f / 255f, 1f));
            closeButton.clicked += () => _shopOnClose?.Invoke();
            panel.Add(closeButton);
        }

        private void RenderShopRows(IReadOnlyList<MetaShopRow> rows)
        {
            _shopRowsContainer.Clear();

            foreach (var row in rows)
            {
                var rowEl = new VisualElement();
                rowEl.style.flexDirection = FlexDirection.Row;
                rowEl.style.alignItems = Align.Center;
                rowEl.style.justifyContent = Justify.SpaceBetween;
                rowEl.style.marginBottom = 8;
                rowEl.style.paddingLeft = 12;
                rowEl.style.paddingRight = 12;
                rowEl.style.paddingTop = 10;
                rowEl.style.paddingBottom = 10;
                rowEl.style.backgroundColor = new StyleColor(new Color(50f / 255f, 36f / 255f, 18f / 255f, 0.95f));
                rowEl.style.borderTopLeftRadius = 8;
                rowEl.style.borderTopRightRadius = 8;
                rowEl.style.borderBottomLeftRadius = 8;
                rowEl.style.borderBottomRightRadius = 8;

                var info = new VisualElement();
                info.style.flexGrow = 1;
                info.style.flexShrink = 1;

                var nameLabel = new Label { text = $"{row.Name}  (Cấp {row.Level}/{row.MaxLevel})" };
                nameLabel.style.fontSize = 17;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLabel.style.color = new StyleColor(new Color(1f, 230f / 255f, 180f / 255f, 1f));
                info.Add(nameLabel);

                var effectLabel = new Label { text = row.EffectDesc };
                effectLabel.style.fontSize = 13;
                effectLabel.style.whiteSpace = WhiteSpace.Normal;
                effectLabel.style.color = new StyleColor(new Color(210f / 255f, 200f / 255f, 185f / 255f, 1f));
                info.Add(effectLabel);

                rowEl.Add(info);

                var track = row.Track;
                var buyButton = new Button();
                buyButton.style.minWidth = 130;
                buyButton.style.height = 44;
                buyButton.style.marginLeft = 12;
                buyButton.style.fontSize = 16;
                buyButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                buyButton.style.unityTextAlign = TextAnchor.MiddleCenter;
                buyButton.style.color = new StyleColor(new Color(0.96f, 1f, 0.96f, 1f));

                if (row.IsMaxed)
                {
                    buyButton.text = "Tối đa";
                    buyButton.style.backgroundColor = new StyleColor(new Color(70f / 255f, 70f / 255f, 70f / 255f, 0.9f));
                    ApplyButtonBorder(buyButton, new Color(120f / 255f, 120f / 255f, 120f / 255f, 1f));
                    buyButton.SetEnabled(false);
                }
                else
                {
                    buyButton.text = $"Mua ({row.Cost})";
                    var bg = row.CanAfford
                        ? new Color(60f / 255f, 120f / 255f, 70f / 255f, 0.95f)
                        : new Color(90f / 255f, 60f / 255f, 50f / 255f, 0.9f);
                    buyButton.style.backgroundColor = new StyleColor(bg);
                    ApplyButtonBorder(buyButton, new Color(120f / 255f, 200f / 255f, 130f / 255f, 1f));
                    buyButton.SetEnabled(row.CanAfford);
                    buyButton.clicked += () => _shopOnBuy?.Invoke(track);
                }

                rowEl.Add(buyButton);
                _shopRowsContainer.Add(rowEl);
            }
        }

        /// <summary>
        /// Dựng overlay Game Over một lần (lazy). Toàn bộ layout dùng inline style để không
        /// phụ thuộc reload USS — tông màu "thất trận" đỏ-đồng trên nền tối.
        /// </summary>
        private void EnsureGameOverBuilt()
        {
            if (_gameOverOverlay != null)
            {
                return;
            }

            _gameOverOverlay = new VisualElement { name = "GameOverOverlay" };
            _gameOverOverlay.style.position = Position.Absolute;
            _gameOverOverlay.style.left = 0;
            _gameOverOverlay.style.top = 0;
            _gameOverOverlay.style.right = 0;
            _gameOverOverlay.style.bottom = 0;
            _gameOverOverlay.style.backgroundColor = new StyleColor(new Color(0.02f, 0.02f, 0.04f, 0.82f));
            _gameOverOverlay.style.alignItems = Align.Center;
            _gameOverOverlay.style.justifyContent = Justify.Center;
            _gameOverOverlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var panel = new VisualElement { name = "GameOverPanel" };
            panel.style.minWidth = 420;
            panel.style.maxWidth = 600;
            panel.style.paddingLeft = 36;
            panel.style.paddingRight = 36;
            panel.style.paddingTop = 32;
            panel.style.paddingBottom = 32;
            panel.style.backgroundColor = new StyleColor(new Color(28f / 255f, 16f / 255f, 12f / 255f, 0.98f));
            panel.style.borderTopWidth = 3;
            panel.style.borderBottomWidth = 3;
            panel.style.borderLeftWidth = 3;
            panel.style.borderRightWidth = 3;
            var borderCol = new StyleColor(new Color(200f / 255f, 70f / 255f, 50f / 255f, 1f));
            panel.style.borderTopColor = borderCol;
            panel.style.borderBottomColor = borderCol;
            panel.style.borderLeftColor = borderCol;
            panel.style.borderRightColor = borderCol;
            panel.style.borderTopLeftRadius = 16;
            panel.style.borderTopRightRadius = 16;
            panel.style.borderBottomLeftRadius = 16;
            panel.style.borderBottomRightRadius = 16;
            panel.style.alignItems = Align.Center;
            _gameOverOverlay.Add(panel);

            _gameOverTitleLabel = new Label { name = "GameOverTitle", text = "Bạn đã thua cuộc" };
            _gameOverTitleLabel.style.fontSize = 38;
            _gameOverTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _gameOverTitleLabel.style.color = new StyleColor(new Color(1f, 90f / 255f, 70f / 255f, 1f));
            _gameOverTitleLabel.style.marginBottom = 18;
            _gameOverTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_gameOverTitleLabel);

            _gameOverRecordBadge = new Label { name = "GameOverRecord", text = "Lập kỷ lục mới!" };
            _gameOverRecordBadge.style.fontSize = 20;
            _gameOverRecordBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            _gameOverRecordBadge.style.color = new StyleColor(new Color(1f, 220f / 255f, 120f / 255f, 1f));
            _gameOverRecordBadge.style.marginBottom = 14;
            _gameOverRecordBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            _gameOverRecordBadge.style.display = DisplayStyle.None;
            panel.Add(_gameOverRecordBadge);

            _gameOverScoreLabel = new Label { name = "GameOverScore" };
            _gameOverScoreLabel.style.fontSize = 22;
            _gameOverScoreLabel.style.color = new StyleColor(new Color(240f / 255f, 230f / 255f, 215f / 255f, 1f));
            _gameOverScoreLabel.style.marginBottom = 6;
            _gameOverScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_gameOverScoreLabel);

            _gameOverHighScoreLabel = new Label { name = "GameOverHighScore" };
            _gameOverHighScoreLabel.style.fontSize = 18;
            _gameOverHighScoreLabel.style.color = new StyleColor(new Color(200f / 255f, 190f / 255f, 175f / 255f, 1f));
            _gameOverHighScoreLabel.style.marginBottom = 10;
            _gameOverHighScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_gameOverHighScoreLabel);

            // GDD Cơ chế 2: dòng Xu cổ kiếm được trong trận (ẩn khi tắt hệ META).
            _gameOverCoinsLabel = new Label { name = "GameOverCoins" };
            _gameOverCoinsLabel.style.fontSize = 20;
            _gameOverCoinsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _gameOverCoinsLabel.style.color = new StyleColor(new Color(1f, 215f / 255f, 120f / 255f, 1f));
            _gameOverCoinsLabel.style.marginBottom = 20;
            _gameOverCoinsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _gameOverCoinsLabel.style.display = DisplayStyle.None;
            panel.Add(_gameOverCoinsLabel);

            // Nút mở Cửa Hàng Xu Cổ (nâng cấp vĩnh viễn) — ẩn khi tắt hệ META.
            _gameOverShopButton = new Button { name = "GameOverShop", text = "Cửa Hàng Xu Cổ" };
            _gameOverShopButton.style.minWidth = 200;
            _gameOverShopButton.style.height = 46;
            _gameOverShopButton.style.marginBottom = 10;
            _gameOverShopButton.style.paddingLeft = 18;
            _gameOverShopButton.style.paddingRight = 18;
            _gameOverShopButton.style.fontSize = 18;
            _gameOverShopButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _gameOverShopButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            _gameOverShopButton.style.backgroundColor = new StyleColor(new Color(150f / 255f, 110f / 255f, 40f / 255f, 0.98f));
            _gameOverShopButton.style.color = new StyleColor(new Color(1f, 245f / 255f, 220f / 255f, 1f));
            ApplyButtonBorder(_gameOverShopButton, new Color(1f, 200f / 255f, 110f / 255f, 1f));
            _gameOverShopButton.style.display = DisplayStyle.None;
            _gameOverShopButton.clicked += HandleGameOverOpenShop;
            panel.Add(_gameOverShopButton);

            _gameOverRestartButton = new Button { name = "GameOverRestart", text = "Chơi lại" };
            _gameOverRestartButton.style.minWidth = 160;
            _gameOverRestartButton.style.height = 50;
            _gameOverRestartButton.style.paddingLeft = 20;
            _gameOverRestartButton.style.paddingRight = 20;
            _gameOverRestartButton.style.fontSize = 20;
            _gameOverRestartButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _gameOverRestartButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            _gameOverRestartButton.style.backgroundColor = new StyleColor(new Color(170f / 255f, 60f / 255f, 45f / 255f, 0.98f));
            _gameOverRestartButton.style.color = new StyleColor(new Color(1f, 245f / 255f, 235f / 255f, 1f));
            _gameOverRestartButton.style.borderTopWidth = 2;
            _gameOverRestartButton.style.borderBottomWidth = 2;
            _gameOverRestartButton.style.borderLeftWidth = 2;
            _gameOverRestartButton.style.borderRightWidth = 2;
            var btnBorder = new StyleColor(new Color(1f, 150f / 255f, 110f / 255f, 1f));
            _gameOverRestartButton.style.borderTopColor = btnBorder;
            _gameOverRestartButton.style.borderBottomColor = btnBorder;
            _gameOverRestartButton.style.borderLeftColor = btnBorder;
            _gameOverRestartButton.style.borderRightColor = btnBorder;
            _gameOverRestartButton.style.borderTopLeftRadius = 8;
            _gameOverRestartButton.style.borderTopRightRadius = 8;
            _gameOverRestartButton.style.borderBottomLeftRadius = 8;
            _gameOverRestartButton.style.borderBottomRightRadius = 8;
            _gameOverRestartButton.clicked += HandleGameOverRestart;
            panel.Add(_gameOverRestartButton);
        }

        /// <summary>
        /// Áp <see cref="HudSnapshot"/> mới lên các Label/VisualElement của HUD.
        /// Được thiết kế để gọi mỗi frame từ <c>GameSceneRoot.LateUpdate</c> hoặc đồng
        /// bộ ngay sau khi state đổi để giữ độ trễ &lt; 100 ms (Requirement 13.2).
        /// Phương thức bỏ qua các trường <c>null</c> nếu UXML thiếu element tương ứng,
        /// giúp HUD vẫn render được khi prefab/UXML đang chỉnh sửa trong Editor.
        /// </summary>
        /// <param name="snap">Snapshot trận đấu mới nhất.</param>
        public void ApplySnapshot(HudSnapshot snap)
        {
            BindRoot();

            // TopCenter — Đợt hiện tại + (tùy chọn) Đợt kế tiếp & Đếm ngược (Req 7.3, 7.6, 9.1).
            // Đợt boss: hiện "BOSS: <tên>" thay cho "Đợt {N}/∞".
            int safeWave = snap.WaveNumber > 0 ? snap.WaveNumber : 1;
            if (_waveLabel != null)
            {
                _waveLabel.text = string.IsNullOrEmpty(snap.BossName)
                    ? Format.Wave(safeWave)
                    : "BOSS: " + snap.BossName;
            }

            int safeCountdown = snap.CountdownSeconds > 0 ? snap.CountdownSeconds : 0;
            if (_nextWaveLabel != null)
            {
                _nextWaveLabel.text = snap.ShowNextWave ? Format.NextWave(safeWave) : string.Empty;
            }

            if (_countdownLabel != null)
            {
                _countdownLabel.text = snap.ShowNextWave ? Format.Countdown(safeCountdown) : string.Empty;
            }

            // Thời gian Đợt — trong Pha_Chuẩn_Bị làm trống (đã có Countdown chuẩn bị);
            // trong Đợt đang chạy hiển thị đồng hồ đếm ngược của Đợt (chế độ time-based)
            // hoặc thời gian đã trôi (chế độ cũ). Khi đã dọn sạch sớm, hiển thị đếm ngược
            // ân hạn trước khi skip sang Đợt kế.
            if (_waveElapsedLabel != null)
            {
                if (snap.ShowNextWave)
                {
                    _waveElapsedLabel.text = string.Empty;
                }
                else if (snap.IsEarlyClearPending)
                {
                    _waveElapsedLabel.text = Format.EarlyClear(snap.EarlyClearCountdownSeconds);
                }
                else if (snap.WaveTimeRemainingSeconds > 0f)
                {
                    _waveElapsedLabel.text = Format.WaveCountdown(snap.WaveTimeRemainingSeconds);
                }
                else
                {
                    _waveElapsedLabel.text = Format.WaveElapsed(snap.WaveElapsedSeconds);
                }
            }

            // TopRight — Điểm phiên & Kỷ lục (Req 8.4, 9.2).
            if (_scoreLabel != null)
            {
                _scoreLabel.text = "Điểm: " + snap.SessionScore;
            }

            if (_highScoreLabel != null)
            {
                _highScoreLabel.text = "Cao nhất: " + snap.HighScore;
            }

            // BottomLeft — Cấp_Thành + vòng tiến trình EXP (Req 4.4, 4.5, 9.4).
            int safeLevel = snap.Level > 0 ? snap.Level : 1;
            if (_levelLabel != null)
            {
                _levelLabel.text = Format.Level(safeLevel);
            }

            // Vàng hiện có — hiển thị ngay dưới "Cấp: X" và trên các icon nâng cấp (cùng BottomLeft).
            // Khi đã có icon Xu (Match_Coin) thì chỉ hiện con số; icon Xu đóng vai ký hiệu tiền tệ.
            if (_goldLabel != null)
            {
                int safeGold = snap.Gold > 0 ? snap.Gold : 0;
                _goldLabel.text = _goldIconApplied ? safeGold.ToString() : "Vàng: " + safeGold;
            }

            if (_expProgress != null)
            {
                float ratio = Format.ExpRatio(snap.CurrentExp, snap.RequiredExp);
                // Scale theo trục Y để vòng tròn "đầy dần" theo tỉ lệ; giữ Z = 1.
                _expProgress.style.scale = new StyleScale(new Scale(new Vector3(1f, ratio, 1f)));
            }

            // BottomCenter — Máu (Req 5.5, 9.5).
            if (snap.MaxHp > 0)
            {
                int hp = snap.Hp;
                if (hp < 0) hp = 0;
                if (hp > snap.MaxHp) hp = snap.MaxHp;

                if (_hpLabel != null)
                {
                    _hpLabel.text = Format.Hp(hp, snap.MaxHp);
                }

                // Thanh máu HUD: fill co theo tỉ lệ HP + đổi màu xanh→vàng→đỏ.
                if (_hpBarFill != null)
                {
                    float ratio = (float)hp / snap.MaxHp;
                    _hpBarFill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
                    _hpBarFill.style.backgroundColor = new StyleColor(HealthBarBuilder.HpColor(ratio));
                }
            }

            // BottomRight — trạng thái 3 skill Special (khóa/hồi chiêu) trên icon.
            UpdateSkillIcon(_iconSkillTrongDong, _cooldownTrongDong, snap.TrongDong);
            UpdateSkillIcon(_iconSkillMuiTen, _cooldownMuiTen, snap.MuiTen);
            UpdateSkillIcon(_iconSkillLuoiGuom, _cooldownLuoiGuom, snap.LuoiGuom);
        }

        // ==== Xu rơi khi diệt Quái (Match_Coin) =========================================

        /// <summary>
        /// Thả một đồng Xu (Match_Coin) tại vị trí thế giới <paramref name="worldPos"/> (chỗ Quái
        /// chết). Xu NẰM YÊN tại đó cho tới khi <see cref="CollectAllCoins"/> được gọi (đầu đợt
        /// đếm ngược sang Đợt kế) mới bay về ô Vàng; tới nơi gọi <paramref name="onCredited"/> để
        /// cộng Phần_Thưởng_Vàng. Thiếu HUD/sprite/camera thì gọi ngay <paramref name="onCredited"/>
        /// để không mất thưởng.
        /// </summary>
        public void DropGoldCoin(Vector3 worldPos, Action onCredited)
        {
            BindRoot();

            var cam = Camera.main;
            if (Root == null || Root.panel == null || _coinSprite == null || cam == null)
            {
                onCredited?.Invoke();
                return;
            }

            // world (gốc dưới-trái) → screen → panel (gốc trên-trái), cùng quy ước IsPointerOverUI.
            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            Vector2 topDown = new Vector2(screen.x, Screen.height - screen.y);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(Root.panel, topDown);

            float size = Mathf.Max(8f, _coinFlySizePx);
            var coin = new VisualElement { name = "GroundCoin" };
            coin.style.position = Position.Absolute;
            coin.style.width = size;
            coin.style.height = size;
            coin.style.backgroundImage = new StyleBackground(_coinSprite);
            coin.pickingMode = PickingMode.Ignore;
            coin.style.left = panelPos.x - size * 0.5f;
            coin.style.top = panelPos.y - size * 0.5f;
            Root.Add(coin);

            _coins.Add(new Coin
            {
                Element = coin,
                RestPos = panelPos,
                Age = 0f,
                BobPhase = UnityEngine.Random.value * 6.2832f,
                Flying = false,
                OnCredited = onCredited,
            });
        }

        /// <summary>
        /// Cho TẤT CẢ Xu đang nằm trên Sân_Đấu lần lượt bay về ô Vàng (mỗi đồng lệch nhau
        /// <see cref="_coinCollectStaggerSeconds"/> để tạo dòng chảy). Gọi khi vào đợt đếm ngược
        /// sang Đợt kế. Mỗi đồng tới nơi sẽ cộng Vàng qua <c>OnCredited</c>. Gọi lại khi đang thu
        /// dở là an toàn (Xu đã bay sẽ được bỏ qua).
        /// </summary>
        public void CollectAllCoins()
        {
            float stagger = 0f;
            for (int i = 0; i < _coins.Count; i++)
            {
                var c = _coins[i];
                if (c.Flying)
                {
                    continue;
                }
                c.Flying = true;
                c.Delay = stagger;
                c.Elapsed = 0f;
                c.Duration = Mathf.Max(0.05f, _coinFlyDurationSeconds);
                stagger += Mathf.Max(0f, _coinCollectStaggerSeconds);
            }
        }

        /// <summary>
        /// Advance các đồng Xu. Xu chưa thu thì nằm yên (pop xuất hiện + bob nhẹ tại chỗ); Xu đang
        /// thu thì bay về ô Vàng, tới nơi gọi <c>OnCredited</c> rồi tự gỡ. Dùng
        /// <see cref="Time.unscaledDeltaTime"/> để chạy mượt kể cả khi game tạm dừng (modal mở).
        /// </summary>
        private void Update()
        {
            if (_coins.Count == 0)
            {
                return;
            }

            Vector2 target = GoldTargetPanelPosition();
            float dt = Time.unscaledDeltaTime;
            float half = Mathf.Max(8f, _coinFlySizePx) * 0.5f;

            for (int i = _coins.Count - 1; i >= 0; i--)
            {
                var c = _coins[i];

                // Chưa thu → nằm yên: pop xuất hiện rồi bob nhẹ để dễ nhận ra là Xu nhặt được.
                if (!c.Flying)
                {
                    c.Age += dt;
                    if (c.Element != null)
                    {
                        float pop = c.Age < 0.12f ? Mathf.Lerp(1.4f, 1f, c.Age / 0.12f) : 1f;
                        c.Element.style.scale = new StyleScale(new Scale(new Vector3(pop, pop, 1f)));
                        float bob = Mathf.Sin((c.Age + c.BobPhase) * 3.5f) * 3f;
                        c.Element.style.left = c.RestPos.x - half;
                        c.Element.style.top = c.RestPos.y - half + bob;
                    }
                    continue;
                }

                // Stagger: chờ tới lượt rồi mới bay.
                if (c.Delay > 0f)
                {
                    c.Delay -= dt;
                    continue;
                }

                c.Elapsed += dt;
                float t = c.Duration > 0f ? Mathf.Clamp01(c.Elapsed / c.Duration) : 1f;
                float eased = t * t * (3f - 2f * t); // smoothstep

                Vector2 center = Vector2.Lerp(c.RestPos, target, eased);
                center.y -= Mathf.Sin(t * Mathf.PI) * 48f; // vồng lên giữa đường cho có "lực hút"

                if (c.Element != null)
                {
                    c.Element.style.left = center.x - half;
                    c.Element.style.top = center.y - half;
                    float s = Mathf.Lerp(1f, 0.6f, eased); // co nhỏ dần khi tới ô Vàng
                    c.Element.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
                }

                if (t >= 1f)
                {
                    c.OnCredited?.Invoke();
                    c.Element?.RemoveFromHierarchy();
                    _coins.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Tâm ô Vàng trong panel coords — ưu tiên icon Xu, rồi nhãn Vàng; fallback góc dưới-trái
        /// khi layout chưa sẵn sàng.
        /// </summary>
        private Vector2 GoldTargetPanelPosition()
        {
            VisualElement targetEl = _goldIcon ?? _goldLabel;
            if (targetEl != null)
            {
                Rect wb = targetEl.worldBound;
                if (wb.width > 0f && !float.IsNaN(wb.x))
                {
                    return new Vector2(wb.x + wb.width * 0.5f, wb.y + wb.height * 0.5f);
                }
            }

            float h = (Root != null && Root.worldBound.height > 0f) ? Root.worldBound.height : Screen.height;
            return new Vector2(64f, h - 64f);
        }
    }
}
