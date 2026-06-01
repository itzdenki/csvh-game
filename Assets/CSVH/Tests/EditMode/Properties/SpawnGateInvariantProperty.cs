// Feature: tower-defense-vn, Property 3: Bất biến vị trí Cổng_Spawn và Vị_Trí_Thành
// Validates: Requirements 1.1, 1.3, 2.1
//
// Property 3 (design.md): For any FieldGeometry hợp lệ và mọi SpawnIntent emitted
// bởi WaveScheduler:
//   gate.X ≤ 0 ∨ gate.Y ≥ 0   (Cổng_Spawn nằm trên biên Tây / Bắc / góc Tây Bắc)
//   TowerPosition.X > 0 ∧ TowerPosition.Y < 0   (Thành ở góc Đông Nam)
//
// Generator (WaveSetupSeedArb) sinh mọi SpawnGate hợp lệ và TowerPosition cố định
// tại (1, -1) trong FieldGeometry — đáp ứng tiền điều kiện của Property 3 sao cho
// chỉ vi phạm thực sự (regression trong WaveScheduler.Tick) mới làm thuộc tính
// thất bại. Test chạy WaveScheduler tới khi đủ một đợt SpawnIntent rồi kiểm gate
// + tower invariants.

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
    /// Hạt giống test cho Property 3: ghép một <see cref="ConfigBundle"/> nhỏ gọn
    /// nhưng hợp lệ (Đợt 1 với 1-2 <see cref="SpawnEntry"/>, mỗi entry count ≤ 5,
    /// interval ≤ 1s, mọi Cổng_Spawn thỏa <c>X ≤ 0 ∨ Y ≥ 0</c>) với một
    /// <see cref="FieldGeometry"/> trong đó <see cref="FieldGeometry.TowerPosition"/>
    /// cố định tại <c>(1, -1)</c> — đáp ứng <c>X &gt; 0 ∧ Y &lt; 0</c> (Requirement 1.1).
    /// <para/>
    /// Dùng class thay vì record để tránh phụ thuộc vào polyfill
    /// <c>System.Runtime.CompilerServices.IsExternalInit</c> trong assembly test
    /// (Core assembly có polyfill riêng nhưng nó internal, không lan sang Tests).
    /// </summary>
    public sealed class WaveSetupSeed
    {
        public ConfigBundle Bundle { get; }
        public FieldGeometry Geometry { get; }

        public WaveSetupSeed(ConfigBundle bundle, FieldGeometry geometry)
        {
            Bundle = bundle;
            Geometry = geometry;
        }
    }

    /// <summary>
    /// Custom <see cref="Arbitrary{T}"/> sinh <see cref="WaveSetupSeed"/> hợp lệ cho
    /// Property 3. Lấy cảm hứng từ <c>ConfigBundleArb</c> ở task 2.4 nhưng:
    /// <list type="bullet">
    ///   <item>Bound nhỏ hơn (count ≤ 5, interval ∈ [0.05, 1.0]s, prep ∈ [0, 1.0]s)
    ///   để mỗi case PBT tiến qua đủ một Đợt trong &lt; ~150 tick.</item>
    ///   <item>Mọi <see cref="WaveConfig.SpawnGates"/> được sinh thiên về một trong
    ///   hai biên hợp lệ (X ≤ 0) hoặc (Y ≥ 0) — Property 2 đã đảm nhiệm chiều
    ///   "rejects invalid"; ở đây ta chỉ test bất biến trên đầu vào hợp lệ.</item>
    ///   <item><see cref="FieldGeometry.TowerPosition"/> cố định <c>(1, -1)</c> —
    ///   thỏa <see cref="FieldPoint.IsValidTowerPoint"/> theo Requirement 1.1.</item>
    /// </list>
    /// </summary>
    public static class WaveSetupSeedArb
    {
        // Tower cố định trong góc Đông Nam (X > 0 ∧ Y < 0): (1, -1).
        // FieldGeometry kích thước nhỏ vừa đủ chứa các Cổng_Spawn được sinh trong
        // [-10, 10] mà không ảnh hưởng đến bất biến cần kiểm.
        private static readonly FieldGeometry Geometry =
            new FieldGeometry(
                HalfWidth: 10f,
                HalfHeight: 10f,
                TowerPosition: new FieldPoint(1f, -1f),
                TowerCollisionRadius: 0.5f);

        public static Arbitrary<WaveSetupSeed> Seed() => Arb.From(GenSeed());

        // ---- Generators cho giá trị nguyên thủy ----

        // Float dương "sạch" trong [0.05, 1.0] cho SpawnIntervalSeconds — đủ nhỏ
        // để Tick(0.1) tiến nhanh qua các spawn nhưng vẫn > 0 (Requirement 7.2).
        private static Gen<float> GenIntervalSeconds() =>
            Gen.Choose(5, 100).Select(i => i / 100f);

        // Float không âm trong [0.0, 1.0] cho PreparationSeconds — giữ Pha_Chuẩn_Bị
        // ngắn để mỗi PBT iteration kết thúc trong ngân sách thời gian.
        private static Gen<float> GenPreparationSeconds() =>
            Gen.Choose(0, 100).Select(i => i / 100f);

        // Float dương trong [0.1, 100] cho stats Quái — đủ rộng nhưng tránh
        // Infinity/NaN; mọi giá trị đều thỏa ràng buộc của ConfigLoader.
        private static Gen<float> GenStatFloat() =>
            Gen.Choose(1, 1000).Select(i => i / 10f);

        // Float bất kỳ (sạch, hữu hạn) trong [-10, 10] cho tọa độ Cổng_Spawn.
        private static Gen<float> GenCoord() =>
            Gen.Choose(-1000, 1000).Select(i => i / 100f);

        // Int không âm ≤ 100 cho rewards — giữ payload nhỏ.
        private static Gen<int> GenStatInt() => Gen.Choose(0, 100);

        // Spawn count phải ≥ 1 để mỗi đợt sinh ≥ 1 SpawnIntent → loop test thật
        // sự quan sát được ít nhất một intent.
        private static Gen<int> GenSpawnCount() => Gen.Choose(1, 5);

        // Tên Quái dạng ASCII chữ hoa (A-Z) độ dài 1-6: tránh vướng escaping JSON
        // và đủ unique. Property 25 sẽ phủ ký tự VN có dấu.
        private static Gen<string> GenName() =>
            Gen.Choose(1, 6).SelectMany(len =>
                Gen.ArrayOf(len, Gen.Choose('A', 'Z'))
                   .Select(arr => new string(arr.Select(i => (char)i).ToArray())));

        // Cổng_Spawn hợp lệ: thiên về một trong hai biên (X ≤ 0) hoặc (Y ≥ 0)
        // bằng cách lấy giá trị tuyệt đối trên một trục theo nhánh ngẫu nhiên.
        private static Gen<FieldPoint> GenSpawnGate() =>
            Gen.Choose(0, 1).SelectMany(branch =>
                GenCoord().SelectMany(x =>
                GenCoord().Select(y =>
                    branch == 0
                        ? new FieldPoint(-Math.Abs(x), y)   // X ≤ 0
                        : new FieldPoint(x, Math.Abs(y))))); // Y ≥ 0

        // ---- Generators cho records cấu hình ----

        // EnemyConfig với Id placeholder ("?" - sẽ được gán "E{n}" sau khi sinh dãy).
        private static Gen<EnemyConfig> GenEnemyBody() =>
            GenName().SelectMany(name =>
            GenStatFloat().SelectMany(maxHp =>
            GenStatFloat().SelectMany(speed =>
            GenStatFloat().SelectMany(melee =>
            GenStatFloat().SelectMany(resist =>
            GenStatInt().SelectMany(gold =>
            GenStatInt().SelectMany(exp =>
            GenStatInt().Select(score =>
                new EnemyConfig(
                    Id: "?",
                    LocalizedName: name,
                    MaxHp: maxHp,
                    Speed: speed,
                    MeleeDamage: melee,
                    Resistance: resist,
                    GoldReward: gold,
                    ExpReward: exp,
                    ScoreReward: score)))))))));

        private static Gen<SpawnEntry> GenSpawnEntry(IReadOnlyList<string> enemyIds) =>
            Gen.Elements(enemyIds.ToArray()).SelectMany(id =>
            GenSpawnCount().SelectMany(count =>
            GenIntervalSeconds().Select(interval =>
                new SpawnEntry(id, count, interval))));

        private static Gen<WaveSetupSeed> GenSeed() =>
            Gen.Choose(1, 2).SelectMany(numEnemies =>
                Gen.ArrayOf(numEnemies, GenEnemyBody()).SelectMany(enemyBodies =>
                {
                    // Gán Id duy nhất "E1".."EN" để SpawnEntry tham chiếu được.
                    var enemies = enemyBodies
                        .Select((e, i) => e with { Id = $"E{i + 1}" })
                        .ToList();
                    var ids = enemies.Select(e => e.Id).ToList();

                    return Gen.Choose(1, 2).SelectMany(numSpawns =>
                        Gen.ArrayOf(numSpawns, GenSpawnEntry(ids)).SelectMany(spawns =>
                            Gen.Choose(1, 3).SelectMany(numGates =>
                                Gen.ArrayOf(numGates, GenSpawnGate()).SelectMany(gates =>
                                    GenPreparationSeconds().Select(prep =>
                                    {
                                        var wave = new WaveConfig(
                                            WaveNumber: 1,
                                            Spawns: (IReadOnlyList<SpawnEntry>)spawns.ToList(),
                                            SpawnGates: (IReadOnlyList<FieldPoint>)gates.ToList(),
                                            PreparationSeconds: prep);
                                        var bundle = new ConfigBundle(
                                            (IReadOnlyList<EnemyConfig>)enemies,
                                            (IReadOnlyList<WaveConfig>)new[] { wave });
                                        return new WaveSetupSeed(bundle, Geometry);
                                    })))));
                }));
    }

    /// <summary>
    /// Property 3: Bất biến vị trí Cổng_Spawn và Vị_Trí_Thành. Với mọi
    /// <see cref="FieldGeometry"/> hợp lệ và mọi <see cref="SpawnIntent"/> phát ra
    /// bởi <see cref="WaveScheduler"/>: <c>gate.X ≤ 0 ∨ gate.Y ≥ 0</c> và
    /// <c>TowerPosition.X &gt; 0 ∧ TowerPosition.Y &lt; 0</c>.
    /// </summary>
    public class SpawnGateInvariantProperty
    {
        // Feature: tower-defense-vn, Property 3
        // Validates: Requirements 1.1, 1.3, 2.1
        [Test]
        public void Property3_SpawnGateInvariant()
        {
            PbtRunner.RunForAll(WaveSetupSeedArb.Seed(), (WaveSetupSeed seed) =>
            {
                // -- Tiền điều kiện trên geometry: TowerPosition phải ở góc Đông Nam.
                //    Đây là phần "TowerPosition.X > 0 ∧ TowerPosition.Y < 0" của Property 3,
                //    được đặt bằng generator (1, -1) và kiểm trực tiếp tại đây.
                if (!seed.Geometry.TowerPosition.IsValidTowerPoint())
                {
                    TestContext.WriteLine(
                        $"TowerPosition vi phạm Requirement 1.1: " +
                        $"({seed.Geometry.TowerPosition.X}, {seed.Geometry.TowerPosition.Y}).");
                    return false;
                }

                // -- Tiền điều kiện trên cấu hình: mọi Cổng_Spawn được khai báo phải hợp lệ
                //    (Requirement 1.3 / 2.1). Generator đã đảm bảo, kiểm lại để nhanh
                //    chóng phát hiện sai sót generator.
                var wave = seed.Bundle.Waves[0];
                for (int i = 0; i < wave.SpawnGates.Count; i++)
                {
                    var gate = wave.SpawnGates[i];
                    if (!gate.IsValidSpawnPoint())
                    {
                        TestContext.WriteLine(
                            $"SpawnGates[{i}] vi phạm Requirements 1.3/2.1: ({gate.X}, {gate.Y}).");
                        return false;
                    }
                }

                // -- Cấu thành scheduler từ bundle.
                var enemiesById = seed.Bundle.Enemies.ToDictionary(e => e.Id);
                var scheduler = new WaveScheduler(seed.Bundle.Waves, enemiesById);
                scheduler.Start();

                // Tổng SpawnIntent dự kiến = Σ count trong wave[0] — loop ticking đến
                // khi quan sát đủ con số này (hoặc vượt ngưỡng MaxFrames để tránh treo).
                int expectedTotal = 0;
                for (int i = 0; i < wave.Spawns.Count; i++) expectedTotal += wave.Spawns[i].Count;

                int totalEmitted = 0;
                // 5000 ticks × 0.1s = 500s sim time; với prep ≤ 1s, interval ≤ 1s,
                // count ≤ 10 (5×2), Đợt kết thúc sau ≪ 12s sim time. 5000 là biên
                // an toàn rộng rãi để bắt regression mà không kéo dài CI.
                const int MaxFrames = 5000;

                for (int frame = 0; frame < MaxFrames && totalEmitted < expectedTotal; frame++)
                {
                    var intents = scheduler.Tick(0.1f, aliveEnemies: 0, spawnCap: 200);
                    for (int i = 0; i < intents.Count; i++)
                    {
                        var intent = intents[i];

                        // -- Bất biến cốt lõi của Property 3: mọi gate phát ra hợp lệ.
                        if (!intent.Gate.IsValidSpawnPoint())
                        {
                            TestContext.WriteLine(
                                $"SpawnIntent với gate vi phạm tại frame {frame}: " +
                                $"({intent.Gate.X}, {intent.Gate.Y}).");
                            return false;
                        }

                        totalEmitted++;
                    }
                }

                // Bảo vệ chống regression khiến scheduler không bao giờ phát SpawnIntent —
                // nếu emit thiếu, bài test này không thực sự kiểm được Property 3.
                if (totalEmitted < expectedTotal)
                {
                    TestContext.WriteLine(
                        $"Scheduler không phát đủ một Đợt trong {MaxFrames} ticks: " +
                        $"emitted={totalEmitted}, expected={expectedTotal}.");
                    return false;
                }

                return true;
            });
        }
    }
}
