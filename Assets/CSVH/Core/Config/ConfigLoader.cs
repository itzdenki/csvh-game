// Feature: tower-defense-vn
// Validates: Requirements 1.4, 2.6, 3.5, 4.6, 10.1, 10.2, 10.3
// Tham chiếu design.md - section "Core - Config Loader / Writer" và bảng
// "Lược đồ ràng buộc (validated tại load)".
//
// Phạm vi hiện thực ở task 2.2:
// - Validate toàn bộ ràng buộc liên quan đến `EnemyConfig`, `WaveConfig`,
//   `SpawnEntry` và `SpawnGate` (Requirements 1.4, 2.6, 7.x, 10.x).
// - Validate cross-reference EnemyId trong `SpawnEntry` phải tồn tại trong
//   `enemies.json` (Requirements 10.2, 10.3).
// - Các ràng buộc `BaseDamage>=0`, `RequiredExp>0`, `LevelScale>=1.0` thuộc
//   về cấu hình Đạn/Tower (chưa có record tương ứng trong ConfigBundle hiện tại
//   theo design "Lược đồ ràng buộc"). Chúng sẽ được bổ sung khi các record
//   `ProjectileConfig`/`TowerConfig` được hiện thực; tham chiếu Properties P2 ở
//   design vẫn yêu cầu loader rejects khi các trường đó vi phạm.

using System;
using System.Collections.Generic;
using CSVH.Core.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Hiện thực mặc định của <see cref="IConfigLoader"/> dùng Newtonsoft.Json
    /// (Linq-to-JSON) để vừa parse vừa giữ thông tin dòng/cột phục vụ chẩn đoán
    /// (Requirement 10.3). Không ném exception trên đường dẫn bình thường: lỗi cú
    /// pháp <see cref="JsonReaderException"/> được chuyển thành <see cref="ConfigError"/>.
    /// </summary>
    public sealed class ConfigLoader : IConfigLoader
    {
        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            LineInfoHandling = LineInfoHandling.Load,
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
        };

        /// <inheritdoc/>
        public Result<ConfigBundle, ConfigError> Load(string wavesJson, string enemiesJson)
        {
            // 1) Parse và validate enemies trước; cần bộ Id để cross-validate waves.
            var enemiesArrR = ParseRootArray(enemiesJson, rootName: "enemies");
            if (enemiesArrR.IsErr) return Result<ConfigBundle, ConfigError>.Err(enemiesArrR.Error);

            var enemiesR = ValidateEnemies(enemiesArrR.Value);
            if (enemiesR.IsErr) return Result<ConfigBundle, ConfigError>.Err(enemiesR.Error);

            var enemies = enemiesR.Value;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < enemies.Count; i++) ids.Add(enemies[i].Id);

            // 2) Parse và validate waves (kể cả cross-reference EnemyId).
            var wavesArrR = ParseRootArray(wavesJson, rootName: "waves");
            if (wavesArrR.IsErr) return Result<ConfigBundle, ConfigError>.Err(wavesArrR.Error);

            var wavesR = ValidateWaves(wavesArrR.Value, ids);
            if (wavesR.IsErr) return Result<ConfigBundle, ConfigError>.Err(wavesR.Error);

            return Result<ConfigBundle, ConfigError>.Ok(new ConfigBundle(enemies, wavesR.Value));
        }

        // ──────────────────────────── Parsing ────────────────────────────

        private static Result<JArray, ConfigError> ParseRootArray(string json, string rootName)
        {
            JToken token;
            try
            {
                token = JToken.Parse(json ?? string.Empty, LoadSettings);
            }
            catch (JsonReaderException ex)
            {
                return Result<JArray, ConfigError>.Err(new ConfigError(
                    FieldPath: "$",
                    Line: ex.LineNumber,
                    Column: ex.LinePosition,
                    Message: $"Invalid JSON syntax in {rootName}.json: {ex.Message}"));
            }

            if (token is JArray arr)
            {
                return Result<JArray, ConfigError>.Ok(arr);
            }

            var (line, col) = GetLineCol(token);
            return Result<JArray, ConfigError>.Err(new ConfigError(
                FieldPath: "$",
                Line: line,
                Column: col,
                Message: $"Root of {rootName}.json must be a JSON array"));
        }

        // ──────────────────────────── Enemies ────────────────────────────

        private static Result<IReadOnlyList<EnemyConfig>, ConfigError> ValidateEnemies(JArray arr)
        {
            var list = new List<EnemyConfig>(arr.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                var elemPath = $"enemies[{i}]";
                var element = arr[i];
                if (element is not JObject obj)
                {
                    var (l, c) = GetLineCol(element);
                    return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(new ConfigError(
                        elemPath, l, c, "Enemy entry must be a JSON object"));
                }

                var idR = ReadNonEmptyString(obj, "id", elemPath);
                if (idR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(idR.Error);

                var nameR = ReadNonEmptyString(obj, "localizedName", elemPath);
                if (nameR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(nameR.Error);

                var maxHpR = ReadFloatStrictlyPositive(obj, "maxHp", elemPath); // Requirement 2.6
                if (maxHpR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(maxHpR.Error);

                var speedR = ReadFloatStrictlyPositive(obj, "speed", elemPath); // Requirement 2.6
                if (speedR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(speedR.Error);

                var meleeR = ReadFloatNonNegative(obj, "meleeDamage", elemPath);
                if (meleeR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(meleeR.Error);

                var resR = ReadFloatNonNegative(obj, "resistance", elemPath);
                if (resR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(resR.Error);

                var goldR = ReadIntAtLeast(obj, "goldReward", elemPath, minInclusive: 0);
                if (goldR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(goldR.Error);

                var expR = ReadIntAtLeast(obj, "expReward", elemPath, minInclusive: 0);
                if (expR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(expR.Error);

                var scoreR = ReadIntAtLeast(obj, "scoreReward", elemPath, minInclusive: 0);
                if (scoreR.IsErr) return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Err(scoreR.Error);

                list.Add(new EnemyConfig(
                    Id: idR.Value,
                    LocalizedName: nameR.Value,
                    MaxHp: maxHpR.Value,
                    Speed: speedR.Value,
                    MeleeDamage: meleeR.Value,
                    Resistance: resR.Value,
                    GoldReward: goldR.Value,
                    ExpReward: expR.Value,
                    ScoreReward: scoreR.Value));
            }
            return Result<IReadOnlyList<EnemyConfig>, ConfigError>.Ok(list);
        }

        // ──────────────────────────── Waves ─────────────────────────────

        private static Result<IReadOnlyList<WaveConfig>, ConfigError> ValidateWaves(
            JArray arr, HashSet<string> knownEnemyIds)
        {
            var list = new List<WaveConfig>(arr.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                var wavePath = $"waves[{i}]";
                var element = arr[i];
                if (element is not JObject obj)
                {
                    var (l, c) = GetLineCol(element);
                    return Result<IReadOnlyList<WaveConfig>, ConfigError>.Err(new ConfigError(
                        wavePath, l, c, "Wave entry must be a JSON object"));
                }

                var waveNumR = ReadIntAtLeast(obj, "waveNumber", wavePath, minInclusive: 1); // Requirement 7.1
                if (waveNumR.IsErr) return Result<IReadOnlyList<WaveConfig>, ConfigError>.Err(waveNumR.Error);

                var prepR = ReadFloatNonNegative(obj, "preparationSeconds", wavePath); // Requirement 7.2
                if (prepR.IsErr) return Result<IReadOnlyList<WaveConfig>, ConfigError>.Err(prepR.Error);

                var spawnsR = ValidateSpawns(obj, $"{wavePath}.spawns", knownEnemyIds);
                if (spawnsR.IsErr) return Result<IReadOnlyList<WaveConfig>, ConfigError>.Err(spawnsR.Error);

                var gatesR = ValidateSpawnGates(obj, $"{wavePath}.spawnGates");
                if (gatesR.IsErr) return Result<IReadOnlyList<WaveConfig>, ConfigError>.Err(gatesR.Error);

                list.Add(new WaveConfig(
                    WaveNumber: waveNumR.Value,
                    Spawns: spawnsR.Value,
                    SpawnGates: gatesR.Value,
                    PreparationSeconds: prepR.Value));
            }
            return Result<IReadOnlyList<WaveConfig>, ConfigError>.Ok(list);
        }

        private static Result<IReadOnlyList<SpawnEntry>, ConfigError> ValidateSpawns(
            JObject waveObj, string spawnsPath, HashSet<string> knownEnemyIds)
        {
            var fieldR = RequireField(waveObj, "spawns", parentPath: TrimLastSegment(spawnsPath));
            if (fieldR.IsErr) return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(fieldR.Error);

            if (fieldR.Value is not JArray arr)
            {
                var (l, c) = GetLineCol(fieldR.Value);
                return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(new ConfigError(
                    spawnsPath, l, c, "Field 'spawns' must be a JSON array"));
            }

            var list = new List<SpawnEntry>(arr.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                var entryPath = $"{spawnsPath}[{i}]";
                if (arr[i] is not JObject obj)
                {
                    var (l, c) = GetLineCol(arr[i]);
                    return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(new ConfigError(
                        entryPath, l, c, "Spawn entry must be a JSON object"));
                }

                var enemyIdR = ReadNonEmptyString(obj, "enemyId", entryPath);
                if (enemyIdR.IsErr) return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(enemyIdR.Error);

                if (!knownEnemyIds.Contains(enemyIdR.Value))
                {
                    var token = obj["enemyId"];
                    var (l, c) = GetLineCol(token ?? (JToken)obj);
                    return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(new ConfigError(
                        $"{entryPath}.enemyId", l, c,
                        $"Unknown enemy id '{enemyIdR.Value}' (not present in enemies.json)"));
                }

                var countR = ReadIntAtLeast(obj, "count", entryPath, minInclusive: 0);
                if (countR.IsErr) return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(countR.Error);

                var intervalR = ReadFloatStrictlyPositive(obj, "spawnIntervalSeconds", entryPath);
                if (intervalR.IsErr) return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Err(intervalR.Error);

                list.Add(new SpawnEntry(enemyIdR.Value, countR.Value, intervalR.Value));
            }
            return Result<IReadOnlyList<SpawnEntry>, ConfigError>.Ok(list);
        }

        private static Result<IReadOnlyList<FieldPoint>, ConfigError> ValidateSpawnGates(
            JObject waveObj, string gatesPath)
        {
            var fieldR = RequireField(waveObj, "spawnGates", parentPath: TrimLastSegment(gatesPath));
            if (fieldR.IsErr) return Result<IReadOnlyList<FieldPoint>, ConfigError>.Err(fieldR.Error);

            if (fieldR.Value is not JArray arr)
            {
                var (l, c) = GetLineCol(fieldR.Value);
                return Result<IReadOnlyList<FieldPoint>, ConfigError>.Err(new ConfigError(
                    gatesPath, l, c, "Field 'spawnGates' must be a JSON array"));
            }

            var list = new List<FieldPoint>(arr.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                var gatePath = $"{gatesPath}[{i}]";
                if (arr[i] is not JObject obj)
                {
                    var (l, c) = GetLineCol(arr[i]);
                    return Result<IReadOnlyList<FieldPoint>, ConfigError>.Err(new ConfigError(
                        gatePath, l, c, "Spawn gate must be a JSON object with 'x' and 'y'"));
                }

                var xR = ReadFloat(obj, "x", gatePath);
                if (xR.IsErr) return Result<IReadOnlyList<FieldPoint>, ConfigError>.Err(xR.Error);

                var yR = ReadFloat(obj, "y", gatePath);
                if (yR.IsErr) return Result<IReadOnlyList<FieldPoint>, ConfigError>.Err(yR.Error);

                var point = new FieldPoint(xR.Value, yR.Value);
                if (!point.IsValidSpawnPoint()) // Requirements 1.3, 1.4, 2.1: X ≤ 0 ∨ Y ≥ 0
                {
                    var (l, c) = GetLineCol(obj);
                    return Result<IReadOnlyList<FieldPoint>, ConfigError>.Err(new ConfigError(
                        gatePath, l, c,
                        $"SpawnGate ({xR.Value}, {yR.Value}) violates 'X ≤ 0 ∨ Y ≥ 0'"));
                }
                list.Add(point);
            }
            return Result<IReadOnlyList<FieldPoint>, ConfigError>.Ok(list);
        }

        // ──────────────────────────── Field readers ──────────────────────

        private static Result<JToken, ConfigError> RequireField(JObject obj, string key, string parentPath)
        {
            if (!obj.TryGetValue(key, out var token) || token is null || token.Type == JTokenType.Null)
            {
                var (l, c) = GetLineCol(obj);
                return Result<JToken, ConfigError>.Err(new ConfigError(
                    FieldPath: $"{parentPath}.{key}",
                    Line: l,
                    Column: c,
                    Message: $"Missing required field '{key}'"));
            }
            return Result<JToken, ConfigError>.Ok(token);
        }

        private static Result<string, ConfigError> ReadNonEmptyString(JObject obj, string key, string parentPath)
        {
            var fieldR = RequireField(obj, key, parentPath);
            if (fieldR.IsErr) return Result<string, ConfigError>.Err(fieldR.Error);

            var token = fieldR.Value;
            if (token.Type != JTokenType.String)
            {
                var (l, c) = GetLineCol(token);
                return Result<string, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be a string"));
            }

            var s = token.Value<string>();
            if (string.IsNullOrEmpty(s))
            {
                var (l, c) = GetLineCol(token);
                return Result<string, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be non-empty"));
            }
            return Result<string, ConfigError>.Ok(s);
        }

        private static Result<float, ConfigError> ReadFloat(JObject obj, string key, string parentPath)
        {
            var fieldR = RequireField(obj, key, parentPath);
            if (fieldR.IsErr) return Result<float, ConfigError>.Err(fieldR.Error);

            var token = fieldR.Value;
            if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
            {
                var (l, c) = GetLineCol(token);
                return Result<float, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be a number"));
            }
            return Result<float, ConfigError>.Ok(token.Value<float>());
        }

        private static Result<float, ConfigError> ReadFloatStrictlyPositive(JObject obj, string key, string parentPath)
        {
            var r = ReadFloat(obj, key, parentPath);
            if (r.IsErr) return r;
            if (!(r.Value > 0f))
            {
                var token = obj[key];
                var (l, c) = GetLineCol(token ?? (JToken)obj);
                return Result<float, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be > 0 (got {r.Value})"));
            }
            return r;
        }

        private static Result<float, ConfigError> ReadFloatNonNegative(JObject obj, string key, string parentPath)
        {
            var r = ReadFloat(obj, key, parentPath);
            if (r.IsErr) return r;
            if (!(r.Value >= 0f))
            {
                var token = obj[key];
                var (l, c) = GetLineCol(token ?? (JToken)obj);
                return Result<float, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be ≥ 0 (got {r.Value})"));
            }
            return r;
        }

        private static Result<int, ConfigError> ReadIntAtLeast(JObject obj, string key, string parentPath, int minInclusive)
        {
            var fieldR = RequireField(obj, key, parentPath);
            if (fieldR.IsErr) return Result<int, ConfigError>.Err(fieldR.Error);

            var token = fieldR.Value;
            if (token.Type != JTokenType.Integer)
            {
                var (l, c) = GetLineCol(token);
                return Result<int, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be an integer"));
            }

            int value;
            try
            {
                value = token.Value<int>();
            }
            catch (OverflowException)
            {
                var (l, c) = GetLineCol(token);
                return Result<int, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' is out of Int32 range"));
            }

            if (value < minInclusive)
            {
                var (l, c) = GetLineCol(token);
                return Result<int, ConfigError>.Err(new ConfigError(
                    $"{parentPath}.{key}", l, c, $"Field '{key}' must be ≥ {minInclusive} (got {value})"));
            }
            return Result<int, ConfigError>.Ok(value);
        }

        // ──────────────────────────── Misc helpers ───────────────────────

        private static (int Line, int Column) GetLineCol(JToken token)
        {
            if (token is IJsonLineInfo info && info.HasLineInfo())
            {
                return (info.LineNumber, info.LinePosition);
            }
            return (0, 0);
        }

        // Trim ".spawns" → parent's path. Used to attribute "missing spawns" errors
        // to the wave object rather than the (non-existent) child path.
        private static string TrimLastSegment(string path)
        {
            int dot = path.LastIndexOf('.');
            return dot < 0 ? path : path.Substring(0, dot);
        }
    }
}
