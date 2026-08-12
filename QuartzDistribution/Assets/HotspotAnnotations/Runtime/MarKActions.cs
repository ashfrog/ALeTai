using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuartzDistribution.HotspotAnnotations
{
    /// <summary>
    /// Reads MarkerDetect's 2D tracking data, moves a Canvas anchor, and controls the line-growth group.
    /// Development simulation writes into ObjectDetect.mObjectDic so simulation and production use the same path.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class MarKActions : MonoBehaviour
    {
        [Header("DLL tracking")]
        [SerializeField] private int mObjectID = 1;
        [SerializeField] private RectTransform trackingAnchor;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField, Min(0f)] private float followSpeed = 14f;
        [SerializeField] private bool followDetectedAngle = true;

        [Header("Development DLL simulation")]
        [SerializeField] private bool simulateTrackingData;
        [SerializeField] private bool simulatedDetected = true;
        [SerializeField] private Vector2 simulatedPosition = new Vector2(960f, 540f);
        [SerializeField] private float simulatedAngle;
        [SerializeField] private Vector2 simulationReferenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private bool simulateMotion;
        [SerializeField] private Vector2 simulatedMotionRadius = new Vector2(90f, 45f);
        [SerializeField, Min(0f)] private float simulatedMotionSpeed = 0.35f;

        [Header("Visual group")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private CanvasGroup visualCanvasGroup;
        [SerializeField] private ScanRingPulse scanRingPulse;
        [SerializeField] private OrthogonalLiveLine[] liveLines;

        [Header("Transition")]
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(0f)] private float lineGrowStagger = 0.08f;

        private bool requestedVisible;
        private bool initialized;
        private bool simulationOwnsEntry;
        private float fadeTarget;
        private Coroutine growRoutine;
        private RectTransform canvasRect;

        public int ObjectID => mObjectID;
        public bool SimulateTrackingData => simulateTrackingData;
        public bool IsVisible => requestedVisible;
        public Vector2 LastTrackedScreenPosition { get; private set; }
        public float LastTrackedAngle { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            SetImmediate(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            initialized = false;
            SetImmediate(false);
        }

        private void OnDisable()
        {
            if (growRoutine != null) StopCoroutine(growRoutine);
            growRoutine = null;
            RemoveOwnedSimulationEntry();
        }

        private void LateUpdate()
        {
            ResolveReferences();
            if (simulateTrackingData)
                UpdateDllSimulation();
            else
                RemoveOwnedSimulationEntry();

            bool detected = TryGetActiveTracking(out DetectObjectDetails details);
            if (detected)
                ApplyTracking(details.objectCenterPosition, details.objectRotationAngle);
            SetRequestedVisible(detected);
            UpdateFade();
        }

        public void Configure(int objectId, RectTransform anchor, Canvas canvas, GameObject root, CanvasGroup group,
            ScanRingPulse pulse, params OrthogonalLiveLine[] lines)
        {
            mObjectID = objectId;
            trackingAnchor = anchor;
            parentCanvas = canvas;
            visualRoot = root;
            visualCanvasGroup = group;
            scanRingPulse = pulse;
            liveLines = lines;
            ResolveReferences();
            SetImmediate(false);
        }

        /// <summary>Compatibility overload for older single-line setups.</summary>
        public void Configure(int objectId, GameObject root, CanvasGroup group, WorldPointUIAnchor unusedWorldAnchor,
            ScanRingPulse pulse, OrthogonalLiveLine line)
        {
            Configure(objectId, line != null && line.transform.parent != null
                    ? line.transform.parent.Find("ScanRingRoot") as RectTransform
                    : null,
                GetComponentInParent<Canvas>(), root, group, pulse, line);
        }

        /// <summary>Enables/disables development tracking simulation.</summary>
        public void SetSimulation(bool enabled)
        {
            simulateTrackingData = enabled;
            simulatedDetected = enabled;
            if (!enabled) RemoveOwnedSimulationEntry();
        }

        /// <summary>Sets a simulated sample in reference-resolution coordinates and injects it into the DLL dictionary.</summary>
        public void SetSimulatedTrackingData(Vector2 referencePosition, float angle, bool detected = true)
        {
            simulatedPosition = referencePosition;
            simulatedAngle = angle;
            simulatedDetected = detected;
            simulateTrackingData = true;
            UpdateDllSimulation();
        }

        /// <summary>Direct development entry for code that already has screen-pixel tracking data.</summary>
        public void PushDllSimulationSample(Vector2 screenPosition, float angle, ObjectState state)
        {
            WriteDllSample(screenPosition, angle, state);
            // Treat direct pushes like an external development data source. The sample remains in the
            // dictionary until the caller replaces/removes it, matching the production DLL contract.
            simulationOwnsEntry = false;
        }

        public void StopSimulation(bool removeDictionaryEntry = true)
        {
            simulateTrackingData = false;
            if (removeDictionaryEntry) RemoveOwnedSimulationEntry();
        }

        private void ResolveReferences()
        {
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
            if (visualRoot == null && visualCanvasGroup != null) visualRoot = visualCanvasGroup.gameObject;
            if (liveLines == null) liveLines = new OrthogonalLiveLine[0];
        }

        private void UpdateDllSimulation()
        {
            Vector2 referencePosition = simulatedPosition;
            if (simulateMotion)
            {
                float phase = Time.unscaledTime * simulatedMotionSpeed * Mathf.PI * 2f;
                referencePosition += new Vector2(Mathf.Cos(phase) * simulatedMotionRadius.x,
                    Mathf.Sin(phase) * simulatedMotionRadius.y);
            }

            Vector2 safeResolution = new Vector2(Mathf.Max(1f, simulationReferenceResolution.x),
                Mathf.Max(1f, simulationReferenceResolution.y));
            Vector2 screenPosition = new Vector2(referencePosition.x * Screen.width / safeResolution.x,
                referencePosition.y * Screen.height / safeResolution.y);
            ObjectState state = simulatedDetected ? ObjectState.Move : ObjectState.End;
            WriteDllSample(screenPosition, simulatedAngle, state);
            simulationOwnsEntry = true;
        }

        private void WriteDllSample(Vector2 screenPosition, float angle, ObjectState state)
        {
            if (ObjectDetect.mObjectDic == null)
                ObjectDetect.mObjectDic = new Dictionary<int, DetectObjectDetails>();
            PointInfos point = new PointInfos(screenPosition, 0);
            ObjectDetect.mObjectDic[mObjectID] = new DetectObjectDetails(point, point, point, angle, screenPosition,
                mObjectID, state, 0f, 0f, 0, 0L);
        }

        private void RemoveOwnedSimulationEntry()
        {
            if (!simulationOwnsEntry) return;
            ObjectDetect.mObjectDic?.Remove(mObjectID);
            simulationOwnsEntry = false;
        }

        private bool TryGetActiveTracking(out DetectObjectDetails details)
        {
            details = default;
            if (ObjectDetect.mObjectDic == null || !ObjectDetect.mObjectDic.TryGetValue(mObjectID, out details))
                return false;
            return details.objectstate == ObjectState.Start || details.objectstate == ObjectState.Move;
        }

        private void ApplyTracking(Vector2 screenPosition, float angle)
        {
            if (trackingAnchor == null || canvasRect == null) return;
            Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 local))
                return;

            LastTrackedScreenPosition = screenPosition;
            LastTrackedAngle = angle;
            float t = followSpeed <= 0f ? 1f : 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            trackingAnchor.anchoredPosition = Vector2.Lerp(trackingAnchor.anchoredPosition, local, t);
            if (followDetectedAngle)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, -angle);
                trackingAnchor.localRotation = Quaternion.Slerp(trackingAnchor.localRotation, targetRotation, t);
            }
        }

        private void SetRequestedVisible(bool visible)
        {
            if (initialized && visible == requestedVisible) return;
            initialized = true;
            requestedVisible = visible;
            fadeTarget = visible ? 1f : 0f;

            if (visible)
            {
                if (visualRoot != null) visualRoot.SetActive(true);
                if (scanRingPulse != null) scanRingPulse.enabled = true;
                if (growRoutine != null) StopCoroutine(growRoutine);
                growRoutine = StartCoroutine(PlayLineGrowth());
            }
        }

        private IEnumerator PlayLineGrowth()
        {
            for (int i = 0; i < liveLines.Length; i++)
            {
                OrthogonalLiveLine line = liveLines[i];
                if (line == null) continue;
                line.enabled = true;
                line.PlayGrowOnce();
                if (lineGrowStagger > 0f) yield return new WaitForSecondsRealtime(lineGrowStagger);
            }
            growRoutine = null;
        }

        private void UpdateFade()
        {
            if (visualCanvasGroup == null) return;
            visualCanvasGroup.alpha = Mathf.MoveTowards(visualCanvasGroup.alpha, fadeTarget,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));
            bool interactive = visualCanvasGroup.alpha > 0.99f && requestedVisible;
            visualCanvasGroup.interactable = interactive;
            visualCanvasGroup.blocksRaycasts = interactive;
            if (!requestedVisible && visualCanvasGroup.alpha <= 0f)
            {
                if (growRoutine != null) StopCoroutine(growRoutine);
                growRoutine = null;
                if (scanRingPulse != null) scanRingPulse.enabled = false;
                for (int i = 0; i < liveLines.Length; i++)
                    if (liveLines[i] != null) liveLines[i].enabled = false;
                if (visualRoot != null) visualRoot.SetActive(false);
            }
        }

        private void SetImmediate(bool visible)
        {
            requestedVisible = visible;
            fadeTarget = visible ? 1f : 0f;
            if (visualCanvasGroup != null)
            {
                visualCanvasGroup.alpha = fadeTarget;
                visualCanvasGroup.interactable = visible;
                visualCanvasGroup.blocksRaycasts = visible;
            }
            if (scanRingPulse != null) scanRingPulse.enabled = visible;
            for (int i = 0; i < liveLines.Length; i++)
                if (liveLines[i] != null) liveLines[i].enabled = visible;
            if (visualRoot != null) visualRoot.SetActive(visible);
        }
    }
}
