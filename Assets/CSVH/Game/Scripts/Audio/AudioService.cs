// Feature: tower-defense-vn, Task 9.3 - AudioService driven by IStorageService
// Validates: Requirements 11.5, 12.1, 12.2, 12.3

using System;
using CSVH.Core.Storage;
using UnityEngine;
using UnityEngine.Audio;

namespace CSVH.Game.Audio
{
    /// <summary>
    /// Bộ điều phối âm thanh cho lớp Unity. Nguồn chân lý duy nhất về âm lượng là
    /// <see cref="IStorageService"/> (PlayerPrefs ở production, in-memory trong tests):
    /// <list type="bullet">
    ///   <item>
    ///   Khi <see cref="Initialize"/> được gọi, service đọc âm lượng music/sfx hiện hành
    ///   từ storage và áp vào <see cref="AudioMixer"/> (Requirements 12.1, 12.2, 12.3).
    ///   </item>
    ///   <item>
    ///   <see cref="SetVolume"/> ghi giá trị mới vào storage (storage tự kẹp <c>[0, 1]</c>
    ///   theo Requirement 12.3) rồi áp lại vào mixer trong cùng frame, đảm bảo HUD &amp;
    ///   AudioMixer luôn đồng bộ với dữ liệu bền vững (Requirement 12.1).
    ///   </item>
    ///   <item>
    ///   BGM nhạc cụ truyền thống Việt Nam (đàn bầu, sáo trúc, trống) được phát trong các
    ///   Đợt thường khi <see cref="_bgmSource"/> và <see cref="_traditionalBgm"/> được gán
    ///   (Requirement 11.5).
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Ánh xạ tuyến tính → dB cho <see cref="AudioMixer"/>: <c>db = 20 * log10(linear)</c>;
    /// khi <c>linear ≈ 0</c> ta gán <c>-80 dB</c> (mute thực tế của Unity Mixer) để tránh
    /// <c>log10(0) = -∞</c>. Tham số mixer mặc định là <c>"MusicVolume"</c> và
    /// <c>"SfxVolume"</c> — phải được expose trên <see cref="AudioMixer"/> ở Inspector.
    /// </para>
    ///
    /// <para>
    /// <see cref="_mixer"/> có thể là <c>null</c> trong scene test/headless; trong trường hợp
    /// đó <see cref="ApplyVolume"/> bỏ qua an toàn (no-op) thay vì ném lỗi để giữ vòng game
    /// chạy được, vì storage vẫn là nguồn chân lý cho persistence.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        // Mixer dùng SetFloat(<exposedParam>, db). Để null vẫn chạy được trong test scene.
        [SerializeField] private AudioMixer _mixer;

        // Tên tham số đã expose trên AudioMixer cho hai kênh — designer có thể đổi trong Inspector.
        [SerializeField] private string _musicGroupParam = "MusicVolume";
        [SerializeField] private string _sfxGroupParam = "SfxVolume";

        // BGM source phát nhạc cụ truyền thống trong các Đợt thường (Requirement 11.5).
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioClip _traditionalBgm;

        // Khoảng dB Unity Mixer dùng cho "mute" thực tế.
        private const float MuteDb = -80f;

        // Ngưỡng coi như tắt tiếng để tránh log10(0).
        private const float SilenceThreshold = 0.0001f;

        private IStorageService _storage;

        /// <summary>
        /// Khởi tạo service: nạp âm lượng từ <paramref name="storage"/> vào mixer và bắt đầu
        /// phát BGM truyền thống nếu đã được gán. GameSceneRoot gọi hàm này sau khi tạo
        /// <see cref="UnityStorageService"/>.
        /// </summary>
        /// <param name="storage">Nguồn chân lý âm lượng &amp; persistence (Requirements 12.1–12.3).</param>
        /// <exception cref="ArgumentNullException">Khi <paramref name="storage"/> là <c>null</c>.</exception>
        public void Initialize(IStorageService storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            // Requirements 12.1, 12.2, 12.3: đọc âm lượng đã lưu (mặc định 1.0 nếu chưa có).
            ApplyVolume(VolumeChannel.Music, _storage.ReadVolume(VolumeChannel.Music));
            ApplyVolume(VolumeChannel.Sfx, _storage.ReadVolume(VolumeChannel.Sfx));

            // Requirement 11.5: BGM nhạc cụ truyền thống cho Đợt thường — chỉ phát khi
            // designer đã gán cả AudioSource lẫn clip. Ở Đợt boss, các hệ thống khác có thể
            // thay clip qua _bgmSource trực tiếp.
            if (_bgmSource != null && _traditionalBgm != null)
            {
                _bgmSource.clip = _traditionalBgm;
                _bgmSource.loop = true;
                _bgmSource.Play();
            }
        }

        /// <summary>
        /// Ghi âm lượng mới cho <paramref name="channel"/> vào storage rồi áp vào mixer.
        /// Storage tự kẹp <c>[0, 1]</c> (Requirement 12.3); ta đọc lại sau khi ghi để đảm
        /// bảo mixer phản ánh đúng giá trị đã clamp.
        /// </summary>
        /// <exception cref="InvalidOperationException">Nếu gọi trước khi <see cref="Initialize"/>.</exception>
        public void SetVolume(VolumeChannel channel, float value)
        {
            if (_storage == null)
            {
                throw new InvalidOperationException("AudioService.SetVolume called before Initialize.");
            }

            _storage.WriteVolume(channel, value);
            ApplyVolume(channel, _storage.ReadVolume(channel));
        }

        /// <summary>
        /// Đọc âm lượng tuyến tính hiện hành cho <paramref name="channel"/> từ storage
        /// (mặc định 1.0 nếu chưa từng ghi). Tiện cho HUD/UI đồng bộ slider.
        /// </summary>
        public float GetVolume(VolumeChannel channel)
        {
            if (_storage == null)
            {
                throw new InvalidOperationException("AudioService.GetVolume called before Initialize.");
            }
            return _storage.ReadVolume(channel);
        }

        /// <summary>
        /// Ép đồng bộ lại âm lượng từ storage vào mixer. Dùng khi storage được cập nhật từ
        /// nơi khác (ví dụ menu cài đặt) và service cần phản ứng theo event.
        /// </summary>
        public void RefreshFromStorage()
        {
            if (_storage == null)
            {
                throw new InvalidOperationException("AudioService.RefreshFromStorage called before Initialize.");
            }
            ApplyVolume(VolumeChannel.Music, _storage.ReadVolume(VolumeChannel.Music));
            ApplyVolume(VolumeChannel.Sfx, _storage.ReadVolume(VolumeChannel.Sfx));
        }

        private void ApplyVolume(VolumeChannel channel, float linearVolume)
        {
            // Mixer có thể null trong test/headless; volume vẫn được lưu qua storage.
            if (_mixer == null)
            {
                return;
            }

            // Linear [0,1] → dB [-80, 0]; <= ngưỡng coi như mute để tránh log10(0).
            var db = linearVolume <= SilenceThreshold
                ? MuteDb
                : Mathf.Log10(Mathf.Clamp01(linearVolume)) * 20f;

            var param = channel switch
            {
                VolumeChannel.Music => _musicGroupParam,
                VolumeChannel.Sfx => _sfxGroupParam,
                _ => null,
            };

            if (!string.IsNullOrEmpty(param))
            {
                _mixer.SetFloat(param, db);
            }
        }
    }
}
