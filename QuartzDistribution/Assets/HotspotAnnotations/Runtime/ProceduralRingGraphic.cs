using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution.HotspotAnnotations
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProceduralRingGraphic : MaskableGraphic
    {
        public enum RingStyle { Solid, Dashes, Ticks }

        [SerializeField] private RingStyle style = RingStyle.Solid;
        [SerializeField, Min(3)] private int elementCount = 48;
        [SerializeField, Min(0.5f)] private float thickness = 3f;
        [SerializeField, Range(0.05f, 1f)] private float fillRatio = 0.55f;
        [SerializeField, Range(0.1f, 1f)] private float radiusRatio = 0.9f;
        [SerializeField, Min(8)] private int solidSegments = 96;

        public RingStyle Style { get => style; set { style = value; SetVerticesDirty(); } }
        public int ElementCount { get => elementCount; set { elementCount = Mathf.Max(3, value); SetVerticesDirty(); } }
        public float Thickness { get => thickness; set { thickness = Mathf.Max(0.5f, value); SetVerticesDirty(); } }
        public float FillRatio { get => fillRatio; set { fillRatio = Mathf.Clamp(value, 0.05f, 1f); SetVerticesDirty(); } }
        public float RadiusRatio { get => radiusRatio; set { radiusRatio = Mathf.Clamp01(value); SetVerticesDirty(); } }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float radius = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.5f * radiusRatio);
            int count = style == RingStyle.Solid ? Mathf.Max(8, solidSegments) : Mathf.Max(3, elementCount);
            float step = Mathf.PI * 2f / count;
            float arc = style == RingStyle.Solid ? step : step * fillRatio;

            for (int i = 0; i < count; i++)
            {
                float start = i * step;
                float end = start + arc;
                AddArcQuad(vh, start, end, radius - thickness * 0.5f, radius + thickness * 0.5f);
            }
        }

        private void AddArcQuad(VertexHelper vh, float start, float end, float innerRadius, float outerRadius)
        {
            int index = vh.currentVertCount;
            Vector2 innerStart = new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * innerRadius;
            Vector2 outerStart = new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * outerRadius;
            Vector2 innerEnd = new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * innerRadius;
            Vector2 outerEnd = new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * outerRadius;
            Color32 c = color;

            vh.AddVert(innerStart, c, Vector2.zero);
            vh.AddVert(outerStart, c, Vector2.up);
            vh.AddVert(outerEnd, c, Vector2.one);
            vh.AddVert(innerEnd, c, Vector2.right);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
