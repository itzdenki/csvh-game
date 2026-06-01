// Feature: tower-defense-vn
// Validates: Requirements 11.1 (≥ 5 tên Loại_Quái lấy cảm hứng văn hóa Việt Nam),
//            11.2 (≥ 3 tên skill Special lấy cảm hứng văn hóa Việt Nam).
// Property 22: Bộ_Văn_Hóa no-orphan / no-dangling.

using System;
using System.Collections.Generic;
using CSVH.Core.Common;

namespace CSVH.Core.Culture
{
    /// <summary>
    /// Bộ_Văn_Hóa: catalog định danh Loại_Quái, skill Special và Loại_Đạn lấy cảm hứng từ
    /// truyền thuyết và lịch sử Việt Nam (Requirement 11.1, 11.2). Là <c>sealed record</c>
    /// có value equality theo nội dung danh sách — override <see cref="Equals(CulturalCatalog)"/>
    /// và <see cref="GetHashCode"/> để so sánh phần tử (Ordinal) thay vì so sánh tham chiếu mặc
    /// định của <see cref="IReadOnlyList{T}"/> trong cơ chế record sinh tự động.
    /// </summary>
    /// <remarks>
    /// Constructor cố ý không ném ngoại lệ khi danh sách thiếu mục: dữ liệu catalog có thể được
    /// nạp tăng dần (ví dụ ScriptableObject mirror trong Unity layer hoặc nạp từ asset bundle).
    /// Code-level enforcement Requirement 11.1 và 11.2 được phơi bày qua thuộc tính
    /// <see cref="IsValid"/> (kiểm tra nhanh) và phương thức <see cref="Validate"/> (trả thông
    /// tin chẩn đoán chi tiết). Tham chiếu thiết kế: section "Core - Cultural Catalog" trong
    /// design.md.
    /// </remarks>
    /// <param name="EnemyNames">Danh sách định danh Loại_Quái (kỳ vọng ≥ 5 mục, Requirement 11.1).</param>
    /// <param name="SpecialNames">Danh sách định danh skill Special (kỳ vọng ≥ 3 mục, Requirement 11.2).</param>
    /// <param name="ProjectileNames">Danh sách định danh Loại_Đạn; không có ràng buộc số lượng tối thiểu.</param>
    public sealed record CulturalCatalog(
        IReadOnlyList<string> EnemyNames,
        IReadOnlyList<string> SpecialNames,
        IReadOnlyList<string> ProjectileNames)
    {
        /// <summary>Số tên Loại_Quái tối thiểu để catalog hợp lệ (Requirement 11.1).</summary>
        public const int MinEnemyCount = 5;

        /// <summary>Số tên skill Special tối thiểu để catalog hợp lệ (Requirement 11.2).</summary>
        public const int MinSpecialCount = 3;

        /// <summary>Số tên Loại_Đạn tối thiểu để catalog có nghĩa (Property 22 no-orphan/no-dangling).</summary>
        public const int MinProjectileCount = 1;

        /// <summary>Số phần tử trong <see cref="EnemyNames"/>; <c>0</c> nếu danh sách <c>null</c>.</summary>
        public int EnemyCount => EnemyNames?.Count ?? 0;

        /// <summary>Số phần tử trong <see cref="SpecialNames"/>; <c>0</c> nếu danh sách <c>null</c>.</summary>
        public int SpecialCount => SpecialNames?.Count ?? 0;

        /// <summary>Số phần tử trong <see cref="ProjectileNames"/>; <c>0</c> nếu danh sách <c>null</c>.</summary>
        public int ProjectileCount => ProjectileNames?.Count ?? 0;

        /// <summary>
        /// Cho biết catalog đã đủ tối thiểu các mục để vận hành gameplay theo
        /// Requirement 11.1 (≥ <see cref="MinEnemyCount"/> EnemyNames) và Requirement 11.2
        /// (≥ <see cref="MinSpecialCount"/> SpecialNames).
        /// </summary>
        public bool IsValid =>
            EnemyCount >= MinEnemyCount && SpecialCount >= MinSpecialCount;

        /// <summary>
        /// Tra cứu một định danh Loại_Quái trong catalog (so khớp Ordinal).
        /// </summary>
        /// <param name="id">Định danh cần tra cứu; <c>null</c> trả <c>false</c>.</param>
        /// <returns><c>true</c> khi <paramref name="id"/> tồn tại trong <see cref="EnemyNames"/>.</returns>
        public bool ContainsEnemy(string id) => ContainsOrdinal(EnemyNames, id);

        /// <summary>
        /// Tra cứu một định danh skill Special trong catalog (so khớp Ordinal).
        /// </summary>
        /// <param name="id">Định danh cần tra cứu; <c>null</c> trả <c>false</c>.</param>
        /// <returns><c>true</c> khi <paramref name="id"/> tồn tại trong <see cref="SpecialNames"/>.</returns>
        public bool ContainsSpecial(string id) => ContainsOrdinal(SpecialNames, id);

        /// <summary>
        /// Tra cứu một định danh Loại_Đạn trong catalog (so khớp Ordinal).
        /// </summary>
        /// <param name="id">Định danh cần tra cứu; <c>null</c> trả <c>false</c>.</param>
        /// <returns><c>true</c> khi <paramref name="id"/> tồn tại trong <see cref="ProjectileNames"/>.</returns>
        public bool ContainsProjectile(string id) => ContainsOrdinal(ProjectileNames, id);

        /// <summary>
        /// Kiểm tra ràng buộc đếm tối thiểu của catalog. Không ném ngoại lệ —
        /// tách code-level enforcement Requirement 11.1 và 11.2 khỏi quá trình tải dữ liệu.
        /// </summary>
        /// <returns>
        /// <c>null</c> khi catalog hợp lệ; ngược lại trả <see cref="CatalogValidationError"/>
        /// mô tả trường vi phạm và số đếm thực tế / tối thiểu.
        /// </returns>
        public CatalogValidationError Validate()
        {
            int enemyCount = EnemyCount;
            if (enemyCount < MinEnemyCount)
            {
                return new CatalogValidationError(
                    nameof(EnemyNames),
                    enemyCount,
                    MinEnemyCount,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "CulturalCatalog cần tối thiểu {0} EnemyNames (Requirement 11.1); hiện có {1}.",
                        MinEnemyCount,
                        enemyCount));
            }

            int specialCount = SpecialCount;
            if (specialCount < MinSpecialCount)
            {
                return new CatalogValidationError(
                    nameof(SpecialNames),
                    specialCount,
                    MinSpecialCount,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "CulturalCatalog cần tối thiểu {0} SpecialNames (Requirement 11.2); hiện có {1}.",
                        MinSpecialCount,
                        specialCount));
            }

            return null;
        }

        /// <summary>
        /// Override Equals để so sánh nội dung danh sách (Ordinal) thay vì tham chiếu — bản
        /// Equals tự sinh của record dùng <see cref="EqualityComparer{T}.Default"/> trên
        /// <see cref="IReadOnlyList{T}"/> và rơi về so sánh tham chiếu, gây sai lệch khi hai
        /// catalog có cùng nội dung nhưng khác instance.
        /// </summary>
        public bool Equals(CulturalCatalog other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ListEqualsOrdinal(EnemyNames, other.EnemyNames)
                && ListEqualsOrdinal(SpecialNames, other.SpecialNames)
                && ListEqualsOrdinal(ProjectileNames, other.ProjectileNames);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            AddListToHash(ref hash, EnemyNames);
            AddListToHash(ref hash, SpecialNames);
            AddListToHash(ref hash, ProjectileNames);
            return hash.ToHashCode();
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> source, string id)
        {
            if (source is null || id is null) return false;
            for (int i = 0; i < source.Count; i++)
            {
                if (string.Equals(source[i], id, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool ListEqualsOrdinal(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static void AddListToHash(ref HashCode hash, IReadOnlyList<string> source)
        {
            if (source is null)
            {
                hash.Add(0);
                return;
            }
            hash.Add(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                hash.Add(source[i] ?? string.Empty, StringComparer.Ordinal);
            }
        }

        /// <summary>
        /// Factory tạo <see cref="CulturalCatalog"/> với kiểm tra nghiêm ngặt: từ chối <c>null</c>,
        /// chuỗi rỗng/whitespace, trùng lặp (Ordinal) trong cùng một danh sách, và đảm bảo số đếm
        /// tối thiểu theo Requirement 11.1 (≥ <see cref="MinEnemyCount"/> EnemyNames),
        /// Requirement 11.2 (≥ <see cref="MinSpecialCount"/> SpecialNames) và
        /// ≥ <see cref="MinProjectileCount"/> ProjectileNames.
        /// </summary>
        /// <param name="enemies">Danh sách định danh Loại_Quái.</param>
        /// <param name="specials">Danh sách định danh skill Special.</param>
        /// <param name="projectiles">Danh sách định danh Loại_Đạn.</param>
        /// <returns>
        /// <see cref="Result{T,E}.Ok"/> chứa catalog hợp lệ; ngược lại
        /// <see cref="Result{T,E}.Err"/> với thông điệp mô tả lỗi đầu tiên gặp phải.
        /// </returns>
        public static Result<CulturalCatalog, string> Create(
            IReadOnlyList<string> enemies,
            IReadOnlyList<string> specials,
            IReadOnlyList<string> projectiles)
        {
            string error = ValidateList(enemies, nameof(enemies), MinEnemyCount, "Requirement 11.1")
                ?? ValidateList(specials, nameof(specials), MinSpecialCount, "Requirement 11.2")
                ?? ValidateList(projectiles, nameof(projectiles), MinProjectileCount, "Property 22");
            if (error is not null)
            {
                return Result<CulturalCatalog, string>.Err(error);
            }

            return Result<CulturalCatalog, string>.Ok(
                new CulturalCatalog(enemies, specials, projectiles));
        }

        private static string ValidateList(
            IReadOnlyList<string> source,
            string name,
            int minCount,
            string requirementTag)
        {
            if (source is null)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "CulturalCatalog.{0} không được null ({1}).",
                    name,
                    requirementTag);
            }

            if (source.Count < minCount)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "CulturalCatalog.{0} cần tối thiểu {1} mục ({2}); hiện có {3}.",
                    name,
                    minCount,
                    requirementTag,
                    source.Count);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                string item = source[i];
                if (string.IsNullOrWhiteSpace(item))
                {
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "CulturalCatalog.{0}[{1}] không được rỗng/whitespace ({2}).",
                        name,
                        i,
                        requirementTag);
                }
                if (!seen.Add(item))
                {
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "CulturalCatalog.{0} chứa định danh trùng lặp '{1}' tại chỉ số {2} ({3}).",
                        name,
                        item,
                        i,
                        requirementTag);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Mô tả vi phạm ràng buộc đếm tối thiểu của <see cref="CulturalCatalog"/>
    /// (Requirement 11.1, 11.2). Bất biến và có value equality.
    /// </summary>
    /// <param name="FieldName">Tên trường vi phạm (<c>EnemyNames</c> hoặc <c>SpecialNames</c>).</param>
    /// <param name="ActualCount">Số phần tử thực tế trong trường vi phạm.</param>
    /// <param name="MinimumCount">Số phần tử tối thiểu yêu cầu cho trường này.</param>
    /// <param name="Message">Thông điệp mô tả lỗi (tiếng Việt) cho người gọi/log/UI.</param>
    public sealed record CatalogValidationError(
        string FieldName,
        int ActualCount,
        int MinimumCount,
        string Message);
}
