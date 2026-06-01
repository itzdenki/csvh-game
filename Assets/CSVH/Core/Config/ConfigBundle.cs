// Feature: tower-defense-vn
// Validates: Requirements 10.2, 10.5, 10.6 (round-trip cấu hình JSON theo value equality).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

using System.Collections.Generic;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Gói cấu hình tổng hợp được <c>ConfigLoader</c> trả về sau khi nạp
    /// <c>enemies.json</c> và <c>waves.json</c>: bộ <see cref="EnemyConfig"/> và
    /// danh sách <see cref="WaveConfig"/>.
    /// <para/>
    /// Record auto-generated <c>Equals</c> so sánh <see cref="IReadOnlyList{T}"/>
    /// theo tham chiếu nên không đủ cho Property 1 (round-trip). Record này override
    /// <see cref="Equals(ConfigBundle)"/> và <see cref="GetHashCode"/> để so sánh theo
    /// nội dung danh sách - dựa vào value equality của <see cref="EnemyConfig"/> và
    /// <see cref="WaveConfig"/> (sau khi WaveConfig đã override theo nội dung).
    /// </summary>
    /// <param name="Enemies">Bộ Loại_Quái khả dụng cho trận. Không được <c>null</c>.</param>
    /// <param name="Waves">Danh sách Đợt theo thứ tự xuất hiện. Không được <c>null</c>.</param>
    public sealed record ConfigBundle(
        IReadOnlyList<EnemyConfig> Enemies,
        IReadOnlyList<WaveConfig> Waves)
    {
        /// <summary>
        /// So sánh hai <see cref="ConfigBundle"/> theo <em>nội dung</em> danh sách.
        /// </summary>
        public bool Equals(ConfigBundle other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;

            return ConfigEqualityHelpers.ListEquals(Enemies, other.Enemies)
                && ConfigEqualityHelpers.ListEquals(Waves, other.Waves);
        }

        /// <summary>
        /// Băm theo nội dung để duy trì hợp đồng với <see cref="Equals(ConfigBundle)"/>.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ConfigEqualityHelpers.ListHashCode(Enemies);
                hash = hash * 31 + ConfigEqualityHelpers.ListHashCode(Waves);
                return hash;
            }
        }
    }
}
