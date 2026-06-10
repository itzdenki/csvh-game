// Feature: tower-defense-vn
// Validates: Requirements 8.6, 12.1, 12.2, 12.3.
// Property 19: Round-trip Kỷ_Lục qua Bộ_Lưu_Trữ — Requirements 8.6, 12.2.
// Property 20: Round-trip âm lượng có clamp và mặc định — Requirements 12.1, 12.2, 12.3.

using System;
using System.Collections.Generic;

namespace CSVH.Core.Storage
{
    /// <summary>
    /// Triển khai <see cref="IStorageService"/> giữ dữ liệu trong RAM, dùng cho EditMode tests
    /// và Property-Based Testing (FsCheck) — không phụ thuộc <c>PlayerPrefs</c> hay file IO.
    ///
    /// <para>
    /// Bất biến (invariants) tương ứng các Property của design:
    /// <list type="bullet">
    ///   <item>
    ///   <b>Property 19 (Requirements 8.6, 12.2)</b>: với mọi <c>k</c> được ghi qua
    ///   <see cref="WriteHighScore(long)"/>, lần gọi <see cref="ReadHighScore"/> kế tiếp trả về
    ///   chính <c>k</c>. Khi chưa từng ghi, <see cref="ReadHighScore"/> trả <c>0</c>.
    ///   </item>
    ///   <item>
    ///   <b>Property 20 (Requirements 12.1, 12.2, 12.3)</b>: <see cref="WriteVolume(VolumeChannel, float)"/>
    ///   kẹp giá trị vào <c>[0.0, 1.0]</c> trước khi lưu; <see cref="ReadVolume(VolumeChannel)"/> kế tiếp
    ///   trả về đúng giá trị đã kẹp; khi kênh chưa được ghi, trả mặc định <c>1.0f</c>.
    ///   </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Class này KHÔNG thread-safe — Core game loop chạy đơn luồng nên không cần khóa;
    /// tests cũng không nên truy cập song song để giữ test deterministic.
    /// </para>
    /// </summary>
    public sealed class InMemoryStorageService : IStorageService
    {
        // Property 20: giá trị mặc định khi kênh chưa được ghi (Requirement 12.2).
        private const float DefaultVolume = 1.0f;

        // Property 19: Kỷ_Lục mặc định 0 khi chưa từng ghi (Requirement 8.6).
        private const long DefaultHighScore = 0L;

        // Dùng nullable<long> để phân biệt "chưa ghi" (null) với "đã ghi 0".
        // Khi null, ReadHighScore trả DefaultHighScore = 0 (Requirement 8.6).
        private long? _highScore;

        // Mỗi kênh giữ giá trị đã được kẹp [0,1]. Không có entry => trả DefaultVolume (Requirement 12.2).
        private readonly Dictionary<VolumeChannel, float> _volumes = new Dictionary<VolumeChannel, float>();

        // Null = chưa từng ghi → ReadMetaProgress trả MetaProgressSnapshot.Empty (GDD Cơ chế 2).
        private MetaProgressSnapshot _meta;

        // Feature: tower-defense-vn — Requirement 8.6, Property 19.
        /// <inheritdoc />
        public long ReadHighScore() => _highScore ?? DefaultHighScore;

        // Feature: tower-defense-vn — Requirement 8.6, Property 19.
        /// <inheritdoc />
        public void WriteHighScore(long value)
        {
            // Property 19 chỉ định round-trip cho k ≥ 0 — kẹp về [0, long.MaxValue] để
            // đảm bảo bất biến "Kỷ_Lục lưu luôn không âm" (Requirement 8.6). Giá trị âm
            // được quy về 0 thay vì ném lỗi để tránh làm gãy game khi callsite vô tình
            // truyền giá trị bất thường.
            _highScore = value < 0L ? 0L : value;
        }

        // Feature: tower-defense-vn — Requirements 12.2, 12.3, Property 20.
        /// <inheritdoc />
        public float ReadVolume(VolumeChannel channel)
        {
            // Khi kênh chưa được ghi, trả mặc định 1.0 (Requirement 12.2).
            return _volumes.TryGetValue(channel, out var stored) ? stored : DefaultVolume;
        }

        // Feature: tower-defense-vn — Requirements 12.1, 12.3, Property 20.
        /// <inheritdoc />
        public void WriteVolume(VolumeChannel channel, float value)
        {
            // Property 20: kẹp [0,1] TRƯỚC khi lưu để Read kế tiếp trả giá trị hợp lệ
            // (Requirement 12.3). Ràng buộc enum kênh để tránh ghi giá trị "rác".
            if (!Enum.IsDefined(typeof(VolumeChannel), channel))
            {
                throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unknown VolumeChannel.");
            }

            // Math.Clamp xử lý tốt mọi finite float; với NaN ta quy ước trả về mặc định
            // để giữ bất biến "giá trị lưu luôn ∈ [0,1]" (Requirement 12.3).
            var clamped = float.IsNaN(value) ? DefaultVolume : Math.Clamp(value, 0.0f, 1.0f);
            _volumes[channel] = clamped;
        }

        // Feature: tower-defense-vn — GDD Cơ chế 2 (Meta Upgrade).
        /// <inheritdoc />
        public MetaProgressSnapshot ReadMetaProgress() => _meta ?? MetaProgressSnapshot.Empty;

        // Feature: tower-defense-vn — GDD Cơ chế 2 (Meta Upgrade).
        /// <inheritdoc />
        public void WriteMetaProgress(MetaProgressSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            // Kẹp mọi trường về ≥ 0 để round-trip luôn trả giá trị hợp lệ (đồng dạng Kỷ_Lục).
            _meta = new MetaProgressSnapshot(
                Coins: snapshot.Coins < 0L ? 0L : snapshot.Coins,
                GateHpLevel: snapshot.GateHpLevel < 0 ? 0 : snapshot.GateHpLevel,
                CrossbowDamageLevel: snapshot.CrossbowDamageLevel < 0 ? 0 : snapshot.CrossbowDamageLevel,
                UltimateCooldownLevel: snapshot.UltimateCooldownLevel < 0 ? 0 : snapshot.UltimateCooldownLevel);
        }
    }
}
