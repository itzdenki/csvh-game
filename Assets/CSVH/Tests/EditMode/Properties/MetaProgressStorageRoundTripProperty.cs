// Feature: tower-defense-vn — GDD Cơ chế 2 (Meta Upgrade). Round-trip tiến trình META
// qua Bộ_Lưu_Trữ (đồng dạng Property 19 cho Kỷ_Lục): ghi rồi đọc phải trả đúng giá trị,
// và đọc khi chưa từng ghi trả MetaProgressSnapshot.Empty.

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Storage;

namespace CSVH.Tests.Edit.Properties
{
    public class MetaProgressStorageRoundTripProperty
    {
        [Test]
        public void RoundTripMetaProgress()
        {
            PbtRunner.RunForAll<NonNegativeInt, NonNegativeInt, NonNegativeInt>((coinsP, lvlAP, lvlBP) =>
            {
                var snap = new MetaProgressSnapshot(
                    Coins: coinsP.Get,
                    GateHpLevel: lvlAP.Get,
                    CrossbowDamageLevel: lvlBP.Get,
                    UltimateCooldownLevel: (lvlAP.Get + lvlBP.Get) % 7);

                var s = new InMemoryStorageService();
                s.WriteMetaProgress(snap);

                // Value-equality của record đủ để kiểm round-trip đầy đủ 4 trường.
                return s.ReadMetaProgress().Equals(snap);
            });
        }

        [Test]
        public void DefaultMetaProgressIsEmpty()
        {
            var s = new InMemoryStorageService();
            Assert.AreEqual(MetaProgressSnapshot.Empty, s.ReadMetaProgress());
        }

        [Test]
        public void NegativeFieldsAreClampedOnWrite()
        {
            var s = new InMemoryStorageService();
            s.WriteMetaProgress(new MetaProgressSnapshot(-10L, -1, -2, -3));
            var read = s.ReadMetaProgress();
            Assert.AreEqual(0L, read.Coins);
            Assert.AreEqual(0, read.GateHpLevel);
            Assert.AreEqual(0, read.CrossbowDamageLevel);
            Assert.AreEqual(0, read.UltimateCooldownLevel);
        }

        [Test]
        public void WriteNullThrows()
        {
            var s = new InMemoryStorageService();
            Assert.Throws<ArgumentNullException>(() => s.WriteMetaProgress(null));
        }
    }
}
