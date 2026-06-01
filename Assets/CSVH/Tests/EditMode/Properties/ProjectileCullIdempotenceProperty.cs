// Feature: tower-defense-vn, Property 8: Idempotence của hành động hủy Đạn ngoài biên
// Validates: Requirements 3.4

using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Combat;
using CSVH.Core.Common;

namespace CSVH.Tests.Edit.Properties
{
    /// <summary>
    /// Property 8 — Bất biến idempotence của <see cref="ProjectileWorld.Cull"/>:
    /// với mọi tập <see cref="ProjectileSnapshot"/> và mọi <see cref="FieldGeometry"/>
    /// hợp lệ, áp dụng <c>Cull</c> hai lần cho cùng kết quả của một lần.
    ///
    /// <para>
    /// Validates: Requirement 3.4. Test này khẳng định rằng các Đạn còn sống sau lần
    /// cull đầu tiên đều nằm trong biên và do đó lần cull thứ hai không loại bỏ thêm
    /// phần tử nào — bao gồm cả thứ tự (so sánh bằng <see cref="Enumerable.SequenceEqual{T}(System.Collections.Generic.IEnumerable{T}, System.Collections.Generic.IEnumerable{T})"/>).
    /// </para>
    /// </summary>
    public class ProjectileCullIdempotenceProperty
    {
        // Phần tham số đầu vào của Property 8: mảng ProjectileSnapshot + halfWidth/halfHeight.
        // Dùng class wrapper thay vì ValueTuple để tránh phụ thuộc reflection thêm.
        public sealed class Inputs
        {
            public ProjectileSnapshot[] Snapshots { get; }
            public float HalfWidth { get; }
            public float HalfHeight { get; }
            public Inputs(ProjectileSnapshot[] snapshots, float halfWidth, float halfHeight)
            {
                Snapshots = snapshots;
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
            }
        }

        // FsCheck 2.x không tự derive ProjectileSnapshot (record struct user-defined),
        // nên xây Arbitrary thủ công bằng cách sinh (int, float, float) rồi map.
        private static Arbitrary<Inputs> InputsArb()
        {
            var snapshotGen =
                Arb.Default.Int32().Generator.SelectMany(id =>
                Arb.Default.Float32().Generator.SelectMany(x =>
                Arb.Default.Float32().Generator.Select(y =>
                    new ProjectileSnapshot(id, new FieldPoint(x, y)))));

            var arrayGen = Gen.ArrayOf(snapshotGen);

            var combined =
                arrayGen.SelectMany(arr =>
                Arb.Default.Float32().Generator.SelectMany(hw =>
                Arb.Default.Float32().Generator.Select(hh =>
                    new Inputs(arr, hw, hh))));

            return Arb.From(combined);
        }

        // Property 8: Cull(Cull(world)) == Cull(world).
        // halfWidth/halfHeight được ép > 0 bằng Math.Abs(...)+1 để tránh geometry suy biến.
        [Test]
        public void Property8_CullIdempotent()
        {
            PbtRunner.RunForAll(InputsArb(), input =>
            {
                var snapshots = input.Snapshots ?? Array.Empty<ProjectileSnapshot>();
                float halfWidth = input.HalfWidth;
                float halfHeight = input.HalfHeight;

                if (float.IsNaN(halfWidth) || float.IsInfinity(halfWidth)) halfWidth = 0f;
                if (float.IsNaN(halfHeight) || float.IsInfinity(halfHeight)) halfHeight = 0f;

                float halfW = MathF.Abs(halfWidth) + 1f;
                float halfH = MathF.Abs(halfHeight) + 1f;
                var geom = new FieldGeometry(halfW, halfH, new FieldPoint(1f, -1f), 0.5f);

                var once = ProjectileWorld.Cull(snapshots, geom);
                var twice = ProjectileWorld.Cull(once, geom);

                return once.SequenceEqual(twice);
            });
        }
    }
}
