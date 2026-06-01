// Feature: tower-defense-vn
// Validates: Requirements 12.1, 12.2, 12.3 (âm lượng nhạc và hiệu ứng tách kênh).

namespace CSVH.Core.Storage
{
    /// <summary>
    /// Kênh âm lượng phân biệt được lưu trữ độc lập trong <see cref="IStorageService"/>:
    /// nhạc nền (<see cref="Music"/>) và hiệu ứng âm thanh (<see cref="Sfx"/>).
    /// Mỗi kênh có khóa lưu trữ riêng (xem <see cref="StorageKeys"/>) và giá trị
    /// trong khoảng <c>[0.0, 1.0]</c>, mặc định <c>1.0</c> khi chưa có dữ liệu
    /// (Requirements 12.1, 12.2, 12.3).
    /// </summary>
    public enum VolumeChannel
    {
        /// <summary>Âm lượng nhạc nền (BGM, ví dụ đàn bầu / sáo trúc / trống).</summary>
        Music = 0,

        /// <summary>Âm lượng hiệu ứng (bắn Đạn, va chạm, kích hoạt Special, ...).</summary>
        Sfx = 1,
    }
}
