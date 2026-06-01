// Feature: tower-defense-vn, Property 21: Dữ liệu Bộ_Lưu_Trữ hỏng → fallback mặc định
// Validates: Requirements 12.4
//
// Property 21: Với mọi payload bytes (ngẫu nhiên hoặc JSON sai cú pháp/sai lược đồ)
// được ghi vào tệp Kỷ_Lục, `UnityStorageService.ReadHighScore()` PHẢI:
//   1) trả về 0 (mặc định an toàn),
//   2) ghi đè tệp bằng schema mặc định `{"highScore":0}`,
//   3) phát ra ít nhất một bản ghi cảnh báo có thể quan sát qua `ILogSink`.
//
// Test này chạy trong PlayMode vì `UnityStorageService` ràng buộc với
// `Application.persistentDataPath` (chỉ truy cập được khi Unity engine khởi tạo).
// Không dùng FsCheck (FsCheck.dll chỉ enable cho Editor-only platform); phạm vi
// đầu vào hữu hạn (sáu lớp tải lỗi điển hình) được liệt kê qua [TestCase].

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CSVH.Core.Logging;
using CSVH.Core.Storage;
using CSVH.Game.Storage;

namespace CSVH.Tests.Play.Properties
{
    [TestFixture]
    public class Property21_CorruptStorageFallback
    {
        /// <summary>
        /// Sink ghi log in-memory để test có thể quan sát các lời gọi
        /// <see cref="ILogSink.Warn"/> theo Property 21 (Requirement 12.4).
        /// </summary>
        private sealed class TestLogSink : ILogSink
        {
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Infos = new List<string>();

            public void Warn(string message) => Warnings.Add(message);
            public void Error(string message) => Errors.Add(message);
            public void Info(string message) => Infos.Add(message);
        }

        private string _highScorePath;

        [SetUp]
        public void SetUp()
        {
            _highScorePath = Path.Combine(Application.persistentDataPath, StorageKeys.HighScoreFile);
            // Bắt đầu mỗi test với trạng thái sạch để loại nhiễu giữa các case.
            if (File.Exists(_highScorePath))
            {
                File.Delete(_highScorePath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_highScorePath != null && File.Exists(_highScorePath))
            {
                File.Delete(_highScorePath);
            }
        }

        // Năm lớp payload hỏng theo phạm vi Property 21 (design.md: "any payload
        // bytes ngẫu nhiên hoặc JSON sai cú pháp"): tệp rỗng, văn bản không phải JSON,
        // JSON chưa đóng ngoặc, JSON đúng cú pháp nhưng sai kiểu (chuỗi thay long),
        // và JSON đúng cú pháp nhưng số âm (vi phạm bất biến long ≥ 0 — Requirement 8.6).
        // Không bao gồm `{"foo":42}` vì JSON đó hợp lệ về cú pháp; Json.NET sẽ trả
        // DTO mặc định (highScore = 0). Hành vi đó vẫn cho ra kết quả an toàn (0)
        // và không nằm trong phạm vi "payload hỏng" của Property 21.
        [TestCase("", TestName = "EmptyFile")]
        [TestCase("not json", TestName = "NotJson")]
        [TestCase("{not closed", TestName = "MalformedJson")]
        [TestCase("{\"highScore\":\"abc\"}", TestName = "WrongType")]
        [TestCase("{\"highScore\":-5}", TestName = "NegativeValue")]
        public void CorruptPayloadFallsBackToDefaultAndLogsWarning(string payload)
        {
            // Arrange: ghi payload hỏng vào đúng đường dẫn UnityStorageService sẽ đọc.
            File.WriteAllText(_highScorePath, payload);

            var log = new TestLogSink();
            var service = new UnityStorageService(log);

            // Act
            long result = service.ReadHighScore();

            // Assert (1): fallback về 0 (Requirement 12.4).
            Assert.That(result, Is.EqualTo(0L),
                "ReadHighScore phải trả 0 khi payload không hợp lệ.");

            // Assert (2): tệp được ghi đè bằng schema mặc định {"highScore":0}.
            Assert.That(File.Exists(_highScorePath), Is.True,
                "Tệp Kỷ_Lục phải tồn tại sau khi fallback ghi default.");
            string after = File.ReadAllText(_highScorePath);
            Assert.That(after, Does.Contain("\"highScore\""),
                "Tệp sau fallback phải chứa khóa 'highScore'.");
            Assert.That(after, Does.Contain("0"),
                "Tệp sau fallback phải lưu giá trị mặc định 0.");

            // Assert (3): ít nhất một cảnh báo quan sát được qua ILogSink.
            Assert.That(log.Warnings, Is.Not.Empty,
                $"ILogSink.Warn phải được gọi ít nhất một lần cho payload hỏng: '{payload}'.");
        }

        [Test]
        public void MissingFileReturnsDefaultWithoutWarning()
        {
            // Hợp đồng đối ngẫu: thiếu tệp KHÔNG phải lỗi (Requirement 8.6),
            // ReadHighScore trả 0 mà KHÔNG phát warning (đó là trạng thái khởi tạo
            // hợp lệ, không phải dữ liệu hỏng theo Property 21).
            Assert.That(File.Exists(_highScorePath), Is.False);

            var log = new TestLogSink();
            var service = new UnityStorageService(log);

            long result = service.ReadHighScore();

            Assert.That(result, Is.EqualTo(0L));
            Assert.That(log.Warnings, Is.Empty,
                "Tệp chưa từng tồn tại không phải dữ liệu hỏng — không cần cảnh báo.");
        }
    }
}
