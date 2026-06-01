// Feature: tower-defense-vn, Property 18: Tích lũy Điểm_Phiên đơn điệu và Kỷ_Lục bằng max
// Validates: Requirements 8.2, 8.3, 8.5

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;
using CSVH.Core.Storage;

namespace CSVH.Tests.Edit.Properties
{
    public class Property18_ScoreMonotonic
    {
        [Test]
        public void SessionScoreIsMonotonic()
        {
            PbtRunner.RunForAll<NonNegativeInt[], NonNegativeInt[]>((kills, waveBonuses) =>
            {
                if (kills == null) kills = System.Array.Empty<NonNegativeInt>();
                if (waveBonuses == null) waveBonuses = System.Array.Empty<NonNegativeInt>();

                var t = new ScoreTracker();
                long prev = t.SessionScore;
                for (int i = 0; i < Math.Min(kills.Length, waveBonuses.Length); i++)
                {
                    t.AddEnemyKill(kills[i].Get);
                    if (t.SessionScore < prev) return false;
                    prev = t.SessionScore;

                    int wave = (i % 50) + 1;
                    t.AddWaveCompletion(wave, waveBonuses[i].Get);
                    if (t.SessionScore < prev) return false;
                    prev = t.SessionScore;
                }
                return true;
            });
        }

        [Test]
        public void HighScoreIsMaxAfterFinalize()
        {
            PbtRunner.RunForAll<NonNegativeInt, NonNegativeInt>((initialHigh, sessionDelta) =>
            {
                var storage = new InMemoryStorageService();
                storage.WriteHighScore(initialHigh.Get);

                var t = new ScoreTracker();
                t.LoadHighScore(storage);
                t.AddEnemyKill(sessionDelta.Get);

                long expectedAfter = Math.Max(t.HighScore, t.SessionScore);
                bool wasNewHigh = t.SessionScore > t.HighScore;

                bool result = t.TryFinalize(storage);

                return t.HighScore == expectedAfter
                    && result == wasNewHigh
                    && storage.ReadHighScore() == t.HighScore;
            });
        }
    }
}
