// Feature: tower-defense-vn
// Validates: Requirements 8.6, 12.1, 12.2, 12.3, 12.4
// Property 19: Round-trip Kỷ_Lục qua Bộ_Lưu_Trữ — Requirements 8.6, 12.2.
// Property 20: Round-trip âm lượng có clamp và mặc định — Requirements 12.1, 12.2, 12.3.
// Property 21: Dữ liệu Bộ_Lưu_Trữ hỏng → fallback mặc định — Requirement 12.4.

using System;
using System.IO;
using CSVH.Core.Logging;
using CSVH.Core.Storage;
using Newtonsoft.Json;
using UnityEngine;

namespace CSVH.Game.Storage
{
    /// <summary>
    /// Triển khai <see cref="IStorageService"/> cho lớp Unity:
    /// <list type="bullet">
    ///   <item>
    ///   Âm lượng (<see cref="VolumeChannel.Music"/>, <see cref="VolumeChannel.Sfx"/>) lưu
    ///   qua <see cref="PlayerPrefs"/> với khóa <see cref="StorageKeys.MusicVolume"/> và
    ///   <see cref="StorageKeys.SfxVolume"/>; giá trị được kẹp vào <c>[0, 1]</c> trước khi
    ///   ghi (Requirement 12.3) và mặc định <c>1.0</c> khi chưa có dữ liệu (Requirement 12.2).
    ///   <see cref="PlayerPrefs.Save"/> đồng bộ trên main thread, hoàn tất trong vài ms,
    ///   nên thoả mãn ràng buộc &lt; 1 giây của Requirement 12.1.
    ///   </item>
    ///   <item>
    ///   Kỷ_Lục lưu trong tệp JSON tại
    ///   <c>Application.persistentDataPath/<see cref="StorageKeys.HighScoreFile"/></c>
    ///   theo schema <c>{"highScore": &lt;long&gt;}</c> (Requirement 8.6).
    ///   </item>
    ///   <item>
    ///   Khi parse JSON thất bại hoặc payload không hợp lệ (Property 21): ghi cảnh báo qua
    ///   <see cref="ILogSink"/>, ghi đè tệp bằng default <c>{"highScore":0}</c> và trả <c>0</c>
    ///   (Requirement 12.4).
    ///   </item>
    /// </list>
    /// </summary>
    public sealed class UnityStorageService : IStorageService
    {
        private const long DefaultHighScore = 0L;
        private const float DefaultVolume = 1.0f;

        private readonly ILogSink _log;
        private readonly string _highScoreFilePath;
        private readonly string _metaProgressFilePath;

        /// <summary>
        /// Khởi tạo service với sink log để báo cáo lỗi parse (Requirement 12.4) và đường
        /// dẫn tệp Kỷ_Lục cố định trong <see cref="Application.persistentDataPath"/>.
        /// </summary>
        /// <param name="log">Sink ghi cảnh báo/lỗi; truyền <c>null</c> sẽ dùng <see cref="NullLogSink.Instance"/>.</param>
        public UnityStorageService(ILogSink log)
        {
            _log = log ?? NullLogSink.Instance;
            _highScoreFilePath = Path.Combine(Application.persistentDataPath, StorageKeys.HighScoreFile);
            _metaProgressFilePath = Path.Combine(Application.persistentDataPath, StorageKeys.MetaProgressFile);
        }

        /// <inheritdoc />
        public long ReadHighScore()
        {
            // Property 19: chưa từng ghi → trả 0 (Requirement 8.6).
            if (!File.Exists(_highScoreFilePath))
            {
                return DefaultHighScore;
            }

            string raw;
            try
            {
                raw = File.ReadAllText(_highScoreFilePath);
            }
            catch (Exception ex)
            {
                // I/O lỗi không nên làm gãy game: log + fallback (Requirement 12.4).
                _log.Warn($"UnityStorageService: failed to read high score file '{_highScoreFilePath}': {ex.Message}");
                return DefaultHighScore;
            }

            try
            {
                var dto = JsonConvert.DeserializeObject<HighScoreDto>(raw);
                if (dto == null || dto.HighScore < 0L)
                {
                    throw new JsonException("HighScore payload is null or negative.");
                }
                return dto.HighScore;
            }
            catch (Exception ex)
            {
                // Property 21 / Requirement 12.4: parse fail → log warning, ghi đè default, trả 0.
                _log.Warn($"UnityStorageService: corrupted high score file '{_highScoreFilePath}', resetting to default. Detail: {ex.Message}");
                TryWriteDefault();
                return DefaultHighScore;
            }
        }

        /// <inheritdoc />
        public void WriteHighScore(long value)
        {
            // Bất biến: Kỷ_Lục lưu luôn không âm (Requirement 8.6, Property 19 cho k ≥ 0).
            var sanitized = value < 0L ? 0L : value;
            WriteHighScoreToDisk(sanitized);
        }

        /// <inheritdoc />
        public float ReadVolume(VolumeChannel channel)
        {
            // Mặc định 1.0 khi chưa ghi (Requirement 12.2).
            return PlayerPrefs.GetFloat(StorageKeys.VolumeKey(channel), DefaultVolume);
        }

        /// <inheritdoc />
        public void WriteVolume(VolumeChannel channel, float value)
        {
            // Property 20 / Requirement 12.3: kẹp [0,1] trước khi lưu, NaN → mặc định 1.0.
            var clamped = float.IsNaN(value) ? DefaultVolume : Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(StorageKeys.VolumeKey(channel), clamped);
            // Save đồng bộ trên main thread, hoàn tất trong vài ms — thoả mãn Requirement 12.1 (< 1s).
            PlayerPrefs.Save();
        }

        private void WriteHighScoreToDisk(long value)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new HighScoreDto { HighScore = value });
                File.WriteAllText(_highScoreFilePath, json);
            }
            catch (Exception ex)
            {
                // Không ném lên trên để tránh dừng vòng game; log để truy vết (Requirement 12.4).
                _log.Error($"UnityStorageService: failed to write high score file '{_highScoreFilePath}': {ex.Message}");
            }
        }

        private void TryWriteDefault()
        {
            WriteHighScoreToDisk(DefaultHighScore);
        }

        // ==== META progress (Xu cổ) — GDD Cơ chế 2 ======================================

        /// <inheritdoc />
        public MetaProgressSnapshot ReadMetaProgress()
        {
            // Chưa từng ghi → mặc định rỗng (đồng dạng Kỷ_Lục, Requirement 12.4 default).
            if (!File.Exists(_metaProgressFilePath))
            {
                return MetaProgressSnapshot.Empty;
            }

            string raw;
            try
            {
                raw = File.ReadAllText(_metaProgressFilePath);
            }
            catch (Exception ex)
            {
                _log.Warn($"UnityStorageService: failed to read meta progress file '{_metaProgressFilePath}': {ex.Message}");
                return MetaProgressSnapshot.Empty;
            }

            try
            {
                var dto = JsonConvert.DeserializeObject<MetaProgressDto>(raw);
                if (dto == null)
                {
                    throw new JsonException("Meta progress payload is null.");
                }

                // Defensive: kẹp mọi trường về ≥ 0 (dữ liệu có thể bị sửa tay/hỏng).
                return new MetaProgressSnapshot(
                    Coins: dto.Coins < 0L ? 0L : dto.Coins,
                    GateHpLevel: dto.GateHpLevel < 0 ? 0 : dto.GateHpLevel,
                    CrossbowDamageLevel: dto.CrossbowDamageLevel < 0 ? 0 : dto.CrossbowDamageLevel,
                    UltimateCooldownLevel: dto.UltimateCooldownLevel < 0 ? 0 : dto.UltimateCooldownLevel);
            }
            catch (Exception ex)
            {
                // Requirement 12.4: parse fail → log warning, ghi đè default, trả Empty.
                _log.Warn($"UnityStorageService: corrupted meta progress file '{_metaProgressFilePath}', resetting to default. Detail: {ex.Message}");
                WriteMetaProgressToDisk(MetaProgressSnapshot.Empty);
                return MetaProgressSnapshot.Empty;
            }
        }

        /// <inheritdoc />
        public void WriteMetaProgress(MetaProgressSnapshot snapshot)
        {
            if (snapshot is null)
            {
                _log.Error("UnityStorageService: WriteMetaProgress called with null snapshot; ignoring.");
                return;
            }

            // Bất biến: mọi trường lưu luôn không âm.
            var sanitized = new MetaProgressSnapshot(
                Coins: snapshot.Coins < 0L ? 0L : snapshot.Coins,
                GateHpLevel: snapshot.GateHpLevel < 0 ? 0 : snapshot.GateHpLevel,
                CrossbowDamageLevel: snapshot.CrossbowDamageLevel < 0 ? 0 : snapshot.CrossbowDamageLevel,
                UltimateCooldownLevel: snapshot.UltimateCooldownLevel < 0 ? 0 : snapshot.UltimateCooldownLevel);
            WriteMetaProgressToDisk(sanitized);
        }

        private void WriteMetaProgressToDisk(MetaProgressSnapshot snapshot)
        {
            try
            {
                var dto = new MetaProgressDto
                {
                    Coins = snapshot.Coins,
                    GateHpLevel = snapshot.GateHpLevel,
                    CrossbowDamageLevel = snapshot.CrossbowDamageLevel,
                    UltimateCooldownLevel = snapshot.UltimateCooldownLevel,
                };
                File.WriteAllText(_metaProgressFilePath, JsonConvert.SerializeObject(dto));
            }
            catch (Exception ex)
            {
                _log.Error($"UnityStorageService: failed to write meta progress file '{_metaProgressFilePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// DTO nội bộ cho schema <c>{"highScore": &lt;long&gt;}</c> (Requirement 8.6).
        /// Đặt private để không lộ chi tiết serialization ra ngoài Game tier.
        /// </summary>
        private sealed class HighScoreDto
        {
            [JsonProperty("highScore")]
            public long HighScore { get; set; }
        }

        /// <summary>
        /// DTO nội bộ cho schema tiến trình META (Xu cổ) — GDD Cơ chế 2.
        /// </summary>
        private sealed class MetaProgressDto
        {
            [JsonProperty("coins")]
            public long Coins { get; set; }

            [JsonProperty("gateHpLevel")]
            public int GateHpLevel { get; set; }

            [JsonProperty("crossbowDamageLevel")]
            public int CrossbowDamageLevel { get; set; }

            [JsonProperty("ultimateCooldownLevel")]
            public int UltimateCooldownLevel { get; set; }
        }
    }
}
