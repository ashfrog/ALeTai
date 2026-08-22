using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>处理热点节点的移动、面板连线生长和显隐；MarKActions 不承担这些业务。</summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup), typeof(MarKActions))]
public sealed class MarkerPanelPresenter : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private MarKActions markerEvents;
    [SerializeField] private RadialPanelLines panelLines;
    [SerializeField] private CanvasGroup[] panelImages;
    [SerializeField] private Canvas parentCanvas;

    [Header("跟踪")]
    [SerializeField] private bool positionIsScreenPoint;
    [SerializeField] private bool useCombinedDisplayCoordinates;
    [SerializeField, Min(1f)] private float combinedDisplayWidth = 7680f;
    [SerializeField, Min(1f)] private float combinedDisplayHeight = 2160f;
    [SerializeField, Min(1f)] private float singleDisplayWidth = 3840f;
    [SerializeField, Min(0)] private int displayIndex;
    [SerializeField] private bool followRotation;
    [SerializeField, Min(0f)] private float followSpeed = 12f;

    [Header("显隐")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float panelRevealAt = 0.65f;

    [Header("展开叶子避让")]
    [Tooltip("只调整叶子面板和分支线，不移动 MarkerPanelGroup 根节点。")]
    [SerializeField] private bool autoArrangeLeaves = true;
    [Tooltip("对过小的叶子夹角进行小幅均分，保持原有布局方向。")]
    [SerializeField] private bool equalizeLeafAngles = true;
    [Tooltip("同一组叶子之间允许的最小夹角，单位为度。")]
    [SerializeField, Range(0f, 180f)] private float minimumLeafAngle = 44f;
    [Tooltip("单个叶子相对初始角度的最大调整量，单位为度。")]
    [SerializeField, Range(0f, 180f)] private float maxLeafAngleAdjustment = 68f;
    [Tooltip("叶子卡片之间保留的最小间距，单位为 Canvas 坐标。")]
    [SerializeField, Min(0f)] private float leafOverlapPadding = 48f;
    [Tooltip("单个叶子相对初始位置的最大偏移，避免展开范围过大。")]
    [SerializeField, Min(0f)] private float maxLeafOffset = 260f;
    [Tooltip("叶子节点和分支线的平滑调整速度。")]
    [SerializeField, Min(0f)] private float leafLayoutSpeed = 10f;

    private static readonly List<MarkerPanelPresenter> activePresenters = new List<MarkerPanelPresenter>();
    private static int lastLayoutFrame = -1;

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private CanvasGroup group;
    private Coroutine fadeRoutine;
    private bool visible;
    private Vector2 trackedPosition;
    private Vector2[] baseLeafPositions = Array.Empty<Vector2>();
    private Vector2[] desiredLeafPositions = Array.Empty<Vector2>();

    public bool IsVisible => visible;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (markerEvents == null) markerEvents = GetComponent<MarKActions>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        CacheLeafPositions();
        SetVisibleImmediate(false);
    }

    private void OnEnable()
    {
        if (!activePresenters.Contains(this)) activePresenters.Add(this);
        if (markerEvents == null) return;
        markerEvents.Started += HandleStart;
        markerEvents.Moved += HandleMove;
        markerEvents.Ended += HandleEnd;
        markerEvents.Undetected += HandleUndetected;
    }

    private void OnDisable()
    {
        activePresenters.Remove(this);
        if (markerEvents == null) return;
        markerEvents.Started -= HandleStart;
        markerEvents.Moved -= HandleMove;
        markerEvents.Ended -= HandleEnd;
        markerEvents.Undetected -= HandleUndetected;
    }

    private void Update()
    {
        if (!visible || panelLines == null || panelImages == null) return;
        float alpha = Mathf.InverseLerp(panelRevealAt, 1f, panelLines.GrowProgress);
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] == null) continue;
            panelImages[i].alpha = alpha;
            panelImages[i].interactable = alpha > 0.01f;
            panelImages[i].blocksRaycasts = alpha > 0.01f;
        }
    }

    private void LateUpdate()
    {
        // 所有跟踪事件完成后统一布局，且每帧只计算一次，避免多个组按执行顺序互相抖动。
        if (lastLayoutFrame == Time.frameCount) return;
        lastLayoutFrame = Time.frameCount;
        ResolveLeafLayouts();
    }

    public void Configure(MarKActions eventsSource, RadialPanelLines lines, Canvas canvas, CanvasGroup[] panels)
    {
        markerEvents = eventsSource;
        panelLines = lines;
        parentCanvas = canvas != null ? canvas : GetComponentInParent<Canvas>();
        panelImages = panels;
        canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        CacheLeafPositions();
    }

    public void ConfigureCombinedDisplay(int targetDisplay, Vector2 combinedResolution, float displayWidth)
    {
        useCombinedDisplayCoordinates = true;
        displayIndex = Mathf.Max(0, targetDisplay);
        combinedDisplayWidth = Mathf.Max(1f, combinedResolution.x);
        combinedDisplayHeight = Mathf.Max(1f, combinedResolution.y);
        singleDisplayWidth = Mathf.Max(1f, displayWidth);
    }

    private void HandleStart(DetectObjectDetails details)
    {
        ApplyPose(details, true);
        ResetLeafPositions();
        ResetPanelZooms();
        visible = true;
        group.interactable = true;
        group.blocksRaycasts = true;
        if (panelImages == null) panelImages = Array.Empty<CanvasGroup>();
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] == null) continue;
            panelImages[i].alpha = 0f;
            panelImages[i].interactable = false;
            panelImages[i].blocksRaycasts = false;
        }
        panelLines?.PlayGrow();
        FadeTo(1f);
    }

    private void HandleMove(DetectObjectDetails details)
    {
        ApplyPose(details, false);
    }

    private void HandleEnd(DetectObjectDetails details)
    {
        ApplyPose(details, false);
        Hide();
    }

    private void HandleUndetected()
    {
        Hide();
    }

    private void Hide()
    {
        visible = false;
        group.interactable = false;
        group.blocksRaycasts = false;
        ResetPanelZooms();
        FadeTo(0f);
    }

    private void ApplyPose(DetectObjectDetails details, bool immediate)
    {
        Vector2 target = details.objectCenterPosition;
        if (useCombinedDisplayCoordinates)
        {
            // ObjectDetect 直接返回 Touch.position：整块宽屏左下角为原点。
            // 转换为当前 4K Canvas 以中心为原点的 anchoredPosition。
            target.x = target.x - displayIndex * singleDisplayWidth - singleDisplayWidth * 0.5f;
            target.y = target.y - combinedDisplayHeight * 0.5f;
        }
        else if (positionIsScreenPoint && canvasRect != null)
        {
            Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, target, eventCamera, out target);
        }

        float t = immediate || followSpeed <= 0f ? 1f : 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        trackedPosition = target;
        // 根节点严格跟随 Marker；避让只改变叶子面板的 anchoredPosition。
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, trackedPosition, t);
        if (followRotation)
            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation,
                Quaternion.Euler(0f, 0f, -details.objectRotationAngle), t);
    }

    private void FadeTo(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = target;
        fadeRoutine = null;
    }

    private void SetVisibleImmediate(bool state)
    {
        visible = state;
        group.alpha = state ? 1f : 0f;
        group.interactable = state;
        group.blocksRaycasts = state;
        if (panelImages == null) return;
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] == null) continue;
            panelImages[i].alpha = state ? 1f : 0f;
            panelImages[i].interactable = state;
            panelImages[i].blocksRaycasts = state;
        }
    }

    private void ResetPanelZooms()
    {
        if (panelImages == null) return;
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] == null) continue;
            PanelImageZoomToggle zoom = panelImages[i].GetComponent<PanelImageZoomToggle>();
            if (zoom != null) zoom.ResetZoom();
        }
    }

    private void CacheLeafPositions()
    {
        int count = panelImages != null ? panelImages.Length : 0;
        baseLeafPositions = new Vector2[count];
        desiredLeafPositions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            RectTransform panelRect = GetPanelRect(i);
            baseLeafPositions[i] = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
            desiredLeafPositions[i] = baseLeafPositions[i];
        }
    }

    private void ResetLeafPositions()
    {
        if (baseLeafPositions.Length != (panelImages != null ? panelImages.Length : 0)) CacheLeafPositions();
        for (int i = 0; i < baseLeafPositions.Length; i++)
        {
            RectTransform panelRect = GetPanelRect(i);
            if (panelRect != null) panelRect.anchoredPosition = baseLeafPositions[i];
            desiredLeafPositions[i] = baseLeafPositions[i];
        }
    }

    private static void ResolveLeafLayouts()
    {
        for (int i = 0; i < activePresenters.Count; i++)
        {
            MarkerPanelPresenter presenter = activePresenters[i];
            if (!CanArrange(presenter)) continue;
            presenter.PrepareDesiredLeafPositions();
            presenter.EqualizeLeafAngles();
        }

        // 以虚拟目标位置做多轮碰撞求解，结果稳定，不把上一帧的偏移重新当作输入。
        for (int pass = 0; pass < 4; pass++)
        {
            for (int i = 0; i < activePresenters.Count; i++)
            {
                MarkerPanelPresenter first = activePresenters[i];
                if (!CanArrange(first)) continue;
                for (int j = i + 1; j < activePresenters.Count; j++)
                {
                    MarkerPanelPresenter second = activePresenters[j];
                    if (!CanArrange(second) || first.canvasRect != second.canvasRect) continue;
                    ResolvePair(first, second);
                }
            }
        }

        for (int i = 0; i < activePresenters.Count; i++)
        {
            MarkerPanelPresenter presenter = activePresenters[i];
            if (!CanArrange(presenter)) continue;
            presenter.ApplyDesiredLeafPositions();
            presenter.panelLines?.RebuildNow();
        }
    }

    private static bool CanArrange(MarkerPanelPresenter presenter)
    {
        return presenter != null && presenter.autoArrangeLeaves && presenter.visible &&
            presenter.group != null && presenter.group.alpha > 0.01f &&
            presenter.canvasRect != null && presenter.panelImages != null &&
            presenter.panelImages.Length > 0;
    }

    private void PrepareDesiredLeafPositions()
    {
        if (baseLeafPositions.Length != panelImages.Length) CacheLeafPositions();
        for (int i = 0; i < baseLeafPositions.Length; i++) desiredLeafPositions[i] = baseLeafPositions[i];
    }

    private void EqualizeLeafAngles()
    {
        if (!equalizeLeafAngles || minimumLeafAngle <= 0f || desiredLeafPositions.Length < 2) return;

        float[] baseAngles = new float[desiredLeafPositions.Length];
        float[] angles = new float[desiredLeafPositions.Length];
        float[] radii = new float[desiredLeafPositions.Length];
        for (int i = 0; i < desiredLeafPositions.Length; i++)
        {
            Vector2 position = baseLeafPositions[i];
            radii[i] = position.magnitude;
            baseAngles[i] = angles[i] = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
        }

        int passes = Mathf.Clamp(desiredLeafPositions.Length, 1, 8);
        for (int pass = 0; pass < passes; pass++)
        {
            for (int i = 0; i < angles.Length; i++)
            {
                if (radii[i] <= 0.01f) continue;
                for (int j = i + 1; j < angles.Length; j++)
                {
                    if (radii[j] <= 0.01f) continue;
                    float delta = Mathf.DeltaAngle(angles[i], angles[j]);
                    float absoluteDelta = Mathf.Abs(delta);
                    if (absoluteDelta >= minimumLeafAngle) continue;

                    float direction = absoluteDelta < 0.001f
                        ? (i < j ? -1f : 1f)
                        : Mathf.Sign(delta);
                    float correction = (minimumLeafAngle - absoluteDelta) * 0.5f;
                    angles[i] = ClampAngleAroundBase(i, angles[i] - direction * correction, baseAngles);
                    angles[j] = ClampAngleAroundBase(j, angles[j] + direction * correction, baseAngles);
                }
            }
        }

        for (int i = 0; i < desiredLeafPositions.Length; i++)
        {
            if (radii[i] <= 0.01f) continue;
            float radians = angles[i] * Mathf.Deg2Rad;
            desiredLeafPositions[i] = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radii[i];
        }
    }

    private float ClampAngleAroundBase(int index, float angle, float[] baseAngles)
    {
        float adjustment = Mathf.DeltaAngle(baseAngles[index], angle);
        return baseAngles[index] + Mathf.Clamp(adjustment, -maxLeafAngleAdjustment, maxLeafAngleAdjustment);
    }

    private static void ResolvePair(MarkerPanelPresenter first, MarkerPanelPresenter second)
    {
        for (int i = 0; i < first.desiredLeafPositions.Length; i++)
        {
            if (!first.TryGetPanelBoundsForDesired(i, out Rect firstBounds)) continue;
            for (int j = 0; j < second.desiredLeafPositions.Length; j++)
            {
                if (!second.TryGetPanelBoundsForDesired(j, out Rect secondBounds)) continue;

                Rect firstPadded = firstBounds;
                Rect secondPadded = secondBounds;
                Inflate(ref firstPadded, first.leafOverlapPadding);
                Inflate(ref secondPadded, second.leafOverlapPadding);
                float overlapX = Mathf.Min(firstPadded.xMax, secondPadded.xMax) -
                    Mathf.Max(firstPadded.xMin, secondPadded.xMin);
                float overlapY = Mathf.Min(firstPadded.yMax, secondPadded.yMax) -
                    Mathf.Max(firstPadded.yMin, secondPadded.yMin);
                if (overlapX <= 0f || overlapY <= 0f) continue;

                Vector2 separation;
                if (overlapX <= overlapY)
                {
                    float sign = firstBounds.center.x < secondBounds.center.x ||
                        (Mathf.Approximately(firstBounds.center.x, secondBounds.center.x) &&
                         first.GetStableSortKey(i) < second.GetStableSortKey(j)) ? -1f : 1f;
                    separation = new Vector2(sign * overlapX, 0f);
                }
                else
                {
                    float sign = firstBounds.center.y < secondBounds.center.y ||
                        (Mathf.Approximately(firstBounds.center.y, secondBounds.center.y) &&
                         first.GetStableSortKey(i) < second.GetStableSortKey(j)) ? -1f : 1f;
                    separation = new Vector2(0f, sign * overlapY);
                }

                first.AddCanvasDisplacement(i, -separation * 0.5f);
                second.AddCanvasDisplacement(j, separation * 0.5f);
            }
        }
    }

    private bool TryGetPanelBoundsForDesired(int index, out Rect bounds)
    {
        bounds = default;
        RectTransform panelRect = GetPanelRect(index);
        if (panelRect == null || panelImages[index] == null || panelImages[index].alpha <= 0.01f ||
            !panelRect.gameObject.activeInHierarchy) return false;

        bounds = GetCanvasRect(panelRect);
        Vector2 localDelta = desiredLeafPositions[index] - panelRect.anchoredPosition;
        Vector2 canvasDelta = canvasRect.InverseTransformVector(rectTransform.TransformVector(localDelta));
        bounds.position += canvasDelta;
        return true;
    }

    private Rect GetCanvasRect(RectTransform target)
    {
        Camera eventCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;
        target.GetWorldCorners(worldCorners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
                localPoint = canvasRect.InverseTransformPoint(worldCorners[i]);
            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void AddCanvasDisplacement(int index, Vector2 canvasDelta)
    {
        if (maxLeafOffset <= 0f) return;
        Vector3 worldDelta = canvasRect.TransformVector(canvasDelta);
        Vector2 localDelta = rectTransform.InverseTransformVector(worldDelta);
        Vector2 candidate = desiredLeafPositions[index] + localDelta;
        desiredLeafPositions[index] = baseLeafPositions[index] +
            Vector2.ClampMagnitude(candidate - baseLeafPositions[index], maxLeafOffset);
    }

    private void ApplyDesiredLeafPositions()
    {
        float t = leafLayoutSpeed <= 0f ? 1f : 1f - Mathf.Exp(-leafLayoutSpeed * Time.unscaledDeltaTime);
        for (int i = 0; i < desiredLeafPositions.Length; i++)
        {
            RectTransform panelRect = GetPanelRect(i);
            if (panelRect == null) continue;
            panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, desiredLeafPositions[i], t);
        }
    }

    private RectTransform GetPanelRect(int index)
    {
        if (panelImages == null || index < 0 || index >= panelImages.Length || panelImages[index] == null)
            return null;
        return panelImages[index].transform as RectTransform;
    }

    private int GetStableSortKey(int leafIndex)
    {
        int objectId = markerEvents != null ? markerEvents.mObjectID : GetInstanceID();
        return objectId * 1000 + leafIndex;
    }

    private static void Inflate(ref Rect rect, float amount)
    {
        rect.xMin -= amount;
        rect.xMax += amount;
        rect.yMin -= amount;
        rect.yMax += amount;
    }

    private readonly Vector3[] worldCorners = new Vector3[4];
}
