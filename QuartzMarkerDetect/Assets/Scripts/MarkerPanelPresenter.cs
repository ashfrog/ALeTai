using System.Collections;
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

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private CanvasGroup group;
    private Coroutine fadeRoutine;
    private bool visible;

    public bool IsVisible => visible;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (markerEvents == null) markerEvents = GetComponent<MarKActions>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        SetVisibleImmediate(false);
    }

    private void OnEnable()
    {
        markerEvents.Started += HandleStart;
        markerEvents.Moved += HandleMove;
        markerEvents.Ended += HandleEnd;
        markerEvents.Undetected += HandleUndetected;
    }

    private void OnDisable()
    {
        markerEvents.Started -= HandleStart;
        markerEvents.Moved -= HandleMove;
        markerEvents.Ended -= HandleEnd;
        markerEvents.Undetected -= HandleUndetected;
    }

    private void Update()
    {
        if (!visible || panelLines == null) return;
        float alpha = Mathf.InverseLerp(panelRevealAt, 1f, panelLines.GrowProgress);
        for (int i = 0; i < panelImages.Length; i++)
            if (panelImages[i] != null) panelImages[i].alpha = alpha;
    }

    public void Configure(MarKActions eventsSource, RadialPanelLines lines, Canvas canvas, CanvasGroup[] panels)
    {
        markerEvents = eventsSource;
        panelLines = lines;
        parentCanvas = canvas;
        panelImages = panels;
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
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
        visible = true;
        for (int i = 0; i < panelImages.Length; i++)
            if (panelImages[i] != null) panelImages[i].alpha = 0f;
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
        FadeTo(0f);
    }

    private void ApplyPose(DetectObjectDetails details, bool immediate)
    {
        Vector2 target = details.objectCenterPosition;
        if (useCombinedDisplayCoordinates)
        {
            // 检测坐标以整块 7680x2160 宽屏左上角为原点；转换为当前 4K Canvas 的中心坐标。
            target.x = target.x - displayIndex * singleDisplayWidth - singleDisplayWidth * 0.5f;
            target.y = combinedDisplayHeight * 0.5f - target.y;
        }
        else if (positionIsScreenPoint && canvasRect != null)
        {
            Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, target, eventCamera, out target);
        }

        float t = immediate || followSpeed <= 0f ? 1f : 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, target, t);
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
        group.interactable = false;
        group.blocksRaycasts = false;
        for (int i = 0; i < panelImages.Length; i++)
            if (panelImages[i] != null) panelImages[i].alpha = state ? 1f : 0f;
    }
}
