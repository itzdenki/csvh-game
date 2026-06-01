// Feature: tower-defense-vn
// Validates: Requirements 8.6, 12.1, 12.2, 12.3 (Bộ_Lưu_Trữ Kỷ_Lục và âm lượng).

namespace CSVH.Core.Storage
{
    /// <summary>
    /// Cổng (port) trừu tượng giúp Core đọc/ghi dữ liệu bền vững mà KHÔNG phụ thuộc
    /// <c>UnityEngine</c>:
    /// <list type="bullet">
    ///   <item>Kỷ_Lục (<see cref="ReadHighScore"/> / <see cref="WriteHighScore"/>) — Requirement 8.6.</item>
    ///   <item>Âm lượng theo kênh (<see cref="ReadVolume"/> / <see cref="WriteVolume"/>) — Requirement 12.1, 12.2, 12.3.</item>
    /// </list>
    ///
    /// <para>
    /// Hợp đồng (contract) bắt buộc với mọi triển khai:
    /// <list type="number">
    ///   <item>
    ///   <see cref="ReadHighScore"/> trả <c>0</c> khi chưa có giá trị (Requirement 8.6); KHÔNG được trả số âm.
    ///   </item>
    ///   <item>
    ///   <see cref="WriteHighScore(long)"/> theo sau bởi <see cref="ReadHighScore"/> trả lại đúng giá trị đã ghi
    ///   với mọi <c>k ∈ [0, 2^31 − 1]</c> (Property 19 round-trip — Requirement 8.6, 12.2).
    ///   </item>
    ///   <item>
    ///   <see cref="ReadVolume(VolumeChannel)"/> trả <c>1.0f</c> khi kênh chưa được ghi (Requirement 12.2 default).
    ///   </item>
    ///   <item>
    ///   <see cref="WriteVolume(VolumeChannel, float)"/> kẹp giá trị vào <c>[0.0, 1.0]</c> trước khi lưu
    ///   (Requirement 12.3); <see cref="ReadVolume"/> kế tiếp trả về giá trị đã kẹp
    ///   (Property 20 round-trip — Requirement 12.1, 12.2, 12.3).
    ///   </item>
    ///   <item>Triển khai có thể là async (PlayerPrefs / file IO) hoặc đồng bộ (in-memory test).</item>
    /// </list>
    /// </para>
    ///
    /// Triển khai test mẫu: <see cref="InMemoryStorageService"/>.
    /// Triển khai production: <c>UnityStorageService</c> (xem task 9.1 — PlayerPrefs + persistentDataPath).
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Đọc Kỷ_Lục đã lưu. Trả <c>0</c> nếu chưa có dữ liệu (Requirement 8.6).
        /// Triển khai production có thể fallback về <c>0</c> khi parse JSON thất bại
        /// và ghi log cảnh báo (Requirement 12.4) — xem <c>UnityStorageService</c>.
        /// </summary>
        long ReadHighScore();

        /// <summary>
        /// Ghi đè Kỷ_Lục mới. Sau lần gọi này, <see cref="ReadHighScore"/>
        /// PHẢI trả về cùng giá trị (Property 19 round-trip — Requirement 8.6, 12.2).
        /// </summary>
        /// <param name="value">Kỷ_Lục mới; kỳ vọng <c>≥ 0</c>.</param>
        void WriteHighScore(long value);

        /// <summary>
        /// Đọc âm lượng cho <paramref name="channel"/> trong khoảng <c>[0.0, 1.0]</c>;
        /// trả mặc định <c>1.0f</c> nếu chưa có dữ liệu (Requirement 12.2).
        /// </summary>
        float ReadVolume(VolumeChannel channel);

        /// <summary>
        /// Ghi âm lượng mới cho <paramref name="channel"/>. Triển khai PHẢI kẹp
        /// <paramref name="value"/> vào <c>[0.0, 1.0]</c> trước khi lưu (Requirement 12.3),
        /// và <see cref="ReadVolume"/> kế tiếp PHẢI trả về giá trị đã kẹp
        /// (Property 20 round-trip — Requirement 12.1, 12.2, 12.3).
        /// </summary>
        void WriteVolume(VolumeChannel channel, float value);
    }
}
