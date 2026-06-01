// Feature: tower-defense-vn
// Validates: Requirements 10.4, 10.5, 10.6 (Bộ_Xuất_Cấu_Hình pretty-print + round-trip).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

using System.Collections.Generic;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Bộ_Xuất_Cấu_Hình: chuyển danh sách <see cref="EnemyConfig"/> hoặc
    /// <see cref="WaveConfig"/> hợp lệ thành chuỗi JSON pretty-print UTF-8 ổn định
    /// (Requirement 10.4) đáp ứng tính chất round-trip
    /// <c>Write(Load(s)) ≡ s</c> sau chuẩn hóa khoảng trắng (Requirement 10.5, 10.6).
    /// <para/>
    /// Hai phương thức ghi tách rời để khớp file vật lý <c>enemies.json</c> và
    /// <c>waves.json</c>; cả hai đều xuất top-level JSON array.
    /// </summary>
    public interface IConfigWriter
    {
        /// <summary>
        /// Ghi danh sách <see cref="EnemyConfig"/> thành chuỗi JSON pretty-print
        /// (top-level array). Khóa được sắp theo thứ tự cố định:
        /// <c>id, localizedName, maxHp, speed, meleeDamage, resistance,
        /// goldReward, expReward, scoreReward</c>.
        /// </summary>
        /// <param name="enemies">Danh sách Loại_Quái cần ghi. Không được <c>null</c>.</param>
        /// <returns>
        /// Chuỗi JSON UTF-8 ổn định: indent 2 space, newline <c>"\n"</c>, kết thúc
        /// bằng đúng một <c>"\n"</c>.
        /// </returns>
        string WriteEnemies(IReadOnlyList<EnemyConfig> enemies);

        /// <summary>
        /// Ghi danh sách <see cref="WaveConfig"/> thành chuỗi JSON pretty-print
        /// (top-level array). Khóa được sắp theo thứ tự cố định trên mỗi cấp:
        /// <list type="bullet">
        ///   <item><c>WaveConfig</c>: <c>waveNumber, preparationSeconds, spawnGates, spawns</c></item>
        ///   <item><c>SpawnEntry</c>: <c>enemyId, count, spawnIntervalSeconds</c></item>
        ///   <item><c>FieldPoint</c>: <c>x, y</c></item>
        /// </list>
        /// </summary>
        /// <param name="waves">Danh sách Đợt cần ghi. Không được <c>null</c>.</param>
        /// <returns>
        /// Chuỗi JSON UTF-8 ổn định: indent 2 space, newline <c>"\n"</c>, kết thúc
        /// bằng đúng một <c>"\n"</c>.
        /// </returns>
        string WriteWaves(IReadOnlyList<WaveConfig> waves);
    }
}
