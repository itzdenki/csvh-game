// Feature: tower-defense-vn — GDD Cơ chế 2 (Meta Upgrade). Property/unit tests cho
// MetaProgressionState: số học mua Xu cổ, bất biến không-mutate khi thiếu/chạm trần,
// đơn điệu của số dư và của bonus theo cấp, round-trip snapshot.

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;
using CSVH.Core.Storage;

namespace CSVH.Tests.Edit.Properties
{
    public class MetaProgressionProperty
    {
        // Bảng meta phẳng để cô lập số học mua (giá cố định, trần cấp nhỏ, hiệu ứng tuyến tính).
        private sealed class TestMetaTable : IMetaUpgradeTable
        {
            public int GateHpPerLevel => 10;
            public float CrossbowDamagePerLevel => 0.5f;
            public float CooldownReductionPerLevel => 0.1f;
            public float MaxCooldownReduction => 0.5f;
            public const int MaxLevel = 5;
            public const int FlatCost = 10;
            public int MaxLevelFor(MetaUpgradeTrack track) => MaxLevel;
            public int CostFor(MetaUpgradeTrack track, int currentLevel) => FlatCost;
        }

        private static MetaProgressionState NewState(long coins, int gate = 0, int crossbow = 0, int cooldown = 0) =>
            new MetaProgressionState(new MetaProgressSnapshot(coins, gate, crossbow, cooldown), new TestMetaTable());

        [Test]
        public void BuyDecrementsCoinsAndIncrementsLevel()
        {
            PbtRunner.RunForAll<PositiveInt>(coinsP =>
            {
                long coins = Math.Max(TestMetaTable.FlatCost, coinsP.Get);
                var state = NewState(coins);
                int preLevel = state.GetLevel(MetaUpgradeTrack.GateHp);

                var outcome = state.TryBuy(MetaUpgradeTrack.GateHp);

                return outcome.Outcome == MetaUpgradeOutcome.Bought
                    && outcome.CostPaid == TestMetaTable.FlatCost
                    && state.Coins == coins - TestMetaTable.FlatCost
                    && state.GetLevel(MetaUpgradeTrack.GateHp) == preLevel + 1;
            });
        }

        [Test]
        public void NotEnoughCoinsDoesNotMutate()
        {
            PbtRunner.RunForAll<NonNegativeInt>(coinsP =>
            {
                long coins = Math.Min(coinsP.Get, TestMetaTable.FlatCost - 1); // < giá
                var state = NewState(coins);
                long preCoins = state.Coins;
                int preLevel = state.GetLevel(MetaUpgradeTrack.CrossbowDamage);

                var outcome = state.TryBuy(MetaUpgradeTrack.CrossbowDamage);

                return outcome.Outcome == MetaUpgradeOutcome.NotEnoughCoins
                    && outcome.CostPaid == 0
                    && state.Coins == preCoins
                    && state.GetLevel(MetaUpgradeTrack.CrossbowDamage) == preLevel;
            });
        }

        [Test]
        public void MaxLevelReachedDoesNotMutate()
        {
            // Khởi tạo nhánh ở đúng trần cấp + thừa Xu cổ → vẫn không mua được.
            var state = NewState(coins: 1_000_000, cooldown: TestMetaTable.MaxLevel);
            long preCoins = state.Coins;

            var outcome = state.TryBuy(MetaUpgradeTrack.UltimateCooldown);

            Assert.AreEqual(MetaUpgradeOutcome.MaxLevelReached, outcome.Outcome);
            Assert.AreEqual(0, outcome.CostPaid);
            Assert.AreEqual(preCoins, state.Coins);
            Assert.AreEqual(TestMetaTable.MaxLevel, state.GetLevel(MetaUpgradeTrack.UltimateCooldown));
        }

        [Test]
        public void AddCoinsIsMonotonicAndExact()
        {
            PbtRunner.RunForAll<NonNegativeInt, NonNegativeInt>((startP, addP) =>
            {
                long start = startP.Get;
                long add = addP.Get;
                var state = NewState(start);
                state.AddCoins(add);
                return state.Coins >= start && state.Coins == start + add;
            });
        }

        [Test]
        public void AddNegativeCoinsThrows()
        {
            var state = NewState(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.AddCoins(-1));
        }

        [Test]
        public void BonusesAreMonotonicInLevel()
        {
            // Với mọi cấp L ∈ [0, MaxLevel-1]: cấp L+1 cho Máu Cổng & Sát thương Nỏ ≥ cấp L,
            // và hệ số hồi chiêu (cooldownScale) ≤ cấp L (giảm hoặc bằng), luôn > 0.
            for (int level = 0; level < TestMetaTable.MaxLevel; level++)
            {
                var lo = NewState(0, gate: level, crossbow: level, cooldown: level).Bonuses;
                var hi = NewState(0, gate: level + 1, crossbow: level + 1, cooldown: level + 1).Bonuses;

                Assert.GreaterOrEqual(hi.GateHpBonus, lo.GateHpBonus, $"GateHp at level {level}");
                Assert.GreaterOrEqual(hi.CrossbowDamageBonus, lo.CrossbowDamageBonus, $"Crossbow at level {level}");
                Assert.LessOrEqual(hi.CooldownScale, lo.CooldownScale, $"CooldownScale at level {level}");
                Assert.Greater(hi.CooldownScale, 0f, $"CooldownScale must stay > 0 at level {level + 1}");
            }
        }

        [Test]
        public void CooldownScaleNeverBelowFloor()
        {
            // Sàn = 1 − MaxCooldownReduction. Dù cấp ở trần, scale không xuống dưới sàn.
            var table = new TestMetaTable();
            float floor = 1f - table.MaxCooldownReduction;
            var state = NewState(0, cooldown: TestMetaTable.MaxLevel);
            Assert.GreaterOrEqual(state.Bonuses.CooldownScale, floor - 1e-6f);
        }

        [Test]
        public void SnapshotRoundTripPreservesState()
        {
            PbtRunner.RunForAll<NonNegativeInt>(coinsP =>
            {
                var original = NewState(coinsP.Get, gate: 2, crossbow: 1, cooldown: 3);
                var snap = original.ToSnapshot();
                var restored = new MetaProgressionState(snap, new TestMetaTable());
                return restored.Coins == original.Coins
                    && restored.GateHpLevel == original.GateHpLevel
                    && restored.CrossbowDamageLevel == original.CrossbowDamageLevel
                    && restored.UltimateCooldownLevel == original.UltimateCooldownLevel;
            });
        }

        [Test]
        public void CorruptSnapshotIsClampedToValidRange()
        {
            // Dữ liệu hỏng (âm / vượt trần) phải được kẹp về miền hợp lệ khi dựng state.
            var state = new MetaProgressionState(
                new MetaProgressSnapshot(-50L, -3, 999, -1), new TestMetaTable());
            Assert.AreEqual(0L, state.Coins);
            Assert.AreEqual(0, state.GateHpLevel);
            Assert.AreEqual(TestMetaTable.MaxLevel, state.CrossbowDamageLevel);
            Assert.AreEqual(0, state.UltimateCooldownLevel);
        }
    }
}
