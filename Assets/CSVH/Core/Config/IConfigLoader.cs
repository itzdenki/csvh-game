// Feature: tower-defense-vn
// Validates: Requirements 1.4, 2.6, 3.5, 4.6, 10.1, 10.2, 10.3 (giao diện Bộ_Nạp_Cấu_Hình).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

using CSVH.Core.Common;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Bộ_Nạp_Cấu_Hình: nạp cặp tệp <c>waves.json</c> và <c>enemies.json</c> thành
    /// <see cref="ConfigBundle"/> bất biến hoặc trả lỗi định vị được.
    /// <para/>
    /// API trả <see cref="Result{T,E}"/> để giữ Core không ném exception trên đường dẫn
    /// bình thường (Requirement 10.3).
    /// </summary>
    public interface IConfigLoader
    {
        /// <summary>
        /// Parse và validate cả hai tệp cấu hình. Cross-reference <see cref="SpawnEntry.EnemyId"/>
        /// phải khớp với một <see cref="EnemyConfig.Id"/> đã nạp.
        /// </summary>
        /// <param name="wavesJson">Nội dung tệp <c>waves.json</c> dạng UTF-8.</param>
        /// <param name="enemiesJson">Nội dung tệp <c>enemies.json</c> dạng UTF-8.</param>
        /// <returns>
        /// <see cref="Result{T,E}.Ok(T)"/> chứa <see cref="ConfigBundle"/> nếu cả hai tệp hợp lệ;
        /// <see cref="Result{T,E}.Err(E)"/> chứa <see cref="ConfigError"/> với
        /// <c>FieldPath/Line/Column</c> nếu phát hiện vi phạm cú pháp hoặc lược đồ.
        /// </returns>
        Result<ConfigBundle, ConfigError> Load(string wavesJson, string enemiesJson);
    }
}
