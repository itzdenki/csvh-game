// Feature: tower-defense-vn, Task 11.3 - EnemySpawnerView MonoBehaviour
// Validates: Requirements 2.1, 2.5, 7.1, 13.4

using System.Collections.Generic;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Wave;
using CSVH.Game.Data;
using UnityEngine;

namespace CSVH.Game.Spawning
{
    /// <summary>
    /// View MonoBehaviour cầu nối giữa <see cref="WaveScheduler.Tick"/> ở tầng Core
    /// và Prefab Quái trong Sân_Đấu. Nhận <see cref="SpawnIntent"/> đã được
    /// scheduler rút khỏi <see cref="SpawnQueue"/> rồi:
    /// <list type="bullet">
    ///   <item>Instantiate Prefab tại <see cref="SpawnIntent.Gate"/> (Requirement 2.1, 7.1).</item>
    ///   <item>Khởi tạo <see cref="EnemyView"/> với <see cref="SpawnIntent.Enemy"/>
    ///   để hỗ trợ ≥ 5 Loại_Quái khác biệt theo cấu hình (Requirement 2.5).</item>
    ///   <item>Đăng ký Quái vào danh sách alive để <c>GameSceneRoot</c> truyền lại
    ///   <see cref="AliveCount"/> cho tick scheduler kế tiếp, đảm bảo cap 200
    ///   Quái sống đồng thời (Requirement 13.4).</item>
    /// </list>
    /// Spawner không tự đếm thời gian: <c>GameSceneRoot</c> (task 13.1) gọi
    /// <see cref="WaveScheduler.Tick"/> mỗi frame, sau đó truyền danh sách trả về
    /// vào <see cref="ApplyIntents"/>. Cách bố trí này giữ trọn logic wave ở Core
    /// và để view chỉ chịu trách nhiệm hiện thực Prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnerView : MonoBehaviour
    {
        // Prefab phải gắn EnemyView; spawner chỉ Initialize với EnemyConfig + path.
        [SerializeField] private GameObject _enemyPrefab;

        // Cap mặc định 200 Quái sống đồng thời (Requirement 13.4). Mặc dù
        // WaveScheduler.Tick cũng tôn trọng cap qua tham số spawnCap, spawner
        // vẫn enforce ở tầng view như một lưới an toàn cuối cùng.
        [SerializeField] private int _aliveCap = 200;

        // Registry ánh xạ EnemyConfig.Id → Sprite. Có thể null trong test/headless;
        // khi null spawner giữ nguyên sprite mặc định trên prefab.
        [SerializeField] private EnemySpriteRegistrySO _spriteRegistry;

        private readonly List<EnemyView> _alive = new List<EnemyView>();
        private FieldGeometry _geometry;

        // Callback forward sự kiện Quái → tầng Core (GameSession). Có thể null trong
        // test/headless: khi null spawner chỉ quản lý vòng đời Prefab như cũ.
        private System.Action<EnemyConfig> _onReachedTower;
        private System.Action<EnemyConfig> _onEnemyKilled;

        /// <summary>Danh sách Quái đang sống đã được spawner quản lý.</summary>
        public IReadOnlyList<EnemyView> AliveEnemies => _alive;

        /// <summary>
        /// Số Quái đang sống — chính là tham số <c>aliveEnemies</c> mà
        /// <c>GameSceneRoot</c> phải truyền vào <see cref="WaveScheduler.Tick"/>
        /// (Requirement 13.4).
        /// </summary>
        public int AliveCount => _alive.Count;

        /// <summary>
        /// Inject <see cref="FieldGeometry"/> để spawner biết Vị_Trí_Thành cuối
        /// đường khi sinh polyline cho Quái (Requirement 2.1). Gọi đúng một lần
        /// sau khi cấu hình đã nạp xong (xem GameSceneRoot, task 13.1).
        /// </summary>
        /// <param name="geometry">Hình học Sân_Đấu để lấy Vị_Trí_Thành.</param>
        /// <param name="onReachedTower">
        /// Callback gọi khi một Quái chạm Thành — dùng để forward
        /// <see cref="EnemyConfig.MeleeDamage"/> vào
        /// <c>GameSession.OnEnemyReachedTower</c> (Requirement 2.3). Có thể <c>null</c>.
        /// </param>
        /// <param name="onEnemyKilled">
        /// Callback gọi khi một Quái bị tiêu diệt — dùng để cộng phần thưởng qua
        /// <c>GameSession.OnEnemyKilled</c> (Requirement 2.4). Có thể <c>null</c>.
        /// </param>
        public void Initialize(
            FieldGeometry geometry,
            System.Action<EnemyConfig> onReachedTower = null,
            System.Action<EnemyConfig> onEnemyKilled = null)
        {
            _geometry = geometry;
            _onReachedTower = onReachedTower;
            _onEnemyKilled = onEnemyKilled;
        }

        /// <summary>
        /// Áp dụng các <see cref="SpawnIntent"/> mà <see cref="WaveScheduler.Tick"/>
        /// vừa trả về. Nếu cap đã đạt, các intent dôi ra bị bỏ qua trong tick này
        /// — scheduler đã tôn trọng cap nhưng spawner enforce lần nữa cho đúng
        /// Requirement 13.4. Truyền <c>null</c> hoặc danh sách rỗng là no-op.
        /// </summary>
        public void ApplyIntents(IReadOnlyList<SpawnIntent> intents)
        {
            if (intents == null || intents.Count == 0)
            {
                return;
            }

            for (int i = 0; i < intents.Count; i++)
            {
                if (_alive.Count >= _aliveCap)
                {
                    // Cap respected (Req 13.4) — bỏ qua phần dư trong tick này.
                    break;
                }

                SpawnOne(intents[i]);
            }
        }

        private void SpawnOne(SpawnIntent intent)
        {
            if (_enemyPrefab == null)
            {
                return;
            }

            var go = Instantiate(_enemyPrefab, transform);
            var view = go.GetComponent<EnemyView>();
            if (view == null)
            {
                // Prefab thiếu EnemyView — không thể quản lý; hủy ngay để tránh leak.
                Destroy(go);
                return;
            }

            // Sinh polyline 2 điểm: Cổng_Spawn → Vị_Trí_Thành (Requirement 2.1).
            // Nếu geometry chưa inject, fallback về điểm hợp lệ trong góc Đông Nam
            // để tránh exception trong môi trường thử nghiệm; production luôn
            // inject FieldGeometry qua Initialize().
            FieldPoint towerPos = _geometry != null
                ? _geometry.TowerPosition
                : new FieldPoint(1f, -1f);
            var path = EnemyPath.BuildPath(intent.Gate, towerPos);

            // Áp sprite theo EnemyConfig.Id nếu registry có cấu hình. Nếu không,
            // giữ sprite mặc định gắn trên prefab — spawner vẫn hoạt động.
            if (_spriteRegistry != null)
            {
                var sprite = _spriteRegistry.GetSprite(intent.Enemy.Id);
                if (sprite != null)
                {
                    var renderer = go.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sprite = sprite;
                    }
                }
            }

            view.Initialize(intent.Enemy, path);
            view.OnKilled += HandleKilled;
            view.OnMeleeHit += HandleMeleeHit;

            _alive.Add(view);
        }

        private void HandleKilled(EnemyView view, EnemyConfig config)
        {
            // Requirement 2.4: Quái bị tiêu diệt → cộng vàng/EXP/điểm qua GameSession.
            _onEnemyKilled?.Invoke(config);
            UnregisterAlive(view);
        }

        private void HandleMeleeHit(EnemyView view, EnemyConfig config)
        {
            // Requirement 2.3: mỗi đòn cận chiến → trừ Máu Thành theo Sát_Thương_Cận_Chiến.
            // KHÔNG gỡ Quái khỏi danh sách: Quái kẹt ngoài Thành và tiếp tục đánh cho
            // tới khi bị Đạn tiêu diệt.
            _onReachedTower?.Invoke(config);
        }

        private void UnregisterAlive(EnemyView view)
        {
            _alive.Remove(view);
            view.OnKilled -= HandleKilled;
            view.OnMeleeHit -= HandleMeleeHit;
        }

        /// <summary>
        /// Áp hiệu ứng một lần kích hoạt skill Special lên mọi Quái còn sống nằm trong
        /// <paramref name="radius"/> (khoảng cách Euclid) tính từ <paramref name="origin"/>:
        /// trừ <paramref name="damagePerHit"/> Máu lặp lại <paramref name="hitCount"/> lần
        /// (nổ/chém) và áp choáng <paramref name="stunSeconds"/> giây nếu &gt; 0.
        /// <para>
        /// Lặp trên một bản sao danh sách vì <see cref="EnemyView.TakeDamage"/> có thể tiêu diệt
        /// Quái và gỡ nó khỏi <see cref="_alive"/> ngay trong vòng lặp.
        /// </para>
        /// </summary>
        /// <param name="origin">Tâm hiệu ứng (thường là Vị_Trí_Thành).</param>
        /// <param name="radius">Bán_Kính ảnh hưởng (&gt; 0).</param>
        /// <param name="hitCount">Số lần áp sát thương lên mỗi Quái trúng (≥ 0).</param>
        /// <param name="damagePerHit">Sát thương mỗi lần (≥ 0).</param>
        /// <param name="stunSeconds">Thời gian choáng áp lên Quái trúng (giây); 0 = không choáng.</param>
        public void ApplySpecialEffect(
            Vector2 origin,
            float radius,
            int hitCount,
            float damagePerHit,
            float stunSeconds)
        {
            if (radius <= 0f || (hitCount <= 0 && stunSeconds <= 0f))
            {
                return;
            }

            float radiusSq = radius * radius;

            // Snapshot để tránh sửa đổi _alive trong khi đang lặp (TakeDamage → HandleKilled).
            var snapshot = _alive.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var view = snapshot[i];
                if (view == null)
                {
                    continue;
                }

                Vector2 pos = view.transform.position;
                float dx = pos.x - origin.x;
                float dy = pos.y - origin.y;
                if (dx * dx + dy * dy > radiusSq)
                {
                    continue;
                }

                if (stunSeconds > 0f)
                {
                    view.ApplyStun(stunSeconds);
                }

                for (int h = 0; h < hitCount && damagePerHit > 0f; h++)
                {
                    view.TakeDamage(damagePerHit);
                }
            }
        }
    }
}
