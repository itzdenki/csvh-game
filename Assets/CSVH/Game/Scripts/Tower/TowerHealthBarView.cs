// Feature: tower-defense-vn — Tower HP bar overlay (Unity view layer)
// Đọc CurrentHp/MaxHp từ GameSession (qua GameSceneRoot) hoặc trực tiếp từ
// nhà cung cấp được inject. Bám pattern HealthBarBuilder dùng chung với Enemy.

using CSVH.Game.UI;
using UnityEngine;

namespace CSVH.Game.Tower
{
    /// <summary>
    /// Thanh máu hiển thị phía trên Thành. Khác <c>EnemyHealthBarView</c> ở chỗ Tower không
    /// có HP riêng trong <see cref="TowerView"/> — nguồn chân lý là
    /// <see cref="CSVH.Core.Game.GameSession"/>. Component lấy provider qua
    /// <see cref="Bind"/> được gọi bởi <c>GameSceneRoot</c> sau khi bootstrap xong.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TowerView))]
    public sealed class TowerHealthBarView : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Chiều rộng thanh máu (đơn vị world). Thường rộng hơn enemy để dễ đọc.")]
        [SerializeField] private float _width = 3.0f;

        [Tooltip("Chiều cao thanh máu.")]
        [SerializeField] private float _height = 0.28f;

        [Tooltip("Khoảng cách thẳng đứng từ tâm Thành lên thanh máu.")]
        [SerializeField] private float _verticalOffset = 1.4f;

        [Header("Rendering")]
        [Tooltip("Sorting order — luôn cao hơn Tower sprite (mặc định 10) và Enemy bar (50).")]
        [SerializeField] private int _sortingOrder = 60;

        [Tooltip("Luôn hiển thị (Thành là chủ thể chính, người chơi cần biết HP cả khi đầy).")]
        [SerializeField] private bool _alwaysVisible = true;

        // Hai delegate đơn giản tránh phụ thuộc cứng vào GameSession (Core) ở Unity layer.
        // GameSceneRoot inject bằng cách gọi Bind(() => session.CurrentHp, () => session.MaxHp).
        private System.Func<int> _hpGetter;
        private System.Func<int> _maxHpGetter;

        private HealthBarHandle _bar;

        private void Awake()
        {
            _bar = HealthBarBuilder.Build(
                parent: transform,
                width: _width,
                height: _height,
                verticalOffset: _verticalOffset,
                sortingOrder: _sortingOrder,
                initialColor: HealthBarBuilder.HpColor(1f));
        }

        /// <summary>
        /// Inject nguồn dữ liệu HP. Gọi trong <see cref="GameSceneRoot.Start"/> sau khi tạo
        /// <see cref="CSVH.Core.Game.GameSession"/>. Truyền <c>null</c> để tạm thời ngắt
        /// — thanh máu sẽ giữ trạng thái cuối cho tới khi bind lại.
        /// </summary>
        public void Bind(System.Func<int> hpGetter, System.Func<int> maxHpGetter)
        {
            _hpGetter = hpGetter;
            _maxHpGetter = maxHpGetter;
        }

        private void LateUpdate()
        {
            if (_bar == null) return;
            if (_hpGetter == null || _maxHpGetter == null)
            {
                // Chưa bind → giữ thanh đầy mặc định để tránh flash 0/0 trong frame đầu.
                _bar.SetVisible(_alwaysVisible);
                return;
            }

            int maxHp = _maxHpGetter();
            int hp = _hpGetter();

            if (maxHp <= 0)
            {
                _bar.SetVisible(_alwaysVisible);
                _bar.SetRatio(0f);
                _bar.Fill.color = HealthBarBuilder.HpColor(0f);
                return;
            }

            float ratio = Mathf.Clamp01((float)hp / maxHp);

            // Chính sách hiển thị Tower: luôn show (theo design HUD vùng giữa-dưới).
            // Khi _alwaysVisible = false thì ẩn lúc full HP cho UX gọn hơn.
            bool visible = _alwaysVisible || ratio < 0.999f;
            _bar.SetVisible(visible);
            if (visible)
            {
                _bar.SetRatio(ratio);
                _bar.Fill.color = HealthBarBuilder.HpColor(ratio);
            }
        }
    }
}
