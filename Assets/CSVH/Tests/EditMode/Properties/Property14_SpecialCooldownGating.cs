// Feature: tower-defense-vn, Property 14: Cooldown gating Special
// Validates: Requirements 6.6, 6.7

using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;
using CSVH.Core.Common;

namespace CSVH.Tests.Edit.Properties
{
    public class Property14_SpecialCooldownGating
    {
        [Test]
        public void ActivatingWhileCooldownIsActiveReturnsFalse()
        {
            PbtRunner.RunForAll<PositiveInt, PositiveInt>((cooldownMs, radiusMs) =>
            {
                float cd = (cooldownMs.Get % 100) + 1f;
                float r = (radiusMs.Get % 100) + 1f;
                var s = new SpecialAbility(cd, r);

                // First activate: should succeed.
                if (!s.TryActivate()) return false;
                if (s.CooldownRemaining != cd) return false;

                // Second activate: should fail and not change CooldownRemaining.
                bool second = s.TryActivate();
                return !second && s.CooldownRemaining == cd;
            });
        }

        [Test]
        public void TickReducesCooldown()
        {
            PbtRunner.RunForAll<PositiveInt, PositiveInt, NonNegativeInt[]>((cdP, rP, dtSeq) =>
            {
                if (dtSeq == null) dtSeq = System.Array.Empty<NonNegativeInt>();
                float cd = (cdP.Get % 50) + 5f;
                float r = (rP.Get % 50) + 5f;
                var s = new SpecialAbility(cd, r);
                s.TryActivate();

                foreach (var dtN in dtSeq)
                {
                    float dt = (dtN.Get % 5) + 0.01f;
                    float prev = s.CooldownRemaining;
                    s.Tick(dt);
                    if (s.CooldownRemaining > prev) return false;
                    if (s.CooldownRemaining < 0) return false;
                }
                return true;
            });
        }

        [Test]
        public void TickReducesCooldownBelowMaxAfterEnoughTime()
        {
            var s = new SpecialAbility(1.0f, 5.0f);
            s.TryActivate();
            s.Tick(2.0f);
            Assert.AreEqual(0f, s.CooldownRemaining);
            Assert.IsTrue(s.IsReady);
        }
    }
}
