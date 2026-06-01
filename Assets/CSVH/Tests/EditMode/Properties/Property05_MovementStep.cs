// Feature: tower-defense-vn, Property 5: Bước di chuyển tỉ lệ với Tốc_Độ và thời gian
// Validates: Requirements 2.2

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Common;
using CSVH.Core.Wave;

namespace CSVH.Tests.Edit.Properties
{
    public class Property05_MovementStep
    {
        [Test]
        public void MovementStepEqualsSpeedTimesDt()
        {
            PbtRunner.RunForAll<NormalFloat, NormalFloat>((speedRaw, dtRaw) =>
            {
                // Map raw normals into bounded ranges so we exercise the segment
                // without falling off the end: speed ∈ [0.01, 50.01), dt ∈ [0.001, 1.001).
                float speed = MathF.Abs((float)speedRaw.Get) % 50f + 0.01f;
                float dt = MathF.Abs((float)dtRaw.Get) % 1f + 0.001f;

                // Skip pathological combos that would exit the first (and only) segment
                // of length 200; remaining input space still covers the property.
                if (speed * dt > 100f)
                {
                    return true;
                }

                var gate = new FieldPoint(-100f, 0f);
                var tower = new FieldPoint(100f, 0f);
                var path = EnemyPath.BuildPath(gate, tower);

                var start = EnemyPath.StartProgress(path);
                var advanced = EnemyPath.AdvanceAlongPath(start, speed, dt, path);

                float dx = advanced.Position.X - gate.X;
                float dy = advanced.Position.Y - gate.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                float expected = speed * dt;

                return MathF.Abs(dist - expected) < 0.01f;
            });
        }
    }
}
