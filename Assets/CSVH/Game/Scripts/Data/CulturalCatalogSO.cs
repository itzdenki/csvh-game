// Feature: tower-defense-vn, Task 10.3 - ScriptableObject mirror của CulturalCatalog
// Validates: Requirements 11.1, 11.2, 11.4

using System.Collections.Generic;
using CSVH.Core.Culture;
using UnityEngine;

namespace CSVH.Game.Data
{
    /// <summary>
    /// ScriptableObject mirror cho <see cref="CulturalCatalog"/> trong Core, để designer chỉnh
    /// danh sách tên Loại_Quái, skill Special và Loại_Đạn lấy cảm hứng văn hóa Việt Nam
    /// (Requirement 11.1, 11.2, 11.4) trực tiếp trong Inspector mà không cần build lại code.
    /// </summary>
    /// <remarks>
    /// Hoa văn / palette trống đồng Đông Sơn (Requirement 11.4) được áp ở phần icon do
    /// HUD/UI Toolkit consume; SO này chỉ giữ định danh để Core tham chiếu.
    /// </remarks>
    [CreateAssetMenu(fileName = "CulturalCatalog", menuName = "CSVH/Cultural Catalog", order = 0)]
    public sealed class CulturalCatalogSO : ScriptableObject
    {
        [Tooltip("Danh sách tên Loại_Quái (Requirement 11.1: ≥ 5 mục).")]
        [SerializeField] private List<string> _enemyNames = new();

        [Tooltip("Danh sách tên skill Special (Requirement 11.2: ≥ 3 mục).")]
        [SerializeField] private List<string> _specialNames = new();

        [Tooltip("Danh sách tên Loại_Đạn để Core tham chiếu trong vòng đời Đạn.")]
        [SerializeField] private List<string> _projectileNames = new();

        /// <summary>Danh sách định danh Loại_Quái (read-only view cho consumer).</summary>
        public IReadOnlyList<string> EnemyNames => _enemyNames;

        /// <summary>Danh sách định danh skill Special (read-only view cho consumer).</summary>
        public IReadOnlyList<string> SpecialNames => _specialNames;

        /// <summary>Danh sách định danh Loại_Đạn (read-only view cho consumer).</summary>
        public IReadOnlyList<string> ProjectileNames => _projectileNames;

        /// <summary>
        /// Chuyển sang record bất biến trong Core để các thành phần thuần C# tham chiếu.
        /// Sao chép sang mảng để tách Lifetime ScriptableObject khỏi Core domain object.
        /// </summary>
        public CulturalCatalog ToCore() => new(
            _enemyNames.ToArray(),
            _specialNames.ToArray(),
            _projectileNames.ToArray());
    }
}
