// Feature: tower-defense-vn, Property 11: Bất biến giới hạn Máu và dừng spawn khi Kết_Thúc_Trận
// Validates: Requirements 5.3, 5.4

using System;
using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Combat;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Wave;

namespace CSVH.Tests.Edit.Properties
{
    public class Property11_HpBoundsAndGameOver
    {
        // Phần A — Requirement 5.3: ClampHp luôn trả giá trị trong [0, max].
        [Test]
        public void HpAlwaysWithinBounds()
        {
            PbtRunner.RunForAll<int, PositiveInt>((newValue, maxP) =>
            {
                int max = Math.Min(maxP.Get, 100000);
                int clamped = CombatResolver.ClampHp(newValue, max);
                return clamped >= 0 && clamped <= max;
            });
        }

        // Phần B — Requirement 5.4: sau OnGameOver, Tick luôn trả danh sách rỗng và state == GameOver.
        // Note: GameSession (task 5.4) chưa có; ở đây chỉ test khía cạnh "OnGameOver halts spawning"
        // trực tiếp trên WaveScheduler (đủ cho Property 11 phần halt).
        [Test]
        public void GameOverHaltsSpawning()
        {
            PbtRunner.RunForAll<NonNegativeInt[], NonNegativeInt[]>((dts, aliveCounts) =>
            {
                if (dts == null) dts = System.Array.Empty<NonNegativeInt>();
                if (aliveCounts == null) aliveCounts = System.Array.Empty<NonNegativeInt>();

                var enemy = new EnemyConfig("E1", "Quái", 10f, 1f, 5f, 0f, 1, 1, 1);
                var spawn = new SpawnEntry("E1", 100, 0.1f);
                var wave = new WaveConfig(1, new[] { spawn }, new[] { new FieldPoint(0f, 5f) }, 0f);
                var dict = new Dictionary<string, EnemyConfig> { { "E1", enemy } };
                var scheduler = new WaveScheduler(new[] { wave }, dict);
                scheduler.Start();
                scheduler.OnGameOver();

                int n = Math.Min(dts.Length, aliveCounts.Length);
                for (int i = 0; i < n; i++)
                {
                    float dt = (dts[i].Get % 5) + 0.01f;
                    int alive = aliveCounts[i].Get % 50;
                    var emitted = scheduler.Tick(dt, alive);
                    if (emitted.Count != 0) return false;
                }
                return scheduler.State == WaveState.GameOver;
            }, maxTest: 50);
        }
    }
}
