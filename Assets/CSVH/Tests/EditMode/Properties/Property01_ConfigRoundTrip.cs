// Feature: tower-defense-vn, Property 1: Round-trip cấu hình JSON
// Validates: Requirements 10.2, 10.4, 10.5, 10.6
//
// Hai chiều round-trip:
//   1) Load(WriteEnemies(b.Enemies), WriteWaves(b.Waves)) ≡ b   (value equality records)
//   2) Cho s_e = WriteEnemies(b.Enemies), s_w = WriteWaves(b.Waves), thì
//      WriteEnemies(Load(s_w, s_e).Enemies) ≡ s_e và
//      WriteWaves  (Load(s_w, s_e).Waves)   ≡ s_w  (sau chuẩn hóa whitespace).
//
// Sinh cấu hình hợp lệ qua FsCheck Arbitrary để bộ nạp không từ chối
// (Property 2 đã đảm nhiệm chiều "rejects invalid"). Mọi ràng buộc lược đồ
// được tôn trọng tại nơi sinh giá trị: floats > 0 / ≥ 0, ints ∈ [0, 1000],
// id quái dạng "E1","E2",..., spawn gates thỏa X ≤ 0 ∨ Y ≥ 0, và
// SpawnEntry chỉ tham chiếu tới id quái đã sinh trong cùng bundle.

using System;
using System.Collections.Generic;
using System.Linq;
using CSVH.Core.Common;
using CSVH.Core.Config;
using FsCheck;
using NUnit.Framework;

namespace CSVH.Tests.Edit.Properties
{
    /// <summary>
    /// Cung cấp <see cref="Arbitrary{T}"/> cho <see cref="ConfigBundle"/> hợp lệ:
    /// 1-5 <see cref="EnemyConfig"/> với id duy nhất "E1".."EN" và 1-3
    /// <see cref="WaveConfig"/> với <see cref="SpawnEntry"/> tham chiếu tới các id đã sinh.
    /// </summary>
    public static class ConfigBundleArb
    {
        // Số lượng và biên giá trị giữ nhỏ để vòng test 100 iterations vẫn nhanh.
        private const int MinEnemies = 1;
        private const int MaxEnemies = 5;
        private const int MinWaves = 1;
        private const int MaxWaves = 3;
        private const int MinSpawnsPerWave = 1;
        private const int MaxSpawnsPerWave = 5;
        private const int MinGatesPerWave = 1;
        private const int MaxGatesPerWave = 4;
        private const int MaxIntScalar = 1000;

        public static Arbitrary<ConfigBundle> Bundle() => Arb.From(GenBundle());

        // ---------------- Generators cho giá trị nguyên thủy ----------------

        // Float dương "sạch" trong (1, 1001] dùng cho MaxHp/Speed/SpawnIntervalSeconds.
        // Math.Abs() + 1f đảm bảo > 0 ngay cả khi int ngẫu nhiên = 0; giá trị bội của
        // 1/100 round-trip ổn định qua Newtonsoft "R" format.
        private static Gen<float> GenPositiveFloat() =>
            Gen.Choose(0, 100_000).Select(i => Math.Abs(i / 100f) + 1f);

        // Float không âm trong [0, 1000] dùng cho MeleeDamage/Resistance/PreparationSeconds.
        private static Gen<float> GenNonNegativeFloat() =>
            Gen.Choose(0, 100_000).Select(i => Math.Abs(i / 100f));

        // Float bất kỳ (sạch, hữu hạn) trong [-1000, 1000] dùng cho tọa độ Cổng_Spawn.
        private static Gen<float> GenAnyFloat() =>
            Gen.Choose(-100_000, 100_000).Select(i => i / 100f);

        // Int không âm ≤ 1000 cho Gold/Exp/Score/Count.
        private static Gen<int> GenNonNegativeIntCapped() =>
            Gen.Choose(0, MaxIntScalar);

        // Chuỗi không rỗng UTF-8 thân thiện. Dùng các ký tự ASCII chữ hoa để
        // tránh vướng escaping JSON tinh vi (đã có round-trip trên ASCII là đủ
        // mạnh cho Property 1; ký tự VN có dấu được phủ ở Property 25).
        private static Gen<string> GenNonEmptyName() =>
            Gen.Choose(1, 8).SelectMany(len =>
                Gen.ArrayOf(len, Gen.Choose('A', 'Z'))
                   .Select(arr => new string(arr.Select(i => (char)i).ToArray())));

        // Cổng_Spawn hợp lệ: thiên về một trong hai biên (X ≤ 0) hoặc (Y ≥ 0)
        // bằng cách lấy giá trị tuyệt đối trên một trục theo nhánh ngẫu nhiên.
        private static Gen<FieldPoint> GenSpawnGate() =>
            Gen.Choose(0, 1).SelectMany(branch =>
                GenAnyFloat().SelectMany(x =>
                GenAnyFloat().Select(y =>
                    branch == 0
                        ? new FieldPoint(-Math.Abs(x), y)   // X ≤ 0
                        : new FieldPoint(x, Math.Abs(y)))));// Y ≥ 0

        // ---------------- Generators cho records cấu hình ----------------

        // EnemyConfig với Id placeholder ("?" - sẽ được gán "E{n}" sau).
        private static Gen<EnemyConfig> GenEnemyBody() =>
            GenNonEmptyName().SelectMany(name =>
            GenPositiveFloat().SelectMany(maxHp =>
            GenPositiveFloat().SelectMany(speed =>
            GenNonNegativeFloat().SelectMany(melee =>
            GenNonNegativeFloat().SelectMany(resist =>
            GenNonNegativeIntCapped().SelectMany(gold =>
            GenNonNegativeIntCapped().SelectMany(exp =>
            GenNonNegativeIntCapped().Select(score =>
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
            GenNonNegativeIntCapped().SelectMany(count =>
            GenPositiveFloat().Select(interval =>
                new SpawnEntry(id, count, interval))));

        private static Gen<IReadOnlyList<SpawnEntry>> GenSpawnEntries(IReadOnlyList<string> enemyIds) =>
            Gen.Choose(MinSpawnsPerWave, MaxSpawnsPerWave).SelectMany(n =>
                Gen.ArrayOf(n, GenSpawnEntry(enemyIds))
                   .Select(arr => (IReadOnlyList<SpawnEntry>)arr.ToList()));

        private static Gen<IReadOnlyList<FieldPoint>> GenSpawnGates() =>
            Gen.Choose(MinGatesPerWave, MaxGatesPerWave).SelectMany(n =>
                Gen.ArrayOf(n, GenSpawnGate())
                   .Select(arr => (IReadOnlyList<FieldPoint>)arr.ToList()));

        // WaveConfig với WaveNumber placeholder (0 - sẽ được gán index+1 sau).
        private static Gen<WaveConfig> GenWaveBody(IReadOnlyList<string> enemyIds) =>
            GenSpawnEntries(enemyIds).SelectMany(spawns =>
            GenSpawnGates().SelectMany(gates =>
            GenNonNegativeFloat().Select(prep =>
                new WaveConfig(
                    WaveNumber: 0,
                    Spawns: spawns,
                    SpawnGates: gates,
                    PreparationSeconds: prep))));

        private static Gen<ConfigBundle> GenBundle() =>
            Gen.Choose(MinEnemies, MaxEnemies).SelectMany(numEnemies =>
                Gen.ArrayOf(numEnemies, GenEnemyBody()).SelectMany(enemyBodies =>
                {
                    // Gán Id duy nhất "E1".."EN" để thỏa ràng buộc no-orphan.
                    var enemies = enemyBodies
                        .Select((e, i) => e with { Id = $"E{i + 1}" })
                        .ToList();
                    var ids = enemies.Select(e => e.Id).ToList();

                    return Gen.Choose(MinWaves, MaxWaves).SelectMany(numWaves =>
                        Gen.ArrayOf(numWaves, GenWaveBody(ids)).Select(waveBodies =>
                        {
                            // Gán WaveNumber 1..numWaves theo thứ tự xuất hiện.
                            var waves = waveBodies
                                .Select((w, i) => w with { WaveNumber = i + 1 })
                                .ToList();
                            return new ConfigBundle(
                                (IReadOnlyList<EnemyConfig>)enemies,
                                (IReadOnlyList<WaveConfig>)waves);
                        }));
                }));
    }

    /// <summary>
    /// Property 1: Round-trip cấu hình JSON. Hai chiều phải bảo toàn nội dung.
    /// </summary>
    public class Property01_ConfigRoundTrip
    {
        // Chuẩn hóa newline để so sánh chuỗi JSON không phụ thuộc OS / trailing space.
        private static string Normalize(string s) =>
            s is null ? null : s.Replace("\r\n", "\n").TrimEnd();

        // Feature: tower-defense-vn, Property 1: Round-trip cấu hình JSON
        // Validates: Requirements 10.2, 10.4, 10.5, 10.6
        //
        // Chiều "value-equality": với mọi ConfigBundle hợp lệ b,
        //     Load(WriteEnemies(b.Enemies), WriteWaves(b.Waves)) == b
        // theo equality của records (đã override theo nội dung danh sách).
        [Test]
        public void Load_after_Write_yields_equal_bundle()
        {
            PbtRunner.RunForAll(ConfigBundleArb.Bundle(), (ConfigBundle bundle) =>
            {
                var writer = new ConfigWriter();
                var loader = new ConfigLoader();

                string enemiesJson = writer.WriteEnemies(bundle.Enemies);
                string wavesJson = writer.WriteWaves(bundle.Waves);

                var result = loader.Load(wavesJson, enemiesJson);
                if (result.IsErr)
                {
                    // Nếu loader từ chối một bundle do generator sinh, đó là tín hiệu
                    // generator sinh sai (vi phạm pre-condition Property 1). Báo cụ thể.
                    TestContext.WriteLine(
                        $"Loader rejected generator output at {result.Error.FieldPath} " +
                        $"(line {result.Error.Line}, col {result.Error.Column}): {result.Error.Message}");
                    return false;
                }

                return result.Value.Equals(bundle);
            });
        }

        // Feature: tower-defense-vn, Property 1: Round-trip cấu hình JSON
        // Validates: Requirements 10.2, 10.4, 10.5, 10.6
        //
        // Chiều "string-stable": cho s_e = WriteEnemies(b.Enemies), s_w = WriteWaves(b.Waves),
        //     WriteEnemies(Load(s_w, s_e).Enemies) ≡ s_e
        //     WriteWaves  (Load(s_w, s_e).Waves)   ≡ s_w
        // sau khi chuẩn hóa newline (Requirement 10.4 quy định indent + LF cố định).
        [Test]
        public void Write_after_Load_yields_equal_string()
        {
            PbtRunner.RunForAll(ConfigBundleArb.Bundle(), (ConfigBundle bundle) =>
            {
                var writer = new ConfigWriter();
                var loader = new ConfigLoader();

                string enemiesJson1 = writer.WriteEnemies(bundle.Enemies);
                string wavesJson1 = writer.WriteWaves(bundle.Waves);

                var result = loader.Load(wavesJson1, enemiesJson1);
                if (result.IsErr)
                {
                    TestContext.WriteLine(
                        $"Loader rejected generator output at {result.Error.FieldPath} " +
                        $"(line {result.Error.Line}, col {result.Error.Column}): {result.Error.Message}");
                    return false;
                }

                string enemiesJson2 = writer.WriteEnemies(result.Value.Enemies);
                string wavesJson2 = writer.WriteWaves(result.Value.Waves);

                return Normalize(enemiesJson1) == Normalize(enemiesJson2)
                    && Normalize(wavesJson1) == Normalize(wavesJson2);
            });
        }
    }
}
