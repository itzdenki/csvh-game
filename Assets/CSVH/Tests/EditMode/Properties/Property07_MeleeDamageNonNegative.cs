// Feature: tower-defense-vn, Property 7: Công thức sát thương Quái lên Thành
// Validates: Requirements 2.3, 5.2

using FsCheck;
using NUnit.Framework;
using CSVH.Core.Combat;

namespace CSVH.Tests.Edit.Properties
{
    public class Property07_MeleeDamageNonNegative
    {
        [Test]
        public void MeleeDamageIsNonNegative()
        {
            PbtRunner.RunForAll<NormalFloat, NormalFloat>((meleeRaw, armorRaw) =>
            {
                float melee = System.MathF.Abs((float)meleeRaw.Get);
                float armor = System.MathF.Abs((float)armorRaw.Get);
                var d = CombatResolver.MeleeDamageOnTower(melee, armor);
                return d >= 0f;
            });
        }

        [Test]
        public void MeleeDamageEqualsClampedFormula()
        {
            PbtRunner.RunForAll<NormalFloat, NormalFloat>((meleeRaw, armorRaw) =>
            {
                float melee = System.MathF.Abs((float)meleeRaw.Get);
                float armor = System.MathF.Abs((float)armorRaw.Get);
                var d = CombatResolver.MeleeDamageOnTower(melee, armor);
                return d == System.MathF.Max(0f, melee - armor);
            });
        }
    }
}
