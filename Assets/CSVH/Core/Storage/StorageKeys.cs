// Feature: tower-defense-vn
// Validates: Requirements 8.6, 12.1, 12.2, 12.3 (khóa/đường dẫn lưu trữ ổn định).

using System;

namespace CSVH.Core.Storage
{
    /// <summary>
    /// Tập hợp các khóa hằng dùng bởi mọi triển khai <see cref="IStorageService"/>
    /// (cả <c>InMemoryStorageService</c> dùng cho test lẫn <c>UnityStorageService</c>
    /// lớp Unity ghi vào <c>PlayerPrefs</c> + <c>Application.persistentDataPath</c>).
    ///
    /// <para>
    /// Việc tập trung khóa tại một nơi giúp:
    /// <list type="bullet">
    ///   <item>Tránh "magic string" rải rác khắp codebase (Property 19, 20 round-trip).</item>
    ///   <item>Đảm bảo cùng một khóa được dùng giữa các phiên chơi để bảo toàn dữ liệu (Requirement 8.6, 12.2).</item>
    ///   <item>Cho phép cập nhật namespace khóa nếu schema lưu trữ phải thay đổi (Requirement 12.4).</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class StorageKeys
    {
        /// <summary>Khóa <c>PlayerPrefs</c> cho âm lượng nhạc nền (Requirement 12.1, 12.2, 12.3).</summary>
        public const string MusicVolume = "csvh.volume.music";

        /// <summary>Khóa <c>PlayerPrefs</c> cho âm lượng hiệu ứng (Requirement 12.1, 12.2, 12.3).</summary>
        public const string SfxVolume = "csvh.volume.sfx";

        /// <summary>
        /// Tên tệp Kỷ_Lục lưu cạnh <c>Application.persistentDataPath</c>
        /// dưới dạng JSON <c>{ "highScore": &lt;long&gt; }</c> (Requirement 8.6).
        /// Core không nắm đường dẫn tuyệt đối — lớp Unity nối với
        /// <c>persistentDataPath</c> tại runtime để tránh phụ thuộc <c>UnityEngine</c>.
        /// </summary>
        public const string HighScoreFile = "highscore.json";

        /// <summary>
        /// Tên tệp tiến trình META (Xu cổ) lưu cạnh <c>Application.persistentDataPath</c>
        /// dưới dạng JSON <c>{ "coins": &lt;long&gt;, "gateHpLevel": &lt;int&gt;,
        /// "crossbowDamageLevel": &lt;int&gt;, "ultimateCooldownLevel": &lt;int&gt; }</c>
        /// (GDD Cơ chế 2 — Meta Upgrade). Như Kỷ_Lục, Core không nắm đường dẫn tuyệt đối.
        /// </summary>
        public const string MetaProgressFile = "meta_progress.json";

        /// <summary>
        /// Trả về khóa <c>PlayerPrefs</c> tương ứng <paramref name="channel"/>.
        /// Là API thuần để dùng được từ cả Core test (<c>InMemoryStorageService</c>)
        /// lẫn lớp Unity, đảm bảo nhất quán khóa (Property 20 round-trip).
        /// </summary>
        public static string VolumeKey(VolumeChannel channel) => channel switch
        {
            VolumeChannel.Music => MusicVolume,
            VolumeChannel.Sfx => SfxVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unknown VolumeChannel."),
        };
    }
}
