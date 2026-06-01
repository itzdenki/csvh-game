// Feature: tower-defense-vn, Task 11.4 - TowerView MonoBehaviour
// Validates: Requirements 1.5, 3.1, 3.2
// - 1.5: Layer order ground < tower < projectile fx (giữ qua sortingOrder của SpriteRenderer khi có).
// - 3.1: Thành bắn liên tục theo nhịp Tốc_Độ_Bắn dọc hướng ngắm do người chơi điều khiển.
// - 3.2: Đạn rời TowerPosition theo Vận_Tốc hướng theo góc ngắm (uỷ thác cho ProjectileView).

using CSVH.Core.Common;
using CSVH.Core.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace CSVH.Game.Tower
{
    /// <summary>
    /// View MonoBehaviour cho Thành (Tower). Người chơi ngắm bằng hai phím Mũi_Tên_Trái /
    /// Mũi_Tên_Phải (hoặc A/D): góc ngắm đo từ đáy (phương ngang), <c>90°</c> là thẳng đứng
    /// lên trên. Mũi tên phải giảm góc (ngắm lệch phải, <c>90° − x</c>), mũi tên trái tăng góc
    /// (ngắm lệch trái, <c>90° + x</c>). Mỗi <see cref="Update"/> giảm cooldown bằng
    /// <see cref="Time.deltaTime"/>; khi cooldown chạm 0, Thành bắn một viên
    /// <see cref="ProjectileView"/> dọc hướng ngắm hiện tại với Vận_Tốc
    /// <see cref="_projectileSpeed"/> (Requirement 3.1, 3.2) — không còn tự dò Quái gần nhất.
    ///
    /// <para>
    /// Hệ_số_Công áp dụng cho Đạn được lấy từ
    /// <see cref="UpgradeSystem.CurrentAttackMultiplier(IUpgradeCostTable)"/> (Requirement 6.5)
    /// và truyền vào <see cref="ProjectileView.Initialize"/> để công thức sát thương được tính
    /// bởi tầng Core (Requirement 3.3).
    /// </para>
    ///
    /// <para>
    /// Trước khi <see cref="Object.Instantiate(Object)"/>, view phải gọi
    /// <see cref="ProjectileView.CanSpawn"/> để tôn trọng cap 500 Đạn đồng thời (Requirement 13.3).
    /// </para>
    ///
    /// <para>
    /// Layer order: prefab Tower nên gán sorting layer "Tower" (cao hơn ground, thấp hơn projectile fx)
    /// trong inspector — giá trị mặc định <see cref="_towerSortingOrder"/> được áp lên
    /// <see cref="SpriteRenderer.sortingOrder"/> nếu có (Requirement 1.5).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerView : MonoBehaviour
    {
        [Header("Combat")]
        [Tooltip("Prefab Đạn — phải chứa component ProjectileView.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Tốc_Độ_Bắn (đạn / giây). Cooldown giữa hai lần bắn = 1 / fireRate.")]
        [SerializeField] private float _fireRatePerSecond = 1.5f;

        [Tooltip("Vận_Tốc Đạn theo đơn vị thế giới mỗi giây.")]
        [SerializeField] private float _projectileSpeed = 8f;

        [Tooltip("Sát_Thương_Cơ_Bản của Đạn trước khi nhân Hệ_số_Công và trừ Kháng.")]
        [SerializeField] private float _projectileBaseDamage = 5f;

        [Header("Ngắm (điều khiển Trái/Phải)")]
        [Tooltip("Góc ngắm khởi đầu so với đáy (độ). 90 = thẳng đứng lên trên.")]
        [SerializeField] private float _aimAngleDeg = 90f;

        [Tooltip("Tốc độ xoay nòng khi giữ phím Trái/Phải (độ / giây).")]
        [SerializeField] private float _aimSpeedDegPerSecond = 90f;

        [Tooltip("Góc ngắm nhỏ nhất so với đáy (độ) — chặn ngắm xuyên xuống đất.")]
        [SerializeField] private float _minAimAngleDeg = 5f;

        [Tooltip("Góc ngắm lớn nhất so với đáy (độ).")]
        [SerializeField] private float _maxAimAngleDeg = 175f;

        [Tooltip("Độ dài đường ngắm khi KHÔNG kéo tới biên sân (đơn vị world). 0 = ẩn.")]
        [SerializeField] private float _aimIndicatorLength = 2f;

        [Tooltip("Kéo dài đường ngắm tới tận biên Sân_Đấu để thấy rõ quỹ đạo đạn (đạn bay thẳng).")]
        [SerializeField] private bool _extendAimToFieldEdge = true;

        [Tooltip("Độ rộng đường ngắm (đơn vị world).")]
        [SerializeField] private float _aimLineWidth = 0.04f;

        [Header("Rendering (Requirement 1.5)")]
        [Tooltip("Sorting order áp lên SpriteRenderer của Thành: ground < tower < projectile fx.")]
        [SerializeField] private int _towerSortingOrder = 10;

        private FieldGeometry _geometry;
        private UpgradeSystem _upgrades;
        private IUpgradeCostTable _costs;
        private float _cooldown;
        private LineRenderer _aimLine;
        private bool _isAiming;

        // Điều kiện cho phép bắn: trả false khi không còn Quái sống của đợt → Thành ngừng
        // bắn. Có thể null (test/headless): khi null Thành bắn liên tục như cũ.
        private System.Func<bool> _canFire;

        /// <summary>
        /// Cấu hình Thành với hình học Sân_Đấu và hệ thống nâng cấp. Phải gọi đúng một lần
        /// sau khi prefab Tower được sinh ra (Requirement 1.5, 6.5). Vị trí thế giới của
        /// GameObject sẽ được đặt về <see cref="FieldGeometry.TowerPosition"/>.
        /// </summary>
        /// <param name="geometry">Hình học Sân_Đấu.</param>
        /// <param name="upgrades">Hệ nâng cấp để lấy Hệ_số_Công.</param>
        /// <param name="costs">Bảng giá nâng cấp.</param>
        /// <param name="canFire">
        /// Điều kiện cho phép bắn — trả <c>false</c> khi không còn Quái sống của đợt để
        /// Thành ngừng bắn (tiết kiệm Đạn, đỡ rối màn hình). Có thể <c>null</c> ⇒ bắn liên tục.
        /// </param>
        public void Initialize(
            FieldGeometry geometry,
            UpgradeSystem upgrades,
            IUpgradeCostTable costs,
            System.Func<bool> canFire = null)
        {
            _geometry = geometry;
            _upgrades = upgrades;
            _costs = costs;
            _canFire = canFire;

            if (_geometry is not null)
            {
                var p = _geometry.TowerPosition;
                transform.position = new Vector3(p.X, p.Y, 0f);
            }
        }

        private void Awake()
        {
            // Áp sorting order Thành nếu có SpriteRenderer (Requirement 1.5). Prefab nên
            // gán sorting layer "Tower" trong inspector để giữ thứ tự đúng so với ground/projectile fx.
            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingOrder = _towerSortingOrder;
            }

            _aimAngleDeg = Mathf.Clamp(_aimAngleDeg, _minAimAngleDeg, _maxAimAngleDeg);
            SetupAimIndicator();
        }

        /// <summary>
        /// Tạo một <see cref="LineRenderer"/> con để vẽ đường ngắm/quỹ đạo đạn — giúp người
        /// chơi thấy đạn sẽ bay tới đâu. Bỏ qua nếu cả <see cref="_aimIndicatorLength"/> ≤ 0
        /// và <see cref="_extendAimToFieldEdge"/> = false.
        /// </summary>
        private void SetupAimIndicator()
        {
            if (_aimIndicatorLength <= 0f && !_extendAimToFieldEdge)
            {
                return;
            }

            var go = new GameObject("AimIndicator");
            go.transform.SetParent(transform, false);
            _aimLine = go.AddComponent<LineRenderer>();
            _aimLine.useWorldSpace = false;
            _aimLine.positionCount = 2;
            _aimLine.startWidth = _aimLineWidth;
            _aimLine.endWidth = _aimLineWidth;
            _aimLine.numCapVertices = 4;
            _aimLine.textureMode = LineTextureMode.Tile;
            
            // Feature: Dashed Line Texture
            var tex = new Texture2D(64, 2, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            Color solid = Color.white;
            Color transparent = new Color(1f, 1f, 1f, 0f);
            for (int x = 0; x < 64; x++)
            {
                Color c = (x < 32) ? solid : transparent;
                tex.SetPixel(x, 0, c);
                tex.SetPixel(x, 1, c);
            }
            tex.Apply();

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = tex;
            // Scale U để nét đứt dày đặc hơn: 2 lặp lại mỗi đơn vị khoảng cách
            mat.mainTextureScale = new Vector2(2f, 1f); 
            
            _aimLine.material = mat;
            _aimLine.startColor = new Color(1f, 0.95f, 0.6f, 0.8f);
            _aimLine.endColor = new Color(1f, 0.9f, 0.4f, 0.3f);
            _aimLine.sortingOrder = _towerSortingOrder + 1;
        }

        private void Update()
        {
            UpdateAim(Time.deltaTime);

            // Ngừng bắn khi không còn Quái sống của đợt (canFire trả false). Vẫn cho xoay
            // nòng để người chơi chuẩn bị hướng cho đợt kế.
            if (_canFire != null && !_canFire())
            {
                return;
            }

            // Requirement 3.1: bắn liên tục theo nhịp Tốc_Độ_Bắn dọc hướng ngắm.
            if (_cooldown > 0f)
            {
                _cooldown -= Time.deltaTime;
            }

            if (_cooldown > 0f)
            {
                return;
            }

            // Requirement 13.3: tôn trọng cap số Đạn đồng thời ≤ 500.
            if (!ProjectileView.CanSpawn())
            {
                return;
            }

            Fire(CurrentAimDirection());
            _cooldown = 1f / Mathf.Max(0.0001f, _fireRatePerSecond);
        }

        /// <summary>
        /// Đọc Touch/Click qua Input System và xoay góc ngắm. Cho phép người chơi
        /// nhấn và kéo trên màn hình để hướng nòng pháo, ngoại trừ khi họ nhấn đè lên UI.
        /// Góc bị kẹp trong <c>[<see cref="_minAimAngleDeg"/>, <see cref="_maxAimAngleDeg"/>]</c>.
        /// </summary>
        private void UpdateAim(float dt)
        {
            var pointer = Pointer.current;
            if (pointer != null)
            {
                // Khi vừa chạm/nhấn chuột xuống
                if (pointer.press.wasPressedThisFrame)
                {
                    bool overUI = false;
                    var hud = FindObjectOfType<CSVH.Game.UI.HUDController>();
                    if (hud != null)
                    {
                        overUI = hud.IsPointerOverUI(pointer.position.ReadValue());
                    }
                    else if (EventSystem.current != null)
                    {
                        // Fallback nếu không tìm thấy HUD
                        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
                        {
                            overUI = EventSystem.current.IsPointerOverGameObject(Touchscreen.current.touches[0].touchId.ReadValue());
                        }
                        else
                        {
                            overUI = EventSystem.current.IsPointerOverGameObject();
                        }
                    }

                    // Nếu bấm trúng UI thì khóa ngắm trong suốt chu kỳ giữ tay này
                    _isAiming = !overUI;
                }

                // Khi nhấc tay/nhả chuột ra
                if (!pointer.press.isPressed)
                {
                    _isAiming = false;
                }

                // Nếu đang trong chu kỳ ngắm hợp lệ
                if (_isAiming)
                {
                    var screenPos = pointer.position.ReadValue();
                    if (Camera.main != null)
                    {
                        var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                        worldPos.z = 0f;
                        Vector2 dir = (worldPos - transform.position).normalized;
                        
                        if (dir.sqrMagnitude > 0.01f)
                        {
                            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                            _aimAngleDeg = Mathf.Clamp(angleDeg, _minAimAngleDeg, _maxAimAngleDeg);
                        }
                    }
                }
            }

            UpdateAimIndicator();
        }

        /// <summary>Vector hướng bắn đơn vị, suy từ <see cref="_aimAngleDeg"/> (đo từ đáy).</summary>
        private Vector2 CurrentAimDirection()
        {
            float rad = _aimAngleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void UpdateAimIndicator()
        {
            if (_aimLine == null)
            {
                return;
            }

            Vector2 d = CurrentAimDirection();

            // Độ dài đường ngắm: nếu bật kéo tới biên, tính giao điểm tia–hộp Sân_Đấu
            // (đạn bay thẳng nên quỹ đạo là tia thẳng); ngược lại dùng độ dài cố định.
            float length = _aimIndicatorLength;
            if (_extendAimToFieldEdge && _geometry is not null)
            {
                length = DistanceToFieldEdge(transform.position, d);
            }

            _aimLine.SetPosition(0, Vector3.zero);
            _aimLine.SetPosition(1, new Vector3(d.x, d.y, 0f) * length);
        }

        /// <summary>
        /// Khoảng cách từ <paramref name="worldOrigin"/> theo hướng <paramref name="dir"/>
        /// tới biên Sân_Đấu (hộp <c>[±HalfWidth, ±HalfHeight]</c>, cùng định nghĩa với
        /// <see cref="CSVH.Core.Combat.ProjectileLogic.IsOutOfField"/>). Đạn bay thẳng nên
        /// đây chính là điểm đạn rời sân.
        /// </summary>
        private float DistanceToFieldEdge(Vector3 worldOrigin, Vector2 dir)
        {
            float hw = _geometry.HalfWidth;
            float hh = _geometry.HalfHeight;
            float ox = worldOrigin.x;
            float oy = worldOrigin.y;

            float best = float.MaxValue;

            // Giao với hai cạnh đứng (x = ±hw).
            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = ((dir.x > 0f ? hw : -hw) - ox) / dir.x;
                if (tx > 0f) best = Mathf.Min(best, tx);
            }
            // Giao với hai cạnh ngang (y = ±hh).
            if (Mathf.Abs(dir.y) > 1e-6f)
            {
                float ty = ((dir.y > 0f ? hh : -hh) - oy) / dir.y;
                if (ty > 0f) best = Mathf.Min(best, ty);
            }

            // Phòng trường hợp suy biến (Thành nằm ngay biên): rơi về độ dài cố định.
            return best == float.MaxValue ? _aimIndicatorLength : best;
        }

        /// <summary>
        /// Sinh một Đạn từ vị trí Thành bay theo <paramref name="direction"/> với
        /// <see cref="_projectileSpeed"/> và hệ_số_công lấy từ <see cref="UpgradeSystem"/>
        /// (Requirement 3.2, 6.5).
        /// </summary>
        private void Fire(Vector2 direction)
        {
            if (_projectilePrefab == null || _geometry is null)
            {
                return;
            }

            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var origin = transform.position;
            var velocity = direction.normalized * _projectileSpeed;

            var go = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            var view = go.GetComponent<ProjectileView>();
            if (view == null)
            {
                // Prefab cấu hình sai — huỷ ngay để không lãng phí slot LiveCount.
                Destroy(go);
                return;
            }

            float multiplier = _upgrades is not null && _costs is not null
                ? _upgrades.CurrentAttackMultiplier(_costs)
                : 1f;

            view.Initialize(_geometry, velocity, _projectileBaseDamage, multiplier);
        }
    }
}
