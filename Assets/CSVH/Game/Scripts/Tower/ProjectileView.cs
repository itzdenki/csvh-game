// Feature: tower-defense-vn
// Validates: Requirements 3.2, 3.3, 3.4, 3.6, 13.3
// - 3.2: Đạn có Vận_Tốc và di chuyển theo vector vận tốc mỗi tick vật lý.
// - 3.3: Sát thương Đạn lên Quái = max(0, base × mult − resist) (uỷ thác CombatResolver.ProjectileDamage).
// - 3.4: Đạn rời biên Sân_Đấu thì bị huỷ (uỷ thác ProjectileLogic.IsOutOfField).
// - 3.6: Mỗi Đạn × mỗi Quái tối đa một lần (uỷ thác ProjectileLogic.TryRegisterHit).
// - 13.3: Cap số Đạn đồng thời ≤ 500 (LiveCount + CanSpawn).

using CSVH.Core.Combat;
using CSVH.Core.Common;
using UnityEngine;

namespace CSVH.Game.Tower
{
    /// <summary>
    /// Lớp view (MonoBehaviour) của một viên Đạn trên Sân_Đấu Unity.
    ///
    /// <list type="bullet">
    ///   <item>Di chuyển kinematic theo <c>velocity</c> mỗi <see cref="FixedUpdate"/> (Requirement 3.2).</item>
    ///   <item>Phát hiện chạm Quái qua <c>Trigger2D</c>, áp <see cref="CombatResolver.ProjectileDamage"/>
    ///         qua hợp đồng <see cref="IProjectileTarget"/> (Requirements 3.3, 3.6).</item>
    ///   <item>Tự huỷ khi rời biên (<see cref="ProjectileLogic.IsOutOfField"/>) hoặc khi đã ghi nhận một
    ///         lần trúng Quái (Requirement 3.4).</item>
    ///   <item>Bộ đếm tĩnh <see cref="LiveCount"/> + <see cref="CanSpawn"/> để caller (TowerView) chặn
    ///         instantiate khi đã đạt cap <see cref="MaxLiveCount"/> = 500 (Requirement 13.3).</item>
    /// </list>
    ///
    /// Toàn bộ phép toán sát thương và predicate biên đều uỷ thác cho lớp Core thuần
    /// (<see cref="ProjectileLogic"/>, <see cref="CombatResolver"/>) để giữ view mỏng và để các bất biến
    /// tương ứng được phủ bởi Property-Based Testing ở <c>CSVH.Core</c> (Properties 6, 8, 9).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class ProjectileView : MonoBehaviour
    {
        // Cap số Đạn đồng thời (Requirement 13.3).
        public const int MaxLiveCount = 500;

        private static int s_liveCount;

        /// <summary>Số Đạn đang sống trong scene (Requirement 13.3).</summary>
        public static int LiveCount => s_liveCount;

        /// <summary>
        /// Caller (TowerView) phải gọi trước khi <c>Instantiate</c> để tôn trọng cap 500
        /// (Requirement 13.3). Trả <c>false</c> khi cap đã đạt.
        /// </summary>
        public static bool CanSpawn() => s_liveCount < MaxLiveCount;

        // --- Per-instance state ---

        // Logic thuần gắn một-một với view này — quản lý sổ ghi trúng (Property 9).
        private ProjectileLogic _logic;

        // Hình học Sân_Đấu để cull khi rời biên (Property 8).
        private FieldGeometry _geometry;

        private Vector2 _velocity;
        private float _baseDamage;
        private float _attackMultiplier;
        private bool _initialized;
        private bool _consumed; // đã ghi nhận trúng Quái và sẽ tự huỷ trong cùng frame

        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            s_liveCount++;
        }

        private void OnDestroy()
        {
            s_liveCount = Mathf.Max(0, s_liveCount - 1);
        }

        /// <summary>
        /// Cấu hình Đạn vừa được instantiate. Phải được gọi đúng một lần ngay sau khi caller
        /// (TowerView) sinh prefab. Sau lệnh này Đạn bắt đầu di chuyển ở <see cref="FixedUpdate"/>.
        /// </summary>
        /// <param name="geometry">Hình học Sân_Đấu — dùng để cull theo biên (Requirement 3.4).</param>
        /// <param name="velocity">Vận_Tốc Đạn theo trục thế giới (Requirement 3.2).</param>
        /// <param name="baseDamage">Sát_Thương_Cơ_Bản Đạn; <c>≥ 0</c>.</param>
        /// <param name="attackMultiplier">Hệ_Số_Công của Thành = <c>1 + cấp × bước</c> (Requirement 6.5).</param>
        public void Initialize(FieldGeometry geometry, Vector2 velocity, float baseDamage, float attackMultiplier)
        {
            _geometry = geometry;
            _velocity = velocity;
            _baseDamage = baseDamage;
            _attackMultiplier = attackMultiplier;
            _logic = new ProjectileLogic();
            _initialized = true;
        }

        private void FixedUpdate()
        {
            if (!_initialized || _consumed) return;

            // Di chuyển theo Vận_Tốc (Requirement 3.2).
            _rigidbody.MovePosition(_rigidbody.position + _velocity * Time.fixedDeltaTime);

            // Cull khi rời biên (Requirement 3.4) — uỷ thác predicate Core.
            if (_geometry is not null)
            {
                var pos = new FieldPoint(_rigidbody.position.x, _rigidbody.position.y);
                if (ProjectileLogic.IsOutOfField(pos, _geometry))
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_initialized || _consumed) return;

            // Yêu cầu mục tiêu thực hiện hợp đồng IProjectileTarget — EnemyView (task 11.1) sẽ triển khai.
            var target = other.GetComponent<IProjectileTarget>();
            if (target is null) return;

            // Property 9 / Requirement 3.6: mỗi Đạn × mỗi Quái tối đa một lần.
            if (!_logic.TryRegisterHit(target.EnemyId)) return;

            // Sát thương hiệu quả uỷ thác Core (Property 6 / Requirement 3.3).
            var damage = CombatResolver.ProjectileDamage(
                new DamageInputs(_baseDamage, _attackMultiplier, target.Resistance));
            target.TakeDamage(damage);

            // Tự huỷ trong cùng frame sau khi đã chạm (Requirement 3.4 — "đã chạm Quái cùng frame").
            _consumed = true;
            Destroy(gameObject);
        }
    }
}
