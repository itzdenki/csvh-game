// Feature: tower-defense-vn, Property 22: Bộ_Văn_Hóa no-orphan / no-dangling
// Validates: Requirements 11.1, 11.2

using NUnit.Framework;
using CSVH.Core.Culture;

namespace CSVH.Tests.Edit.Properties
{
    public class Property22_CulturalCatalog
    {
        [Test]
        public void CreateRejectsLessThanFiveEnemies()
        {
            var r = CulturalCatalog.Create(
                new[] { "A", "B", "C" },
                new[] { "S1", "S2", "S3" },
                new[] { "P1" });
            Assert.IsTrue(r.IsErr);
        }

        [Test]
        public void CreateRejectsLessThanThreeSpecials()
        {
            var r = CulturalCatalog.Create(
                new[] { "A", "B", "C", "D", "E" },
                new[] { "S1" },
                new[] { "P1" });
            Assert.IsTrue(r.IsErr);
        }

        [Test]
        public void CreateRejectsDuplicates()
        {
            var r = CulturalCatalog.Create(
                new[] { "A", "A", "C", "D", "E" },
                new[] { "S1", "S2", "S3" },
                new[] { "P1" });
            Assert.IsTrue(r.IsErr);
        }

        [Test]
        public void CreateRejectsEmptyOrWhitespaceEntries()
        {
            var r = CulturalCatalog.Create(
                new[] { "A", " ", "C", "D", "E" },
                new[] { "S1", "S2", "S3" },
                new[] { "P1" });
            Assert.IsTrue(r.IsErr);
        }

        [Test]
        public void ContainsLookupsWork()
        {
            var c = new CulturalCatalog(
                new[] { "Hồ_Tinh", "Quân_Tống", "Quân_Nguyên_Mông", "Mộc_Tinh", "Thuồng_Luồng" },
                new[] { "Trống_Đồng_Đông_Sơn", "Lưỡi_Gươm_Lê_Lợi", "Mũi_Tên_An_Dương_Vương" },
                new[] { "Mũi_Tên" });
            Assert.IsTrue(c.ContainsEnemy("Hồ_Tinh"));
            Assert.IsFalse(c.ContainsEnemy("XYZ"));
            Assert.IsTrue(c.ContainsSpecial("Lưỡi_Gươm_Lê_Lợi"));
            Assert.IsFalse(c.ContainsSpecial("ABC"));
            Assert.GreaterOrEqual(c.EnemyCount, 5);
            Assert.GreaterOrEqual(c.SpecialCount, 3);
        }
    }
}
