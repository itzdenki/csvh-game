// Feature: tower-defense-vn
// Validates: Requirements 6.3, 6.7, 6.8

using System;
using UnityEngine;
using UnityEngine.UIElements;
using CSVH.Core.Progression;

namespace CSVH.Game.UI
{
    /// <summary>
    /// VisualElement-based view cho từng icon nâng cấp (Công/Giáp/Special/EXP).
    /// Hiển thị giá hiện tại; với <see cref="IconKind.Special"/>, hiển thị Thời_Gian_Hồi
    /// và nhấp nháy khi đang cooldown.
    /// Toast "Không đủ Vàng" được hiển thị qua <see cref="HudToast.Show"/> khi
    /// <see cref="UpgradeSystem.TryBuy"/> trả <see cref="UpgradeOutcome.NotEnoughGold"/>
    /// (Requirement 6.3).
    /// </summary>
    public sealed class UpgradeIconView
    {
        public enum IconKind { Attack, Armor, Special, Exp }

        public IconKind Kind { get; }
        public Button Root { get; }
        public Label CostLabel { get; }
        public Label CooldownLabel { get; }

        private float _cooldownRemaining;
        private float _cooldownMax;

        public event Action<IconKind> OnClicked;

        public UpgradeIconView(IconKind kind, Button root)
        {
            Kind = kind;
            Root = root ?? throw new ArgumentNullException(nameof(root));

            CostLabel = new Label { name = "CostLabel" };
            CostLabel.AddToClassList("hud-icon-cost");
            Root.Add(CostLabel);

            if (kind == IconKind.Special)
            {
                CooldownLabel = new Label { name = "CooldownLabel" };
                CooldownLabel.AddToClassList("hud-icon-cooldown");
                Root.Add(CooldownLabel);
            }

            Root.clicked += () => OnClicked?.Invoke(Kind);
        }

        /// <summary>Cập nhật giá hiển thị (Requirement 6.7).</summary>
        public void UpdateCost(int cost)
        {
            CostLabel.text = cost.ToString();
        }

        /// <summary>
        /// Cập nhật Thời_Gian_Hồi cho icon Special (Requirement 6.8).
        /// Khi <paramref name="remaining"/> &gt; 0, icon nhấp nháy; ngược lại trở về opacity 1.
        /// </summary>
        public void UpdateCooldown(float remaining, float max)
        {
            _cooldownRemaining = remaining;
            _cooldownMax = max;

            if (Kind != IconKind.Special) return;

            if (CooldownLabel != null)
            {
                CooldownLabel.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            }

            // Nhấp nháy khi đang cooldown
            if (remaining > 0f)
            {
                float t = (Time.time * 4f) % 1f;
                Root.style.opacity = 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI);
            }
            else
            {
                Root.style.opacity = 1f;
            }
        }
    }

    /// <summary>
    /// Static helper hiển thị toast tạm thời lên HUD root.
    /// Dùng để hiện "Không đủ Vàng" khi <see cref="UpgradeOutcome.NotEnoughGold"/>
    /// (Requirement 6.3).
    /// </summary>
    public static class HudToast
    {
        private static VisualElement _toastRoot;

        public static void Initialize(VisualElement root)
        {
            _toastRoot = root;
        }

        public static void Show(string message, float durationSeconds = 1.5f)
        {
            ShowInternal(message, durationSeconds, isError: false);
        }

        /// <summary>
        /// Hiển thị toast lỗi (ví dụ "Không đủ Vàng" khi <see cref="UpgradeOutcome.NotEnoughGold"/>),
        /// thêm class <c>hud-toast-error</c> để nhấn mạnh viền/màu cảnh báo (Requirement 6.3).
        /// </summary>
        public static void ShowError(string message, float durationSeconds = 1.5f)
        {
            ShowInternal(message, durationSeconds, isError: true);
        }

        private static void ShowInternal(string message, float durationSeconds, bool isError)
        {
            if (_toastRoot == null) return;

            var label = new Label(message) { name = "ToastLabel" };
            label.AddToClassList("hud-toast");
            if (isError)
            {
                label.AddToClassList("hud-toast-error");
            }
            _toastRoot.Add(label);

            _toastRoot.schedule
                .Execute(() => label.RemoveFromHierarchy())
                .StartingIn((long)(durationSeconds * 1000f));
        }
    }
}
