// Feature: tower-defense-vn, Property 25: Vietnamese translation completeness
// Validates: Requirements 11.3

using System.Collections.Generic;

namespace CSVH.Core.Hud
{
    /// <summary>
    /// Bundle bản dịch tiếng Việt mặc định cho mã ngôn ngữ <c>"vi"</c>.
    /// Mỗi giá trị là chuỗi UTF-8 không rỗng và chứa ít nhất một ký tự có dấu —
    /// giữ đúng tinh thần "Cổ Sử Việt Hùng" (Req 11.3).
    /// </summary>
    /// <remarks>
    /// Khi thêm khóa mới vào <see cref="UiStringKeys"/>, hãy bổ sung bản dịch tại
    /// đây. Property 25 sẽ phát hiện khóa thiếu trong CI nếu quên.
    /// </remarks>
    public static class VietnameseBundle
    {
        /// <summary>
        /// Dựng và trả về bundle bất biến gồm mọi nhãn UI tiếng Việt.
        /// Trả <see cref="IReadOnlyDictionary{TKey,TValue}"/> để caller không
        /// thể sửa (ngăn rò rỉ trạng thái giữa các <see cref="Localizer"/>).
        /// </summary>
        public static IReadOnlyDictionary<string, string> Build()
        {
            // Sử dụng dictionary thường rồi trả qua interface IReadOnlyDictionary.
            // Đủ cho yêu cầu hiện tại (read-after-build) và tương thích .NET Standard 2.1.
            var bundle = new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                // --- Game / màn chính ---
                { UiStringKeys.GameTitle,   "Cổ Sử Việt Hùng" },
                { UiStringKeys.GameStart,   "Bắt đầu" },
                { UiStringKeys.GameResume,  "Tiếp tục" },
                { UiStringKeys.GameQuit,    "Thoát" },

                // --- HUD trong trận ---
                { UiStringKeys.WavePrefix,      "Đợt" },
                { UiStringKeys.WaveNext,        "Đợt kế tiếp" },
                { UiStringKeys.Countdown,       "Đếm ngược" },
                { UiStringKeys.HudNextWave,     "Đợt kế tiếp" },
                // Mẫu nội bộ; Format.Wave/NextWave sinh chuỗi cụ thể với số Đợt.
                { UiStringKeys.HudWaveOf,       "Đợt {0}/∞" },
                { UiStringKeys.HudCountdown,    "Đếm ngược" },
                { UiStringKeys.HpFormat,        "Máu" },
                { UiStringKeys.LevelPrefix,     "Cấp" },
                { UiStringKeys.ScorePrefix,     "Điểm" },
                { UiStringKeys.HighScorePrefix, "Cao nhất" },
                { UiStringKeys.GoldPrefix,      "Vàng" },
                { UiStringKeys.HudSessionScore, "Điểm phiên" },
                { UiStringKeys.HudHighScore,    "Kỷ lục" },

                // --- Nâng cấp ---
                { UiStringKeys.UpgradeArmor,      "Giáp" },
                { UiStringKeys.UpgradeAttack,     "Công" },
                { UiStringKeys.UpgradeSpecial,    "Đặc biệt" },
                { UiStringKeys.UpgradeExp,        "Kinh nghiệm" },
                { UiStringKeys.NotEnoughGold,     "Không đủ Vàng" },
                { UiStringKeys.SpecialOnCooldown, "Chiêu đặc biệt đang hồi" },

                // --- Pha chuẩn bị ---
                { UiStringKeys.PreparationLabel, "Chuẩn bị" },

                // --- Kết thúc trận ---
                { UiStringKeys.GameOverTitle,        "Kết thúc trận" },
                { UiStringKeys.GameOverNewHighScore, "Lập kỷ lục mới!" },

                // --- Cấu hình ---
                { UiStringKeys.ConfigLoadError,   "Cấu hình lỗi" },
                { UiStringKeys.ConfigMissingFile, "Thiếu tệp cấu hình" },

                // --- Âm thanh ---
                { UiStringKeys.AudioMusicVolume, "Âm lượng nhạc" },
                { UiStringKeys.AudioSfxVolume,   "Âm lượng hiệu ứng" },

                // --- Cài đặt ---
                { UiStringKeys.SettingsTitle, "Cài đặt" },
            };

            return bundle;
        }
    }
}
