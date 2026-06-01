// Feature: tower-defense-vn, Property 13: Số học mua nâng cấp và confluence
// Validates: Requirements 6.2, 6.3, 6.4, 6.5

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;

namespace CSVH.Tests.Edit.Properties
{
    public class Property13_UpgradeConfluence
    {
        private sealed class FlatCostTable : IUpgradeCostTable
        {
            public float BaseArmor => 0f;
            public float ArmorStep => 5f;
            public float AttackStep => 0.1f;
            public int BaseCost => 10;
            public float CostGrowth => 1.0f;
            // flat cost simplifies confluence
            public int CostFor(UpgradeTrack track, int currentLevel) => BaseCost;
        }

        [Test]
        public void BuyDecrementsGoldAndIncrementsLevel()
        {
            PbtRunner.RunForAll<PositiveInt>(goldP =>
            {
                int gold = goldP.Get;
                if (gold < 10) gold = 10;
                var costs = new FlatCostTable();
                var sys = new UpgradeSystem(initialGold: gold);
                var preLevel = sys.ArmorLevel;
                var outcome = sys.TryBuy(UpgradeTrack.Armor, costs);
                if (outcome.Outcome != UpgradeOutcome.Bought) return false;
                return sys.Gold == gold - 10 && sys.ArmorLevel == preLevel + 1;
            });
        }

        [Test]
        public void NotEnoughGoldDoesNotMutate()
        {
            PbtRunner.RunForAll<NonNegativeInt>(goldP =>
            {
                int gold = Math.Min(goldP.Get, 9); // < cost
                var costs = new FlatCostTable();
                var sys = new UpgradeSystem(initialGold: gold);
                var preLevel = sys.ArmorLevel;
                var preGold = sys.Gold;
                var outcome = sys.TryBuy(UpgradeTrack.Armor, costs);
                return outcome.Outcome == UpgradeOutcome.NotEnoughGold
                    && sys.Gold == preGold
                    && sys.ArmorLevel == preLevel;
            });
        }

        [Test]
        public void ConfluenceArmorAttackOrder()
        {
            PbtRunner.RunForAll<byte[]>(orderRaw =>
            {
                // Generate two random permutations of buys (Armor or Attack);
                // apply them in two orders, compare end state.
                if (orderRaw == null || orderRaw.Length == 0) return true;
                int n = Math.Min(orderRaw.Length, 20);
                var ops = new System.Collections.Generic.List<UpgradeTrack>();
                for (int i = 0; i < n; i++)
                {
                    ops.Add((orderRaw[i] & 1) == 0 ? UpgradeTrack.Armor : UpgradeTrack.Attack);
                }
                var permA = ops;
                var permB = new System.Collections.Generic.List<UpgradeTrack>(ops);
                permB.Reverse(); // simple reversed permutation

                int initialGold = n * 10;
                var costs = new FlatCostTable();
                var sysA = new UpgradeSystem(initialGold);
                var sysB = new UpgradeSystem(initialGold);
                foreach (var op in permA) sysA.TryBuy(op, costs);
                foreach (var op in permB) sysB.TryBuy(op, costs);
                return sysA.Gold == sysB.Gold
                    && sysA.ArmorLevel == sysB.ArmorLevel
                    && sysA.AttackLevel == sysB.AttackLevel;
            }, maxTest: 50);
        }
    }
}
