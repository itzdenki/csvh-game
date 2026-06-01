// Feature: tower-defense-vn, Property 19: Round-trip Kỷ_Lục qua Bộ_Lưu_Trữ
// Validates: Requirements 8.6, 12.2

using FsCheck;
using NUnit.Framework;
using CSVH.Core.Storage;

namespace CSVH.Tests.Edit.Properties
{
    public class Property19_HighScoreRoundTrip
    {
        [Test]
        public void RoundTripHighScore()
        {
            PbtRunner.RunForAll<NonNegativeInt>(kP =>
            {
                long k = (long)kP.Get;
                if (k > int.MaxValue) k = int.MaxValue;
                var s = new InMemoryStorageService();
                s.WriteHighScore(k);
                return s.ReadHighScore() == k;
            });
        }

        [Test]
        public void DefaultHighScoreIsZero()
        {
            var s = new InMemoryStorageService();
            Assert.AreEqual(0L, s.ReadHighScore());
        }
    }
}
