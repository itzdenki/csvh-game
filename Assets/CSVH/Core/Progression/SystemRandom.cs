// Feature: tower-defense-vn — impl IRandom dùng System.Random cho runtime.
// Validates: Requirements 6.6.

using System;

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Hiện thực <see cref="IRandom"/> bằng <see cref="System.Random"/> cho runtime trong
    /// Unity (System.Random thuộc .NET BCL nên Core vẫn không tham chiếu UnityEngine).
    /// Test dùng một <c>FakeRandom</c> riêng để có kết quả tất định.
    /// </summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _rng;

        /// <summary>Tạo RNG với seed thời gian (không tất định) cho gameplay thật.</summary>
        public SystemRandom()
        {
            _rng = new Random();
        }

        /// <summary>Tạo RNG với <paramref name="seed"/> cố định để tái lập kết quả khi cần.</summary>
        public SystemRandom(int seed)
        {
            _rng = new Random(seed);
        }

        /// <inheritdoc />
        public double NextDouble() => _rng.NextDouble();
    }
}
