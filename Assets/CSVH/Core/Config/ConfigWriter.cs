// Feature: tower-defense-vn
// Validates: Requirements 10.4, 10.5, 10.6 (pretty-print UTF-8 ổn định + round-trip).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Hiện thực mặc định của <see cref="IConfigWriter"/>.
    /// <para/>
    /// Ràng buộc đầu ra (Requirement 10.4):
    /// <list type="bullet">
    ///   <item>Indent 2 space, mỗi cấp lồng nhau.</item>
    ///   <item>Newline luôn là <c>"\n"</c> (LF) bất kể OS - đầu ra deterministic
    ///   trên Windows/macOS/Linux.</item>
    ///   <item>Khóa JSON theo camelCase và <em>thứ tự cố định</em> (xem
    ///   <see cref="IConfigWriter.WriteEnemies"/>, <see cref="IConfigWriter.WriteWaves"/>).</item>
    ///   <item>Số thực ghi với
    ///   <see cref="FloatFormatHandling.String"/> + <see cref="CultureInfo.InvariantCulture"/>
    ///   để không phụ thuộc locale (vd. dấu phẩy thập phân của <c>vi-VN</c>).</item>
    ///   <item>Top-level luôn là một JSON array, kết thúc bằng đúng một <c>"\n"</c>.</item>
    /// </list>
    /// <para/>
    /// Tính chất round-trip (Requirement 10.5, 10.6): với mọi chuỗi <c>s</c> được
    /// sinh bởi class này, <c>Write(Load(s))</c> bằng <c>s</c> sau chuẩn hóa khoảng
    /// trắng - vì cả thứ tự khóa, indent và newline đều cố định.
    /// </summary>
    public sealed class ConfigWriter : IConfigWriter
    {
        // Hằng số dùng chung cho toàn class (giữ private để không lộ chi tiết format).
        private const int IndentSpaces = 2;
        private const string Newline = "\n";

        /// <inheritdoc />
        public string WriteEnemies(IReadOnlyList<EnemyConfig> enemies)
        {
            if (enemies is null) throw new ArgumentNullException(nameof(enemies));

            return WriteArray(enemies, WriteEnemy);
        }

        /// <inheritdoc />
        public string WriteWaves(IReadOnlyList<WaveConfig> waves)
        {
            if (waves is null) throw new ArgumentNullException(nameof(waves));

            return WriteArray(waves, WriteWave);
        }

        // ==== Helpers - top-level array writer ===========================================

        /// <summary>
        /// Khung ghi top-level JSON array. Tạo <see cref="StringWriter"/> dùng
        /// invariant culture + LF newline; bọc bằng <see cref="JsonTextWriter"/>
        /// indent 2 space; gọi <paramref name="writeItem"/> cho mỗi phần tử rồi
        /// nối thêm trailing <c>"\n"</c>.
        /// </summary>
        private static string WriteArray<T>(
            IReadOnlyList<T> items,
            Action<JsonTextWriter, T> writeItem)
        {
            // StringWriter.NewLine kiểm soát ký tự xuống dòng do JsonTextWriter phát ra
            // khi Formatting.Indented - đặt LF để đầu ra ổn định trên mọi OS.
            var sw = new StringWriter(CultureInfo.InvariantCulture)
            {
                NewLine = Newline,
            };

            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.IndentChar = ' ';
                writer.Indentation = IndentSpaces;
                writer.QuoteName = true;
                writer.QuoteChar = '"';
                // Để số thực không bị làm tròn theo chiều ngược (luôn parse lại đúng).
                writer.FloatFormatHandling = FloatFormatHandling.String;
                writer.Culture = CultureInfo.InvariantCulture;

                writer.WriteStartArray();
                for (int i = 0; i < items.Count; i++)
                {
                    writeItem(writer, items[i]);
                }
                writer.WriteEndArray();
            }

            // JsonTextWriter không tự thêm trailing newline; chuẩn hóa kết thúc thành
            // đúng một LF để các công cụ POSIX (diff/cat) thân thiện và tính chất
            // round-trip giữ nguyên.
            return sw.ToString() + Newline;
        }

        // ==== EnemyConfig =================================================================

        /// <summary>
        /// Ghi một <see cref="EnemyConfig"/> theo thứ tự khóa cố định
        /// <c>id, localizedName, maxHp, speed, meleeDamage, resistance,
        /// goldReward, expReward, scoreReward</c>.
        /// </summary>
        private static void WriteEnemy(JsonTextWriter writer, EnemyConfig e)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteValue(e.Id);

            writer.WritePropertyName("localizedName");
            writer.WriteValue(e.LocalizedName);

            writer.WritePropertyName("maxHp");
            writer.WriteValue(e.MaxHp);

            writer.WritePropertyName("speed");
            writer.WriteValue(e.Speed);

            writer.WritePropertyName("meleeDamage");
            writer.WriteValue(e.MeleeDamage);

            writer.WritePropertyName("resistance");
            writer.WriteValue(e.Resistance);

            writer.WritePropertyName("goldReward");
            writer.WriteValue(e.GoldReward);

            writer.WritePropertyName("expReward");
            writer.WriteValue(e.ExpReward);

            writer.WritePropertyName("scoreReward");
            writer.WriteValue(e.ScoreReward);

            writer.WriteEndObject();
        }

        // ==== WaveConfig / SpawnEntry / FieldPoint =======================================

        /// <summary>
        /// Ghi một <see cref="WaveConfig"/> theo thứ tự khóa cố định
        /// <c>waveNumber, preparationSeconds, spawnGates, spawns</c>.
        /// </summary>
        private static void WriteWave(JsonTextWriter writer, WaveConfig w)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("waveNumber");
            writer.WriteValue(w.WaveNumber);

            writer.WritePropertyName("preparationSeconds");
            writer.WriteValue(w.PreparationSeconds);

            writer.WritePropertyName("spawnGates");
            writer.WriteStartArray();
            if (w.SpawnGates is { } gates)
            {
                for (int i = 0; i < gates.Count; i++)
                {
                    WriteFieldPoint(writer, gates[i]);
                }
            }
            writer.WriteEndArray();

            writer.WritePropertyName("spawns");
            writer.WriteStartArray();
            if (w.Spawns is { } spawns)
            {
                for (int i = 0; i < spawns.Count; i++)
                {
                    WriteSpawnEntry(writer, spawns[i]);
                }
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Ghi một <see cref="SpawnEntry"/> theo thứ tự khóa cố định
        /// <c>enemyId, count, spawnIntervalSeconds</c>.
        /// </summary>
        private static void WriteSpawnEntry(JsonTextWriter writer, SpawnEntry s)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("enemyId");
            writer.WriteValue(s.EnemyId);

            writer.WritePropertyName("count");
            writer.WriteValue(s.Count);

            writer.WritePropertyName("spawnIntervalSeconds");
            writer.WriteValue(s.SpawnIntervalSeconds);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Ghi một <see cref="Common.FieldPoint"/> theo thứ tự khóa cố định
        /// <c>x, y</c>.
        /// </summary>
        private static void WriteFieldPoint(JsonTextWriter writer, Common.FieldPoint p)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("x");
            writer.WriteValue(p.X);

            writer.WritePropertyName("y");
            writer.WriteValue(p.Y);

            writer.WriteEndObject();
        }
    }
}
