// Feature: tower-defense-vn, Property 4: Đường_Đi_Quái có hai đầu mút đúng
// Validates: Requirements 2.1

using FsCheck;
using NUnit.Framework;
using CSVH.Core.Common;
using CSVH.Core.Wave;

namespace CSVH.Tests.Edit.Properties
{
    public class Property04_PathEndpoints
    {
        // For any gate ∈ SpawnGates and a fixed TowerPosition, BuildPath(gate, tower)
        // must yield a polyline with at least two points where path[0] == gate and
        // path[^1] == tower. NormalFloat constrains generated coordinates to finite
        // values so we don't probe NaN/Infinity that are out of the requirement scope.
        [Test]
        public void PathStartsAtGateAndEndsAtTower()
        {
            PbtRunner.RunForAll<NormalFloat, NormalFloat, NormalFloat, NormalFloat>(
                (gxRaw, gyRaw, txRaw, tyRaw) =>
                {
                    float gx = (float)gxRaw.Get;
                    float gy = (float)gyRaw.Get;
                    float tx = (float)txRaw.Get;
                    float ty = (float)tyRaw.Get;

                    var gate = new FieldPoint(gx, gy);
                    var tower = new FieldPoint(tx, ty);
                    var path = EnemyPath.BuildPath(gate, tower);

                    return path.Count >= 2
                        && path[0] == gate
                        && path[path.Count - 1] == tower;
                });
        }
    }
}
