// Feature: tower-defense-vn, Task 13.3 - PlayMode integration smoke test
// Validates: Requirements 1.5, 2.4, 7.2, 7.4, 11.5
//
// Smoke kịch bản end-to-end ở mức Core thuần (không load SampleScene): nạp
// `enemies.json` + `waves.json` từ StreamingAssets, dựng `WaveScheduler`, drive
// `Tick(...)` với `aliveEnemies = 0` để mô phỏng "Quái bị tiêu diệt ngay khi
// spawn" và xác nhận:
//   1. Tổng `SpawnIntent` phát ra cho Đợt 1 = Σ count (Property 15 / Req 7.2).
//   2. Sau khi cạn spawn entries + queue rỗng + alive = 0, `State` chuyển sang
//      `WaveState.Cleared` (Req 1.5 - vòng đời Đợt; Req 2.4 - Quái biến mất khi
//      Máu_Quái về 0).
//   3. `OnWaveCleared()` tăng `CurrentWave` đúng 1 (Req 7.4).
//
// Hai test còn lại kiểm tra "presence checks" mà task yêu cầu:
//   - `HudUxmlRegionsArePresent`: HUD.uxml chứa đủ 6 vùng anchor (Req 9.x);
//     bổ trợ cho task ghi chú "Kiểm tra HUD region presence".
//   - `AudioBgmClipPathExists`: `AudioService` có SerializeField `_traditionalBgm`
//     để phát BGM nhạc cụ truyền thống (Req 11.5).
//
// Task không yêu cầu tự load scene SampleScene (vì sẽ phụ thuộc nặng vào prefab
// pipeline đang được wire ở Tasks 12.x). Phạm vi gói gọn trong Core scheduler
// và presence check ở mức asset.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CSVH.Core.Config;
using CSVH.Core.Wave;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CSVH.Tests.Play.Integration
{
    /// <summary>
    /// Smoke test "một Đợt end-to-end" cho lớp Core: từ JSON → scheduler →
    /// state chuyển sang Cleared → CurrentWave tăng. Bổ sung hai test kiểm
    /// tra "presence" cho HUD UXML regions và AudioService BGM slot.
    /// </summary>
    [TestFixture]
    public class OneWaveSmoke
    {
        /// <summary>
        /// Drive scheduler giả định mọi Quái bị tiêu diệt tức thời (alive = 0)
        /// để cô lập logic spawn + state machine. Yêu cầu:
        ///   - Tổng <see cref="SpawnIntent"/> phát ra trong Đợt 1 = Σ count.
        ///   - <see cref="WaveScheduler.State"/> kết thúc bằng <see cref="WaveState.Cleared"/>.
        ///   - <see cref="WaveScheduler.OnWaveCleared"/> tăng <see cref="WaveScheduler.CurrentWave"/> đúng 1.
        /// </summary>
        [UnityTest]
        public IEnumerator OneWaveCompletesAndIncrementsCurrentWave()
        {
            // 1. Nạp JSON từ StreamingAssets — dùng đúng đường dẫn runtime mà
            //    GameSceneRoot dùng ở production (đảm bảo asset thật được parse).
            var streaming = Application.streamingAssetsPath;
            var wavesPath = Path.Combine(streaming, "waves.json");
            var enemiesPath = Path.Combine(streaming, "enemies.json");

            Assert.IsTrue(File.Exists(wavesPath), $"Thiếu waves.json tại {wavesPath}.");
            Assert.IsTrue(File.Exists(enemiesPath), $"Thiếu enemies.json tại {enemiesPath}.");

            var wavesJson = File.ReadAllText(wavesPath);
            var enemiesJson = File.ReadAllText(enemiesPath);

            var loader = new ConfigLoader();
            var result = loader.Load(wavesJson, enemiesJson);
            Assert.IsTrue(result.IsOk,
                $"ConfigLoader.Load thất bại: {(result.IsErr ? result.Error.Message : "")}.");

            var bundle = result.Value;
            Assert.GreaterOrEqual(bundle.Waves.Count, 5,
                "Cấu hình mặc định phải có ít nhất 5 Đợt (Đợt 1..5 theo Req 7.6/7.7).");

            // 2. Dựng scheduler — mirror những gì GameSceneRoot làm khi nối dây
            //    Core với view layer, nhưng không tạo prefab.
            var enemiesById = new Dictionary<string, EnemyConfig>();
            foreach (var e in bundle.Enemies) enemiesById[e.Id] = e;

            var scheduler = new WaveScheduler(bundle.Waves, enemiesById);
            scheduler.Start();
            int initialWave = scheduler.CurrentWave;
            Assert.AreEqual(1, initialWave, "Scheduler phải bắt đầu ở CurrentWave = 1 (Req 7.4).");

            // 3. Drive scheduler từng frame với dt = 0.1s và aliveEnemies = 0.
            //    Khi mọi spawn entry đã phát hết và queue rỗng, scheduler tự
            //    chuyển sang Cleared (Req 2.4 mô phỏng: Quái "biến mất" tức thời).
            int totalEmitted = 0;
            int frames = 0;
            const int maxFrames = 5000; // Bao trùm Pha_Chuẩn_Bị + Đợt 1 (≈10s + ~10s).
            const float dt = 0.1f;

            while (scheduler.State != WaveState.Cleared && frames < maxFrames)
            {
                var intents = scheduler.Tick(dt, aliveEnemies: 0, spawnCap: 200);
                totalEmitted += intents.Count;
                frames++;

                // Yield định kỳ để test runner co-op (tránh giữ main thread).
                if (frames % 100 == 0) yield return null;
            }

            Assert.That(scheduler.State, Is.EqualTo(WaveState.Cleared),
                $"Scheduler phải chuyển sang Cleared trong vòng {maxFrames} frame; state hiện tại = {scheduler.State}, frames = {frames}.");

            // 4. Tổng emitted phải khớp Σ count của Đợt 1 (Property 15).
            int expected = 0;
            foreach (var s in bundle.Waves[0].Spawns) expected += s.Count;
            Assert.That(totalEmitted, Is.EqualTo(expected),
                "Tổng số SpawnIntent phát trong Đợt 1 phải bằng Σ count (Property 15).");

            // 5. OnWaveCleared phải tăng CurrentWave đúng 1 (Req 7.4 / Property 16).
            scheduler.OnWaveCleared();
            Assert.That(scheduler.CurrentWave, Is.EqualTo(initialWave + 1),
                "CurrentWave phải tăng đúng 1 sau khi Đợt 1 hoàn tất.");
            Assert.That(scheduler.State, Is.EqualTo(WaveState.Preparing),
                "Sau OnWaveCleared, scheduler phải vào Pha_Chuẩn_Bị cho Đợt kế (Req 7.2).");

            yield return null;
        }

        /// <summary>
        /// Kiểm tra HUD.uxml có đủ 6 vùng anchor (TopLeft, TopCenter, TopRight,
        /// BottomLeft, BottomCenter, BottomRight) — Req 9.1..9.6. Đây là
        /// "presence check" theo task 13.3 (Property 24 đã đảm nhận layout).
        /// Test chỉ chạy trong Editor (cần AssetDatabase); player build coi
        /// như Inconclusive.
        /// </summary>
        [Test]
        public void HudUxmlRegionsArePresent()
        {
#if UNITY_EDITOR
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/CSVH/Game/UI/HUD.uxml");
            Assert.IsNotNull(uxml, "Không tìm thấy HUD.uxml ở Assets/CSVH/Game/UI/.");

            var tree = uxml.CloneTree();
            string[] regions =
            {
                "TopLeft", "TopCenter", "TopRight",
                "BottomLeft", "BottomCenter", "BottomRight",
            };
            foreach (var name in regions)
            {
                Assert.IsNotNull(tree.Q<VisualElement>(name),
                    $"HUD.uxml thiếu vùng '{name}' (Req 9.x).");
            }
#else
            Assert.Inconclusive("HudUxmlRegionsArePresent cần AssetDatabase (Editor PlayMode).");
#endif
        }

        /// <summary>
        /// Kiểm tra <see cref="CSVH.Game.Audio.AudioService"/> expose
        /// SerializeField <c>_traditionalBgm</c> để phát BGM nhạc cụ truyền
        /// thống Việt Nam trong các Đợt thường (Req 11.5). Dùng reflection
        /// để không phụ thuộc vào việc instance hóa MonoBehaviour ngoài scene.
        /// </summary>
        [Test]
        public void AudioBgmClipPathExists()
        {
            var t = typeof(CSVH.Game.Audio.AudioService);
            var bgm = t.GetField(
                "_traditionalBgm",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(bgm,
                "AudioService phải expose SerializeField _traditionalBgm cho BGM truyền thống (Req 11.5).");
            Assert.AreEqual(typeof(AudioClip), bgm.FieldType,
                "_traditionalBgm phải có kiểu AudioClip để gán nhạc cụ truyền thống Việt Nam.");
        }
    }
}
