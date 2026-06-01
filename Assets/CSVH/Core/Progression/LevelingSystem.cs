// Feature: tower-defense-vn, Property 10: Bất biến hệ leveling
//   - Level đơn điệu không giảm.
//   - 0 ≤ CurrentExp < RequiredExp sau mỗi cập nhật.
//   - RequiredExp_n = ⌈RequiredExp_{n-1} × scale⌉ mỗi khi lên cấp.
//   - Tổng EXP đã cộng (kẹp tại int.MaxValue) = Σ levelCosts + CurrentExp.
// Validates: Requirements 4.1, 4.2, 4.3, 4.5

using System;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Kết quả của một lần gọi <see cref="LevelingSystem.AddExp(int)"/>.
    /// Là <c>readonly record struct</c> để có value equality và allocation-free.
    /// Bao gồm số cấp đã tăng và snapshot trạng thái cuối để caller (ví dụ HUD)
    /// có thể đọc mà không cần truy ngược lại <see cref="LevelingSystem"/>.
    /// </summary>
    /// <param name="LevelsGained">Số cấp đã tăng trong lần cộng EXP đó (≥ 0).</param>
    /// <param name="FinalLevel">Giá trị <see cref="LevelingSystem.Level"/> sau khi xử lý xong.</param>
    /// <param name="FinalCurrentExp">Giá trị <see cref="LevelingSystem.CurrentExp"/> sau khi xử lý xong (∈ [0, FinalRequiredExp)).</param>
    /// <param name="FinalRequiredExp">Giá trị <see cref="LevelingSystem.RequiredExp"/> sau khi xử lý xong (&gt; 0).</param>
    public readonly record struct LevelUpResult(
        int LevelsGained,
        int FinalLevel,
        int FinalCurrentExp,
        int FinalRequiredExp);

    /// <summary>
    /// Hệ thống cấp/EXP của Thành. Pure C#, không tham chiếu UnityEngine.
    /// Bất biến sau mỗi lần gọi <see cref="AddExp"/>:
    /// <list type="bullet">
    ///   <item><description><c>Level ≥ 1</c> và không bao giờ giảm so với trạng thái trước đó.</description></item>
    ///   <item><description><c>0 ≤ CurrentExp &lt; RequiredExp</c>.</description></item>
    ///   <item><description><c>RequiredExp</c> sau mỗi lần lên cấp = <c>⌈RequiredExp × scale⌉</c>, kẹp tại <see cref="int.MaxValue"/>.</description></item>
    /// </list>
    /// Tham chiếu thiết kế: section "Core - Leveling System" trong design.md.
    /// </summary>
    public sealed class LevelingSystem
    {
        private readonly float _scale;
        private readonly int _baseRequired;

        /// <summary>Cấp_Thành hiện tại; khởi tạo 1, đơn điệu không giảm (Requirement 4.1, 4.3).</summary>
        public int Level { get; private set; } = 1;

        /// <summary>EXP_Hiện_Tại; luôn nằm trong <c>[0, RequiredExp)</c> sau mỗi cập nhật (Requirement 4.3, 4.5).</summary>
        public int CurrentExp { get; private set; }

        /// <summary>EXP_Cần_Cấp; cập nhật theo <c>⌈RequiredExp × scale⌉</c> mỗi lần lên cấp (Requirement 4.3).</summary>
        public int RequiredExp { get; private set; }

        /// <summary>
        /// Khởi tạo hệ thống với <paramref name="baseRequired"/> là EXP cần để đạt cấp 2
        /// và hệ số thang cấp <paramref name="scale"/>.
        /// </summary>
        /// <param name="baseRequired">EXP cần cấp ban đầu; phải &gt; 0 (Requirement 4.6).</param>
        /// <param name="scale">Hệ số tăng EXP mỗi cấp; phải ≥ 1.0 (lược đồ thiết kế).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Khi <paramref name="baseRequired"/> ≤ 0, hoặc <paramref name="scale"/> &lt; 1.0, hoặc <paramref name="scale"/> là NaN.
        /// Đây là lỗi lập trình: theo nguyên tắc Core, exception chỉ dành cho lỗi lập trình.
        /// </exception>
        public LevelingSystem(int baseRequired, float scale)
        {
            if (baseRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseRequired), baseRequired, "baseRequired must be > 0.");
            }

            // Dùng !(>= 1.0f) để bắt cả NaN lẫn scale < 1.
            if (!(scale >= 1.0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale), scale, "scale must be >= 1.0 and not NaN.");
            }

            _baseRequired = baseRequired;
            _scale = scale;
            RequiredExp = baseRequired;
        }

        /// <summary>
        /// Cộng EXP vào <see cref="CurrentExp"/> rồi lặp lên cấp khi đạt ngưỡng.
        /// Phép cộng và phép nhân bằng <see cref="_scale"/> được kẹp tại <see cref="int.MaxValue"/>
        /// để tránh tràn số trong các chuỗi gọi cực đoan (ví dụ FsCheck với input lớn).
        /// </summary>
        /// <param name="amount">Lượng EXP cần cộng; phải ≥ 0 (Requirement 4.2).</param>
        /// <returns>
        /// <see cref="LevelUpResult"/> chứa số cấp đã tăng và snapshot
        /// (<see cref="Level"/>, <see cref="CurrentExp"/>, <see cref="RequiredExp"/>) sau khi xử lý.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="amount"/> &lt; 0.</exception>
        public LevelUpResult AddExp(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "amount must be >= 0.");
            }

            // Cộng kẹp: tránh tràn int khi CurrentExp + amount > int.MaxValue.
            CurrentExp = AddClampedToMax(CurrentExp, amount);

            int levelsGained = 0;
            // Bất biến vòng lặp (Requirement 4.3): mỗi lần lặp giảm CurrentExp đúng RequiredExp,
            // tăng Level (đơn điệu), rồi cập nhật RequiredExp = ⌈RequiredExp × scale⌉.
            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level = AddClampedToMax(Level, 1);
                RequiredExp = ScaleCeilingClampedToMax(RequiredExp, _scale);
                levelsGained = AddClampedToMax(levelsGained, 1);

                // Khi RequiredExp đã chạm int.MaxValue, vòng lặp sẽ tự kết thúc ở vòng kế
                // vì CurrentExp < RequiredExp = int.MaxValue.
            }

            return new LevelUpResult(levelsGained, Level, CurrentExp, RequiredExp);
        }

        /// <summary>Cộng hai số nguyên không âm, kẹp kết quả tại <see cref="int.MaxValue"/>.</summary>
        private static int AddClampedToMax(int a, int b)
        {
            // Caller đảm bảo a ≥ 0, b ≥ 0; dùng long để phát hiện tràn.
            long sum = (long)a + b;
            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }

        /// <summary>
        /// Tính <c>⌈value × scale⌉</c> dưới dạng <see cref="int"/>, kẹp tại <see cref="int.MaxValue"/>.
        /// Dùng <see cref="double"/> để giảm sai số khi <paramref name="value"/> lớn (mantissa float chỉ 24-bit).
        /// </summary>
        private static int ScaleCeilingClampedToMax(int value, float scale)
        {
            // value ≥ 1 (RequiredExp luôn > 0 theo bất biến) và scale ≥ 1.0 ⇒ ceil ≥ value.
            double ceil = Math.Ceiling((double)value * scale);
            if (ceil >= int.MaxValue) return int.MaxValue;
            return (int)ceil;
        }
    }
}
