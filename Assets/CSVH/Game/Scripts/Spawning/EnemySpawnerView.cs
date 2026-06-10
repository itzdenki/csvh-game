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

        [Tooltip("Chiều cao hiển thị chuẩn (world units) cho mỗi Quái: sprite được scale về độ cao " +
                 "này rồi nhân hệ số riêng trong registry, tránh quái quá bé/quá to do ảnh nguồn khác " +
                 "độ phân giải. 0 = giữ nguyên scale của prefab.")]
        [SerializeField] private float _enemyTargetHeight = 1.0f;

        private readonly List<EnemyView> _alive = new List<EnemyView>();
        private FieldGeometry _geometry;

        // Callback forward sự kiện Quái → tầng Core (GameSession). Có thể null trong
        // test/headless: khi null spawner chỉ quản lý vòng đời Prefab như cũ.
        private System.Action<EnemyConfig> _onReachedTower;
        private System.Action<EnemyConfig, Vector3> _onEnemyKilled;

        // Quạ Đen (sinh khi chết): khi một Quái có Id thuộc _deathSpawnTriggerIds bị tiêu diệt,
        // có _deathSpawnChance xác suất sinh _deathSpawnConfig ngay tại chỗ Quái ngã xuống.
        private EnemyConfig _deathSpawnConfig;
        private HashSet<string> _deathSpawnTriggerIds;
        private float _deathSpawnChance;
        // Điều kiện bổ sung (vd "từ Đợt 16 trở đi"); null = luôn cho phép.
        private System.Func<bool> _deathSpawnEligible;

        // Id Quái được bật cơ chế "hóa khô" (boss Mộc Tinh).
        private string _enrageEnemyId;

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
        /// <c>GameSession.OnEnemyKilled</c> (Requirement 2.4). Tham số thứ hai là vị trí
        /// thế giới nơi Quái chết, để view sinh hiệu ứng "Xu rơi" tại đúng chỗ. Có thể <c>null</c>.
        /// </param>
        public void Initialize(
            FieldGeometry geometry,
            System.Action<EnemyConfig> onReachedTower = null,
            System.Action<EnemyConfig, Vector3> onEnemyKilled = null)
        {
            _geometry = geometry;
            _onReachedTower = onReachedTower;
            _onEnemyKilled = onEnemyKilled;
        }

        /// <summary>
        /// Cấu hình cơ chế Quạ Đen: khi một Quái có Id thuộc <paramref name="triggerIds"/> bị tiêu
        /// diệt, có <paramref name="chance"/> (0..1) xác suất sinh <paramref name="spawnOnDeath"/>
        /// ngay tại vị trí Quái ngã xuống. Truyền <paramref name="spawnOnDeath"/> = <c>null</c>
        /// hoặc <paramref name="chance"/> ≤ 0 để tắt.
        /// </summary>
        /// <param name="eligible">
        /// Điều kiện bổ sung kiểm tại thời điểm Quái chết (vd chỉ từ Đợt 16 trở đi —
        /// trước đó Quạ Đen chưa "mở khóa" theo tiến trình chương). <c>null</c> = luôn cho phép.
        /// </param>
        public void ConfigureDeathSpawn(
            EnemyConfig spawnOnDeath,
            HashSet<string> triggerIds,
            float chance,
            System.Func<bool> eligible = null)
        {
            _deathSpawnConfig = spawnOnDeath;
            _deathSpawnTriggerIds = triggerIds;
            _deathSpawnChance = chance;
            _deathSpawnEligible = eligible;
        }

        /// <summary>
        /// Đặt Id Quái sẽ được bật cơ chế "hóa khô" khi spawn (boss Mộc Tinh). Mỗi cá thể spawn
        /// ra có Id trùng sẽ được gọi <see cref="EnemyView.EnableEnrage"/>.
        /// </summary>
        public void SetEnrageEnemy(string enemyId)
        {
            _enrageEnemyId = enemyId;
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

        private void SpawnOne(SpawnIntent intent) => SpawnEnemy(intent.Enemy, intent.Gate);

        /// <summary>
        /// Sinh một Quái <paramref name="config"/> bắt đầu tại <paramref name="startPoint"/> — Cổng_Spawn
        /// thường, hoặc vị trí Quái khác ngã xuống (cơ chế Quạ Đen) — đi theo polyline tới Vị_Trí_Thành.
        /// Trả về <see cref="EnemyView"/> vừa tạo, hoặc <c>null</c> khi thiếu prefab/cấu hình hoặc đã đạt cap.
        /// </summary>
        private EnemyView SpawnEnemy(EnemyConfig config, FieldPoint startPoint)
        {
            if (_enemyPrefab == null || config == null)
            {
                return null;
            }

            // Lưới an toàn cap (Requirement 13.4) — áp cho cả death-spawn Quạ Đen.
            if (_alive.Count >= _aliveCap)
            {
                return null;
            }

            var go = Instantiate(_enemyPrefab, transform);
            var view = go.GetComponent<EnemyView>();
            if (view == null)
            {
                // Prefab thiếu EnemyView — không thể quản lý; hủy ngay để tránh leak.
                Destroy(go);
                return null;
            }

            // Sinh polyline 2 điểm: điểm xuất phát → Vị_Trí_Thành (Requirement 2.1).
            // Nếu geometry chưa inject, fallback về điểm hợp lệ trong góc Đông Nam
            // để tránh exception trong môi trường thử nghiệm; production luôn
            // inject FieldGeometry qua Initialize().
            FieldPoint towerPos = _geometry != null
                ? _geometry.TowerPosition
                : new FieldPoint(1f, -1f);
            var path = EnemyPath.BuildPath(startPoint, towerPos);

            // Áp sprite theo EnemyConfig.Id nếu registry có cấu hình (nếu không, giữ sprite mặc
            // định trên prefab) rồi CHUẨN HÓA kích thước: scale để sprite cao đúng _enemyTargetHeight
            // world units, nhân hệ số riêng của Loại_Quái — để Quái không quá bé/quá to dù ảnh nguồn
            // khác độ phân giải.
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null && _spriteRegistry != null)
            {
                var sprite = _spriteRegistry.GetSprite(config.Id);
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                }

                if (renderer.sprite != null && _enemyTargetHeight > 0f)
                {
                    float spriteHeight = renderer.sprite.bounds.size.y;
                    if (spriteHeight > 0.0001f)
                    {
                        float s = (_enemyTargetHeight / spriteHeight) * _spriteRegistry.GetScale(config.Id);
                        go.transform.localScale = new Vector3(s, s, 1f);
                    }
                }
            }

            view.Initialize(config, path);

            // Boss Mộc Tinh: bật cơ chế "hóa khô" cho đúng cá thể boss.
            if (!string.IsNullOrEmpty(_enrageEnemyId) && config.Id == _enrageEnemyId)
            {
                view.EnableEnrage();
            }

            view.OnKilled += HandleKilled;
            view.OnMeleeHit += HandleMeleeHit;

            _alive.Add(view);
            return view;
        }

        private void HandleKilled(EnemyView view, EnemyConfig config)
        {
            // Requirement 2.4: Quái bị tiêu diệt → cộng vàng/EXP/điểm qua GameSession.
            // Kèm vị trí chết (transform vẫn còn hợp lệ vì OnKilled raise trước Destroy)
            // để view sinh hiệu ứng "Xu rơi" tại đúng chỗ Quái ngã xuống.
            Vector3 deathPos = view != null ? view.transform.position : Vector3.zero;
            _onEnemyKilled?.Invoke(config, deathPos);
            UnregisterAlive(view);

            // Quạ Đen: xác suất trồi lên từ xác Bù Nhìn Rơm / Gốc Cây Ma.
            TrySpawnDeathCrow(config, deathPos);
        }

        /// <summary>
        /// Quạ Đen: nếu <paramref name="deadConfig"/> nằm trong danh sách kích hoạt và quay trúng
        /// xác suất <see cref="_deathSpawnChance"/>, sinh một Quạ Đen ngay tại <paramref name="deathPos"/>
        /// (Quạ bay từ đó về Thành). No-op khi chưa cấu hình hoặc Quái không thuộc nhóm kích hoạt.
        /// </summary>
        private void TrySpawnDeathCrow(EnemyConfig deadConfig, Vector3 deathPos)
        {
            if (_deathSpawnConfig == null || _deathSpawnChance <= 0f
                || _deathSpawnTriggerIds == null || deadConfig == null)
            {
                return;
            }

            if (!_deathSpawnTriggerIds.Contains(deadConfig.Id))
            {
                return;
            }

            // Điều kiện mở khóa (vd Quạ Đen chỉ trồi lên từ Đợt 16+ — trước đó người chơi
            // chưa từng gặp loại Quái này theo tiến trình chương).
            if (_deathSpawnEligible != null && !_deathSpawnEligible())
            {
                return;
            }

            if (UnityEngine.Random.value > _deathSpawnChance)
            {
                return;
            }

            SpawnEnemy(_deathSpawnConfig, new FieldPoint(deathPos.x, deathPos.y));
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
