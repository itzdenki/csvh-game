// Feature: tower-defense-vn, Property 12: Nâng cấp Giáp tăng Máu_Tối_Đa bảo toàn ràng buộc
// Validates: Requirements 5.6

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;

namespace CSVH.Tests.Edit.Properties
{
    public class Property12_ArmorUpgradePreservesHp
    {
        /// <summary>
        /// Test double for <see cref="IUpgradeCostTable"/> với <see cref="ArmorStep"/> tuỳ biến
        /// và chi phí cố định để cô lập bất biến HP/MaxHp khỏi đường cong giá.
        /// </summary>
        private sealed class StubCostTable : IUpgradeCostTable
        {
            public float BaseArmor { get; init; } = 0f;
            public float ArmorStep { get; init; } = 10f;
            public float AttackStep { get; init; } = 0.1f;
            public int BaseCost { get; init; } = 50;
            public float CostGrowth { get; init; } = 1.2f;
            public int CostFor(UpgradeTrack track, int currentLevel) => BaseCost;
        }

        /// <summary>
        /// Property 12: với mọi <c>(CurrentHp, MaxHp)</c> sao cho <c>0 ≤ CurrentHp ≤ MaxHp</c>,
        /// việc mua một bậc Armor phơi <c>BuyOutcome.MaxHpDelta == costs.ArmorStep</c>; cộng
        /// delta vào cả <c>CurrentHp</c> và <c>MaxHp</c> giữ nguyên ràng buộc <c>≤</c>.
        /// </summary>
        [Test]
        public void ArmorBuyEmitsExpectedHpDelta()
        {
            PbtRunner.RunForAll<NonNegativeInt, PositiveInt>((currentHpP, maxHpP) =>
            {
                int currentHp = currentHpP.Get;
                int maxHp = maxHpP.Get;
                if (currentHp > maxHp) return true; // pre-condition: 0 ≤ CurrentHp ≤ MaxHp

                var costs = new StubCostTable { ArmorStep = 25f, BaseCost = 50 };
                var sys = new UpgradeSystem(initialGold: 1000);

                var outcome = sys.TryBuy(UpgradeTrack.Armor, costs);
                if (outcome.Outcome != UpgradeOutcome.Bought) return false;
                if (outcome.MaxHpDelta != costs.ArmorStep) return false;

                // Áp delta theo cùng quy ước Property 12 (Δ cộng vào cả CurrentHp và MaxHp).
                int newCur = currentHp + (int)Math.Round(outcome.MaxHpDelta);
                int newMax = maxHp + (int)Math.Round(outcome.MaxHpDelta);

                // Requirement 5.6: bất biến CurrentHp ≤ MaxHp được bảo toàn.
                return newCur <= newMax;
            });
        }

        /// <summary>
        /// Mua nhánh không phải Armor không được phát sinh delta máu (Requirement 5.6).
        /// </summary>
        [Test]
        public void NonArmorBuyHasZeroHpDelta()
        {
            var costs = new StubCostTable { BaseCost = 10 };
            var sys = new UpgradeSystem(initialGold: 1000);

            var atk = sys.TryBuy(UpgradeTrack.Attack, costs);
            Assert.AreEqual(0f, atk.MaxHpDelta);

            var spe = sys.TryBuy(UpgradeTrack.Special, costs);
            Assert.AreEqual(0f, spe.MaxHpDelta);
        }
    }
}
