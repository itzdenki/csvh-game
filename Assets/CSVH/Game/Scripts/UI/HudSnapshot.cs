// Feature: tower-defense-vn, Task 12.1 - HudSnapshot DTO bound to HUDController
// Validates: Requirements 4.4, 4.5, 5.5, 6.8, 7.3, 7.6, 8.4, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 13.2

namespace CSVH.Game.UI
{
    /// <summary>
    /// Bản chụp bất biến trạng thái trận đấu để render HUD trong một frame.
    /// HUDController tiêu thụ qua callback <c>Action&lt;HudSnapshot&gt;</c>: không phụ
    /// thuộc vào MonoBehaviour lifecycle của các view khác nên có thể đẩy đồng bộ
    /// trong cùng frame mà người chơi gây sự kiện (Requirement 13.2 - phản hồi ≤ 100 ms).
    /// <para/>
    /// Là <c>readonly record struct</c> nên có value equality và allocation-free —
    /// phù hợp để dựng mới mỗi frame mà không tạo áp lực GC.
    /// </summary>
    /// <param name="WaveNumber">Số Đợt hiện tại (≥ 1) — dùng cho Format.Wave / Format.NextWave (Req 7.6, 9.1).</param>
    /// <param name="CountdownSeconds">Số giây nguyên còn lại trong Pha_Chuẩn_Bị (≥ 0) (Req 7.3).</param>
    /// <param name="WaveElapsedSeconds">Thời gian (giây) Đợt hiện tại đã chạy kể từ khi vào Active. ≥ 0; reset mỗi Đợt mới.</param>
    /// <param name="WaveTimeRemainingSeconds">Số giây còn lại của Đợt ở chế độ đếm ngược (≥ 0). 0 khi chế độ time-based tắt.</param>
    /// <param name="IsEarlyClearPending">Đợt đã sạch Quái sớm, đang chờ ân hạn trước khi skip sang Đợt kế.</param>
    /// <param name="EarlyClearCountdownSeconds">Số giây còn lại của khoảng ân hạn "dọn sạch sớm" (≥ 0).</param>
    /// <param name="Level">Cấp_Thành (≥ 1) (Req 4.4).</param>
    /// <param name="CurrentExp">EXP_Hiện_Tại (≥ 0) (Req 4.5).</param>
    /// <param name="RequiredExp">EXP_Cần_Cấp (&gt; 0 sau khi LevelingSystem khởi tạo) (Req 4.5).</param>
    /// <param name="Hp">Máu_Hiện_Tại trong [0, MaxHp] (Req 5.5).</param>
    /// <param name="MaxHp">Máu_Tối_Đa (&gt; 0) (Req 5.5).</param>
    /// <param name="SessionScore">Điểm_Phiên hiện tại (Req 8.4).</param>
    /// <param name="HighScore">Kỷ_Lục đã lưu (Req 8.4).</param>
    /// <param name="Gold">Vàng hiện tại (Req 6.8).</param>
    /// <param name="ArmorLvl">Cấp_Nâng_Cấp Giáp (Req 6.8).</param>
    /// <param name="AttackLvl">Cấp_Nâng_Cấp Công (Req 6.8).</param>
    /// <param name="TrongDong">Thông tin HUD của skill Trống Đồng Đông Sơn (Req 6.8).</param>
    /// <param name="MuiTen">Thông tin HUD của skill Mũi Tên An Dương Vương (Req 6.8).</param>
    /// <param name="LuoiGuom">Thông tin HUD của skill Lưỡi Gươm Lê Lợi (Req 6.8).</param>
    /// <param name="ShowNextWave">
    /// Khi <c>true</c>, HUD hiển thị "Đợt kế tiếp" + "Đếm ngược" tại TopCenter (Pha_Chuẩn_Bị, Req 7.3).
    /// Khi <c>false</c>, hai dòng đó được làm trống và chỉ giữ "Đợt {N}/∞" (Đợt đang diễn ra, Req 7.6).
    /// </param>
    public readonly record struct HudSnapshot(
        int WaveNumber, int CountdownSeconds,
        int Level, int CurrentExp, int RequiredExp,
        int Hp, int MaxHp,
        long SessionScore, long HighScore,
        int Gold, int ArmorLvl, int AttackLvl,
        SkillHudInfo TrongDong, SkillHudInfo MuiTen, SkillHudInfo LuoiGuom,
        bool ShowNextWave,
        float WaveElapsedSeconds = 0f, float WaveTimeRemainingSeconds = 0f,
        bool IsEarlyClearPending = false, float EarlyClearCountdownSeconds = 0f);

    /// <summary>
    /// Thông tin hiển thị HUD cho MỘT skill Special: cấp hiện tại + tiến trình hồi chiêu.
    /// Là <c>readonly record struct</c> nên allocation-free khi dựng snapshot mỗi frame.
    /// </summary>
    /// <param name="Level">Cấp hiện tại của skill (≥ 1).</param>
    /// <param name="CooldownRemaining">Thời_Gian_Hồi_Còn_Lại (giây), 0 khi sẵn sàng.</param>
    /// <param name="CooldownMax">Thời_Gian_Hồi tối đa hiện tại (giây), &gt; 0.</param>
    /// <param name="IsUnlocked"><c>true</c> khi skill đã được mua/mở khoá; <c>false</c> thì HUD hiển thị tối.</param>
    public readonly record struct SkillHudInfo(
        int Level, float CooldownRemaining, float CooldownMax, bool IsUnlocked = false);

    /// <summary>
    /// Dữ liệu cho MỘT tab skill trong bảng "Skill Đặc biệt" (dạng 3 tab). View chỉ đọc để
    /// dựng nội dung tab + nút hành động (Mua khi còn khóa / Nâng cấp khi đã mở khoá), không
    /// chứa logic. <c>readonly record struct</c> nên allocation-free.
    /// </summary>
    /// <param name="Kind">Skill mà tab đại diện.</param>
    /// <param name="Name">Tên hiển thị (ví dụ "Trống Đồng Đông Sơn").</param>
    /// <param name="IsUnlocked"><c>true</c> nếu đã mua/mở khoá → nút là "Nâng cấp".</param>
    /// <param name="Level">Cấp hiện tại (≥ 1).</param>
    /// <param name="EffectDesc">Mô tả hiệu ứng skill (1-2 câu).</param>
    /// <param name="UnlockCost">Giá Vàng để mở khoá lần đầu.</param>
    /// <param name="UpgradeCost">Giá Vàng để nâng cấp tiếp theo (khi đã mở khoá).</param>
    public readonly record struct SkillTabInfo(
        CSVH.Core.Progression.SpecialSkillKind Kind,
        string Name,
        bool IsUnlocked,
        int Level,
        string EffectDesc,
        int UnlockCost,
        int UpgradeCost);
}
