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

        private bool _bound;

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
        private Button _gameOverRestartButton;
        private Action _gameOverOnRestart;

        /// <summary><c>true</c> khi bảng nâng cấp hoặc bảng Skill Đặc biệt đang mở (game nên tạm dừng).</summary>
        public bool IsModalOpen =>
            (_modalOverlay != null && _modalOverlay.parent != null)
            || (_hubOverlay != null && _hubOverlay.parent != null);

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

        /// <summary>Truy cập vùng TopLeft cho avatar Quái (Requirement 9.3).</summary>
        public VisualElement TopLeftRegion => _topLeft;

        /// <summary>Truy cập vùng BottomRight cho art Thành (Requirement 9.6).</summary>
        public VisualElement BottomRightRegion => _bottomRight;

        /// <summary>Truy cập vòng tiến trình EXP để các view phụ (gradient, sprite) gắn vào.</summary>
        public VisualElement ExpProgressElement => _expProgress;

        /// <summary>Truy cập <see cref="UIDocument.rootVisualElement"/> chuẩn hóa cho các view khác.</summary>
        public VisualElement Root { get; private set; }

        private void Awake()
        {
            BindRoot();
        }

        private void OnEnable()
        {
            BindRoot();
            BindIcons();
        }

        private void OnDisable()
        {
            UnbindIcons();
        }

        private void BindRoot()
        {
            if (_bound)
            {
                return;
            }

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

            _cooldownTrongDong = EnsureCooldownLabel(_iconSkillTrongDong);
            _cooldownMuiTen = EnsureCooldownLabel(_iconSkillMuiTen);
            _cooldownLuoiGuom = EnsureCooldownLabel(_iconSkillLuoiGuom);

            ApplyIconSprites();

            _bound = _topLeft != null && _topCenter != null && _topRight != null
                && _bottomLeft != null && _bottomCenter != null && _bottomRight != null;
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
        }

        private void HandleAttackClicked() => OnIconAttackClicked?.Invoke();
        private void HandleArmorClicked() => OnIconArmorClicked?.Invoke();
        private void HandleSpecialClicked() => OnIconSpecialClicked?.Invoke();
        private void HandleSkillTrongDongClicked() => OnSkillIconClicked?.Invoke(SpecialSkillKind.TrongDong);
        private void HandleSkillMuiTenClicked() => OnSkillIconClicked?.Invoke(SpecialSkillKind.MuiTen);
        private void HandleSkillLuoiGuomClicked() => OnSkillIconClicked?.Invoke(SpecialSkillKind.LuoiGuom);
        private void HandleExpClicked() => OnIconExpClicked?.Invoke();

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
        public void ShowUpgradeModal(
            string title,
            string body,
            string confirmText,
            Action onConfirm,
            Action onCancel = null,
            bool confirmEnabled = true)
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
        public void ShowGameOverScreen(
            string title,
            string scoreText,
            string highScoreText,
            bool isNewHighScore,
            Action onRestart)
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
            _gameOverHighScoreLabel.style.marginBottom = 24;
            _gameOverHighScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_gameOverHighScoreLabel);

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
            int safeWave = snap.WaveNumber > 0 ? snap.WaveNumber : 1;
            if (_waveLabel != null)
            {
                _waveLabel.text = Format.Wave(safeWave);
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
            if (_goldLabel != null)
            {
                int safeGold = snap.Gold > 0 ? snap.Gold : 0;
                _goldLabel.text = "Vàng: " + safeGold;
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
    }
}
