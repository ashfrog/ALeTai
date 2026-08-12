using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution.HotspotAnnotations
{
    [RequireComponent(typeof(CanvasRenderer))]
    [DefaultExecutionOrder(0)]
    public sealed class OrthogonalLiveLine : MaskableGraphic
    {
        public enum AttachmentSide { Left, Right, Top, Bottom }

        [Header("Anchors")]
        [SerializeField] private RectTransform startAnchor;
        [SerializeField] private RectTransform endAnchor;
        [SerializeField] private Canvas parentCanvas;

        [Header("Appearance")]
        [SerializeField, Min(0.5f)] private float lineWidth = 2.5f;
        [SerializeField] private bool showCornerDots = true;
        [SerializeField, Min(1f)] private float cornerDotSize = 8f;
        [SerializeField, Min(1f)] private float edgeInset = 4f;

        [Header("Grow animation")]
        [SerializeField, Min(0.01f)] private float growDuration = 0.65f;
        [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly List<Vector2> pathPoints = new List<Vector2>(4);
        private readonly Vector3[] worldCorners = new Vector3[4];
        private float growProgress = 1f;
        private float growTimer;
        private bool isGrowing;
        private Vector2 previousStart;
        private Rect previousEndRect;

        public IReadOnlyList<Vector2> CurrentPath => pathPoints;
        public float GrowProgress => growProgress;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        }

        private void LateUpdate()
        {
            if (startAnchor == null || endAnchor == null)
            {
                if (pathPoints.Count > 0) { pathPoints.Clear(); SetVerticesDirty(); }
                return;
            }

            Vector2 start = WorldToLineLocal(startAnchor.TransformPoint(startAnchor.rect.center));
            Rect endRect = GetRectInLineSpace(endAnchor);
            if (isGrowing)
            {
                growTimer += Time.deltaTime;
                growProgress = Mathf.Clamp01(growTimer / Mathf.Max(0.01f, growDuration));
                if (growProgress >= 1f) isGrowing = false;
            }

            if ((start - previousStart).sqrMagnitude > 0.0001f || !RectsApproximatelyEqual(endRect, previousEndRect) || isGrowing)
            {
                RebuildPath(start, endRect);
                previousStart = start;
                previousEndRect = endRect;
                SetVerticesDirty();
            }
        }

        public void SetAnchors(RectTransform start, RectTransform end)
        {
            startAnchor = start;
            endAnchor = end;
            pathPoints.Clear();
            SetVerticesDirty();
        }

        public void Configure(RectTransform start, RectTransform end, Canvas canvas)
        {
            startAnchor = start;
            endAnchor = end;
            parentCanvas = canvas;
            SetAllDirty();
        }

        public void PlayGrowOnce()
        {
            growTimer = 0f;
            growProgress = 0f;
            isGrowing = true;
            SetVerticesDirty();
        }

        public void ShowImmediately()
        {
            growTimer = growDuration;
            growProgress = 1f;
            isGrowing = false;
            SetVerticesDirty();
        }

        public void RebuildPath(Vector2 start, Rect cardRect)
        {
            AttachmentSide side = ChooseAttachmentSide(start, cardRect);
            List<Vector2> built = BuildOrthogonalPath(start, cardRect, side, edgeInset);
            pathPoints.Clear();
            pathPoints.AddRange(built);
        }

        public static AttachmentSide ChooseAttachmentSide(Vector2 start, Rect cardRect)
        {
            Vector2 center = cardRect.center;
            Vector2 delta = start - center;
            float normalizedX = Mathf.Abs(delta.x) / Mathf.Max(1f, cardRect.width);
            float normalizedY = Mathf.Abs(delta.y) / Mathf.Max(1f, cardRect.height);
            if (normalizedX >= normalizedY)
                return delta.x < 0f ? AttachmentSide.Left : AttachmentSide.Right;
            return delta.y < 0f ? AttachmentSide.Bottom : AttachmentSide.Top;
        }

        public static List<Vector2> BuildOrthogonalPath(Vector2 start, Rect cardRect, AttachmentSide side, float inset)
        {
            Vector2 end;
            switch (side)
            {
                case AttachmentSide.Left: end = new Vector2(cardRect.xMin - inset, Mathf.Clamp(start.y, cardRect.yMin, cardRect.yMax)); break;
                case AttachmentSide.Right: end = new Vector2(cardRect.xMax + inset, Mathf.Clamp(start.y, cardRect.yMin, cardRect.yMax)); break;
                case AttachmentSide.Top: end = new Vector2(Mathf.Clamp(start.x, cardRect.xMin, cardRect.xMax), cardRect.yMax + inset); break;
                default: end = new Vector2(Mathf.Clamp(start.x, cardRect.xMin, cardRect.xMax), cardRect.yMin - inset); break;
            }

            List<Vector2> points = new List<Vector2>(4) { start };
            if (side == AttachmentSide.Left || side == AttachmentSide.Right)
            {
                float midX = (start.x + end.x) * 0.5f;
                AddIfDistinct(points, new Vector2(midX, start.y));
                AddIfDistinct(points, new Vector2(midX, end.y));
            }
            else
            {
                float midY = (start.y + end.y) * 0.5f;
                AddIfDistinct(points, new Vector2(start.x, midY));
                AddIfDistinct(points, new Vector2(end.x, midY));
            }
            AddIfDistinct(points, end);
            return points;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (pathPoints.Count < 2) return;

            float total = 0f;
            for (int i = 0; i < pathPoints.Count - 1; i++) total += Vector2.Distance(pathPoints[i], pathPoints[i + 1]);
            float visibleLength = total * growCurve.Evaluate(growProgress);
            float consumed = 0f;

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Vector2 from = pathPoints[i];
                Vector2 to = pathPoints[i + 1];
                float length = Vector2.Distance(from, to);
                float shown = Mathf.Clamp(visibleLength - consumed, 0f, length);
                if (shown > 0.001f)
                {
                    Vector2 shownTo = Vector2.Lerp(from, to, shown / Mathf.Max(0.001f, length));
                    AddLineQuad(vh, from, shownTo, lineWidth, color);
                }
                consumed += length;
            }

            if (!showCornerDots) return;
            consumed = 0f;
            for (int i = 1; i < pathPoints.Count - 1; i++)
            {
                consumed += Vector2.Distance(pathPoints[i - 1], pathPoints[i]);
                if (visibleLength + 0.01f >= consumed)
                    AddCircle(vh, pathPoints[i], cornerDotSize * 0.5f, color, 16);
            }
        }

        private Vector2 WorldToLineLocal(Vector3 worldPosition)
        {
            Camera eventCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera
                : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, eventCamera, out Vector2 local);
            return local;
        }

        private Rect GetRectInLineSpace(RectTransform target)
        {
            target.GetWorldCorners(worldCorners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = WorldToLineLocal(worldCorners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void AddIfDistinct(List<Vector2> points, Vector2 value)
        {
            if (points.Count == 0 || (points[points.Count - 1] - value).sqrMagnitude > 0.25f)
                points.Add(value);
        }

        private static bool RectsApproximatelyEqual(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f
                   && Mathf.Abs(a.width - b.width) < 0.01f && Mathf.Abs(a.height - b.height) < 0.01f;
        }

        private static void AddLineQuad(VertexHelper vh, Vector2 from, Vector2 to, float width, Color32 color)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            int index = vh.currentVertCount;
            vh.AddVert(from - normal, color, Vector2.zero);
            vh.AddVert(from + normal, color, Vector2.up);
            vh.AddVert(to + normal, color, Vector2.one);
            vh.AddVert(to - normal, color, Vector2.right);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddCircle(VertexHelper vh, Vector2 center, float radius, Color32 color, int segments)
        {
            int centerIndex = vh.currentVertCount;
            vh.AddVert(center, color, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(point, color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
        }
    }
}
