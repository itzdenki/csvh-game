// Feature: tower-defense-vn, Property 25: Bản dịch tiếng Việt đầy đủ
// Validates: Requirements 11.3

using System.Collections.Generic;
using NUnit.Framework;
using CSVH.Core.Hud;

namespace CSVH.Tests.Edit.Properties
{
    public class Property25_VietnameseTranslation
    {
        [Test]
        public void EveryUiKeyHasNonEmptyVietnameseTranslation()
        {
            var loc = Localizer.CreateDefaultVietnamese();
            foreach (var key in UiStringKeys.AllKeys)
            {
                var v = loc.Get(key, "vi");
                Assert.IsFalse(string.IsNullOrEmpty(v), $"Translation missing for key: {key}");
                Assert.IsFalse(v.StartsWith("[?"), $"Placeholder returned for key: {key}");
            }
        }

        [Test]
        public void EveryPlayerFacingLabelContainsVietnameseDiacritic()
        {
            var loc = Localizer.CreateDefaultVietnamese();
            var diacritics = "ăâđêôơưĂÂĐÊÔƠƯáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ";
            foreach (var key in UiStringKeys.PlayerFacingLabels)
            {
                var v = loc.Get(key, "vi");
                bool hasDiacritic = false;
                foreach (var ch in v)
                {
                    if (diacritics.IndexOf(ch) >= 0)
                    {
                        hasDiacritic = true;
                        break;
                    }
                }
                Assert.IsTrue(hasDiacritic, $"Label '{key}' = '{v}' lacks Vietnamese diacritic.");
            }
        }

        [Test]
        public void MissingKeyReturnsPlaceholderAndLogsWarning()
        {
            var stub = new TestLogSink();
            var loc = Localizer.CreateDefaultVietnamese(stub);
            var v = loc.Get("nonexistent.key", "vi");
            Assert.AreEqual("[?nonexistent.key]", v);
            Assert.IsTrue(stub.Warnings.Count > 0);
        }

        private sealed class TestLogSink : CSVH.Core.Logging.ILogSink
        {
            public List<string> Warnings = new();
            public void Warn(string m) => Warnings.Add(m);
            public void Error(string m) { }
            public void Info(string m) { }
        }
    }
}
