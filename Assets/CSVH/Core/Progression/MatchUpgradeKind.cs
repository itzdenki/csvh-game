// Feature: in-match-upgrades — 9 nâng cấp TRONG TRẬN (reset mỗi trận, mua bằng Vàng).

namespace CSVH.Core.Progression
{
    /// <summary>
    /// Chín nâng cấp trong trận của Nỏ Thần / Thành Lũy (GDD "Nâng cấp trong trận"):
    /// <list type="bullet">
    ///   <item><see cref="Damage"/> — Sát Thương: +% sát thương cơ bản mỗi mũi tên.</item>
    ///   <item><see cref="AttackSpeed"/> — Tốc Đánh: +% Fire Rate (mũi tên / giây).</item>
    ///   <item><see cref="Crit"/> — Chí Mạng: mỗi cấp tăng CẢ tỷ lệ kích hoạt LẪN sát thương chí mạng.</item>
    ///   <item><see cref="Multishot"/> — Làn Đạn: +1 mũi tên mỗi lần bắn cho mỗi cấp (có trần).</item>
    ///   <item><see cref="ProjectileSpeed"/> — Tốc Độ Bay: +% vận tốc mũi tên.</item>
    ///   <item><see cref="FortifiedBase"/> — Cường Hóa Thành: +% HP tối đa của Thành Lũy.</item>
    ///   <item><see cref="BaseRegen"/> — Hồi Phục Thành: hồi HP cố định mỗi giây.</item>
    ///   <item><see cref="IceArrow"/> — Nỏ Băng: mũi tên làm chậm Quái khi trúng.</item>
    ///   <item><see cref="PoisonArrow"/> — Nỏ Độc: mũi tên gây độc (% sát thương Nỏ / giây).</item>
    ///   <item><see cref="GoldRush"/> — Hoàng Kim (Kinh Tế): tỉ lệ nhận GẤP ĐÔI Vàng mỗi lần hạ gục.</item>
    /// </list>
    /// </summary>
    public enum MatchUpgradeKind
    {
        Damage = 0,
        AttackSpeed = 1,
        Crit = 2,
        Multishot = 3,
        ProjectileSpeed = 4,
        FortifiedBase = 5,
        BaseRegen = 6,
        IceArrow = 7,
        PoisonArrow = 8,
        GoldRush = 9,
    }
}
