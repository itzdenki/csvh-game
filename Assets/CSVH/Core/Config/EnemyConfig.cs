// Feature: tower-defense-vn
// Validates: Requirements 10.2, 10.5, 10.6 (round-trip cấu hình JSON dựa trên value equality).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

namespace CSVH.Core.Config
{
    /// <summary>
    /// Cấu hình một Loại_Quái bất biến đọc từ <c>enemies.json</c>.
    /// Là <c>sealed record</c> với toàn bộ trường nguyên thủy nên value equality
    /// auto-generated đã đủ để hỗ trợ thuộc tính round-trip (Property 1).
    /// </summary>
    /// <param name="Id">Định danh ổn định của Loại_Quái (vd <c>"Hồ_Tinh"</c>) dùng làm khóa tham chiếu.</param>
    /// <param name="LocalizedName">Tên hiển thị (UTF-8) đã được bản địa hóa tiếng Việt (Requirement 11.3).</param>
    /// <param name="MaxHp">Máu_Quái khởi tạo. Ràng buộc <c>&gt; 0</c> kiểm tra tại ConfigLoader (Requirement 2.6).</param>
    /// <param name="Speed">Tốc_Độ di chuyển dọc Đường_Đi_Quái. Ràng buộc <c>&gt; 0</c> (Requirement 2.6).</param>
    /// <param name="MeleeDamage">Sát_Thương_Cận_Chiến gây lên Thành. Ràng buộc <c>≥ 0</c>.</param>
    /// <param name="Resistance">Kháng sát thương từ Đạn. Ràng buộc <c>≥ 0</c>.</param>
    /// <param name="GoldReward">Phần_Thưởng_Vàng khi tiêu diệt. Ràng buộc <c>≥ 0</c>.</param>
    /// <param name="ExpReward">Phần_Thưởng_EXP khi tiêu diệt. Ràng buộc <c>≥ 0</c>.</param>
    /// <param name="ScoreReward">Phần_Thưởng_Điểm khi tiêu diệt. Ràng buộc <c>≥ 0</c>.</param>
    public sealed record EnemyConfig(
        string Id,
        string LocalizedName,
        float MaxHp,
        float Speed,
        float MeleeDamage,
        float Resistance,
        int GoldReward,
        int ExpReward,
        int ScoreReward);
}
