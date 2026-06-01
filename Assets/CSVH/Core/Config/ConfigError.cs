// Feature: tower-defense-vn
// Validates: Requirements 1.4, 2.6, 3.5, 4.6, 10.1, 10.2, 10.3 (chẩn đoán lỗi cấu hình kèm vị trí trong nguồn JSON).
// Tham chiếu design.md - section "Core - Config Loader / Writer" và "Error Handling".

namespace CSVH.Core.Config
{
    /// <summary>
    /// Mô tả một lỗi phát hiện bởi Bộ_Nạp_Cấu_Hình khi parse hoặc validate
    /// <c>waves.json</c> / <c>enemies.json</c>. Trường <see cref="FieldPath"/> chỉ rõ
    /// trường vi phạm theo cú pháp dot/bracket (ví dụ <c>enemies[2].maxHp</c> hoặc
    /// <c>waves[0].spawns[1].enemyId</c>); <see cref="Line"/>/<see cref="Column"/> trỏ
    /// về vị trí trong tệp nguồn để hỗ trợ định vị (Requirement 10.3).
    /// </summary>
    /// <param name="FieldPath">Đường dẫn trường JSON vi phạm. Trả về <c>"$"</c> khi lỗi nằm ở mức gốc.</param>
    /// <param name="Line">Số dòng (1-based) trong nguồn JSON; <c>0</c> nếu không có thông tin.</param>
    /// <param name="Column">Vị trí cột (1-based) trong nguồn JSON; <c>0</c> nếu không có thông tin.</param>
    /// <param name="Message">Mô tả lỗi cho người vận hành (đặt theo phong cách "field must be ...").</param>
    public sealed record ConfigError(
        string FieldPath,
        int Line,
        int Column,
        string Message);
}
