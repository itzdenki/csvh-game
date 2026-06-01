// Feature: tower-defense-vn, Properties cho SpecialSkillSystem (3 skill Special).
// Validates: Requirements 6.1, 6.2, 6.3, 6.6, 6.7.
// Cập nhật: skill nay bắt đầu ở trạng thái KHÓA — phải TryUnlock trước khi dùng/nâng cấp.

using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;

namespace CSVH.Tests.Edit.Properties
{
    public class Property23_SpecialSkillSystem
    {
        // RNG tất định cho test: phát lại một dãy giá trị [0,1) cố định, lặp vòng.
        private sealed class FakeRandom : IRandom
        {
            private readonly double[] _values;
            private int _i;

            public FakeRandom(params double[] values)
            {
                _values = (values != null && values.Length > 0) ? values : new[] { 0.0 };
            }

            public double NextDouble()
            {
                double v = _values[_i % _values.Length];
                _i++;
                return v;
            }
        }

        private sealed class FakeTable : ISpecialSkillTable
        {
            private readonly SpecialSkillParams _p;
            public FakeTable(SpecialSkillParams p) { _p = p; }
            public SpecialSkillParams ParamsFor(SpecialSkillKind kind) => _p;
        }

        private static SpecialSkillParams DefaultParams() => new(
            BaseDamage: 20f, DamageStep: 8f,
            BaseCooldown: 12f, CooldownStep: 0.6f, MinCooldown: 3f,
            Radius: 5f, BaseHitCount: 1, ExtraEffectChanceStep: 0.1f,
            BaseStunSeconds: 1.2f, StunStep: 0.1f, BaseStunChance: 0.5f,
            BaseCost: 80, CostGrowth: 1.3f, UnlockCost: 100);

        private static SpecialSkillSystem NewSystem() =>
            new SpecialSkillSystem(new FakeTable(DefaultParams()));

        // Hệ thống với một skill đã mở khoá (ví vàng dư dả để không nhiễu các phép thử khác).
        private static SpecialSkillSystem NewUnlocked(SpecialSkillKind kind)
        {
            var sys = NewSystem();
            sys.TryUnlock(kind, new UpgradeSystem(initialGold: 1_000_000));
            return sys;
        }

        // Hệ thống với cả 3 skill đã mở khoá.
        private static SpecialSkillSystem NewAllUnlocked()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 1_000_000);
            sys.TryUnlock(SpecialSkillKind.TrongDong, wallet);
            sys.TryUnlock(SpecialSkillKind.MuiTen, wallet);
            sys.TryUnlock(SpecialSkillKind.LuoiGuom, wallet);
            return sys;
        }

        // ----- Cơ chế mở khoá (mua skill) -----

        [Test]
        public void SkillStartsLockedAndCannotActivate()
        {
            var sys = NewSystem();
            var rng = new FakeRandom(0.5);

            Assert.IsFalse(sys.IsUnlocked(SpecialSkillKind.TrongDong), "Skill phải bắt đầu ở trạng thái khóa.");
            var act = sys.TryActivate(SpecialSkillKind.TrongDong, rng);
            Assert.IsFalse(act.Activated, "Skill chưa mở khoá thì không kích hoạt được.");
        }

        [Test]
        public void TryUnlockWithEnoughGoldUnlocksAndSpends()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 1000);
            int cost = sys.UnlockCostFor(SpecialSkillKind.MuiTen);

            var outcome = sys.TryUnlock(SpecialSkillKind.MuiTen, wallet);

            Assert.AreEqual(UpgradeOutcome.Bought, outcome);
            Assert.IsTrue(sys.IsUnlocked(SpecialSkillKind.MuiTen));
            Assert.AreEqual(1000 - cost, wallet.Gold);
        }

        [Test]
        public void TryUnlockWithInsufficientGoldStaysLocked()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 0);

            var outcome = sys.TryUnlock(SpecialSkillKind.LuoiGuom, wallet);

            Assert.AreEqual(UpgradeOutcome.NotEnoughGold, outcome);
            Assert.IsFalse(sys.IsUnlocked(SpecialSkillKind.LuoiGuom));
            Assert.AreEqual(0, wallet.Gold);
        }

        [Test]
        public void CannotUpgradeBeforeUnlock()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 1_000_000);

            int before = sys.GetLevel(SpecialSkillKind.TrongDong);
            var outcome = sys.TryBuyUpgrade(SpecialSkillKind.TrongDong, wallet);

            Assert.AreEqual(UpgradeOutcome.NotEnoughGold, outcome, "Chưa mở khoá thì không có nhánh nâng cấp.");
            Assert.AreEqual(before, sys.GetLevel(SpecialSkillKind.TrongDong));
            Assert.AreEqual(1_000_000, wallet.Gold, "Không được trừ Vàng khi skill còn khóa.");
        }

        [Test]
        public void UnlockingDoesNotSpendTwice()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 1000);

            sys.TryUnlock(SpecialSkillKind.TrongDong, wallet);
            int afterFirst = wallet.Gold;
            var second = sys.TryUnlock(SpecialSkillKind.TrongDong, wallet);

            Assert.AreEqual(UpgradeOutcome.Bought, second);
            Assert.AreEqual(afterFirst, wallet.Gold, "Mở khoá lần hai không được trừ thêm Vàng.");
        }

        // ----- Sau khi mở khoá: kích hoạt + cooldown -----

        [Test]
        public void ActivatingWhileCooldownActiveReturnsNotReady()
        {
            var sys = NewUnlocked(SpecialSkillKind.TrongDong);
            var rng = new FakeRandom(0.5);

            var first = sys.TryActivate(SpecialSkillKind.TrongDong, rng);
            Assert.IsTrue(first.Activated, "Lần đầu (đã mở khoá) phải kích hoạt được.");

            var second = sys.TryActivate(SpecialSkillKind.TrongDong, rng);
            Assert.IsFalse(second.Activated, "Lần hai khi còn hồi chiêu phải thất bại.");
        }

        [Test]
        public void EachSkillCooldownIsIndependent()
        {
            var sys = NewAllUnlocked();
            var rng = new FakeRandom(0.5);

            Assert.IsTrue(sys.TryActivate(SpecialSkillKind.TrongDong, rng).Activated);
            // Skill khác vẫn sẵn sàng dù Trống Đồng đang hồi chiêu.
            Assert.IsTrue(sys.TryActivate(SpecialSkillKind.MuiTen, rng).Activated);
            Assert.IsTrue(sys.TryActivate(SpecialSkillKind.LuoiGuom, rng).Activated);
        }

        [Test]
        public void TickReducesCooldownToZeroThenReady()
        {
            var sys = NewUnlocked(SpecialSkillKind.LuoiGuom);
            var rng = new FakeRandom(0.0);
            sys.TryActivate(SpecialSkillKind.LuoiGuom, rng);

            Assert.Greater(sys.GetCooldownRemaining(SpecialSkillKind.LuoiGuom), 0f);
            sys.Tick(1000f);
            Assert.AreEqual(0f, sys.GetCooldownRemaining(SpecialSkillKind.LuoiGuom));
            Assert.IsTrue(sys.TryActivate(SpecialSkillKind.LuoiGuom, rng).Activated);
        }

        // ----- Mua nâng cấp (sau khi mở khoá) -----

        [Test]
        public void BuyUpgradeDecrementsGoldAndIncrementsLevel()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 1000);
            sys.TryUnlock(SpecialSkillKind.MuiTen, wallet);

            int cost = sys.CostFor(SpecialSkillKind.MuiTen);
            int before = sys.GetLevel(SpecialSkillKind.MuiTen);
            int goldBefore = wallet.Gold;

            var outcome = sys.TryBuyUpgrade(SpecialSkillKind.MuiTen, wallet);

            Assert.AreEqual(UpgradeOutcome.Bought, outcome);
            Assert.AreEqual(before + 1, sys.GetLevel(SpecialSkillKind.MuiTen));
            Assert.AreEqual(goldBefore - cost, wallet.Gold);
        }

        [Test]
        public void BuyUpgradeWithInsufficientGoldIsNoOp()
        {
            var sys = NewSystem();
            // Ví chỉ đủ mở khoá, sau đó còn 0 Vàng → không nâng cấp được.
            int unlock = sys.UnlockCostFor(SpecialSkillKind.TrongDong);
            var wallet = new UpgradeSystem(initialGold: unlock);
            sys.TryUnlock(SpecialSkillKind.TrongDong, wallet);
            Assert.AreEqual(0, wallet.Gold);

            int before = sys.GetLevel(SpecialSkillKind.TrongDong);
            var outcome = sys.TryBuyUpgrade(SpecialSkillKind.TrongDong, wallet);

            Assert.AreEqual(UpgradeOutcome.NotEnoughGold, outcome);
            Assert.AreEqual(before, sys.GetLevel(SpecialSkillKind.TrongDong));
            Assert.AreEqual(0, wallet.Gold);
        }

        [Test]
        public void GoldNeverNegativeAfterRepeatedBuys()
        {
            PbtRunner.RunForAll<PositiveInt>(goldP =>
            {
                int gold = goldP.Get % 5000;
                var sys = NewSystem();
                var wallet = new UpgradeSystem(initialGold: gold);

                // Thử mua + mở khoá + nâng cấp lặp lại; ví không bao giờ được âm.
                for (int i = 0; i < 50; i++)
                {
                    sys.TryUnlock(SpecialSkillKind.LuoiGuom, wallet);
                    sys.TryBuyUpgrade(SpecialSkillKind.LuoiGuom, wallet);
                    if (wallet.Gold < 0) return false;
                }
                return wallet.Gold >= 0;
            });
        }

        [Test]
        public void DamageIncreasesAndCooldownDecreasesWithLevel()
        {
            var sys = NewSystem();
            var wallet = new UpgradeSystem(initialGold: 1_000_000);
            sys.TryUnlock(SpecialSkillKind.TrongDong, wallet);
            var state = sys.State(SpecialSkillKind.TrongDong);

            float prevDamage = state.CurrentDamage;
            float prevCooldown = state.CurrentCooldownMax;

            for (int i = 0; i < 20; i++)
            {
                sys.TryBuyUpgrade(SpecialSkillKind.TrongDong, wallet);
                Assert.GreaterOrEqual(state.CurrentDamage, prevDamage, "Sát thương không được giảm khi lên cấp.");
                Assert.LessOrEqual(state.CurrentCooldownMax, prevCooldown, "Hồi chiêu không được tăng khi lên cấp.");
                Assert.GreaterOrEqual(state.CurrentCooldownMax, DefaultParams().MinCooldown - 0.001f, "Hồi chiêu phải ≥ sàn.");
                prevDamage = state.CurrentDamage;
                prevCooldown = state.CurrentCooldownMax;
            }
        }

        // ----- Tính tất định + hiệu ứng "%" -----

        [Test]
        public void ActivationDeterministicWithSameSeed()
        {
            // Cùng dãy RNG → cùng kết quả (HitCount, StunSeconds).
            var sysA = NewUnlocked(SpecialSkillKind.MuiTen);
            var sysB = NewUnlocked(SpecialSkillKind.MuiTen);
            var rngA = new FakeRandom(0.05, 0.95);
            var rngB = new FakeRandom(0.05, 0.95);

            var a = sysA.TryActivate(SpecialSkillKind.MuiTen, rngA);
            var b = sysB.TryActivate(SpecialSkillKind.MuiTen, rngB);

            Assert.AreEqual(a.HitCount, b.HitCount);
            Assert.AreEqual(a.StunSeconds, b.StunSeconds);
            Assert.AreEqual(a.DamagePerHit, b.DamagePerHit);
        }

        [Test]
        public void MuiTenStunsWhenRollBelowChance()
        {
            var sys = NewUnlocked(SpecialSkillKind.MuiTen);
            // BaseStunChance = 0.5; roll 0.1 < 0.5 → dính choáng.
            var rng = new FakeRandom(0.1);
            var act = sys.TryActivate(SpecialSkillKind.MuiTen, rng);

            Assert.IsTrue(act.Activated);
            Assert.Greater(act.StunSeconds, 0f, "Roll dưới ngưỡng phải gây choáng.");
        }

        [Test]
        public void MuiTenNoStunWhenRollAboveChance()
        {
            var sys = NewUnlocked(SpecialSkillKind.MuiTen);
            // roll 0.9 ≥ 0.5 → không choáng.
            var rng = new FakeRandom(0.9);
            var act = sys.TryActivate(SpecialSkillKind.MuiTen, rng);

            Assert.IsTrue(act.Activated);
            Assert.AreEqual(0f, act.StunSeconds, "Roll trên ngưỡng không được gây choáng.");
        }
    }
}
