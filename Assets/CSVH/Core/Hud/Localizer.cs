// Feature: tower-defense-vn, Property 25: Vietnamese translation completeness
// Validates: Requirements 11.3

using System;
using System.Collections.Generic;
using CSVH.Core.Logging;

namespace CSVH.Core.Hud
{
    /// <summary>
    /// Bộ tra cứu chuỗi UI theo mã ngôn ngữ. Pure C# — không phụ thuộc UnityEngine.
    /// Tầng Unity sẽ tạo một thực thể, đăng ký <see cref="VietnameseBundle.Build"/>
    /// dưới mã <c>"vi"</c>, và gọi <see cref="Get(string,string)"/> tại các view.
    /// </summary>
    /// <remarks>
    /// Hợp đồng:
    /// <list type="bullet">
    /// <item>Khi không tìm thấy bundle hoặc khóa, <see cref="Get(string,string)"/>
    /// trả về <c>"[?key]"</c> (placeholder hiển thị) và ghi cảnh báo qua <see cref="ILogSink"/>.</item>
    /// <item>Cảnh báo dùng cùng mẫu để tests so khớp dễ (Property 25).</item>
    /// <item>Trả về placeholder thay vì throw để HUD luôn render được, kể cả khi
    /// bundle chưa kịp đăng ký trong khung hình đầu tiên.</item>
    /// </list>
    /// </remarks>
    public sealed class Localizer
    {
        /// <summary>Mã ngôn ngữ mặc định cho phiên bản hiện tại của trò chơi.</summary>
        public const string DefaultLanguage = "vi";

        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _bundles;
        private readonly ILogSink _logSink;

        /// <summary>
        /// Khởi tạo bộ tra cứu rỗng. Truyền <paramref name="logSink"/> = <c>null</c>
        /// để vô hiệu hóa log (sử dụng <see cref="NullLogSink"/>). Constructor mặc
        /// định cũng tự đăng ký bundle <c>"vi"</c> qua <see cref="VietnameseBundle.Build"/>
        /// để call-site điển hình không phải làm thủ công.
        /// </summary>
        public Localizer(ILogSink logSink = null)
        {
            _bundles = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            _logSink = logSink ?? NullLogSink.Instance;

            // Đăng ký sẵn bundle tiếng Việt — đây là ngôn ngữ duy nhất đang hỗ trợ
            // (Req 11.3). Test có thể gọi RegisterBundle để thay thế nếu cần.
            _bundles[DefaultLanguage] = VietnameseBundle.Build();
        }

        /// <summary>
        /// Factory tiện dụng: tạo <see cref="Localizer"/> đã đăng ký sẵn bundle
        /// <c>"vi"</c>. Tương đương <c>new Localizer()</c>; tách ra để call-site
        /// (đặc biệt là test Property 25) đọc dễ hơn.
        /// </summary>
        public static Localizer CreateDefaultVietnamese()
        {
            return new Localizer();
        }

        /// <summary>
        /// Factory tiện dụng cho phép truyền <see cref="ILogSink"/> để quan sát
        /// cảnh báo khi tra cứu khóa thiếu (Property 25).
        /// </summary>
        public static Localizer CreateDefaultVietnamese(ILogSink logSink)
        {
            return new Localizer(logSink);
        }

        /// <summary>
        /// Đăng ký một bundle dịch cho mã ngôn ngữ <paramref name="langCode"/>.
        /// Đăng ký lại cùng mã sẽ thay thế bundle cũ — tiện cho test fixture.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Khi <paramref name="langCode"/> hoặc <paramref name="bundle"/> là <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Khi <paramref name="langCode"/> rỗng.</exception>
        public void RegisterBundle(string langCode, IReadOnlyDictionary<string, string> bundle)
        {
            if (langCode is null) throw new ArgumentNullException(nameof(langCode));
            if (langCode.Length == 0)
            {
                throw new ArgumentException("Mã ngôn ngữ không được rỗng.", nameof(langCode));
            }

            if (bundle is null) throw new ArgumentNullException(nameof(bundle));

            _bundles[langCode] = bundle;
        }

        /// <summary>
        /// Tra cứu chuỗi đã dịch cho <paramref name="key"/> theo
        /// <paramref name="langCode"/>. Trả <c>"[?key]"</c> và phát cảnh báo nếu
        /// không tìm thấy bundle hoặc khóa.
        /// </summary>
        /// <param name="key">Khóa từ <see cref="UiStringKeys"/>.</param>
        /// <param name="langCode">Mã ngôn ngữ; mặc định <see cref="DefaultLanguage"/>.</param>
        /// <exception cref="ArgumentNullException">Khi <paramref name="key"/> là <c>null</c>.</exception>
        public string Get(string key, string langCode = DefaultLanguage)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));

            string placeholder = "[?" + key + "]";

            if (string.IsNullOrEmpty(langCode))
            {
                _logSink.Warn($"Localizer: thiếu mã ngôn ngữ khi tra khóa '{key}', trả placeholder.");
                return placeholder;
            }

            if (!_bundles.TryGetValue(langCode, out var bundle))
            {
                _logSink.Warn($"Localizer: chưa đăng ký bundle cho ngôn ngữ '{langCode}' (khóa '{key}').");
                return placeholder;
            }

            if (!bundle.TryGetValue(key, out var value) || value is null)
            {
                _logSink.Warn($"Localizer: thiếu khóa '{key}' trong bundle '{langCode}'.");
                return placeholder;
            }

            return value;
        }

        /// <summary>
        /// Phiên bản try-pattern — không phát cảnh báo, không trả placeholder.
        /// Dùng khi caller muốn fallback yên lặng (vd. chuỗi tùy chọn).
        /// </summary>
        public bool TryGet(string key, string langCode, out string value)
        {
            if (key is null || string.IsNullOrEmpty(langCode))
            {
                value = null;
                return false;
            }

            if (!_bundles.TryGetValue(langCode, out var bundle))
            {
                value = null;
                return false;
            }

            if (!bundle.TryGetValue(key, out var translation) || translation is null)
            {
                value = null;
                return false;
            }

            value = translation;
            return true;
        }
    }
}
