// Feature: tower-defense-vn, Property 2: Bộ_Nạp_Cấu_Hình từ chối trường vi phạm ràng buộc
// Validates: Requirements 1.4, 2.6, 3.5, 4.6, 10.3
//
// Với mọi cấu hình có ít nhất một trường vi phạm ràng buộc lược đồ
// (Speed ≤ 0, MaxHp ≤ 0, MeleeDamage < 0, *Reward < 0, Count < 0,
// SpawnIntervalSeconds ≤ 0, hoặc SpawnGate vi phạm "X ≤ 0 ∨ Y ≥ 0"),
// ConfigLoader.Load(...) phải trả Result.Err với FieldPath chứa
// tên trường vi phạm để định vị nguồn lỗi (Requirement 10.3).
//
// Sinh ngẫu nhiên một trong các MutationKind, áp lên một baseline JSON
// hợp lệ rồi gọi Load. Tất cả mutation đều giữ phần còn lại của cấu hình
// hợp lệ để cô lập ràng buộc cụ thể bị vi phạm. Vì loader validate enemies
// trước rồi mới đến waves, các mutation cấp wave (count/interval/gate)
// luôn đi kèm enemies hợp lệ.

using System.Globalization;
using CSVH.Core.Config;
using FsCheck;
using NUnit.Framework;

namespace CSVH.Tests.Edit.Properties
{
    /// <summary>
    /// Tag enum liệt kê các loại đột biến áp lên baseline JSON hợp lệ
    /// để vi phạm đúng một ràng buộc lược đồ tại một thời điểm.
    /// </summary>
    public enum InvalidMutationKind
    {
        /// <summary>Đặt enemies[0].maxHp ≤ 0 (Requirement 2.6).</summary>
        NegativeMaxHp,

        /// <summary>Đặt enemies[0].speed = 0 (Requirement 2.6).</summary>
        ZeroSpeed,

        /// <summary>Đặt enemies[0].meleeDamage &lt; 0.</summary>
        NegativeMeleeDamage,

        /// <summary>Đặt enemies[0].goldReward &lt; 0.</summary>
        NegativeReward,

        /// <summary>Đặt waves[0].spawns[0].count &lt; 0.</summary>
        NegativeCount,

        /// <summary>Đặt waves[0].spawns[0].spawnIntervalSeconds = 0.</summary>
        ZeroSpawnInterval,

        /// <summary>Đặt waves[0].spawnGates[0] thỏa (X &gt; 0 ∧ Y &lt; 0) (Requirement 1.4).</summary>
        InvalidSpawnGate,
    }

    /// <summary>
    /// Wrapper đơn giản đóng gói một <see cref="InvalidMutationKind"/>.
    /// <para/>
    /// Dùng class thay vì record để tránh phụ thuộc vào polyfill
    /// <c>System.Runtime.CompilerServices.IsExternalInit</c> trong assembly test
    /// (Core assembly có polyfill internal nhưng nó không lan sang Tests).
    /// Có thể mở rộng sau này bằng các trường mang giá trị vi phạm cụ thể nếu cần.
    /// </summary>
    public sealed class InvalidConfigCase
    {
        public InvalidMutationKind Kind { get; }

        public InvalidConfigCase(InvalidMutationKind kind)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// Cung cấp <see cref="Arbitrary{T}"/> cho <see cref="InvalidConfigCase"/>:
    /// chọn đều một trong các <see cref="InvalidMutationKind"/>.
    /// </summary>
    public static class InvalidConfigCaseArb
    {
        public static Arbitrary<InvalidConfigCase> Cases() =>
            Arb.From(
                Gen.Elements(
                    InvalidMutationKind.NegativeMaxHp,
                    InvalidMutationKind.ZeroSpeed,
                    InvalidMutationKind.NegativeMeleeDamage,
                    InvalidMutationKind.NegativeReward,
                    InvalidMutationKind.NegativeCount,
                    InvalidMutationKind.ZeroSpawnInterval,
                    InvalidMutationKind.InvalidSpawnGate
                ).Select(k => new InvalidConfigCase(k)));
    }

    /// <summary>
    /// Property 2: ConfigLoader từ chối cấu hình có trường vi phạm ràng buộc
    /// và FieldPath xác định trường vi phạm.
    /// </summary>
    public class ConfigRejectInvalidProperty
    {
        // Baseline values (đều hợp lệ): mọi mutation chỉ thay đổi đúng một trường.
        private const float BaselineMaxHp = 30.0f;
        private const float BaselineSpeed = 1.4f;
        private const float BaselineMelee = 5.0f;
        private const int BaselineGold = 5;
        private const int BaselineCount = 6;
        private const float BaselineInterval = 1.5f;
        private const float BaselineGateX = -8.0f; // X ≤ 0 ⇒ hợp lệ
        private const float BaselineGateY = 4.5f;

        /// <summary>
        /// Build cặp (enemies.json, waves.json) sau khi áp đúng một mutation và
        /// đoạn FieldPath dự kiến phải xuất hiện trong <see cref="ConfigError.FieldPath"/>.
        /// </summary>
        private static (string enemiesJson, string wavesJson, string expectedFieldFragment)
            BuildMutated(InvalidMutationKind kind)
        {
            float maxHp = BaselineMaxHp;
            float speed = BaselineSpeed;
            float melee = BaselineMelee;
            int gold = BaselineGold;
            int count = BaselineCount;
            float interval = BaselineInterval;
            float gateX = BaselineGateX;
            float gateY = BaselineGateY;
            string fieldFragment;

            switch (kind)
            {
                case InvalidMutationKind.NegativeMaxHp:
                    maxHp = -1.0f; // Vi phạm > 0
                    fieldFragment = "maxHp";
                    break;
                case InvalidMutationKind.ZeroSpeed:
                    speed = 0.0f; // Vi phạm > 0
                    fieldFragment = "speed";
                    break;
                case InvalidMutationKind.NegativeMeleeDamage:
                    melee = -1.0f; // Vi phạm ≥ 0
                    fieldFragment = "meleeDamage";
                    break;
                case InvalidMutationKind.NegativeReward:
                    gold = -1; // Vi phạm ≥ 0
                    fieldFragment = "goldReward";
                    break;
                case InvalidMutationKind.NegativeCount:
                    count = -1; // Vi phạm ≥ 0
                    fieldFragment = "count";
                    break;
                case InvalidMutationKind.ZeroSpawnInterval:
                    interval = 0.0f; // Vi phạm > 0
                    fieldFragment = "spawnIntervalSeconds";
                    break;
                case InvalidMutationKind.InvalidSpawnGate:
                    gateX = 5.0f;  // X > 0
                    gateY = -3.0f; // Y < 0 ⇒ vi phạm "X ≤ 0 ∨ Y ≥ 0"
                    fieldFragment = "spawnGates";
                    break;
                default:
                    fieldFragment = "$";
                    break;
            }

            // Sử dụng InvariantCulture để tránh locale (vd dấu phẩy thập phân vi-VN).
            var inv = CultureInfo.InvariantCulture;
            string maxHpStr = maxHp.ToString(inv);
            string speedStr = speed.ToString(inv);
            string meleeStr = melee.ToString(inv);
            string intervalStr = interval.ToString(inv);
            string gateXStr = gateX.ToString(inv);
            string gateYStr = gateY.ToString(inv);

            string enemiesJson =
                "[\n" +
                "  {\n" +
                "    \"id\": \"E1\",\n" +
                "    \"localizedName\": \"HoTinh\",\n" +
                $"    \"maxHp\": {maxHpStr},\n" +
                $"    \"speed\": {speedStr},\n" +
                $"    \"meleeDamage\": {meleeStr},\n" +
                "    \"resistance\": 0.0,\n" +
                $"    \"goldReward\": {gold},\n" +
                "    \"expReward\": 8,\n" +
                "    \"scoreReward\": 10\n" +
                "  }\n" +
                "]\n";

            string wavesJson =
                "[\n" +
                "  {\n" +
                "    \"waveNumber\": 1,\n" +
                "    \"preparationSeconds\": 10.0,\n" +
                "    \"spawnGates\": [\n" +
                $"      {{ \"x\": {gateXStr}, \"y\": {gateYStr} }}\n" +
                "    ],\n" +
                "    \"spawns\": [\n" +
                "      {\n" +
                "        \"enemyId\": \"E1\",\n" +
                $"        \"count\": {count},\n" +
                $"        \"spawnIntervalSeconds\": {intervalStr}\n" +
                "      }\n" +
                "    ]\n" +
                "  }\n" +
                "]\n";

            return (enemiesJson, wavesJson, fieldFragment);
        }

        // Feature: tower-defense-vn, Property 2: ConfigLoader rejects invalid fields
        // Validates: Requirements 1.4, 2.6, 3.5, 4.6, 10.3
        [Test]
        public void Property2_RejectInvalidField()
        {
            PbtRunner.RunForAll(InvalidConfigCaseArb.Cases(), (InvalidConfigCase invalidCase) =>
            {
                var (enemiesJson, wavesJson, expectedField) = BuildMutated(invalidCase.Kind);
                var loader = new ConfigLoader();

                var result = loader.Load(wavesJson, enemiesJson);

                if (!result.IsErr)
                {
                    TestContext.WriteLine(
                        $"Expected ConfigLoader to reject mutation '{invalidCase.Kind}' but got Ok.");
                    return false;
                }

                string fieldPath = result.Error.FieldPath;
                if (string.IsNullOrEmpty(fieldPath) || fieldPath.IndexOf(expectedField, System.StringComparison.Ordinal) < 0)
                {
                    TestContext.WriteLine(
                        $"Mutation '{invalidCase.Kind}': expected FieldPath to contain '{expectedField}' " +
                        $"but got '{fieldPath}'. Message: {result.Error.Message}");
                    return false;
                }

                return true;
            });
        }
    }
}
