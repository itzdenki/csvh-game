// Feature: tower-defense-vn, Property 3: Bất biến vị trí Cổng_Spawn và Vị_Trí_Thành
// Validates: Requirements 1.1, 1.3, 2.1

using FsCheck;
using NUnit.Framework;
using CSVH.Core.Common;

namespace CSVH.Tests.Edit.Properties
{
    public class Property03_SpawnGateInvariant
    {
        [Test]
        public void ValidSpawnPointPredicate()
        {
            PbtRunner.RunForAll<float, float>((x, y) =>
            {
                var p = new FieldPoint(x, y);
                return p.IsValidSpawnPoint() == (x <= 0f || y >= 0f);
            });
        }

        [Test]
        public void ValidTowerPointPredicate()
        {
            PbtRunner.RunForAll<float, float>((x, y) =>
            {
                var p = new FieldPoint(x, y);
                return p.IsValidTowerPoint() == (x > 0f && y < 0f);
            });
        }
    }
}
