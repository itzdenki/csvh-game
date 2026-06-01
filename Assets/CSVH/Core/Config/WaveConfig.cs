// Feature: tower-defense-vn
// Validates: Requirements 10.2, 10.5, 10.6 (value equality round-trip)
//            Requirements 7.1, 7.2 (cấu trúc Đợt và Pha_Chuẩn_Bị)
//            Requirements 1.3, 2.1 (Cổng_Spawn nằm trên biên hợp lệ).
// Tham chiếu design.md - section "Core - Config Loader / Writer".

using System.Collections.Generic;
using CSVH.Core.Common;

namespace CSVH.Core.Config
{
    /// <summary>
    /// Cấu hình bất biến cho một Đợt: số thứ tự, danh sách <see cref="SpawnEntry"/>,
    /// các <see cref="FieldPoint"/> Cổng_Spawn và thời lượng <see cref="PreparationSeconds"/>
    /// của Pha_Chuẩn_Bị trước Đợt.
    /// <para/>
    /// Record auto-generated <c>Equals</c> so sánh <see cref="IReadOnlyList{T}"/> theo
    /// <em>tham chiếu</em>, không đủ cho thuộc tính round-trip (Requirement 10.5/10.6).
    /// Record này override <see cref="Equals(WaveConfig)"/> và <see cref="GetHashCode"/>
    /// để so sánh và băm theo <em>nội dung</em> danh sách.
    /// </summary>
    /// <param name="WaveNumber">Số thứ tự Đợt. Ràng buộc <c>≥ 1</c> (Requirement 7.1) kiểm tại ConfigLoader.</param>
    /// <param name="Spawns">Danh sách yêu cầu spawn của Đợt. Không được <c>null</c>.</param>
    /// <param name="SpawnGates">Tập Cổng_Spawn dùng cho Đợt. Mỗi điểm phải thỏa <c>X ≤ 0 ∨ Y ≥ 0</c> (Requirement 1.3, 2.1).</param>
    /// <param name="PreparationSeconds">Thời lượng Pha_Chuẩn_Bị (giây). Ràng buộc <c>≥ 0</c> (Requirement 7.2).</param>
    public sealed record WaveConfig(
        int WaveNumber,
        IReadOnlyList<SpawnEntry> Spawns,
        IReadOnlyList<FieldPoint> SpawnGates,
        float PreparationSeconds)
    {
        /// <summary>
        /// So sánh hai <see cref="WaveConfig"/> theo <em>nội dung</em> danh sách.
        /// Override này cần thiết vì record auto-generated chỉ so sánh tham chiếu cho
        /// <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        public bool Equals(WaveConfig other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;

            return WaveNumber == other.WaveNumber
                && PreparationSeconds.Equals(other.PreparationSeconds)
                && ConfigEqualityHelpers.ListEquals(Spawns, other.Spawns)
                && ConfigEqualityHelpers.ListEquals(SpawnGates, other.SpawnGates);
        }

        /// <summary>
        /// Băm theo nội dung để duy trì hợp đồng với <see cref="Equals(WaveConfig)"/>.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + WaveNumber.GetHashCode();
                hash = hash * 31 + PreparationSeconds.GetHashCode();
                hash = hash * 31 + ConfigEqualityHelpers.ListHashCode(Spawns);
                hash = hash * 31 + ConfigEqualityHelpers.ListHashCode(SpawnGates);
                return hash;
            }
        }
    }
}
