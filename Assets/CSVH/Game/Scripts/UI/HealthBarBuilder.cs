// Feature: tower-defense-vn — Shared health-bar builder for Enemy/Tower views.
// Extracts the runtime construction of two SpriteRenderer quads (background + fill)
// so both EnemyHealthBarView and TowerHealthBarView can reuse the same layout pattern.

using UnityEngine;

namespace CSVH.Game.UI
{
    /// <summary>
    /// Handle bất biến tới các <see cref="SpriteRenderer"/> con của một thanh máu vừa
    /// được dựng tại runtime. Caller giữ handle này để cập nhật <c>scale.x</c> của
    /// <see cref="FillTransform"/> mỗi frame theo tỉ lệ HP.
    /// </summary>
    public sealed class HealthBarHandle
    {
        public SpriteRenderer Background { get; }
        public SpriteRenderer Fill { get; }
        public Transform FillTransform { get; }
        public float Width { get; }

        internal HealthBarHandle(SpriteRenderer bg, SpriteRenderer fill, Transform fillTransform, float width)
        {
            Background = bg;
            Fill = fill;
            FillTransform = fillTransform;
            Width = width;
        }

        /// <summary>
        /// Bật/tắt cả hai SpriteRenderer mà không destroy GameObject. Idempotent.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (Background != null && Background.enabled != visible) Background.enabled = visible;
            if (Fill != null && Fill.enabled != visible) Fill.enabled = visible;
        }

        /// <summary>
        /// Cập nhật fill theo <paramref name="ratio"/> ∈ [0,1]: scale.x = Width × ratio,
        /// đồng thời shift localPosition.x để mép trái fill quad luôn dán vào FillPivot.
        /// </summary>
        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            float scaleX = Width * ratio;

            var s = FillTransform.localScale;
            s.x = scaleX;
            FillTransform.localScale = s;

            var p = FillTransform.localPosition;
            p.x = scaleX * 0.5f;
            FillTransform.localPosition = p;
        }
    }

    /// <summary>
    /// Helper static dựng cấu trúc thanh máu giống nhau cho Enemy/Tower:
    /// <c>HealthBarAnchor → Bg + FillPivot/Fill</c>. Sprite trắng 1×1 dùng chung
    /// (không cấp phát Texture cho mỗi thanh) — chỉ tô màu qua
    /// <see cref="SpriteRenderer.color"/>.
    /// </summary>
    public static class HealthBarBuilder
    {
        // Sprite trắng 1×1 dùng chung cho mọi thanh máu trong cảnh.
        private static Sprite _sharedQuadSprite;

        /// <summary>
        /// Dựng thanh máu là child của <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">Transform Quái/Thành mà thanh máu sẽ neo theo.</param>
        /// <param name="width">Chiều rộng thanh (đơn vị world).</param>
        /// <param name="height">Chiều cao thanh.</param>
        /// <param name="verticalOffset">Khoảng cách dọc từ tâm parent lên anchor.</param>
        /// <param name="sortingOrder">Sorting order cho cả background; fill = +1.</param>
        /// <param name="initialColor">Màu khởi tạo của fill (vd. xanh lá khi HP đầy).</param>
        public static HealthBarHandle Build(
            Transform parent,
            float width,
            float height,
            float verticalOffset,
            int sortingOrder,
            Color initialColor)
        {
            EnsureSharedSprite();

            // Anchor neo phía trên đầu chủ thể.
            var anchor = new GameObject("HealthBarAnchor").transform;
            anchor.SetParent(parent, worldPositionStays: false);
            anchor.localPosition = new Vector3(0f, verticalOffset, 0f);
            anchor.localScale = Vector3.one;

            // Background full-width, pivot center.
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(anchor, worldPositionStays: false);
            bgGo.transform.localPosition = Vector3.zero;
            bgGo.transform.localScale = new Vector3(width, height, 1f);
            var bgRenderer = bgGo.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = _sharedQuadSprite;
            bgRenderer.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            bgRenderer.sortingOrder = sortingOrder;

            // Pivot ở mép trái thanh — gốc anchor cho fill quad (giúp fill co từ phải sang trái).
            var fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(anchor, worldPositionStays: false);
            fillPivot.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
            fillPivot.localScale = Vector3.one;

            // Fill quad: pivot center → cần dịch +scale.x/2 mỗi frame để mép trái dán pivot.
            var fillGo = new GameObject("Fill");
            var fillTransform = fillGo.transform;
            fillTransform.SetParent(fillPivot, worldPositionStays: false);
            fillTransform.localPosition = new Vector3(width * 0.5f, 0f, 0f);
            fillTransform.localScale = new Vector3(width, height * 0.85f, 1f);
            var fillRenderer = fillGo.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = _sharedQuadSprite;
            fillRenderer.color = initialColor;
            fillRenderer.sortingOrder = sortingOrder + 1;

            return new HealthBarHandle(bgRenderer, fillRenderer, fillTransform, width);
        }

        /// <summary>
        /// Bảng màu HP chung: xanh lá (đầy) → vàng (50%) → đỏ (cạn). Allocation-free.
        /// </summary>
        public static Color HpColor(float ratio)
        {
            var green = new Color(0.2f, 0.85f, 0.3f, 1f);
            var yellow = new Color(0.95f, 0.85f, 0.2f, 1f);
            var red = new Color(0.9f, 0.2f, 0.2f, 1f);

            if (ratio >= 0.5f)
            {
                float t = (1f - ratio) / 0.5f;
                return Color.Lerp(green, yellow, t);
            }
            else
            {
                float t = (0.5f - ratio) / 0.5f;
                return Color.Lerp(yellow, red, t);
            }
        }

        // Sinh sprite trắng 1×1 dùng chung cho mọi instance — tránh cấp phát Texture mỗi thanh.
        private static void EnsureSharedSprite()
        {
            if (_sharedQuadSprite != null) return;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            _sharedQuadSprite = Sprite.Create(
                tex,
                new Rect(0, 0, 1, 1),
                pivot: new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
            _sharedQuadSprite.hideFlags = HideFlags.HideAndDontSave;
            _sharedQuadSprite.name = "HealthBar_Quad";
        }
    }
}
