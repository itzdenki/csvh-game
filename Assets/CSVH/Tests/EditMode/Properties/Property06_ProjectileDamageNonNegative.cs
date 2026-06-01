// Feature: tower-defense-vn, Property 6: Công thức sát thương Đạn lên Quái
// Validates: Requirements 3.3
//
// For any (BaseDamage, AttackMultiplier, Resistance) all >= 0,
// CombatResolver.ProjectileDamage(...) returns max(0, BaseDamage*AttackMultiplier - Resistance)
// and is always >= 0.
//
// FsCheck has NonNegativeInt out of the box but no NonNegativeFloat. We use
// NormalFloat (which excludes NaN/Infinity) and take Math.Abs to constrain the
// input space to non-negative finite floats — matching the precondition stated
// in the task.

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Combat;

namespace CSVH.Tests.Edit.Properties
{
    public class Property06_ProjectileDamageNonNegative
    {
        // Feature: tower-defense-vn, Property 6: Công thức sát thương Đạn lên Quái
        // Validates: Requirements 3.3
        //
        // Single combined assertion per task spec:
        //   dmg >= 0 && dmg == MathF.Max(0, base * mult - resist)
        [Test]
        public void Property6_ProjectileDamageNonNegative()
        {
            PbtRunner.RunForAll<NormalFloat, NormalFloat, NormalFloat>(
                (baseDamageRaw, multRaw, resistRaw) =>
                {
                    // Project to non-negative finite floats (NormalFloat already excludes NaN/Infinity).
                    float baseDamage = MathF.Abs((float)baseDamageRaw.Get);
                    float mult = MathF.Abs((float)multRaw.Get);
                    float resist = MathF.Abs((float)resistRaw.Get);

                    var inputs = new DamageInputs(baseDamage, mult, resist);
                    float dmg = CombatResolver.ProjectileDamage(inputs);

                    float expected = MathF.Max(
                        0f,
                        inputs.BaseDamage * inputs.AttackMultiplier - inputs.TargetResistance);

                    return dmg >= 0f && dmg == expected;
                });
        }

        // ------- Example tests (NUnit) for quick regression coverage -------

        [Test]
        public void DamageExample_BaseTimesMultMinusResist()
        {
            // base=10, mult=2, resist=5 → max(0, 20 - 5) = 15
            var d = CombatResolver.ProjectileDamage(new DamageInputs(10f, 2f, 5f));
            Assert.That(d, Is.EqualTo(15f));
        }

        [Test]
        public void DamageExample_ClampedToZeroWhenResistExceeds()
        {
            // base=3, mult=1, resist=10 → max(0, 3 - 10) = 0
            var d = CombatResolver.ProjectileDamage(new DamageInputs(3f, 1f, 10f));
            Assert.That(d, Is.EqualTo(0f));
        }
    }
}
