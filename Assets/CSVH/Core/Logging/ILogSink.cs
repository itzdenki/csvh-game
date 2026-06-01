// Feature: tower-defense-vn, Property 21 / 25: observable log sink
// Validates: Requirements 11.3, 12.4

namespace CSVH.Core.Logging
{
    /// <summary>
    /// Cổng ghi log thuần Core (không phụ thuộc UnityEngine). Tầng Unity sẽ
    /// implement adapter chuyển sang <c>UnityEngine.Debug</c>; tests dùng
    /// implementation in-memory để quan sát lời gọi (Property 21 cho fallback
    /// storage và Property 25 cho cảnh báo khi <c>Localizer.Get</c> không tìm
    /// thấy khóa).
    /// </summary>
    public interface ILogSink
    {
        /// <summary>Ghi cảnh báo phục vụ chẩn đoán (vd. khóa dịch thiếu, dữ liệu hỏng).</summary>
        void Warn(string message);

        /// <summary>Ghi lỗi (đường dẫn không khả thi nhưng cần lưu vết).</summary>
        void Error(string message);

        /// <summary>Ghi thông tin (sự kiện nhịp sống, không phải cảnh báo).</summary>
        void Info(string message);
    }

    /// <summary>
    /// Triển khai no-op của <see cref="ILogSink"/>. Tiện cho tests và call-site
    /// muốn vô hiệu hóa log mà không phải kiểm <c>null</c>.
    /// </summary>
    public sealed class NullLogSink : ILogSink
    {
        /// <summary>Singleton dùng chung; an toàn vì không giữ trạng thái.</summary>
        public static readonly NullLogSink Instance = new NullLogSink();

        /// <inheritdoc />
        public void Warn(string message)
        {
        }

        /// <inheritdoc />
        public void Error(string message)
        {
        }

        /// <inheritdoc />
        public void Info(string message)
        {
        }
    }
}
