// Feature: tower-defense-vn, Task 9.2 - Unity adapter cho ILogSink
// Validates: Requirements 12.4

using CSVH.Core.Logging;
using UnityEngine;

namespace CSVH.Game.Logging
{
    /// <summary>
    /// Adapter của <see cref="ILogSink"/> chuyển các lời gọi log sang
    /// <see cref="UnityEngine.Debug"/>. Dùng ở tầng Unity (CSVH.Game) để Core
    /// vẫn không phụ thuộc UnityEngine; tests có thể thay bằng sink in-memory
    /// để quan sát (Property 21 - fallback Bộ_Lưu_Trữ và Property 25 - cảnh báo
    /// khóa dịch thiếu).
    /// </summary>
    public sealed class UnityLogSink : ILogSink
    {
        /// <inheritdoc />
        public void Warn(string message) => Debug.LogWarning(message);

        /// <inheritdoc />
        public void Error(string message) => Debug.LogError(message);

        /// <inheritdoc />
        public void Info(string message) => Debug.Log(message);
    }
}
