// Feature: tower-defense-vn, Property 23: HUD formatter strings
// Validates: Requirements 4.4, 4.5, 5.5, 7.3, 7.6, 8.4

using System;

namespace CSVH.Core.Hud
{
    /// <summary>
    /// Bộ trợ giúp định dạng chuỗi HUD cho Tower Defense Việt Nam.
    /// Tất cả thành viên là pure, deterministic và không phụ thuộc UnityEngine.
    /// Định dạng tuân thủ Property 23 của tài liệu thiết kế và các Yêu cầu được liệt kê.
    /// </summary>
    public static class Format
    {
        /// <summary>
        /// Trả về chuỗi hiển thị Đợt hiện tại theo định dạng <c>"Đợt {N}/∞"</c>.
        /// (Requirement 7.6, 9.1).
        /// </summary>
        /// <param name="n">Số Đợt hiện tại; phải <c>≥ 1</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="n"/> &lt; 1.</exception>
        public static string Wave(int n)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(n), n, "Số Đợt phải ≥ 1 theo Requirement 7.1.");
            }

            return $"Đợt {n}/∞";
        }

        /// <summary>
        /// Trả về chuỗi hiển thị Đợt kế tiếp theo định dạng <c>"Đợt kế tiếp: {N+1}"</c>.
        /// (Requirement 7.3).
        /// </summary>
        /// <param name="n">Số Đợt hiện tại; phải <c>≥ 1</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="n"/> &lt; 1.</exception>
        public static string NextWave(int n)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(n), n, "Số Đợt phải ≥ 1 theo Requirement 7.1.");
            }

            return $"Đợt kế tiếp: {n + 1}";
        }

        /// <summary>
        /// Trả về chuỗi đếm ngược Pha_Chuẩn_Bị theo định dạng <c>"Đếm ngược: {sec}"</c>.
        /// (Requirement 7.3).
        /// </summary>
        /// <param name="seconds">Số giây nguyên còn lại; phải <c>≥ 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="seconds"/> &lt; 0.</exception>
        public static string Countdown(int seconds)
        {
            if (seconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "Đếm ngược không được âm.");
            }

            return $"Đếm ngược: {seconds}";
        }

        /// <summary>
        /// Phiên bản float của <see cref="Countdown(int)"/>: làm tròn lên (ceil) về số giây
        /// nguyên rồi định dạng, để hiển thị chỉ rơi về <c>0</c> đúng tại thời điểm
        /// <paramref name="seconds"/> = 0. Tiện cho call-site truyền thẳng
        /// <c>WaveScheduler.Countdown</c>.
        /// </summary>
        /// <param name="seconds">Số giây còn lại; phải <c>≥ 0</c> và là số hữu hạn.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <paramref name="seconds"/> âm, <c>NaN</c> hoặc vô cực.
        /// </exception>
        public static string Countdown(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "Đếm ngược phải là số hữu hạn không âm.");
            }

            int whole = (int)Math.Ceiling(seconds);
            return $"Đếm ngược: {whole}";
        }

        /// <summary>
        /// Trả về chuỗi Máu theo định dạng <c>"{cur}/{max}"</c>.
        /// (Requirement 5.5, 9.5).
        /// </summary>
        /// <param name="current">Máu_Hiện_Tại; phải nằm trong <c>[0, max]</c>.</param>
        /// <param name="max">Máu_Tối_Đa; phải <c>&gt; 0</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <paramref name="max"/> &lt;= 0 hoặc <paramref name="current"/> ngoài <c>[0, max]</c>.
        /// </exception>
        public static string Hp(int current, int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(max), max, "Máu_Tối_Đa phải > 0 theo Requirement 5.1.");
            }

            if (current < 0 || current > max)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(current), current, "Máu_Hiện_Tại phải nằm trong [0, Máu_Tối_Đa] theo Requirement 5.3.");
            }

            return $"{current}/{max}";
        }

        /// <summary>
        /// Trả về chuỗi Cấp_Thành theo định dạng <c>"Cấp: {lvl}"</c>.
        /// (Requirement 4.4).
        /// </summary>
        /// <param name="level">Cấp_Thành; phải <c>≥ 1</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="level"/> &lt; 1.</exception>
        public static string Level(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(level), level, "Cấp_Thành phải ≥ 1 theo Requirement 4.1.");
            }

            return $"Cấp: {level}";
        }

        /// <summary>
        /// Trả về chuỗi thông báo "dọn sạch sớm" — đếm ngược tới lúc skip sang Đợt kế:
        /// <c>"Đã dọn sạch! Đợt kế sau {s}s"</c>. Lenient: kẹp về <c>0</c> nếu âm/NaN/Infinity.
        /// </summary>
        /// <param name="seconds">Số giây còn lại của khoảng ân hạn.</param>
        public static string EarlyClear(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                seconds = 0f;
            }

            int whole = (int)Math.Ceiling(seconds);
            return $"Đã dọn sạch! Đợt kế sau {whole}s";
        }

        /// <summary>
        /// Trả về chuỗi đếm ngược thời gian của Đợt đang chạy theo định dạng
        /// <c>"Còn lại: {m:ss}"</c> (hoặc <c>"Còn lại: {s}s"</c> khi &lt; 60 giây).
        /// Dùng cho chế độ Đợt-theo-thời-gian. Lenient: kẹp về <c>0</c> nếu giá trị
        /// âm/NaN/Infinity.
        /// </summary>
        /// <param name="seconds">Số giây còn lại của Đợt.</param>
        public static string WaveCountdown(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                seconds = 0f;
            }

            int whole = (int)Math.Ceiling(seconds);
            int minutes = whole / 60;
            int secs = whole % 60;

            return minutes > 0
                ? $"Còn lại: {minutes}:{secs:D2}"
                : $"Còn lại: {secs}s";
        }

        /// <summary>
        /// Trả về chuỗi thời gian chạy Đợt theo định dạng <c>"Thời gian: {m:ss}"</c>.
        /// Khi <paramref name="seconds"/> &lt; 60 hiển thị dạng <c>"Thời gian: {s}s"</c>.
        /// Lenient: kẹp về <c>0</c> nếu giá trị âm/NaN/Infinity để HUD không vỡ khi
        /// nhận snapshot bất thường.
        /// </summary>
        /// <param name="seconds">Số giây Đợt đã chạy.</param>
        public static string WaveElapsed(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                seconds = 0f;
            }

            int whole = (int)Math.Floor(seconds);
            int minutes = whole / 60;
            int secs = whole % 60;

            return minutes > 0
                ? $"Thời gian: {minutes}:{secs:D2}"
                : $"Thời gian: {secs}s";
        }

        /// <summary>
        /// Tỉ lệ EXP hiển thị cho vòng tiến trình Trái Dưới (Requirement 4.5).
        /// Giá trị luôn nằm trong <c>[0, 1]</c>.
        /// Lenient: nếu <paramref name="required"/> &lt;= 0, trả <c>0f</c> (không ném) để
        /// HUD vẫn render được trước khi <c>LevelingSystem</c> được khởi tạo.
        /// </summary>
        /// <param name="current">EXP_Hiện_Tại.</param>
        /// <param name="required">EXP_Cần_Cấp.</param>
        public static float ExpRatio(int current, int required)
        {
            if (required <= 0)
            {
                return 0f;
            }

            float ratio = (float)current / required;
            return Math.Clamp(ratio, 0f, 1f);
        }
    }
}
