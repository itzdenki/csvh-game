// Feature: tower-defense-vn, Task 11.1 - EnemyView MonoBehaviour
// Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5

using System;
using System.Collections.Generic;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Wave;
using CSVH.Game.Tower;
using UnityEngine;

namespace CSVH.Game.Spawning
{
    /// <summary>
    /// View MonoBehaviour cho một Quái trong Sân_Đấu. Toàn bộ logic đường đi và
    /// chuyển động dùng <see cref="EnemyPath"/> ở tầng Core (pure C#); component
    /// này chỉ chịu trách nhiệm:
    /// <list type="bullet">
    ///   <item>
    ///   Bắt đầu tại Cổng_Spawn và di chuyển dọc Đường_Đi_Quái với
    ///   <see cref="EnemyConfig.Speed"/> (Requirements 2.1, 2.2).
    ///   </item>
    ///   <item>
    ///   Trừ HP khi nhận damage thông qua <see cref="TakeDamage"/>; raise
    ///   <see cref="OnKilled"/> kèm <see cref="EnemyConfig"/> để tầng spawner /
    ///   game session cộng phần thưởng (Requirement 2.4).
    ///   </item>
    ///   <item>
    ///   Khi tới gần Vị_Trí_Thành, dừng ở ngoài và chuyển sang cận chiến: lùi-tiến
    ///   liên tục, mỗi nhịp lao tới raise <see cref="OnMeleeHit"/> để Thành mất Máu
    ///   dần. Quái chỉ biến mất khi bị Đạn tiêu diệt (Requirement 2.3).
    ///   </item>
    ///   <item>
    ///   Triển khai <see cref="IProjectileTarget"/> để <see cref="ProjectileView"/>
    ///   gọi <see cref="TakeDamage"/> qua hợp đồng Core (Requirement 3.3, 3.6).
    ///   </item>
    /// </list>
    /// Hỗ trợ ≥ 5 Loại_Quái khác biệt thông qua <see cref="EnemyConfig"/>
    /// (Requirement 2.5) — view không gắn cứng stat nào.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyView : MonoBehaviour, IProjectileTarget
    {
        // Sai số kẹp vị trí về điểm cuối path; EnemyPath.AdvanceAlongPath đã clamp
        // chính xác về path[^1] khi vượt — so sánh bằng giá trị float ổn định.
        private const float ReachedEpsilon = 0.001f;

        [Header("Cận chiến tại Thành")]
        [Tooltip("Khoảng cách Quái dừng trước Vị_Trí_Thành (đơn vị world). Giữ Quái 'kẹt ở ngoài' thay vì chồng lên Thành.")]
        [SerializeField] private float _meleeStandoff = 0.7f;

        [Tooltip("Biên độ lùi-tiến mỗi nhịp cận chiến (đơn vị world).")]
        [SerializeField] private float _meleeLunge = 0.5f;

        [Tooltip("Tốc độ di chuyển khi lùi-tiến cận chiến (đơn vị world / giây).")]
        [SerializeField] private float _meleeSpeed = 2.5f;

        [Header("Hóa khô (boss Mộc Tinh) — chỉ bật cho cá thể boss")]
        [Tooltip("Ngưỡng máu (tỉ lệ MaxHp) kích hoạt 'hóa khô'. 0.1 = còn 10% máu.")]
        [SerializeField] private float _enrageHpThreshold = 0.1f;
        [Tooltip("Giảm tỉ lệ sát thương nhận vào khi hóa khô. 0.9 = giảm 90%.")]
        [SerializeField] private float _enrageDamageReduction = 0.9f;
        [Tooltip("Hồi máu mỗi giây khi hóa khô.")]
        [SerializeField] private float _enrageHealPerSecond = 60f;
        [Tooltip("Thời gian mỗi lần hóa khô (giây).")]
        [SerializeField] private float _enrageDurationSeconds = 5f;
        [Tooltip("Số lần hóa khô tối đa mỗi đời. Hồi máu + tái kích hoạt vô hạn tạo vòng lặp " +
                 "bất tử tại ngưỡng máu, nên mặc định chỉ 1 lần. 0 = không giới hạn.")]
        [SerializeField, Min(0)] private int _enrageMaxActivations = 1;
        [Tooltip("Màu nhuốm lên Quái khi hóa khô (gợi ý trạng thái 'khô cứng').")]
        [SerializeField] private Color _enrageTint = new Color(0.62f, 0.47f, 0.30f, 1f);

        [Header("Hiệu ứng nâng cấp trong trận (Nỏ Băng / Nỏ Độc)")]
        [Tooltip("Màu nhuốm lên Quái khi đang bị làm chậm (Nỏ Băng).")]
        [SerializeField] private Color _slowTint = new Color(0.55f, 0.75f, 1f, 1f);
        [Tooltip("Màu nhuốm lên Quái khi đang trúng độc (Nỏ Độc).")]
        [SerializeField] private Color _poisonTint = new Color(0.55f, 0.95f, 0.45f, 1f);

        // Bộ phát ID đơn điệu tăng dùng cho ProjectileLogic.TryRegisterHit để mỗi
        // Quái có một định danh duy nhất trong vòng đời session (Requirement 3.6).
        private static int _nextId = 1;

        /// <summary>Định danh runtime của Quái, gán khi <see cref="Initialize"/> chạy.</summary>
        public int EnemyId { get; private set; }

        /// <summary>Kháng sát thương từ Đạn (sao chép từ <see cref="EnemyConfig"/>).</summary>
        public float Resistance { get; private set; }

        /// <summary>Máu_Quái hiện tại; luôn nằm trong <c>[0, MaxHp]</c>.</summary>
        public float Hp { get; private set; }

        /// <summary>Máu_Quái tối đa, sao chép từ <see cref="EnemyConfig.MaxHp"/>.</summary>
        public float MaxHp { get; private set; }

        /// <summary>
        /// Raise khi <see cref="Hp"/> ≤ 0 (Requirement 2.4). Subscriber chịu
        /// trách nhiệm cộng phần thưởng từ <see cref="EnemyConfig"/>.
        /// </summary>
        public event Action<EnemyView, EnemyConfig> OnKilled;

        /// <summary>
        /// Raise mỗi khi Quái thực hiện một đòn cận chiến vào Thành (lặp lại theo
        /// nhịp lùi-tiến). Subscriber chịu trách nhiệm áp Sát_Thương_Cận_Chiến của
        /// Quái lên Thành sau khi áp Giáp (Requirement 2.3). Khác với hành vi cũ,
        /// Quái KHÔNG tự hủy khi tới Thành — nó kẹt lại ngoài Thành và đánh liên tục
        /// cho tới khi bị tiêu diệt bằng Đạn.
        /// </summary>
        public event Action<EnemyView, EnemyConfig> OnMeleeHit;

        private EnemyConfig _config;
        private IReadOnlyList<FieldPoint> _path;
        private PathProgress _progress;
        private bool _isAlive = true;

        // Trạng thái cận chiến: khi true, Quái đã tới sát Thành và chuyển sang
        // vòng lặp lùi-tiến thay vì đi theo path. Vị trí "neo" là điểm dừng cách
        // Thành _meleeStandoff; Quái dao động quanh neo này.
        private bool _inMelee;
        private Vector3 _meleeAnchor;     // điểm xa nhất (lùi về)
        private Vector3 _meleeStrikePos;  // điểm gần Thành nhất (lao tới → gây đòn)
        private bool _advancing = true;   // true: đang lao tới; false: đang lùi
        private bool _hitAppliedThisLunge; // chống gây nhiều đòn trong một lần chạm

        // Thời gian choáng còn lại (giây) do skill Mũi Tên An Dương Vương gây ra. Khi > 0,
        // Quái đứng im: không di chuyển theo path và không thực hiện nhịp cận chiến.
        private float _stunRemaining;

        // Nỏ Băng: tỷ lệ làm chậm (0..1) + thời gian còn lại. Tốc độ hiệu dụng =
        // Speed × (1 − _slowFraction) khi _slowRemaining > 0.
        private float _slowFraction;
        private float _slowRemaining;

        // Nỏ Độc: sát thương mỗi giây + thời gian còn lại; trừ máu dần qua TakeDamage.
        private float _poisonDps;
        private float _poisonRemaining;

        // Boss "hóa khô" (Mộc Tinh): _enrageEnabled bật bởi spawner cho đúng cá thể boss;
        // _petrifyRemaining > 0 trong lúc hóa khô (đứng im, giảm sát thương, hồi máu).
        // _enrageActivations đếm số lần đã hóa khô để tôn trọng _enrageMaxActivations.
        private bool _enrageEnabled;
        private int _enrageActivations;
        private float _petrifyRemaining;
        private SpriteRenderer _renderer;
        private Color _baseColor = Color.white;
        private bool Petrified => _petrifyRemaining > 0f;

        /// <summary>
        /// Bật cơ chế "hóa khô" cho cá thể này (spawner gọi cho boss Mộc Tinh). Khi máu rơi
        /// xuống <see cref="_enrageHpThreshold"/> × MaxHp, Quái hóa khô: đứng im, giảm
        /// <see cref="_enrageDamageReduction"/> sát thương nhận vào và hồi
        /// <see cref="_enrageHealPerSecond"/> máu/giây trong <see cref="_enrageDurationSeconds"/> giây;
        /// tái kích hoạt mỗi lần chạm lại ngưỡng.
        /// </summary>
        public void EnableEnrage() => _enrageEnabled = true;

        /// <summary>
        /// Cấu hình Quái với <paramref name="config"/> và polyline
        /// <paramref name="path"/> đã sinh từ Core (Requirement 2.1). Vị trí thế
        /// giới được đặt về điểm đầu polyline (Cổng_Spawn).
        /// </summary>
        /// <exception cref="ArgumentNullException">Khi <paramref name="config"/> hoặc <paramref name="path"/> là <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Khi polyline có ít hơn 2 điểm.</exception>
        public void Initialize(EnemyConfig config, IReadOnlyList<FieldPoint> path)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _path = path ?? throw new ArgumentNullException(nameof(path));

            EnemyId = _nextId++;
            Resistance = config.Resistance;
            MaxHp = config.MaxHp;
            Hp = config.MaxHp;

            // EnemyPath.StartProgress validate path.Count ≥ 2 và đặt vị trí về path[0].
            _progress = EnemyPath.StartProgress(path);
            var p = _progress.Position;
            transform.position = new Vector3(p.X, p.Y, 0f);
        }

        private void Awake()
        {
            // Quái di chuyển hoàn toàn theo logic Core, không cần vật lý động lực học —
            // dùng Rigidbody2D Kinematic để Physics2D vẫn detect overlap với Đạn (Trigger).
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // ProjectileView dùng OnTriggerEnter2D để áp sát thương — collider phải là trigger.
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // Cache SpriteRenderer + màu gốc để nhuốm màu khi hóa khô rồi khôi phục.
            if (TryGetComponent(out _renderer))
            {
                _baseColor = _renderer.color;
            }
        }

        private void FixedUpdate()
        {
            // Bảo vệ vòng update khi chưa Initialize hoặc đã chết.
            if (!_isAlive || _config == null || _path == null)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;

            // Nỏ Độc: trừ máu dần qua TakeDamage (đi chung đường kill/OnKilled, và tự nhận
            // giảm sát thương khi boss đang hóa khô). TakeDamage có thể giết Quái → thoát sớm.
            if (_poisonRemaining > 0f)
            {
                _poisonRemaining = Mathf.Max(0f, _poisonRemaining - dt);
                TakeDamage(_poisonDps * dt);
                if (!_isAlive)
                {
                    return;
                }
                if (_poisonRemaining <= 0f)
                {
                    _poisonDps = 0f;
                    RefreshTint();
                }
            }

            // Nỏ Băng: đếm ngược thời gian làm chậm; hết hạn thì khôi phục tốc độ + màu.
            if (_slowRemaining > 0f)
            {
                _slowRemaining = Mathf.Max(0f, _slowRemaining - dt);
                if (_slowRemaining <= 0f)
                {
                    _slowFraction = 0f;
                    RefreshTint();
                }
            }

            // Boss "hóa khô": đứng im + hồi máu, bỏ qua di chuyển/cận chiến/choáng. Hết thời
            // gian thì khôi phục màu và trở lại bình thường (có thể hóa khô lại khi xuống ngưỡng).
            if (Petrified)
            {
                _petrifyRemaining -= dt;
                Hp = Mathf.Min(MaxHp, Hp + _enrageHealPerSecond * dt);
                if (_petrifyRemaining <= 0f)
                {
                    EndPetrify();
                }
                return;
            }

            // Skill Mũi Tên gây choáng: trong khi còn choáng, Quái đứng im (không đi, không
            // cận chiến). Đếm ngược bằng dt và bỏ qua phần di chuyển của frame này.
            if (_stunRemaining > 0f)
            {
                _stunRemaining = Mathf.Max(0f, _stunRemaining - dt);
                return;
            }

            // Khi đã vào tầm cận chiến, chạy vòng lặp lùi-tiến và bỏ qua di chuyển theo path.
            if (_inMelee)
            {
                TickMelee(dt);
                return;
            }

            // Requirement 2.2: WHILE Hp > 0 và chưa tới Thành, di chuyển dọc đường
            // với speed × dt. EnemyPath.AdvanceAlongPath kẹp vị trí về điểm cuối khi vượt.
            // Nỏ Băng: tốc độ hiệu dụng giảm theo _slowFraction khi đang bị làm chậm.
            _progress = EnemyPath.AdvanceAlongPath(_progress, EffectiveSpeed(_config.Speed), dt, _path);
            var p = _progress.Position;
            transform.position = new Vector3(p.X, p.Y, 0f);

            // Khi tới đủ gần Vị_Trí_Thành (điểm cuối polyline), chuyển sang cận chiến
            // thay vì biến mất. Quái sẽ kẹt ở ngoài và đánh liên tục (Requirement 2.3).
            var endpoint = _path[_path.Count - 1];
            var towerPos = new Vector3(endpoint.X, endpoint.Y, 0f);
            float distToTower = Vector3.Distance(transform.position, towerPos);
            if (distToTower <= _meleeStandoff + ReachedEpsilon)
            {
                EnterMelee(towerPos);
            }
        }

        /// <summary>
        /// Thiết lập hai mốc dao động quanh điểm dừng: <see cref="_meleeAnchor"/>
        /// (cách Thành <see cref="_meleeStandoff"/>) và <see cref="_meleeStrikePos"/>
        /// (gần hơn một đoạn <see cref="_meleeLunge"/>). Quái lao từ anchor → strike,
        /// gây một đòn rồi lùi về, lặp lại.
        /// </summary>
        private void EnterMelee(Vector3 towerPos)
        {
            _inMelee = true;
            _advancing = true;
            _hitAppliedThisLunge = false;

            Vector3 fromTower = transform.position - towerPos;
            // Nếu Quái trùng tâm Thành (hiếm), chọn hướng mặc định để tránh chia 0.
            if (fromTower.sqrMagnitude < 1e-6f)
            {
                fromTower = Vector3.up;
            }
            Vector3 dir = fromTower.normalized;

            _meleeAnchor = towerPos + dir * _meleeStandoff;
            _meleeStrikePos = towerPos + dir * Mathf.Max(0.05f, _meleeStandoff - _meleeLunge);
            transform.position = _meleeAnchor;
        }

        /// <summary>
        /// Một bước của vòng lặp cận chiến: di chuyển giữa <see cref="_meleeAnchor"/>
        /// và <see cref="_meleeStrikePos"/>. Khi chạm điểm strike, raise
        /// <see cref="OnMeleeHit"/> đúng một lần mỗi nhịp lao tới để trừ Máu Thành dần.
        /// </summary>
        private void TickMelee(float dt)
        {
            Vector3 target = _advancing ? _meleeStrikePos : _meleeAnchor;
            transform.position = Vector3.MoveTowards(
                transform.position, target, EffectiveSpeed(_meleeSpeed) * dt);

            if (Vector3.Distance(transform.position, target) <= 0.01f)
            {
                if (_advancing)
                {
                    // Tới sát Thành → gây một đòn cận chiến, rồi đổi sang lùi.
                    if (!_hitAppliedThisLunge)
                    {
                        OnMeleeHit?.Invoke(this, _config);
                        _hitAppliedThisLunge = true;
                    }
                    _advancing = false;
                }
                else
                {
                    // Đã lùi xong → cho phép gây đòn ở nhịp lao tới kế tiếp.
                    _hitAppliedThisLunge = false;
                    _advancing = true;
                }
            }
        }

        /// <summary>
        /// Trừ <paramref name="damage"/> vào <see cref="Hp"/> rồi kẹp về <c>[0, MaxHp]</c>.
        /// Khi Hp đạt 0, raise <see cref="OnKilled"/> đúng một lần và tự hủy
        /// (Requirement 2.4). Lệnh gọi sau khi đã chết là no-op.
        /// </summary>
        /// <param name="damage">Sát thương đã được tính ở
        /// <c>CombatResolver.ProjectileDamage</c> (đảm bảo ≥ 0 theo Requirement 3.3).</param>
        public void TakeDamage(float damage)
        {
            if (!_isAlive)
            {
                return;
            }

            // Damage âm là lỗi gọi từ caller; ở tầng view ta kẹp về 0 để giữ bất biến HP đơn điệu.
            if (damage < 0f)
            {
                damage = 0f;
            }

            // Boss đang hóa khô: giảm phần lớn sát thương nhận vào.
            if (Petrified)
            {
                damage *= Mathf.Clamp01(1f - _enrageDamageReduction);
            }

            Hp = Mathf.Max(0f, Hp - damage);
            if (Hp <= 0f)
            {
                _isAlive = false;
                OnKilled?.Invoke(this, _config);
                Destroy(gameObject);
                return;
            }

            // Boss Mộc Tinh: vừa rơi xuống ngưỡng máu (và chưa đang hóa khô) → hóa khô tức thì.
            // Giới hạn _enrageMaxActivations lần mỗi đời (0 = vô hạn): hồi máu + tái kích hoạt
            // vô hạn sẽ tạo vòng lặp bất tử quanh ngưỡng máu vì lượng hồi luôn vượt sát thương
            // người chơi gây được trong lúc boss đang giảm 90% sát thương nhận vào.
            if (_enrageEnabled && !Petrified && Hp <= _enrageHpThreshold * MaxHp
                && (_enrageMaxActivations <= 0 || _enrageActivations < _enrageMaxActivations))
            {
                StartPetrify();
            }
        }

        /// <summary>Bắt đầu "hóa khô": đặt đồng hồ hiệu ứng, đếm lượt và nhuốm màu khô cứng.</summary>
        private void StartPetrify()
        {
            _enrageActivations++;
            _petrifyRemaining = _enrageDurationSeconds;
            RefreshTint();
        }

        /// <summary>Kết thúc "hóa khô": khôi phục màu theo các hiệu ứng còn lại.</summary>
        private void EndPetrify()
        {
            _petrifyRemaining = 0f;
            RefreshTint();
        }

        /// <summary>
        /// Tốc độ hiệu dụng sau khi áp làm chậm của Nỏ Băng:
        /// <c>speed × (1 − _slowFraction)</c> khi đang trong thời gian làm chậm.
        /// </summary>
        private float EffectiveSpeed(float speed)
        {
            if (_slowRemaining <= 0f || _slowFraction <= 0f)
            {
                return speed;
            }

            return speed * Mathf.Clamp01(1f - _slowFraction);
        }

        /// <summary>
        /// Chọn màu nhuốm theo ưu tiên trạng thái: hóa khô &gt; độc &gt; làm chậm &gt; màu gốc.
        /// Gọi mỗi khi một hiệu ứng bắt đầu/kết thúc để màu luôn phản ánh trạng thái hiện tại.
        /// </summary>
        private void RefreshTint()
        {
            if (_renderer == null)
            {
                return;
            }

            if (Petrified)
            {
                _renderer.color = _enrageTint;
            }
            else if (_poisonRemaining > 0f)
            {
                _renderer.color = _poisonTint;
            }
            else if (_slowRemaining > 0f)
            {
                _renderer.color = _slowTint;
            }
            else
            {
                _renderer.color = _baseColor;
            }
        }

        /// <summary>
        /// Áp làm chậm của Nỏ Băng (xem <see cref="IProjectileTarget.ApplySlow"/>): lấy max
        /// từng thành phần với hiệu ứng đang có để các phát trúng liên tiếp không rút ngắn nhau.
        /// No-op khi Quái đã chết hoặc tham số không dương.
        /// </summary>
        public void ApplySlow(float fraction, float seconds)
        {
            if (!_isAlive || fraction <= 0f || seconds <= 0f)
            {
                return;
            }

            _slowFraction = Mathf.Max(_slowFraction, Mathf.Clamp01(fraction));
            _slowRemaining = Mathf.Max(_slowRemaining, seconds);
            RefreshTint();
        }

        /// <summary>
        /// Áp độc của Nỏ Độc (xem <see cref="IProjectileTarget.ApplyPoison"/>): lấy max DPS
        /// và làm tươi thời gian. No-op khi Quái đã chết hoặc tham số không dương.
        /// </summary>
        public void ApplyPoison(float damagePerSecond, float seconds)
        {
            if (!_isAlive || damagePerSecond <= 0f || seconds <= 0f)
            {
                return;
            }

            _poisonDps = Mathf.Max(_poisonDps, damagePerSecond);
            _poisonRemaining = Mathf.Max(_poisonRemaining, seconds);
            RefreshTint();
        }

        /// <summary>
        /// Áp choáng <paramref name="seconds"/> giây lên Quái (skill Mũi Tên An Dương Vương).
        /// Lấy max với thời gian choáng hiện có để các lần áp chồng nhau không rút ngắn choáng.
        /// Giá trị ≤ 0 hoặc gọi sau khi Quái chết là no-op.
        /// </summary>
        /// <param name="seconds">Thời gian choáng (giây).</param>
        public void ApplyStun(float seconds)
        {
            if (!_isAlive || seconds <= 0f)
            {
                return;
            }

            _stunRemaining = Mathf.Max(_stunRemaining, seconds);
        }
    }
}
