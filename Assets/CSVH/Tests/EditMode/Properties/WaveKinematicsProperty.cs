// Feature: tower-defense-vn, Property 15: Vận động học wave (model-based)
// Validates: Requirements 7.2, 13.3, 13.4
//
// Mô hình kinematic cho WaveScheduler. Sinh ngẫu nhiên một WaveScenario nhỏ
// (1-5 SpawnEntry với Count ∈ [1,10], Σ count ≤ 50) rồi chạy scheduler đến khi
// State == Cleared, kiểm 4 bất biến của Property 15:
//   1) Tổng SpawnIntent phát ra = Σ count trong cấu hình Đợt.
//   2) Số Quái sống đồng thời ≤ 200 (Cap_Sống, Requirement 13.4).
//   3) Khi đợt kết thúc: WaveState == Cleared.
//   4) Sau OnWaveCleared(): Countdown == PreparationSeconds của Đợt kế (Requirement 7.2).

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
    /// <summary>
    /// Mô tả cấu hình một kịch bản hai-Đợt deterministic dùng để kiểm Property 15:
    /// một Đợt "main" với danh sách <see cref="SpawnEntry"/> có Count nhỏ và một Đợt
    /// "next" để kiểm reset Countdown sau <c>OnWaveCleared</c>.
    /// <para/>
    /// Dùng class thay vì record để tránh phụ thuộc vào polyfill
    /// <c>System.Runtime.CompilerServices.IsExternalInit</c> trong assembly test
    /// (Core assembly có polyfill riêng nhưng nó internal, không lan sang Tests).
    /// </summary>
    public sealed class WaveScenario
    {
        public IReadOnlyList<SpawnEntry> Spawns { get; }
        public float MainPreparationSeconds { get; }
        public float NextPreparationSeconds { get; }

        public WaveScenario(
            IReadOnlyList<SpawnEntry> spawns,
            float mainPreparationSeconds,
            float nextPreparationSeconds)
        {
            Spawns = spawns;
            MainPreparationSeconds = mainPreparationSeconds;
            NextPreparationSeconds = nextPreparationSeconds;
        }
    }

    /// <summary>
    /// Sinh <see cref="WaveScenario"/> hợp lệ cho Property 15: 1-5 spawn entry,
    /// mỗi Count ∈ [1,10], tổng Count ≤ 50, interval > 0, prep seconds ≥ 0 và
    /// một Đợt kế tiếp với prep seconds rõ ràng để kiểm reset.
    /// </summary>
    public static class WaveScenarioArb
    {
        private const int MinEntries = 1;
        private const int MaxEntries = 5;
        private const int MinCountPerEntry = 1;
        private const int MaxCountPerEntry = 10;
        private const int MaxTotalCount = 50;

        public static Arbitrary<WaveScenario> Scenario() => Arb.From(GenScenario());

        private static Gen<float> GenInterval() =>
            Gen.Choose(1, 200).Select(i => i / 100f); // (0.01, 2.00]

        private static Gen<float> GenPreparation() =>
            Gen.Choose(0, 500).Select(i => i / 100f); // [0, 5.00]

        private static Gen<int> GenCount() =>
            Gen.Choose(MinCountPerEntry, MaxCountPerEntry);

        private static Gen<SpawnEntry> GenSpawnEntry() =>
            GenCount().SelectMany(count =>
            GenInterval().Select(interval =>
                new SpawnEntry("E1", count, interval)));

        private static Gen<IReadOnlyList<SpawnEntry>> GenSpawnEntries() =>
            Gen.Choose(MinEntries, MaxEntries).SelectMany(n =>
                Gen.ArrayOf(n, GenSpawnEntry()).Select(arr =>
                {
                    // Cap tổng count ≤ MaxTotalCount: cắt entry cuối nếu vượt.
                    var list = new List<SpawnEntry>(arr.Length);
                    int total = 0;
                    foreach (var e in arr)
                    {
                        int allowed = MaxTotalCount - total;
                        if (allowed <= 0) break;
                        int c = Math.Min(e.Count, allowed);
                        list.Add(new SpawnEntry(e.EnemyId, c, e.SpawnIntervalSeconds));
                        total += c;
                    }
                    if (list.Count == 0)
                    {
                        // Bảo hiểm: luôn có ít nhất một entry hợp lệ.
                        list.Add(new SpawnEntry("E1", 1, 0.5f));
                    }
                    return (IReadOnlyList<SpawnEntry>)list;
                }));

        private static Gen<WaveScenario> GenScenario() =>
            GenSpawnEntries().SelectMany(spawns =>
            GenPreparation().SelectMany(mainPrep =>
            GenPreparation().Select(nextPrep =>
                new WaveScenario(spawns, mainPrep, nextPrep))));
    }

    /// <summary>
    /// Property 15: Vận động học wave (model-based).
    /// </summary>
    public class WaveKinematicsProperty
    {
        private const int SpawnCap = 200;
        private const int MaxFrames = 1000;
        private const float Dt = 0.1f;

        private static EnemyConfig MakeEnemy() =>
            new EnemyConfig(
                Id: "E1",
                LocalizedName: "E1",
                MaxHp: 100f,
                Speed: 1f,
                MeleeDamage: 10f,
                Resistance: 0f,
                GoldReward: 10,
                ExpReward: 10,
                ScoreReward: 10);

        private static IReadOnlyDictionary<string, EnemyConfig> MakeEnemyDict() =>
            new Dictionary<string, EnemyConfig> { ["E1"] = MakeEnemy() };

        private static WaveConfig MakeMainWave(WaveScenario s) =>
            new WaveConfig(
                WaveNumber: 1,
                Spawns: s.Spawns,
                SpawnGates: new List<FieldPoint> { new FieldPoint(0f, 5f) },
                PreparationSeconds: s.MainPreparationSeconds);

        private static WaveConfig MakeNextWave(WaveScenario s) =>
            new WaveConfig(
                WaveNumber: 2,
                Spawns: new List<SpawnEntry> { new SpawnEntry("E1", 1, 1.0f) },
                SpawnGates: new List<FieldPoint> { new FieldPoint(0f, 5f) },
                PreparationSeconds: s.NextPreparationSeconds);

        // Feature: tower-defense-vn, Property 15: Vận động học wave (model-based)
        // Validates: Requirements 7.2, 13.3, 13.4
        //
        // Chạy scheduler trên một WaveScenario sinh ngẫu nhiên đến khi đợt kết thúc
        // (State == Cleared) hoặc đạt MaxFrames; kiểm 4 bất biến của Property 15.
        // Mô hình "kill 1 quái/frame" giữ aliveEnemies bị chặn nhỏ để cap-respect
        // có ý nghĩa và đảm bảo đợt cuối cùng cũng vào Cleared.
        [Test]
        public void Property15_WaveKinematics()
        {
            PbtRunner.RunForAll(WaveScenarioArb.Scenario(), (WaveScenario scenario) =>
            {
                int expectedTotal = scenario.Spawns.Sum(s => s.Count);

                var waves = new List<WaveConfig>
                {
                    MakeMainWave(scenario),
                    MakeNextWave(scenario),
                };
                var scheduler = new WaveScheduler(waves, MakeEnemyDict());
                scheduler.Start();

                int totalEmitted = 0;
                int aliveEnemies = 0;
                int maxAliveObserved = 0;
                bool reachedCleared = false;

                for (int frame = 0; frame < MaxFrames; frame++)
                {
                    int aliveBefore = aliveEnemies;
                    var intents = scheduler.Tick(Dt, aliveEnemies, SpawnCap);

                    // Bất biến 2: cap respect mỗi tick.
                    int allowed = Math.Max(0, SpawnCap - aliveBefore);
                    if (intents.Count > allowed)
                    {
                        TestContext.WriteLine(
                            $"Cap violated at frame {frame}: emitted={intents.Count} > allowed={allowed} (alive={aliveBefore}).");
                        return false;
                    }

                    totalEmitted += intents.Count;
                    aliveEnemies += intents.Count;
                    if (aliveEnemies > maxAliveObserved) maxAliveObserved = aliveEnemies;

                    // Mô hình "kill 1 quái/frame" để cuối cùng aliveEnemies về 0,
                    // cho phép scheduler tự transition vào Cleared.
                    if (aliveEnemies > 0) aliveEnemies--;

                    if (scheduler.State == WaveState.Cleared)
                    {
                        reachedCleared = true;
                        break;
                    }
                }

                // Bất biến 1: tổng spawn = Σ count.
                if (totalEmitted != expectedTotal)
                {
                    TestContext.WriteLine(
                        $"Total spawn mismatch: emitted={totalEmitted}, expected={expectedTotal}, " +
                        $"entries=[{string.Join(",", scenario.Spawns.Select(s => s.Count))}].");
                    return false;
                }

                // Bất biến 2 (overall): max alive ≤ SpawnCap.
                if (maxAliveObserved > SpawnCap)
                {
                    TestContext.WriteLine(
                        $"Max alive {maxAliveObserved} exceeded SpawnCap {SpawnCap}.");
                    return false;
                }

                // Bất biến 3: state cuối là Cleared.
                if (!reachedCleared || scheduler.State != WaveState.Cleared)
                {
                    TestContext.WriteLine(
                        $"Scheduler did not reach Cleared (final State={scheduler.State}, frames={MaxFrames}).");
                    return false;
                }

                // Bất biến 4: OnWaveCleared reset Countdown về PreparationSeconds của Đợt kế.
                scheduler.OnWaveCleared();
                if (scheduler.State != WaveState.Preparing)
                {
                    TestContext.WriteLine(
                        $"After OnWaveCleared, State should be Preparing but was {scheduler.State}.");
                    return false;
                }
                if (scheduler.CurrentWave != 2)
                {
                    TestContext.WriteLine(
                        $"After OnWaveCleared, CurrentWave should be 2 but was {scheduler.CurrentWave}.");
                    return false;
                }
                if (Math.Abs(scheduler.Countdown - scenario.NextPreparationSeconds) > 1e-5f)
                {
                    TestContext.WriteLine(
                        $"After OnWaveCleared, Countdown should be {scenario.NextPreparationSeconds} but was {scheduler.Countdown}.");
                    return false;
                }

                return true;
            }, maxTest: 50);
        }
    }
}
