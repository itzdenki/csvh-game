// Feature: tower-defense-vn, Property 17: Boss-wave predicate
// Validates: Requirements 7.7

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Wave;

namespace CSVH.Tests.Edit.Properties
{
    public class Property17_BossWavePredicate
    {
        [Test]
        public void IsBossWaveEqualsModulo5()
        {
            PbtRunner.RunForAll<PositiveInt>(waveNum =>
            {
                // Cap n at 50 to keep the iterative scheduler advancement bounded.
                int n = Math.Min(waveNum.Get, 50);

                // Build a scheduler with N copies of a simple wave so we can advance to wave n.
                var enemy = new EnemyConfig("E1", "Quái", 10f, 1f, 5f, 0f, 1, 1, 1);
                var spawn = new SpawnEntry("E1", 1, 1f);
                var waves = Enumerable.Range(1, Math.Max(n, 1))
                    .Select(i => new WaveConfig(
                        i,
                        new[] { spawn },
                        new[] { new FieldPoint(0, 5) },
                        0.5f))
                    .ToArray();
                var dict = new Dictionary<string, EnemyConfig> { { "E1", enemy } };
                var scheduler = new WaveScheduler(waves, dict);

                // Advance CurrentWave by ticking through Preparing/Active and clearing each wave.
                scheduler.Start();
                while (scheduler.CurrentWave < n)
                {
                    scheduler.Tick(1f, 0);
                    if (scheduler.State == WaveState.Active || scheduler.State == WaveState.Cleared)
                    {
                        scheduler.OnWaveCleared();
                    }
                }

                bool expected = scheduler.CurrentWave % 5 == 0;
                return scheduler.IsBossWave == expected;
            });
        }
    }
}
