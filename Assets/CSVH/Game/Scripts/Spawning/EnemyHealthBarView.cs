// Feature: tower-defense-vn — Enemy HP bar overlay (Unity view layer)
// Dựng thanh máu qua HealthBarBuilder shared, cập nhật mỗi frame theo Hp/MaxHp.

using CSVH.Game.UI;
using UnityEngine;

namespace CSVH.Game.Spawning
{
    /// <summary>
    /// Thanh máu hiển thị phía trên đầu Quái. Component gọi
    /// <see cref="HealthBarBuilder.Build"/> trong <see cref="Awake"/> nên prefab Quái không
    /// cần sửa cấu trúc — chỉ cần thêm component này.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyView))]
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private float _width = 1.5f;
        [SerializeField] private float _height = 0.18f;
        [SerializeField] private float _verticalOffset = 1.1f;

        [Header("Rendering")]
        [SerializeField] private int _sortingOrder = 50;

        [Tooltip("Ẩn thanh máu khi Quái đầy HP để giảm clutter.")]
        [SerializeField] private bool _hideWhenFull = true;

        private EnemyView _enemy;
        private HealthBarHandle _bar;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _enemy = GetComponent<EnemyView>();
            _renderer = GetComponent<SpriteRenderer>();
            _bar = HealthBarBuilder.Build(
                parent: transform,
                width: _width,
                height: _height,
                verticalOffset: _verticalOffset,
                sortingOrder: _sortingOrder,
                initialColor: HealthBarBuilder.HpColor(1f));
        }

        private void LateUpdate()
        {
            if (_enemy == null || _bar == null) return;

            var anchor = _bar.Anchor;

            // Thanh máu KHÔNG được phóng theo kích thước Quái (spawner đã scale transform Quái để
            // chuẩn hóa sprite). Bù ngược bằng lossyScale rồi đặt world-scale = camRatio (chỉ phụ
            // thuộc zoom camera), để thanh máu giữ cùng độ rộng cho mọi Quái.
            float parentScale = transform.lossyScale.y;
            if (parentScale <= 0.0001f) parentScale = 1f;
            float camRatio = Camera.main != null ? Camera.main.orthographicSize / 5f : 1f;
            anchor.localScale = Vector3.one * (camRatio / parentScale);

            // Neo ngay trên ĐỈNH sprite Quái (world-space) — đúng cho mọi kích thước Quái, không
            // bị đẩy quá cao như khi cộng offset trong không gian đã scale.
            if (_renderer != null && _renderer.sprite != null)
            {
                Bounds b = _renderer.bounds;
                anchor.position = new Vector3(b.center.x, b.max.y + 0.15f * camRatio, transform.position.z);
            }

            float maxHp = _enemy.MaxHp;
            if (maxHp <= 0f)
            {
                _bar.SetVisible(false);
                return;
            }

            float ratio = Mathf.Clamp01(_enemy.Hp / maxHp);

            if (_hideWhenFull && ratio >= 0.999f)
            {
                _bar.SetVisible(false);
                return;
            }

            _bar.SetVisible(true);
            _bar.SetRatio(ratio);
            _bar.Fill.color = HealthBarBuilder.HpColor(ratio);
        }
    }
}
