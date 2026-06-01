// Feature: tower-defense-vn — InputService bridging InputSystem_Actions + 3 skill Special.
// Validates: Requirements 6.2, 6.6, 13.2

using System;
using CSVH.Core.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CSVH.Game.Input
{
    /// <summary>
    /// Bridge giữa Unity Input System và các hệ Core <see cref="UpgradeSystem"/>,
    /// <see cref="SpecialSkillSystem"/>. Cung cấp một điểm vào duy nhất cho:
    /// <list type="bullet">
    ///   <item>Mua nâng cấp Giáp/Công (Requirement 6.2) — phím tắt + nhấp icon HUD.</item>
    ///   <item>Mua nâng cấp từng skill Special (Trống Đồng / Mũi Tên / Lưỡi Gươm).</item>
    ///   <item>Kích hoạt từng skill Special trong trận với gating cooldown (Requirement 6.6).</item>
    /// </list>
    ///
    /// <para>
    /// <b>Kích hoạt skill bằng phím:</b> để không phụ thuộc cấu hình action map (file
    /// <c>InputSystem_Actions</c> có thể khác nhau giữa các bản), 3 skill được kích hoạt qua
    /// đọc trực tiếp <see cref="Keyboard.current"/> trong <see cref="Update"/>:
    /// <c>Z</c> = Trống Đồng, <c>X</c> = Mũi Tên, <c>C</c> = Lưỡi Gươm. Việc mua nâng cấp skill
    /// đi qua nhấp icon HUD (mở bảng nâng cấp ở GameSceneRoot).
    /// </para>
    ///
    /// <para>
    /// Hai action mua Giáp/Công vẫn lấy từ <see cref="InputActionAsset"/> (mặc định
    /// <c>Previous</c>=phím 1, <c>Next</c>=phím 2). Resolve không ném exception: thiếu action
    /// thì log cảnh báo và bỏ qua riêng action đó.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputService : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _playerMapName = "Player";
        [SerializeField] private string _upgradeArmorActionName = "Previous";
        [SerializeField] private string _upgradeAttackActionName = "Next";

        // Hệ Core đã bind. Có thể null trước khi GameSceneRoot gọi Bind() — Request* sẽ chỉ
        // raise event mà không thay đổi gameplay (an toàn cho EditMode tests).
        private UpgradeSystem _upgradeSystem;
        private IUpgradeCostTable _costs;
        private SpecialSkillSystem _specialSkills;
        private IRandom _rng;

        private InputAction _upgradeArmorAction;
        private InputAction _upgradeAttackAction;

        /// <summary>
        /// Phát sinh khi người chơi yêu cầu nâng cấp nhánh Giáp/Công (phím tắt hoặc nhấp icon).
        /// </summary>
        public event Action<UpgradeTrack, UpgradeOutcome> UpgradeRequested;

        /// <summary>
        /// Phát sinh khi người chơi yêu cầu mua nâng cấp một skill Special. HUD dùng để hiện
        /// toast "Không đủ Vàng" (Requirement 6.3) khi <see cref="UpgradeOutcome.NotEnoughGold"/>.
        /// </summary>
        public event Action<SpecialSkillKind, UpgradeOutcome> SkillUpgradeRequested;

        /// <summary>
        /// Phát sinh khi người chơi kích hoạt một skill Special. <see cref="SpecialActivation.Activated"/>
        /// = <c>false</c> nếu đang hồi chiêu (Requirement 6.7); GameSceneRoot subscribe để áp hiệu
        /// ứng lên Quái khi <c>true</c>.
        /// </summary>
        public event Action<SpecialActivation> SkillActivated;

        /// <summary>
        /// Liên kết các hệ Core đã khởi tạo bởi GameSceneRoot. Cho phép truyền <c>null</c> trong
        /// test/headless để chỉ kiểm tra event flow.
        /// </summary>
        public void Bind(
            UpgradeSystem upgradeSystem,
            IUpgradeCostTable costs,
            SpecialSkillSystem specialSkills,
            IRandom rng)
        {
            _upgradeSystem = upgradeSystem;
            _costs = costs;
            _specialSkills = specialSkills;
            _rng = rng;
        }

        /// <summary>
        /// Yêu cầu mua một bậc nhánh Giáp/Công (Requirement 6.2). Trả
        /// <see cref="UpgradeOutcome.NotEnoughGold"/> khi chưa bind đủ hệ; event vẫn phát để HUD quan sát.
        /// </summary>
        public UpgradeOutcome RequestUpgrade(UpgradeTrack track)
        {
            UpgradeOutcome outcome = UpgradeOutcome.NotEnoughGold;
            if (_upgradeSystem != null && _costs != null)
            {
                var result = _upgradeSystem.TryBuy(track, _costs);
                outcome = result.Outcome;
            }
            UpgradeRequested?.Invoke(track, outcome);
            return outcome;
        }

        /// <summary>
        /// Yêu cầu mua một bậc nâng cấp cho skill <paramref name="kind"/>. Dùng vàng trong
        /// <see cref="UpgradeSystem"/> qua <see cref="SpecialSkillSystem.TryBuyUpgrade"/>.
        /// </summary>
        public UpgradeOutcome RequestUpgradeSkill(SpecialSkillKind kind)
        {
            UpgradeOutcome outcome = UpgradeOutcome.NotEnoughGold;
            if (_specialSkills != null && _upgradeSystem != null)
            {
                outcome = _specialSkills.TryBuyUpgrade(kind, _upgradeSystem);
            }
            SkillUpgradeRequested?.Invoke(kind, outcome);
            return outcome;
        }

        /// <summary>
        /// Yêu cầu "mua"/mở khoá skill <paramref name="kind"/> lần đầu bằng Vàng trong
        /// <see cref="UpgradeSystem"/> qua <see cref="SpecialSkillSystem.TryUnlock"/>. Dùng chung
        /// event <see cref="SkillUpgradeRequested"/> để HUD hiện toast khi thiếu Vàng.
        /// </summary>
        public UpgradeOutcome RequestUnlockSkill(SpecialSkillKind kind)
        {
            UpgradeOutcome outcome = UpgradeOutcome.NotEnoughGold;
            if (_specialSkills != null && _upgradeSystem != null)
            {
                outcome = _specialSkills.TryUnlock(kind, _upgradeSystem);
            }
            SkillUpgradeRequested?.Invoke(kind, outcome);
            return outcome;
        }

        /// <summary>
        /// Yêu cầu kích hoạt skill <paramref name="kind"/> (Requirement 6.6, 6.7). Trả
        /// <see cref="SpecialActivation"/> mô tả hiệu ứng; nếu đang hồi chiêu thì
        /// <see cref="SpecialActivation.Activated"/> = <c>false</c>.
        /// </summary>
        public SpecialActivation RequestActivateSkill(SpecialSkillKind kind)
        {
            SpecialActivation activation = SpecialActivation.NotReady(kind);
            if (_specialSkills != null)
            {
                activation = _specialSkills.TryActivate(kind, _rng);
            }
            SkillActivated?.Invoke(activation);
            return activation;
        }

        private void Update()
        {
            // Kích hoạt skill bằng phím tắt — đọc trực tiếp keyboard để độc lập action map.
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.zKey.wasPressedThisFrame) RequestActivateSkill(SpecialSkillKind.TrongDong);
            if (kb.xKey.wasPressedThisFrame) RequestActivateSkill(SpecialSkillKind.MuiTen);
            if (kb.cKey.wasPressedThisFrame) RequestActivateSkill(SpecialSkillKind.LuoiGuom);
        }

        private void OnEnable()
        {
            if (_actions == null)
            {
                Debug.LogWarning(
                    "InputService: InputActionAsset chưa được gán; phím mua Giáp/Công sẽ không hoạt động. " +
                    "Kích hoạt skill (Z/X/C) vẫn dùng được qua bàn phím.", this);
                return;
            }

            var map = _actions.FindActionMap(_playerMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogWarning(
                    $"InputService: action map '{_playerMapName}' không tìm thấy trong {_actions.name}.",
                    this);
                return;
            }

            _upgradeArmorAction = ResolveAction(map, _upgradeArmorActionName);
            _upgradeAttackAction = ResolveAction(map, _upgradeAttackActionName);

            Subscribe(_upgradeArmorAction, OnUpgradeArmorPerformed);
            Subscribe(_upgradeAttackAction, OnUpgradeAttackPerformed);

            map.Enable();
        }

        private void OnDisable()
        {
            Unsubscribe(_upgradeArmorAction, OnUpgradeArmorPerformed);
            Unsubscribe(_upgradeAttackAction, OnUpgradeAttackPerformed);

            _upgradeArmorAction = null;
            _upgradeAttackAction = null;
        }

        private InputAction ResolveAction(InputActionMap map, string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return null;
            }

            var action = map.FindAction(actionName, throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogWarning(
                    $"InputService: action '{actionName}' không tồn tại trong map '{_playerMapName}'. " +
                    "Phím tắt tương ứng sẽ bị tắt; có thể đổi tên trong Inspector hoặc thêm action mới.",
                    this);
            }
            return action;
        }

        private static void Subscribe(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.performed += callback;
            }
        }

        private static void Unsubscribe(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.performed -= callback;
            }
        }

        private void OnUpgradeArmorPerformed(InputAction.CallbackContext _) => RequestUpgrade(UpgradeTrack.Armor);
        private void OnUpgradeAttackPerformed(InputAction.CallbackContext _) => RequestUpgrade(UpgradeTrack.Attack);
    }
}
