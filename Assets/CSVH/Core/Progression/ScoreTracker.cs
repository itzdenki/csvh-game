// Feature: tower-defense-vn, Property 18: Tích lũy Điểm_Phiên đơn điệu và Kỷ_Lục bằng max
//   - SessionScore đơn điệu không giảm.
//   - SessionScore = Σ reward (AddEnemyKill) + Σ (waveBonusBase × waveNumber) (AddWaveCompletion).
//   - Sau Finalize: HighScore' = max(HighScore, SessionScore).
// Validates: Requirements 8.1, 8.2, 8.3, 8.5, 8.6

using System;
using CSVH.Core.Storage;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Theo dõi Điểm_Phiên (<see cref="SessionScore"/>) và Kỷ_Lục (<see cref="HighScore"/>) của Thành.
    /// Pure C#, không tham chiếu UnityEngine.
    ///
    /// <para>
    /// Bất biến trong vòng đời một instance:
    /// <list type="bullet">
    ///   <item><description><see cref="SessionScore"/> ≥ 0 và đơn điệu không giảm trong toàn bộ phiên (Requirement 8.1, 8.2, 8.3).</description></item>
    ///   <item><description><see cref="HighScore"/> ≥ 0; chỉ tăng (hoặc giữ nguyên) sau mỗi <see cref="TryFinalize"/> (Requirement 8.5).</description></item>
    ///   <item><description>Mọi phép cộng được kẹp tại <see cref="long.MaxValue"/> để tránh tràn số.</description></item>
    /// </list>
    /// </para>
    ///
    /// Tham chiếu thiết kế: section "Core - Score Tracker" trong design.md, Property 18.
    /// </summary>
    public sealed class ScoreTracker
    {
        /// <summary>
        /// Điểm_Phiên hiện tại; khởi tạo <c>0</c> khi tạo instance (Requirement 8.1) và
        /// đơn điệu không giảm theo các sự kiện <see cref="AddEnemyKill"/> /
        /// <see cref="AddWaveCompletion"/> (Requirement 8.2, 8.3).
        /// </summary>
        public long SessionScore { get; private set; }

        /// <summary>
        /// Kỷ_Lục đã biết. Mặc định <c>0</c> trước khi gọi <see cref="LoadHighScore"/>
        /// (Requirement 8.6: nếu Bộ_Lưu_Trữ chưa có giá trị thì coi như <c>0</c>).
        /// </summary>
        public long HighScore { get; private set; }

        /// <summary>
        /// Cộng <paramref name="reward"/> vào <see cref="SessionScore"/> khi một Quái bị tiêu diệt.
        /// Phép cộng được kẹp tại <see cref="long.MaxValue"/> để bảo toàn bất biến đơn điệu
        /// kể cả với chuỗi sự kiện cực đoan trong PBT.
        /// </summary>
        /// <param name="reward">Phần_Thưởng_Điểm; phải ≥ 0 (Requirement 8.2).</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="reward"/> &lt; 0.</exception>
        public void AddEnemyKill(int reward)
        {
            if (reward < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reward), reward, "reward must be >= 0.");
            }

            SessionScore = AddClampedToMax(SessionScore, reward);
        }

        /// <summary>
        /// Cộng <c>waveBonusBase × waveNumber</c> vào <see cref="SessionScore"/> khi một Đợt
        /// hoàn thành (Requirement 8.3). Phép nhân thực hiện bằng <see cref="long"/> để
        /// tránh tràn <see cref="int"/>; phép cộng cuối được kẹp tại <see cref="long.MaxValue"/>.
        /// </summary>
        /// <param name="waveNumber">Số Đợt vừa hoàn thành; phải ≥ 0.</param>
        /// <param name="waveBonusBase">Thưởng_Hoàn_Thành_Đợt cơ bản; phải ≥ 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <paramref name="waveNumber"/> hoặc <paramref name="waveBonusBase"/> &lt; 0.
        /// </exception>
        public void AddWaveCompletion(int waveNumber, int waveBonusBase)
        {
            if (waveNumber < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(waveNumber), waveNumber, "waveNumber must be >= 0.");
            }

            if (waveBonusBase < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(waveBonusBase), waveBonusBase, "waveBonusBase must be >= 0.");
            }

            // Cả hai toán hạng đều ≥ 0 và ≤ int.MaxValue; tích long an toàn (≤ ~4.6e18).
            long bonus = (long)waveBonusBase * waveNumber;
            SessionScore = AddClampedToMaxLong(SessionScore, bonus);
        }

        /// <summary>
        /// Đọc Kỷ_Lục từ <paramref name="storage"/> và gán vào <see cref="HighScore"/>.
        /// Áp dụng <c>max(0, storage.ReadHighScore())</c> để đảm bảo bất biến không âm
        /// kể cả khi triển khai trả về số âm bất thường (defensive — vẫn tôn trọng hợp đồng
        /// trong <see cref="IStorageService.ReadHighScore"/> trả 0 cho default — Requirement 8.6).
        /// </summary>
        /// <param name="storage">Bộ_Lưu_Trữ; không được <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Khi <paramref name="storage"/> là <c>null</c>.</exception>
        public void LoadHighScore(IStorageService storage)
        {
            if (storage is null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            long stored = storage.ReadHighScore();
            HighScore = stored < 0 ? 0 : stored;
        }

        /// <summary>
        /// Hoàn tất phiên: nếu <see cref="SessionScore"/> &gt; <see cref="HighScore"/>, cập nhật
        /// <see cref="HighScore"/> bằng <see cref="SessionScore"/> và ghi vào <paramref name="storage"/>;
        /// trả về <c>true</c> khi lập kỷ lục mới, ngược lại <c>false</c> và không đụng <paramref name="storage"/>.
        /// (Requirement 8.5: chỉ ghi khi vượt kỷ lục.)
        /// </summary>
        /// <param name="storage">Bộ_Lưu_Trữ; không được <c>null</c>.</param>
        /// <returns><c>true</c> nếu vừa lập kỷ lục mới; ngược lại <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Khi <paramref name="storage"/> là <c>null</c>.</exception>
        public bool TryFinalize(IStorageService storage)
        {
            if (storage is null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (SessionScore > HighScore)
            {
                HighScore = SessionScore;
                storage.WriteHighScore(HighScore);
                return true;
            }

            return false;
        }

        /// <summary>Cộng một <see cref="int"/> không âm vào <see cref="long"/>, kẹp tại <see cref="long.MaxValue"/>.</summary>
        private static long AddClampedToMax(long a, int b)
        {
            // Caller đảm bảo a ≥ 0, b ≥ 0; long.MaxValue - a ≥ 0 nên so sánh an toàn không tràn.
            if (b > long.MaxValue - a) return long.MaxValue;
            return a + b;
        }

        /// <summary>Cộng hai <see cref="long"/> không âm, kẹp tại <see cref="long.MaxValue"/>.</summary>
        private static long AddClampedToMaxLong(long a, long b)
        {
            if (b > long.MaxValue - a) return long.MaxValue;
            return a + b;
        }
    }
}
