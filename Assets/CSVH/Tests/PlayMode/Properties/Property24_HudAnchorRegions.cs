// Feature: tower-defense-vn, Property 24: HUD giữ vùng anchor khi đổi độ phân giải
// Validates: Requirements 9.7
//
// Property 24 (design.md): For any kích thước màn hình `(w, h)` trong
// `[640..3840] × [480..2160]`, sau khi HUD được layout (UXML + USS), mỗi vùng
// (TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight) có hộp
// chữ nhật giới hạn nằm trong góc/cạnh tương ứng (ví dụ TopLeft thỏa
// `xmin < w/3 ∧ ymin < h/3`) trong sai số 5%.
//
// Test này chạy trong PlayMode vì UI Toolkit layout chỉ được giải quyết bởi
// UIDocument/PanelSettings + Panel runtime của UnityEngine.UIElements (không
// truy cập được trong EditMode unit test thuần). FsCheck PBT (Editor-only) cũng
// không khả dụng tại đây; phạm vi đầu vào hữu hạn (5 mức độ phân giải đại
// diện) bao phủ hai biên 640×480 và 3840×2160 cùng các bậc HD/FullHD/QHD.
//
// Layout strategy: thay vì bắt buộc GameView phải đúng kích thước (vốn không
// reliable trong test runner), test dựng một VisualElement con cố định
// (w × h) trong UIDocument.rootVisualElement, clone HUD.uxml vào, gắn HUD.uss,
// rồi kiểm tra worldBound của 6 vùng theo toạ độ container. CSS của HUD dùng
// width/height: 33% nên các vùng tự đặt theo kích thước container, độc lập
// kích thước GameView.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CSVH.Tests.Play.Properties
{
    /// <summary>
    /// Property 24: với mọi kích thước (w, h) ∈ [640..3840] × [480..2160], các
    /// vùng anchor của HUD nằm trong ô lưới 1/3 × 1/3 tương ứng (sai số 5%).
    /// </summary>
    [TestFixture]
    public class Property24_HudAnchorRegions
    {
        // Sai số tương đối 5% theo trục — đặc tả Property 24.
        private const float Tolerance = 0.05f;

        // Năm cặp độ phân giải đại diện cho [640..3840] × [480..2160]:
        // - Biên dưới 640×480 (VGA)
        // - Bậc trung gian HD (1280×720), FullHD (1920×1080), QHD (2560×1440)
        // - Biên trên 4K UHD (3840×2160)
        // Mỗi case là một [UnityTest] riêng để Test Runner báo cáo độc lập;
        // [TestCase] kết hợp với [UnityTest] có hành vi khác nhau giữa các
        // phiên bản UTF nên ta tránh phụ thuộc vào nó.

        [UnityTest]
        public IEnumerator AnchorRegionsStayInQuadrants_640x480()
        {
            yield return RunForResolution(640, 480);
        }

        [UnityTest]
        public IEnumerator AnchorRegionsStayInQuadrants_1280x720()
        {
            yield return RunForResolution(1280, 720);
        }

        [UnityTest]
        public IEnumerator AnchorRegionsStayInQuadrants_1920x1080()
        {
            yield return RunForResolution(1920, 1080);
        }

        [UnityTest]
        public IEnumerator AnchorRegionsStayInQuadrants_2560x1440()
        {
            yield return RunForResolution(2560, 1440);
        }

        [UnityTest]
        public IEnumerator AnchorRegionsStayInQuadrants_3840x2160()
        {
            yield return RunForResolution(3840, 2160);
        }

        private static IEnumerator RunForResolution(int w, int h)
        {
            // ARRANGE — dựng UIDocument runtime với PanelSettings (ConstantPixelSize)
            // và một container con cố định (w × h) trong rootVisualElement.
            var go = new GameObject("HUD_Property24");
            PanelSettings ps = null;
            try
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.scaleMode = PanelScaleMode.ConstantPixelSize;

                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = ps;

                // Đợi một frame để UIDocument tạo rootVisualElement gắn vào panel.
                yield return null;

                var root = doc.rootVisualElement;
                Assert.IsNotNull(root,
                    "UIDocument.rootVisualElement phải tồn tại sau khi gán PanelSettings.");

                // Container đặt absolute kích thước (w,h) — biến độ phân giải test
                // thành "kích thước canvas" mà CSS percent (33%) sẽ tính theo đó.
                var container = new VisualElement { name = "TestContainer" };
                container.style.position = Position.Absolute;
                container.style.left = 0;
                container.style.top = 0;
                container.style.width = w;
                container.style.height = h;
                // Không cho container co/giãn theo flex của parent.
                container.style.flexGrow = 0;
                container.style.flexShrink = 0;
                root.Add(container);

                VisualTreeAsset uxml = null;
                StyleSheet uss = null;
#if UNITY_EDITOR
                uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/CSVH/Game/UI/HUD.uxml");
                uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/CSVH/Game/UI/HUD.uss");
#else
                // Property 24 dựa vào AssetDatabase để load HUD.uxml/.uss; trong
                // player build không có editor API nên test này không thể chạy.
                Assert.Inconclusive("Property24 cần Editor PlayMode (AssetDatabase).");
                yield break;
#endif
                Assert.IsNotNull(uxml, "Không tìm thấy HUD.uxml ở Assets/CSVH/Game/UI/.");
                Assert.IsNotNull(uss, "Không tìm thấy HUD.uss ở Assets/CSVH/Game/UI/.");

                uxml.CloneTree(container);
                container.styleSheets.Add(uss);

                // Hai frame để UIElements panel hoàn tất style + layout pass.
                yield return null;
                yield return null;

                // ACT/ASSERT — đọc worldBound của container và 6 vùng. Mỗi vùng
                // được kiểm tra: hộp chữ nhật giới hạn (sau khi quy về toạ độ
                // container) nằm trong ô 1/3 × 1/3 tương ứng với sai số 5%.
                Vector2 origin = container.worldBound.position;

                var topLeft = container.Q<VisualElement>("TopLeft");
                var topCenter = container.Q<VisualElement>("TopCenter");
                var topRight = container.Q<VisualElement>("TopRight");
                var bottomLeft = container.Q<VisualElement>("BottomLeft");
                var bottomCenter = container.Q<VisualElement>("BottomCenter");
                var bottomRight = container.Q<VisualElement>("BottomRight");

                Assert.IsNotNull(topLeft, "Vùng TopLeft phải có trong HUD.uxml (Req 9.1).");
                Assert.IsNotNull(topCenter, "Vùng TopCenter phải có trong HUD.uxml (Req 9.1).");
                Assert.IsNotNull(topRight, "Vùng TopRight phải có trong HUD.uxml (Req 9.2).");
                Assert.IsNotNull(bottomLeft, "Vùng BottomLeft phải có trong HUD.uxml (Req 9.4).");
                Assert.IsNotNull(bottomCenter, "Vùng BottomCenter phải có trong HUD.uxml (Req 9.5).");
                Assert.IsNotNull(bottomRight, "Vùng BottomRight phải có trong HUD.uxml (Req 9.6).");

                float third = w * (1f / 3f);
                float twoThirds = w * (2f / 3f);
                float thirdH = h * (1f / 3f);
                float twoThirdsH = h * (2f / 3f);

                AssertRegion("TopLeft", topLeft, origin,
                    xMinExpected: 0f, xMaxExpected: third,
                    yMinExpected: 0f, yMaxExpected: thirdH,
                    w: w, h: h);

                AssertRegion("TopCenter", topCenter, origin,
                    xMinExpected: third, xMaxExpected: twoThirds,
                    yMinExpected: 0f, yMaxExpected: thirdH,
                    w: w, h: h);

                AssertRegion("TopRight", topRight, origin,
                    xMinExpected: twoThirds, xMaxExpected: w,
                    yMinExpected: 0f, yMaxExpected: thirdH,
                    w: w, h: h);

                AssertRegion("BottomLeft", bottomLeft, origin,
                    xMinExpected: 0f, xMaxExpected: third,
                    yMinExpected: twoThirdsH, yMaxExpected: h,
                    w: w, h: h);

                AssertRegion("BottomCenter", bottomCenter, origin,
                    xMinExpected: third, xMaxExpected: twoThirds,
                    yMinExpected: twoThirdsH, yMaxExpected: h,
                    w: w, h: h);

                AssertRegion("BottomRight", bottomRight, origin,
                    xMinExpected: twoThirds, xMaxExpected: w,
                    yMinExpected: twoThirdsH, yMaxExpected: h,
                    w: w, h: h);
            }
            finally
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
                if (ps != null)
                {
                    Object.DestroyImmediate(ps);
                }
            }
        }

        /// <summary>
        /// Kiểm tra worldBound của một vùng (sau khi quy về toạ độ container)
        /// nằm trong khung kỳ vọng (xMinExpected..xMaxExpected,
        /// yMinExpected..yMaxExpected) với sai số 5% theo từng trục — đặc tả
        /// Property 24, "trong sai số 5%".
        /// </summary>
        private static void AssertRegion(
            string name, VisualElement region, Vector2 origin,
            float xMinExpected, float xMaxExpected,
            float yMinExpected, float yMaxExpected,
            float w, float h)
        {
            float tolW = w * Tolerance;
            float tolH = h * Tolerance;

            Rect wb = region.worldBound;
            float xMin = wb.xMin - origin.x;
            float xMax = wb.xMax - origin.x;
            float yMin = wb.yMin - origin.y;
            float yMax = wb.yMax - origin.y;

            Assert.That(xMin, Is.InRange(xMinExpected - tolW, xMinExpected + tolW),
                $"{name}: xMin = {xMin:F2} ngoài sai số 5% quanh {xMinExpected:F2} (w={w}).");
            Assert.That(xMax, Is.InRange(xMaxExpected - tolW, xMaxExpected + tolW),
                $"{name}: xMax = {xMax:F2} ngoài sai số 5% quanh {xMaxExpected:F2} (w={w}).");
            Assert.That(yMin, Is.InRange(yMinExpected - tolH, yMinExpected + tolH),
                $"{name}: yMin = {yMin:F2} ngoài sai số 5% quanh {yMinExpected:F2} (h={h}).");
            Assert.That(yMax, Is.InRange(yMaxExpected - tolH, yMaxExpected + tolH),
                $"{name}: yMax = {yMax:F2} ngoài sai số 5% quanh {yMaxExpected:F2} (h={h}).");
        }
    }
}
