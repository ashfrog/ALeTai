using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/Zoomable UI Panel")]
public sealed class ZoomableUIPanel : MonoBehaviour,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IScrollHandler
{
    [Serializable]
    public sealed class ZoomChangedEvent : UnityEvent<float> { }

    [Serializable]
    public sealed class EdgePullChangedEvent : UnityEvent<Vector2> { }

    private const int InvalidPointerId = int.MinValue;
    private const float PositionEpsilon = 0.01f;
    private const float VelocityEpsilon = 1f;
    private const float Ln2 = 0.69314718056f;
    private const float VelocityTrackingHalfLife = 0.04f;
    private const float InertiaIdleTimeout = 0.08f;
    private const float FlingSlowdownRatio = 0.5f;
    private const float MinimumMotionDuration = 0.05f;
    private const float MaximumMotionDuration = 3f;
    private const float MaximumSimulationStep = 0.1f;

    [Header("References")]
    [Tooltip("The direct child that will be panned and scaled.")]
    [SerializeField] private RectTransform content;

    [Header("Zoom")]
    [Tooltip("Smallest scale relative to the captured default view.")]
    [SerializeField, Min(0.01f)] private float minZoom = 0.8f;
    [Tooltip("Largest scale relative to the captured default view.")]
    [SerializeField, Min(0.01f)] private float maxZoom = 5f;
    [Tooltip("Mouse-wheel zoom response. Higher values zoom farther per wheel step.")]
    [FormerlySerializedAs("wheelZoomStep")]
    [SerializeField, Range(0.01f, 0.5f)] private float scrollZoomSensitivity = 0.1f;
    [Tooltip("Restore the captured position and scale whenever the component is enabled.")]
    [SerializeField] private bool resetOnEnable = true;

    [Header("Movement")]
    [Tooltip("Allow resisted movement beyond the viewport, followed by spring-back.")]
    [FormerlySerializedAs("elasticEdges")]
    [SerializeField] private bool allowOverscroll = true;
    [Tooltip("How far content follows the pointer beyond an edge. Higher values feel softer.")]
    [FormerlySerializedAs("edgeResistance")]
    [SerializeField, Range(0.05f, 1f)] private float overscrollFlexibility = 0.1f;
    [Tooltip("Time in seconds for overscrolled content to settle back. Higher values are softer.")]
    [FormerlySerializedAs("elasticity")]
    [SerializeField, Range(MinimumMotionDuration, 1f)] private float springBackDuration = 0.1f;
    [Tooltip("Continue moving after the pointer is released.")]
    [FormerlySerializedAs("inertia")]
    [SerializeField] private bool enableInertia = true;
    [Tooltip("Time in seconds for inertial speed to halve. Higher values glide longer.")]
    [SerializeField, Range(MinimumMotionDuration, MaximumMotionDuration)]
    private float inertiaHalfLife = 0.35f;
    [Tooltip("Minimum release speed in UI units per second required to start a fling.")]
    [FormerlySerializedAs("minimumFlingVelocity")]
    [SerializeField, Min(0f)] private float minimumFlingSpeed = 180f;
    [Tooltip("Maximum inertial release speed in UI units per second.")]
    [SerializeField, Min(1f)] private float maximumFlingSpeed = 8000f;

    // Kept only to migrate existing scenes from ScrollRect-style retention-per-second semantics.
    [SerializeField, HideInInspector] private float decelerationRate = -1f;

    [Header("Events")]
    [SerializeField] private ZoomChangedEvent onZoomChanged = new ZoomChangedEvent();
    [SerializeField] private EdgePullChangedEvent onEdgePullChanged = new EdgePullChangedEvent();

    private readonly Dictionary<int, PointerEventData> ownedTouchPointers =
        new Dictionary<int, PointerEventData>();
    private readonly List<int> endedTouchPointers = new List<int>(2);

    private RectTransform viewport;
    private Canvas rootCanvas;
    private Bounds localContentBounds;
    private Rect cachedContentRect;
    private Vector3 initialLocalScale;
    private Vector2 initialAnchoredPosition;
    private Vector3 initialBoundsCenter;
    private Vector2 contentStartPosition;
    private Vector2 lastMeasuredDragVelocity;
    private Vector2 pointerStartLocalCursor;
    private Vector2 lastPinchLocalCenter;
    private Vector2 velocity;
    private Vector2 edgePull;
    private Camera lastEventCamera;
    private float currentZoom = 1f;
    private float lastDragMovementTime;
    private float lastDragSampleTime;
    private float pinchStartDistance;
    private float lastPinchDistance;
    private bool initialized;
    private bool pinchCandidate;
    private bool pinching;
    private bool dragging;
    private bool hasDragStartCursor;
    private bool hasLastPinchLocalCenter;
    private bool localContentBoundsValid;
    private bool configurationWarningLogged;
    private int activeDragPointerId = InvalidPointerId;

    public RectTransform Content { get { return content; } }
    public float CurrentZoom { get { return currentZoom; } }
    public Vector2 Velocity { get { return velocity; } }
    public Vector2 EdgePull { get { return edgePull; } }
    public bool IsDragging { get { return dragging; } }
    public bool IsPinching { get { return pinching; } }
    public ZoomChangedEvent OnZoomChanged { get { return onZoomChanged; } }
    public EdgePullChangedEvent OnEdgePullChanged { get { return onEdgePullChanged; } }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!Initialize())
        {
            return;
        }

        ClearGestureState();
        if (resetOnEnable)
        {
            ResetView();
        }
        else
        {
            ClampContentToViewport();
        }
    }

    private void OnDisable()
    {
        ClearGestureState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ClearGestureState();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            ClearGestureState();
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (ownedTouchPointers.Count > 0)
        {
            UpdateOwnedTouchPointers();
            UpdatePinch();
        }
        else if (pinchCandidate || pinching)
        {
            EndPinch();
        }

        Vector2 previousPosition = content.anchoredPosition;
        UpdateMotion(Time.unscaledDeltaTime);
        if (content.anchoredPosition != previousPosition)
        {
            UpdateEdgeFeedback();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!initialized || !isActiveAndEnabled)
        {
            return;
        }

        CacheLocalContentBounds();

        if (Mathf.Approximately(currentZoom, 1f))
        {
            CaptureInitialBoundsCenter();
        }
        else
        {
            ClampContentToViewport();
        }
    }

    private void OnValidate()
    {
        MigrateLegacyDecelerationRate();
        minZoom = Mathf.Clamp(minZoom, 0.01f, 1f);
        maxZoom = Mathf.Max(1f, maxZoom);
        if (maxZoom < minZoom)
        {
            maxZoom = minZoom;
        }

        scrollZoomSensitivity = Mathf.Clamp(scrollZoomSensitivity, 0.01f, 0.5f);
        overscrollFlexibility = Mathf.Clamp(overscrollFlexibility, 0.05f, 1f);
        springBackDuration = Mathf.Clamp(springBackDuration, MinimumMotionDuration, 1f);
        inertiaHalfLife = Mathf.Clamp(
            inertiaHalfLife, MinimumMotionDuration, MaximumMotionDuration);
        minimumFlingSpeed = Mathf.Max(0f, minimumFlingSpeed);
        maximumFlingSpeed = Mathf.Max(minimumFlingSpeed, maximumFlingSpeed);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!Initialize() || eventData == null)
        {
            return;
        }

        eventData.useDragThreshold = true;
        lastEventCamera = ResolveEventCamera(eventData);

        if (eventData.pointerId >= 0)
        {
            ownedTouchPointers[eventData.pointerId] = eventData;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanHandleDrag(eventData))
        {
            return;
        }

        if (activeDragPointerId != InvalidPointerId
            && activeDragPointerId != eventData.pointerId)
        {
            return;
        }

        lastEventCamera = ResolveEventCamera(eventData);
        activeDragPointerId = eventData.pointerId;
        dragging = true;
        velocity = Vector2.zero;
        lastMeasuredDragVelocity = Vector2.zero;
        contentStartPosition = content.anchoredPosition;
        lastDragMovementTime = Time.unscaledTime;
        lastDragSampleTime = Time.unscaledTime;
        hasDragStartCursor = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, eventData.position, lastEventCamera, out pointerStartLocalCursor);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanHandleDrag(eventData)
            || eventData.pointerId != activeDragPointerId
            || pinching
            || CountActiveOwnedTouches() >= 2)
        {
            return;
        }

        Camera eventCamera = ResolveEventCamera(eventData);
        Vector2 currentPoint;
        Vector2 previousPosition = content.anchoredPosition;
        if (hasDragStartCursor
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport, eventData.position, eventCamera, out currentPoint))
        {
            SetDragPosition(contentStartPosition + currentPoint - pointerStartLocalCursor);
        }
        else
        {
            float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            SetDragPosition(
                content.anchoredPosition + eventData.delta / Mathf.Max(0.01f, scaleFactor));
        }

        Vector2 positionDelta = content.anchoredPosition - previousPosition;
        if (positionDelta.sqrMagnitude > PositionEpsilon * PositionEpsilon)
        {
            TrackDragVelocity(positionDelta, Time.unscaledTime);
        }

        UpdateEdgeFeedback();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId >= 0)
        {
            ownedTouchPointers.Remove(eventData.pointerId);
        }

        if (eventData != null && eventData.pointerId == activeDragPointerId)
        {
            if (ShouldStartInertia())
            {
                float releaseSpeed = Mathf.Min(
                    velocity.magnitude, lastMeasuredDragVelocity.magnitude);
                releaseSpeed = Mathf.Min(releaseSpeed, maximumFlingSpeed);
                velocity = lastMeasuredDragVelocity.normalized * releaseSpeed;
            }
            else
            {
                velocity = Vector2.zero;
            }

            dragging = false;
            activeDragPointerId = InvalidPointerId;
            hasDragStartCursor = false;

            if (!allowOverscroll)
            {
                ClampContentToViewport();
            }
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!Initialize() || eventData == null || Mathf.Approximately(eventData.scrollDelta.y, 0f))
        {
            return;
        }

        Camera eventCamera = ResolveEventCamera(eventData);
        float zoomFactor = Mathf.Exp(scrollZoomSensitivity * eventData.scrollDelta.y);
        ApplyZoom(currentZoom * zoomFactor, eventData.position, eventCamera);
        eventData.Use();
    }

    [ContextMenu("Reset View")]
    public void ResetView()
    {
        if (!Initialize())
        {
            return;
        }

        bool zoomChanged = !Mathf.Approximately(currentZoom, 1f);
        content.localScale = initialLocalScale;
        content.anchoredPosition = initialAnchoredPosition;
        currentZoom = 1f;
        ClearGestureState();
        SetEdgePull(Vector2.zero);

        if (zoomChanged)
        {
            onZoomChanged.Invoke(currentZoom);
        }
    }

    public void SetZoom(float zoom)
    {
        if (!Initialize())
        {
            return;
        }

        Camera eventCamera = ResolveEventCamera(null);
        Vector3 viewportCenter = viewport.TransformPoint(viewport.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, viewportCenter);
        ApplyZoom(zoom, screenPoint, eventCamera);
    }

    public void ZoomBy(float factor)
    {
        if (factor <= 0f)
        {
            return;
        }

        SetZoom(currentZoom * factor);
    }

    public void StopMovement()
    {
        velocity = Vector2.zero;
    }

    public void RefreshContentBounds()
    {
        if (!Initialize())
        {
            return;
        }

        CacheLocalContentBounds();
        if (Mathf.Approximately(currentZoom, 1f))
        {
            CaptureInitialBoundsCenter();
        }

        ClampContentToViewport();
        UpdateEdgeFeedback();
    }

    private bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        viewport = transform as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();

        if (content == null)
        {
            LogConfigurationWarning("Content is not assigned.");
            return false;
        }

        if (content.parent != transform)
        {
            LogConfigurationWarning("Content must be a direct child of the viewport.");
            return false;
        }

        MigrateLegacyDecelerationRate();

        if (minZoom <= 0f || maxZoom < minZoom || maxZoom < 1f || minZoom > 1f)
        {
            LogConfigurationWarning("Zoom limits are invalid; expected minZoom <= 1 <= maxZoom.");
            minZoom = Mathf.Clamp(minZoom, 0.01f, 1f);
            maxZoom = Mathf.Max(1f, maxZoom, minZoom);
        }

        scrollZoomSensitivity = Mathf.Clamp(scrollZoomSensitivity, 0.01f, 0.5f);
        overscrollFlexibility = Mathf.Clamp(overscrollFlexibility, 0.05f, 1f);
        springBackDuration = Mathf.Clamp(springBackDuration, MinimumMotionDuration, 1f);
        inertiaHalfLife = Mathf.Clamp(
            inertiaHalfLife, MinimumMotionDuration, MaximumMotionDuration);
        minimumFlingSpeed = Mathf.Max(0f, minimumFlingSpeed);
        maximumFlingSpeed = Mathf.Max(minimumFlingSpeed, maximumFlingSpeed);
        initialLocalScale = content.localScale;
        initialAnchoredPosition = content.anchoredPosition;
        currentZoom = 1f;
        CacheLocalContentBounds();
        CaptureInitialBoundsCenter();
        initialized = true;
        configurationWarningLogged = false;
        return true;
    }

    private bool CanHandleDrag(PointerEventData eventData)
    {
        if (!Initialize() || eventData == null)
        {
            return false;
        }

        return eventData.pointerId >= 0 || eventData.button == PointerEventData.InputButton.Left;
    }

    private void MigrateLegacyDecelerationRate()
    {
        if (decelerationRate < 0f)
        {
            return;
        }

        if (decelerationRate <= Mathf.Epsilon)
        {
            inertiaHalfLife = MinimumMotionDuration;
        }
        else if (decelerationRate >= 1f - Mathf.Epsilon)
        {
            inertiaHalfLife = MaximumMotionDuration;
        }
        else
        {
            inertiaHalfLife = Mathf.Clamp(
                Ln2 / -Mathf.Log(decelerationRate),
                MinimumMotionDuration,
                MaximumMotionDuration);
        }

        decelerationRate = -1f;
    }

    private void UpdateOwnedTouchPointers()
    {
        if (ownedTouchPointers.Count == 0)
        {
            return;
        }

        endedTouchPointers.Clear();
        foreach (KeyValuePair<int, PointerEventData> pointer in ownedTouchPointers)
        {
            Touch touch;
            if (!TryGetTouch(pointer.Key, out touch)
                || touch.phase == TouchPhase.Ended
                || touch.phase == TouchPhase.Canceled)
            {
                endedTouchPointers.Add(pointer.Key);
            }
        }

        if (endedTouchPointers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < endedTouchPointers.Count; i++)
        {
            int pointerId = endedTouchPointers[i];
            ownedTouchPointers.Remove(pointerId);

            if (pointerId == activeDragPointerId)
            {
                dragging = false;
                activeDragPointerId = InvalidPointerId;
                hasDragStartCursor = false;
            }
        }
    }

    private void UpdatePinch()
    {
        Touch first;
        Touch second;
        if (!TryGetFirstTwoOwnedTouches(out first, out second))
        {
            EndPinch();
            return;
        }

        float currentDistance = Vector2.Distance(first.position, second.position);
        if (currentDistance <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 pinchCenter = (first.position + second.position) * 0.5f;
        Vector2 currentLocalCenter;
        bool hasCurrentLocalCenter = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, pinchCenter, lastEventCamera, out currentLocalCenter);

        if (!pinchCandidate)
        {
            pinchCandidate = true;
            pinchStartDistance = currentDistance;
            lastPinchDistance = currentDistance;
            lastPinchLocalCenter = currentLocalCenter;
            hasLastPinchLocalCenter = hasCurrentLocalCenter;
            return;
        }

        if (!pinching)
        {
            float dragThreshold = EventSystem.current != null
                ? EventSystem.current.pixelDragThreshold
                : 10f;
            float pinchThreshold = Mathf.Max(8f, dragThreshold * 2f);
            if (Mathf.Abs(currentDistance - pinchStartDistance) < pinchThreshold)
            {
                lastPinchDistance = currentDistance;
                return;
            }

            pinching = true;
            dragging = false;
            activeDragPointerId = InvalidPointerId;
            hasDragStartCursor = false;
            velocity = Vector2.zero;
            lastMeasuredDragVelocity = Vector2.zero;
            pinchStartDistance = currentDistance;
            lastPinchDistance = currentDistance;
            lastPinchLocalCenter = currentLocalCenter;
            hasLastPinchLocalCenter = hasCurrentLocalCenter;
            CancelOwnedPointerClicks();
            return;
        }

        if (hasCurrentLocalCenter && hasLastPinchLocalCenter)
        {
            SetDragPosition(
                content.anchoredPosition + currentLocalCenter - lastPinchLocalCenter);
        }

        if (lastPinchDistance > Mathf.Epsilon)
        {
            ApplyZoom(currentZoom * (currentDistance / lastPinchDistance), pinchCenter, lastEventCamera);
        }

        lastPinchDistance = currentDistance;
        lastPinchLocalCenter = currentLocalCenter;
        hasLastPinchLocalCenter = hasCurrentLocalCenter;
        UpdateEdgeFeedback();
    }

    private bool TryGetFirstTwoOwnedTouches(out Touch first, out Touch second)
    {
        first = default(Touch);
        second = default(Touch);
        int found = 0;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (!ownedTouchPointers.ContainsKey(touch.fingerId)
                || touch.phase == TouchPhase.Ended
                || touch.phase == TouchPhase.Canceled)
            {
                continue;
            }

            if (found == 0)
            {
                first = touch;
            }
            else
            {
                second = touch;
                return true;
            }

            found++;
        }

        return false;
    }

    private int CountActiveOwnedTouches()
    {
        int count = 0;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (ownedTouchPointers.ContainsKey(touch.fingerId)
                && touch.phase != TouchPhase.Ended
                && touch.phase != TouchPhase.Canceled)
            {
                count++;
            }
        }

        return count;
    }

    private bool TryGetOnlyOwnedTouch(out Touch result)
    {
        result = default(Touch);
        int found = 0;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (!ownedTouchPointers.ContainsKey(touch.fingerId)
                || touch.phase == TouchPhase.Ended
                || touch.phase == TouchPhase.Canceled)
            {
                continue;
            }

            result = touch;
            found++;
            if (found > 1)
            {
                return false;
            }
        }

        return found == 1;
    }

    private static bool TryGetTouch(int fingerId, out Touch result)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId == fingerId)
            {
                result = touch;
                return true;
            }
        }

        result = default(Touch);
        return false;
    }

    private void CancelOwnedPointerClicks()
    {
        foreach (PointerEventData pointerEvent in ownedTouchPointers.Values)
        {
            if (pointerEvent != null)
            {
                pointerEvent.eligibleForClick = false;
            }
        }
    }

    private void ApplyZoom(float requestedZoom, Vector2 screenPoint, Camera eventCamera)
    {
        float targetZoom = Mathf.Clamp(requestedZoom, minZoom, maxZoom);
        if (Mathf.Approximately(targetZoom, currentZoom))
        {
            return;
        }

        Vector2 desiredViewportPoint;
        Vector2 contentPoint;
        bool hasViewportPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, screenPoint, eventCamera, out desiredViewportPoint);
        bool hasContentPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, screenPoint, eventCamera, out contentPoint);

        content.localScale = new Vector3(
            initialLocalScale.x * targetZoom,
            initialLocalScale.y * targetZoom,
            initialLocalScale.z);
        currentZoom = targetZoom;
        velocity = Vector2.zero;

        if (hasViewportPoint && hasContentPoint)
        {
            Vector3 contentPointAfterScale = viewport.InverseTransformPoint(
                content.TransformPoint(contentPoint));
            content.anchoredPosition += desiredViewportPoint - (Vector2)contentPointAfterScale;
        }

        ClampContentToViewport();
        UpdateEdgeFeedback();
        onZoomChanged.Invoke(currentZoom);
    }

    private void SetDragPosition(Vector2 position)
    {
        content.anchoredPosition = position;
        Vector2 correction = CalculateBoundsCorrection();
        if (!allowOverscroll)
        {
            content.anchoredPosition += correction;
            return;
        }

        Vector2 viewportSize = viewport.rect.size;
        content.anchoredPosition += new Vector2(
            correction.x - RubberDelta(correction.x, viewportSize.x),
            correction.y - RubberDelta(correction.y, viewportSize.y));
    }

    private float RubberDelta(float correction, float viewSize)
    {
        if (Mathf.Approximately(correction, 0f) || viewSize <= Mathf.Epsilon)
        {
            return 0f;
        }

        float stretched = Mathf.Abs(correction);
        float rubber =
            (1f - 1f / (stretched * overscrollFlexibility / viewSize + 1f)) * viewSize;
        return rubber * Mathf.Sign(correction);
    }

    private void UpdateMotion(float deltaTime)
    {
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        deltaTime = Mathf.Min(deltaTime, MaximumSimulationStep);

        if (dragging)
        {
            ClearStaleDragVelocity();
            return;
        }

        if (pinching)
        {
            return;
        }

        if (velocity.sqrMagnitude <= VelocityEpsilon * VelocityEpsilon
            && edgePull.sqrMagnitude <= 0.000001f)
        {
            velocity = Vector2.zero;
            return;
        }

        Vector2 correction = CalculateBoundsCorrection();
        if (!allowOverscroll && correction.sqrMagnitude > PositionEpsilon * PositionEpsilon)
        {
            content.anchoredPosition += correction;
            velocity = Vector2.zero;
            return;
        }

        Vector2 position = content.anchoredPosition;
        UpdateAxisMotion(
            ref position.x, ref velocity.x, correction.x, deltaTime);
        UpdateAxisMotion(
            ref position.y, ref velocity.y, correction.y, deltaTime);
        content.anchoredPosition = position;
    }

    private void TrackDragVelocity(Vector2 positionDelta, float sampleTime)
    {
        float deltaTime = sampleTime - lastDragSampleTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            deltaTime = Mathf.Max(Time.unscaledDeltaTime, 1f / 240f);
        }

        Vector2 measuredVelocity = Vector2.ClampMagnitude(
            positionDelta / deltaTime, maximumFlingSpeed);
        lastMeasuredDragVelocity = measuredVelocity;
        float blend = 1f - Mathf.Exp(-Ln2 * deltaTime / VelocityTrackingHalfLife);
        velocity = Vector2.Lerp(velocity, measuredVelocity, blend);
        lastDragMovementTime = sampleTime;
        lastDragSampleTime = sampleTime;
    }

    private void ClearStaleDragVelocity()
    {
        if (Time.unscaledTime - lastDragMovementTime < InertiaIdleTimeout)
        {
            return;
        }

        velocity = Vector2.zero;
        lastMeasuredDragVelocity = Vector2.zero;
    }

    private bool ShouldStartInertia()
    {
        if (!enableInertia
            || Time.unscaledTime - lastDragMovementTime >= InertiaIdleTimeout
            || edgePull.sqrMagnitude > 0.000001f)
        {
            return false;
        }

        float measuredSpeed = lastMeasuredDragVelocity.magnitude;
        float trackedSpeed = velocity.magnitude;
        if (measuredSpeed < minimumFlingSpeed
            || trackedSpeed < VelocityEpsilon
            || measuredSpeed < trackedSpeed * FlingSlowdownRatio)
        {
            return false;
        }

        return Vector2.Dot(lastMeasuredDragVelocity, velocity) > 0f;
    }

    private void UpdateAxisMotion(
        ref float position,
        ref float axisVelocity,
        float correction,
        float deltaTime)
    {
        if (Mathf.Abs(correction) > PositionEpsilon)
        {
            float target = position + correction;
            StepCriticalDampedSpring(
                ref position, ref axisVelocity, target, springBackDuration, deltaTime);

            if (Mathf.Abs(target - position) <= PositionEpsilon
                && Mathf.Abs(axisVelocity) <= VelocityEpsilon)
            {
                position = target;
                axisVelocity = 0f;
            }

            return;
        }

        if (!enableInertia)
        {
            axisVelocity = 0f;
            return;
        }

        IntegrateInertia(ref position, ref axisVelocity, inertiaHalfLife, deltaTime);
        if (Mathf.Abs(axisVelocity) < VelocityEpsilon)
        {
            axisVelocity = 0f;
        }
    }

    private static void IntegrateInertia(
        ref float position,
        ref float axisVelocity,
        float halfLife,
        float deltaTime)
    {
        float damping = Ln2 / Mathf.Max(MinimumMotionDuration, halfLife);
        float decay = Mathf.Exp(-damping * deltaTime);
        position += axisVelocity * (1f - decay) / damping;
        axisVelocity *= decay;
    }

    private static void StepCriticalDampedSpring(
        ref float position,
        ref float axisVelocity,
        float target,
        float duration,
        float deltaTime)
    {
        float offset = position - target;
        float angularFrequency = 2f / Mathf.Max(MinimumMotionDuration, duration);
        float decay = Mathf.Exp(-angularFrequency * deltaTime);
        float transient = (axisVelocity + angularFrequency * offset) * deltaTime;
        float nextOffset = (offset + transient) * decay;
        float nextVelocity = (axisVelocity - angularFrequency * transient) * decay;

        if (offset * nextOffset <= 0f)
        {
            position = target;
            axisVelocity = 0f;
            return;
        }

        position = target + nextOffset;
        axisVelocity = nextVelocity;
    }

    private void UpdateEdgeFeedback()
    {
        if (!initialized || content == null)
        {
            return;
        }

        Vector2 correction = CalculateBoundsCorrection();
        Vector2 size = viewport.rect.size;
        Vector2 normalizedPull = new Vector2(
            size.x > Mathf.Epsilon ? Mathf.Clamp(-correction.x / size.x, -1f, 1f) : 0f,
            size.y > Mathf.Epsilon ? Mathf.Clamp(-correction.y / size.y, -1f, 1f) : 0f);
        SetEdgePull(normalizedPull);
    }

    private void SetEdgePull(Vector2 value)
    {
        if ((value - edgePull).sqrMagnitude <= 0.000001f)
        {
            return;
        }

        edgePull = value;
        onEdgePullChanged.Invoke(edgePull);
    }

    private void ClampContentToViewport()
    {
        if (!initialized || content == null)
        {
            return;
        }

        content.anchoredPosition += CalculateBoundsCorrection();
        velocity = Vector2.zero;
        SetEdgePull(Vector2.zero);
    }

    private Vector2 CalculateBoundsCorrection()
    {
        Bounds contentBounds = GetContentBounds();
        Rect viewportRect = viewport.rect;

        return new Vector2(
            CalculateAxisCorrection(
            contentBounds.min.x,
            contentBounds.max.x,
            contentBounds.center.x,
            contentBounds.size.x,
            viewportRect.xMin,
            viewportRect.xMax,
            initialBoundsCenter.x),
            CalculateAxisCorrection(
            contentBounds.min.y,
            contentBounds.max.y,
            contentBounds.center.y,
            contentBounds.size.y,
            viewportRect.yMin,
            viewportRect.yMax,
            initialBoundsCenter.y));
    }

    private static float CalculateAxisCorrection(
        float contentMin,
        float contentMax,
        float contentCenter,
        float contentSize,
        float viewportMin,
        float viewportMax,
        float initialCenter)
    {
        float viewportSize = viewportMax - viewportMin;
        if (contentSize <= viewportSize)
        {
            return initialCenter - contentCenter;
        }

        if (contentMin > viewportMin)
        {
            return viewportMin - contentMin;
        }

        if (contentMax < viewportMax)
        {
            return viewportMax - contentMax;
        }

        return 0f;
    }

    private Bounds GetContentBounds()
    {
        if (!localContentBoundsValid || content.rect != cachedContentRect)
        {
            CacheLocalContentBounds();
        }

        Matrix4x4 contentToViewport = viewport.worldToLocalMatrix * content.localToWorldMatrix;
        Vector3 center = localContentBounds.center;
        Vector3 extents = localContentBounds.extents;
        Vector3 corner = contentToViewport.MultiplyPoint3x4(
            new Vector3(center.x - extents.x, center.y - extents.y, center.z));
        Bounds bounds = new Bounds(corner, Vector3.zero);
        bounds.Encapsulate(contentToViewport.MultiplyPoint3x4(
            new Vector3(center.x - extents.x, center.y + extents.y, center.z)));
        bounds.Encapsulate(contentToViewport.MultiplyPoint3x4(
            new Vector3(center.x + extents.x, center.y - extents.y, center.z)));
        bounds.Encapsulate(contentToViewport.MultiplyPoint3x4(
            new Vector3(center.x + extents.x, center.y + extents.y, center.z)));
        return bounds;
    }

    private void CacheLocalContentBounds()
    {
        // Descendant traversal is kept out of the per-frame drag path.
        localContentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, content);
        cachedContentRect = content.rect;
        localContentBoundsValid = true;
    }

    private void CaptureInitialBoundsCenter()
    {
        Vector2 anchoredOffset = initialAnchoredPosition - content.anchoredPosition;
        initialBoundsCenter = GetContentBounds().center
            + new Vector3(anchoredOffset.x, anchoredOffset.y, 0f);
    }

    private Camera ResolveEventCamera(PointerEventData eventData)
    {
        if (eventData != null)
        {
            if (eventData.pressEventCamera != null)
            {
                return eventData.pressEventCamera;
            }

            if (eventData.enterEventCamera != null)
            {
                return eventData.enterEventCamera;
            }
        }

        return rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;
    }

    private void EndPinch()
    {
        bool shouldResumeDrag = pinching;
        pinchCandidate = false;
        pinching = false;
        pinchStartDistance = 0f;
        lastPinchDistance = 0f;
        hasLastPinchLocalCenter = false;

        if (shouldResumeDrag)
        {
            ResumeSingleTouchDrag();
        }
    }

    private void ResumeSingleTouchDrag()
    {
        Touch touch;
        if (!TryGetOnlyOwnedTouch(out touch))
        {
            return;
        }

        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport, touch.position, lastEventCamera, out localCursor))
        {
            return;
        }

        activeDragPointerId = touch.fingerId;
        dragging = true;
        contentStartPosition = content.anchoredPosition;
        pointerStartLocalCursor = localCursor;
        hasDragStartCursor = true;
        velocity = Vector2.zero;
        lastMeasuredDragVelocity = Vector2.zero;
        lastDragMovementTime = Time.unscaledTime;
        lastDragSampleTime = Time.unscaledTime;
    }

    private void ClearGestureState()
    {
        ownedTouchPointers.Clear();
        lastEventCamera = null;
        dragging = false;
        activeDragPointerId = InvalidPointerId;
        hasDragStartCursor = false;
        velocity = Vector2.zero;
        lastMeasuredDragVelocity = Vector2.zero;
        EndPinch();
    }

    private void LogConfigurationWarning(string message)
    {
        if (configurationWarningLogged)
        {
            return;
        }

        configurationWarningLogged = true;
        Debug.LogWarning(string.Format("ZoomableUIPanel on '{0}': {1}", name, message), this);
    }
}
