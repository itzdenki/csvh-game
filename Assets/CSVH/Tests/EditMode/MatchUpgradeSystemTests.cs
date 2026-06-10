// Feature: in-match-upgrades — unit test cho MatchUpgradeSystem (Core thuần C#).

using CSVH.Core.Progression;
using NUnit.Framework;

namespace CSVH.Tests.Edit
{
    /// <summary>
    /// Kiểm chứng các bất biến của <see cref="MatchUpgradeSystem"/>:
    /// mua thành công trừ đúng Vàng + tăng đúng 1 cấp; thiếu Vàng không đổi trạng thái;
    /// các hệ số suy ra khớp bảng GDD (Lv1 = +5%, Nỏ Băng Lv1 = 15%…); trần làm chậm/chí mạng.
    /// </summary>
    public sealed class MatchUpgradeSystemTests
    {
        /// <summary>Bảng cố định khớp số liệu GDD; giá phẳng 100 Vàng mỗi mốc cho dễ kiểm.</summary>
        private sealed class FakeTable : IMatchUpgradeTable
        {
            public float DamagePerLevel => 0.05f;
            public float AttackSpeedPerLevel => 0.05f;
            public float CritChancePerLevel => 0.05f;
            public float CritChanceCap => 1f;
            public float BaseCritMultiplier => 1.5f;
            public float CritDamagePerLevel => 0.25f;
            public float ProjectileSpeedPerLevel => 0.05f;
            public float FortifyHpPerLevel => 0.05f;
            public float RegenHpPerLevel => 5f;
            public float IceSlowBase => 0.05f;
            public float IceSlowPerLevel => 0.10f;
            public float IceSlowCap => 0.8f;
            public float IceSlowDurationSeconds => 2f;
            public float PoisonDpsPerLevel => 0.05f;
            public float PoisonDurationSeconds => 3f;
            public float GoldRushChanceBase => 0.075f;
            public float GoldRushChancePerLevel => 0.025f;
            public float GoldRushChanceCap => 0.5f;
            public float GoldRushBonusFraction => 1f;
            public float MultishotSpreadDegrees => 7f;

            public int CostFor(MatchUpgradeKind kind, int currentLevel) => 100;

            // Làn Đạn trần 3 cấp (tối đa 4 mũi tên); các nâng cấp khác không trần.
            public int MaxLevelFor(MatchUpgradeKind kind)
                => kind == MatchUpgradeKind.Multishot ? 3 : 0;
        }

        [Test]
        public void TryBuy_DuVang_TruVangVaTangDungMotCap()
        {
            var wallet = new UpgradeSystem(initialGold: 250);
            var sys = new MatchUpgradeSystem(new FakeTable());

            var outcome = sys.TryBuy(MatchUpgradeKind.Damage, wallet);

            Assert.That(outcome.Outcome, Is.EqualTo(UpgradeOutcome.Bought));
            Assert.That(outcome.CostPaid, Is.EqualTo(100));
            Assert.That(outcome.NewLevel, Is.EqualTo(1));
            Assert.That(sys.GetLevel(MatchUpgradeKind.Damage), Is.EqualTo(1));
            Assert.That(wallet.Gold, Is.EqualTo(150));
        }

        [Test]
        public void TryBuy_ThieuVang_KhongDoiTrangThai()
        {
            var wallet = new UpgradeSystem(initialGold: 50);
            var sys = new MatchUpgradeSystem(new FakeTable());

            var outcome = sys.TryBuy(MatchUpgradeKind.IceArrow, wallet);

            Assert.That(outcome.Outcome, Is.EqualTo(UpgradeOutcome.NotEnoughGold));
            Assert.That(outcome.CostPaid, Is.EqualTo(0));
            Assert.That(sys.GetLevel(MatchUpgradeKind.IceArrow), Is.EqualTo(0));
            Assert.That(wallet.Gold, Is.EqualTo(50));
        }

        [Test]
        public void HeSoSuyRa_KhopBangGdd_Lv1VaLv5()
        {
            var wallet = new UpgradeSystem(initialGold: 10_000);
            var sys = new MatchUpgradeSystem(new FakeTable());

            // Lv1 từng nâng cấp. Chí Mạng đã GỘP: một cấp tăng cả tỷ lệ lẫn sát thương.
            sys.TryBuy(MatchUpgradeKind.Damage, wallet);
            sys.TryBuy(MatchUpgradeKind.AttackSpeed, wallet);
            sys.TryBuy(MatchUpgradeKind.Crit, wallet);
            sys.TryBuy(MatchUpgradeKind.BaseRegen, wallet);
            sys.TryBuy(MatchUpgradeKind.IceArrow, wallet);
            sys.TryBuy(MatchUpgradeKind.PoisonArrow, wallet);

            Assert.That(sys.DamageMultiplier, Is.EqualTo(1.05f).Within(1e-5f));
            Assert.That(sys.FireRateMultiplier, Is.EqualTo(1.05f).Within(1e-5f));
            Assert.That(sys.CritChance, Is.EqualTo(0.05f).Within(1e-5f));
            Assert.That(sys.CritMultiplier, Is.EqualTo(1.75f).Within(1e-5f));
            Assert.That(sys.RegenHpPerSecond, Is.EqualTo(5f).Within(1e-5f));
            Assert.That(sys.IceSlowFraction, Is.EqualTo(0.15f).Within(1e-5f)); // Lv1 = 15%
            Assert.That(sys.PoisonDpsFraction, Is.EqualTo(0.05f).Within(1e-5f));

            // Hoàng Kim Lv1 = 7.5% + 2.5% = 10% (khớp thiết kế "10-20%").
            sys.TryBuy(MatchUpgradeKind.GoldRush, wallet);
            Assert.That(sys.GoldRushChance, Is.EqualTo(0.10f).Within(1e-5f));

            // Lên Lv5 Nỏ Băng: 5% + 5×10% = 55% (khớp bảng GDD).
            for (int i = 0; i < 4; i++) sys.TryBuy(MatchUpgradeKind.IceArrow, wallet);
            Assert.That(sys.IceSlowFraction, Is.EqualTo(0.55f).Within(1e-5f));
        }

        [Test]
        public void NoBang_VuotTran_BiKepTaiCap()
        {
            var wallet = new UpgradeSystem(initialGold: 100_000);
            var sys = new MatchUpgradeSystem(new FakeTable());

            // Lv20 → 5% + 200% nhưng phải kẹp tại 80%.
            for (int i = 0; i < 20; i++) sys.TryBuy(MatchUpgradeKind.IceArrow, wallet);

            Assert.That(sys.IceSlowFraction, Is.EqualTo(0.8f).Within(1e-5f));
        }

        [Test]
        public void ChuaNang_KhongCoHieuUng()
        {
            var sys = new MatchUpgradeSystem(new FakeTable());

            Assert.That(sys.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(sys.FireRateMultiplier, Is.EqualTo(1f));
            Assert.That(sys.CritChance, Is.EqualTo(0f));
            Assert.That(sys.ProjectileSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(sys.RegenHpPerSecond, Is.EqualTo(0f));
            Assert.That(sys.IceSlowFraction, Is.EqualTo(0f));
            Assert.That(sys.IceSlowDurationSeconds, Is.EqualTo(0f));
            Assert.That(sys.PoisonDpsFraction, Is.EqualTo(0f));
            Assert.That(sys.PoisonDurationSeconds, Is.EqualTo(0f));
            Assert.That(sys.GoldRushChance, Is.EqualTo(0f));
            Assert.That(sys.ExtraProjectiles, Is.EqualTo(0));
        }

        [Test]
        public void LanDan_TranBaCap_MuaTiepTraMaxedKhongTruVang()
        {
            var wallet = new UpgradeSystem(initialGold: 10_000);
            var sys = new MatchUpgradeSystem(new FakeTable());

            // Mua tới trần 3 cấp → 1 + 3 = 4 mũi tên.
            for (int i = 0; i < 3; i++)
            {
                Assert.That(sys.TryBuy(MatchUpgradeKind.Multishot, wallet).Outcome,
                    Is.EqualTo(UpgradeOutcome.Bought));
            }
            Assert.That(sys.ExtraProjectiles, Is.EqualTo(3));
            Assert.That(sys.IsMaxed(MatchUpgradeKind.Multishot), Is.True);

            // Mua lần 4: trả Maxed, không trừ Vàng, cấp không đổi.
            int goldBefore = wallet.Gold;
            var outcome = sys.TryBuy(MatchUpgradeKind.Multishot, wallet);
            Assert.That(outcome.Outcome, Is.EqualTo(UpgradeOutcome.Maxed));
            Assert.That(outcome.CostPaid, Is.EqualTo(0));
            Assert.That(wallet.Gold, Is.EqualTo(goldBefore));
            Assert.That(sys.GetLevel(MatchUpgradeKind.Multishot), Is.EqualTo(3));
        }

        [Test]
        public void HoangKim_Lv5Bang20PhanTram_VaBiKepTran()
        {
            var wallet = new UpgradeSystem(initialGold: 100_000);
            var sys = new MatchUpgradeSystem(new FakeTable());

            // Lv5: 7.5% + 5 × 2.5% = 20% (khớp thiết kế "10-20%").
            for (int i = 0; i < 5; i++) sys.TryBuy(MatchUpgradeKind.GoldRush, wallet);
            Assert.That(sys.GoldRushChance, Is.EqualTo(0.20f).Within(1e-5f));

            // Lv30 → 7.5% + 75% nhưng kẹp trần 50%.
            for (int i = 0; i < 25; i++) sys.TryBuy(MatchUpgradeKind.GoldRush, wallet);
            Assert.That(sys.GoldRushChance, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void CuongHoaThanh_DeltaTinhTrenHpGoc()
        {
            var sys = new MatchUpgradeSystem(new FakeTable());
            // +5% của HP gốc cho MỖI lần mua, không lãi kép.
            Assert.That(sys.FortifyHpDeltaFor(200), Is.EqualTo(10f).Within(1e-5f));
        }
    }
}
