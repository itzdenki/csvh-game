// Feature: tower-defense-vn, Property 25: Vietnamese translation completeness
// Validates: Requirements 11.3

namespace CSVH.Core.Hud
{
    /// <summary>
    /// Tập khóa định danh ổn định cho mọi nhãn UI mà người chơi nhìn thấy.
    /// Mọi khóa khai báo ở đây phải có bản dịch tương ứng trong
    /// <see cref="VietnameseBundle"/> (Property 25). Khi thêm khóa mới, hãy
    /// bổ sung chuỗi dịch tiếng Việt có dấu trong cùng commit.
    /// </summary>
    /// <remarks>
    /// Quy ước đặt tên: <c>{Vùng}.{Nhãn}</c>. Vùng thường khớp với tầng UI
    /// (Game, Hud, Upgrade, GameOver, Config, Audio, Settings). Khóa có
    /// hậu tố <c>WaveOf</c> dùng nội bộ bởi <see cref="Format"/> và không
    /// hiển thị trực tiếp.
    /// </remarks>
    public static class UiStringKeys
    {
        // --- Game / màn chính ---
        public const string GameTitle = "Game.Title";
        public const string GameStart = "Game.Start";
        public const string GameResume = "Game.Resume";
        public const string GameQuit = "Game.Quit";

        // --- HUD trong trận ---
        /// <summary>Nhãn "Đợt" cô đọng để ghép với số Đợt (Req 7.6).</summary>
        public const string WavePrefix = "ui.wave.prefix";
        /// <summary>Nhãn "Đợt kế tiếp" trong Pha_Chuẩn_Bị (Req 7.3).</summary>
        public const string WaveNext = "ui.wave.next";
        /// <summary>Nhãn đếm ngược (Req 7.3).</summary>
        public const string Countdown = "ui.countdown";
        /// <summary>Mẫu hiển thị "Đợt {N}/∞" — dùng nội bộ bởi <see cref="Format"/>.</summary>
        public const string HudWaveOf = "Hud.WaveOf";
        public const string HudNextWave = "Hud.NextWave";
        public const string HudCountdown = "Hud.Countdown";
        /// <summary>Nhãn cụm Máu trên HUD (Req 5.5).</summary>
        public const string HpFormat = "ui.hp.format";
        /// <summary>Nhãn Cấp_Thành ở HUD trên (Req 4.4).</summary>
        public const string LevelPrefix = "ui.level.prefix";
        /// <summary>Nhãn Điểm phiên — số được nối phía sau (Req 8.4).</summary>
        public const string ScorePrefix = "ui.score.session";
        /// <summary>Nhãn Kỷ_Lục (Req 8.4).</summary>
        public const string HighScorePrefix = "ui.score.high";
        /// <summary>Nhãn cụm Vàng (Req 6.3).</summary>
        public const string GoldPrefix = "ui.gold";
        public const string HudSessionScore = "Hud.SessionScore";
        public const string HudHighScore = "Hud.HighScore";

        // --- Nâng cấp ---
        public const string UpgradeArmor = "ui.upgrade.armor";
        public const string UpgradeAttack = "ui.upgrade.attack";
        public const string UpgradeSpecial = "ui.upgrade.special";
        public const string UpgradeExp = "ui.upgrade.exp";
        /// <summary>Toast khi <c>UpgradeSystem.TryBuy</c> trả <c>NotEnoughGold</c> (Req 6.3).</summary>
        public const string NotEnoughGold = "ui.toast.not_enough_gold";
        /// <summary>Toast khi kích hoạt Special trong lúc còn cooldown (Req 6.7).</summary>
        public const string SpecialOnCooldown = "ui.toast.special_cooldown";

        // --- Pha chuẩn bị ---
        /// <summary>Nhãn cho khu vực Pha_Chuẩn_Bị (Req 7.3).</summary>
        public const string PreparationLabel = "ui.preparation.label";

        // --- Kết thúc trận ---
        public const string GameOverTitle = "ui.gameover.title";
        public const string GameOverNewHighScore = "GameOver.NewHighScore";

        // --- Cấu hình ---
        public const string ConfigLoadError = "ui.config.error";
        public const string ConfigMissingFile = "Config.MissingFile";

        // --- Âm thanh ---
        public const string AudioMusicVolume = "Audio.MusicVolume";
        public const string AudioSfxVolume = "Audio.SfxVolume";

        // --- Cài đặt ---
        public const string SettingsTitle = "Settings.Title";

        /// <summary>
        /// Danh sách bất biến mọi khóa UI khai báo bên trên (đếm bằng tay để
        /// tránh reflection runtime). Property test (Property 25) và
        /// <see cref="VietnameseBundle"/> dựa vào đây để bảo đảm độ phủ bản dịch.
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<string> AllKeys =
            new string[]
            {
                // Đồng bộ với <see cref="VietnameseBundle.Build"/> — mọi khóa thêm
                // mới phải xuất hiện ở cả hai nơi để Property 25 (test phủ bản
                // dịch) còn pass.
                GameTitle,
                GameStart,
                GameResume,
                GameQuit,
                WavePrefix,
                WaveNext,
                Countdown,
                HudWaveOf,
                HudNextWave,
                HudCountdown,
                HpFormat,
                LevelPrefix,
                ScorePrefix,
                HighScorePrefix,
                GoldPrefix,
                HudSessionScore,
                HudHighScore,
                UpgradeArmor,
                UpgradeAttack,
                UpgradeSpecial,
                UpgradeExp,
                NotEnoughGold,
                SpecialOnCooldown,
                PreparationLabel,
                GameOverTitle,
                GameOverNewHighScore,
                ConfigLoadError,
                ConfigMissingFile,
                AudioMusicVolume,
                AudioSfxVolume,
                SettingsTitle,
            };

        /// <summary>
        /// Bí danh giữ tương thích ngược với code/test gọi <c>UiStringKeys.All</c>.
        /// Trỏ thẳng tới <see cref="AllKeys"/>.
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<string> All = AllKeys;

        /// <summary>
        /// Bí danh đọc-only cho <see cref="AllKeys"/>. Trong dự án này mọi khóa khai
        /// báo trong <see cref="UiStringKeys"/> đều là nhãn người chơi nhìn thấy
        /// nên hai danh sách trùng nhau; tách tên giúp call-site đọc dễ hơn khi
        /// chỉ quan tâm tới những chuỗi cần dịch (Property 25, Req 11.3).
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<string> PlayerFacingLabels = AllKeys;
    }
}
