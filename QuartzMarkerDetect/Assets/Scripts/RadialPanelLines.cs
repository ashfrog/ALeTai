using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>从扫描圆边缘斜向生长，再经一个拐点水平连接到多个图片面板。</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RadialPanelLines : MaskableGraphic
{
    [SerializeField] private RectTransform centerAnchor;
    [SerializeField] private RectTransform[] panelTargets;
    [SerializeField, Min(0.5f)] private float lineWidth = 3f;
    [SerializeField, Range(20f, 70f)] private float diagonalAngle = 45f;
    [SerializeField, Min(1f)] private float diagonalLength = 34f;
    [SerializeField, Min(0f)] private float minimumHorizontalLength = 24f;
    [SerializeField, Min(0.01f)] private float growDuration = 0.75f;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly List<List<Vector2>> paths = new List<List<Vector2>>();
    private readonly Vector3[] corners = new Vector3[4];
    private float growTimer;
    private bool growing;

    public float GrowProgress { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        GrowProgress = 0f;
    }

    private void LateUpdate()
    {
        RebuildPaths();
        if (growing)
        {
            growTimer += Time.unscaledDeltaTime;
            GrowProgress = Mathf.Clamp01(growTimer / growDuration);
            if (GrowProgress >= 1f) growing = false;
        }
        SetVerticesDirty();
    }

    public void Configure(RectTransform center, RectTransform[] targets)
    {
        centerAnchor = center;
        panelTargets = targets;
        RebuildPaths();
        SetAllDirty();
    }

    /// <summary>立即按当前叶子节点位置重建分支线。</summary>
    public void RebuildNow()
    {
        RebuildPaths();
        SetVerticesDirty();
    }

    public void PlayGrow()
    {
        growTimer = 0f;
        GrowProgress = 0f;
        growing = true;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float progress = growCurve.Evaluate(GrowProgress);
        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            DrawPath(vh, paths[pathIndex], progress);
    }

    private void RebuildPaths()
    {
        paths.Clear();
        if (centerAnchor == null || panelTargets == null) return;
        Vector2 center = WorldToLocal(centerAnchor.TransformPoint(centerAnchor.rect.center));
        float ringRadius = GetRingRadius(center);
        for (int i = 0; i < panelTargets.Length; i++)
        {
            if (panelTargets[i] == null) continue;
            Rect panelRect = GetLocalRect(panelTargets[i]);
            float horizontalSign = panelRect.center.x >= center.x ? 1f : -1f;
            float verticalSign = panelRect.center.y >= center.y ? 1f : -1f;

            // 连接点始终落在面板朝向圆圈的一侧，最后一段因此保持水平。
            // 起点位于扫描圆周。默认短 45° 斜线与水平线构成约 135° 的小夹角。
            float angle = diagonalAngle * Mathf.Deg2Rad;
            Vector2 startDirection = new Vector2(
                horizontalSign * Mathf.Cos(angle),
                verticalSign * Mathf.Sin(angle));
            Vector2 start = center + startDirection * ringRadius;

            Vector2 desiredCorner = start + startDirection * diagonalLength;
            float connectionY = Mathf.Clamp(desiredCorner.y, panelRect.yMin, panelRect.yMax);
            Vector2 end = new Vector2(horizontalSign > 0f ? panelRect.xMin : panelRect.xMax, connectionY);
            float verticalDistance = Mathf.Abs(connectionY - start.y);
            float diagonalRun = verticalDistance / Mathf.Max(0.01f, Mathf.Tan(angle));
            float availableRun = Mathf.Max(0f, horizontalSign * (end.x - start.x) - minimumHorizontalLength);
            diagonalRun = Mathf.Min(diagonalRun, availableRun);
            Vector2 corner = new Vector2(start.x + horizontalSign * diagonalRun, connectionY);

            List<Vector2> path = new List<Vector2> { start };
            AddDistinct(path, corner);
            AddDistinct(path, end);
            paths.Add(path);
        }
    }

    private float GetRingRadius(Vector2 center)
    {
        Vector2 localRight = WorldToLocal(centerAnchor.TransformPoint(
            centerAnchor.rect.center + Vector2.right * centerAnchor.rect.width * 0.5f));
        Vector2 localUp = WorldToLocal(centerAnchor.TransformPoint(
            centerAnchor.rect.center + Vector2.up * centerAnchor.rect.height * 0.5f));
        return Mathf.Min(Vector2.Distance(center, localRight), Vector2.Distance(center, localUp));
    }

    private void DrawPath(VertexHelper vh, List<Vector2> path, float progress)
    {
        if (path.Count < 2) return;
        float total = 0f;
        for (int i = 0; i < path.Count - 1; i++) total += Vector2.Distance(path[i], path[i + 1]);
        float visible = total * progress;
        float consumed = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 from = path[i];
            Vector2 to = path[i + 1];
            float length = Vector2.Distance(from, to);
            float shown = Mathf.Clamp(visible - consumed, 0f, length);
            if (shown > 0.001f)
                AddLine(vh, from, Vector2.Lerp(from, to, shown / length));
            consumed += length;
        }
    }

    private Vector2 WorldToLocal(Vector3 world)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out Vector2 local);
        return local;
    }

    private Rect GetLocalRect(RectTransform target)
    {
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 point = WorldToLocal(corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void AddDistinct(List<Vector2> path, Vector2 point)
    {
        if ((path[path.Count - 1] - point).sqrMagnitude > 0.25f) path.Add(point);
    }

    private void AddLine(VertexHelper vh, Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * lineWidth * 0.5f;
        int index = vh.currentVertCount;
        Color32 c = color;
        vh.AddVert(from - normal, c, Vector2.zero);
        vh.AddVert(from + normal, c, Vector2.up);
        vh.AddVert(to + normal, c, Vector2.one);
        vh.AddVert(to - normal, c, Vector2.right);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

}
