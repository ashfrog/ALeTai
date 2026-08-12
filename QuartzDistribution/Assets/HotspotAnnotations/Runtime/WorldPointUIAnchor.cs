using UnityEngine;

namespace QuartzDistribution.HotspotAnnotations
{
    /// <summary>Projects a world-space Transform onto a RectTransform inside a screen-space Canvas.</summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class WorldPointUIAnchor : MonoBehaviour
    {
        [Header("World target")]
        [SerializeField] private Transform worldTarget;
        [SerializeField] private Camera targetCamera;

        [Header("UI")]
        [SerializeField] private RectTransform uiAnchor;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Vector2 screenOffset;

        [Header("Visibility")]
        [SerializeField] private bool invalidateBehindCamera = true;
        [SerializeField] private bool invalidateOutsideViewport = true;
        [SerializeField, Min(0f)] private float viewportPadding = 0.02f;

        public Transform WorldTarget => worldTarget;
        public bool IsProjectionValid { get; private set; }
        public Vector3 LastScreenPosition { get; private set; }

        private RectTransform canvasRect;

        private void Reset()
        {
            uiAnchor = transform as RectTransform;
            parentCanvas = GetComponentInParent<Canvas>();
            targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshNow();
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        public void SetWorldTarget(Transform target)
        {
            worldTarget = target;
            RefreshNow();
        }

        public void Configure(Transform target, RectTransform anchor, Canvas canvas, Camera camera = null)
        {
            worldTarget = target;
            uiAnchor = anchor;
            parentCanvas = canvas;
            targetCamera = camera;
            ResolveReferences();
            RefreshNow();
        }

        public bool RefreshNow()
        {
            ResolveReferences();
            if (worldTarget == null || uiAnchor == null || parentCanvas == null || targetCamera == null || canvasRect == null)
            {
                IsProjectionValid = false;
                return false;
            }

            Vector3 viewport = targetCamera.WorldToViewportPoint(worldTarget.position);
            bool inFront = viewport.z > targetCamera.nearClipPlane;
            bool inViewport = viewport.x >= -viewportPadding && viewport.x <= 1f + viewportPadding
                              && viewport.y >= -viewportPadding && viewport.y <= 1f + viewportPadding;
            IsProjectionValid = (!invalidateBehindCamera || inFront)
                                && (!invalidateOutsideViewport || inViewport);
            if (!IsProjectionValid)
                return false;

            LastScreenPosition = targetCamera.WorldToScreenPoint(worldTarget.position);
            LastScreenPosition += (Vector3)screenOffset;
            Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : parentCanvas.worldCamera != null ? parentCanvas.worldCamera : targetCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, LastScreenPosition, eventCamera, out Vector2 localPoint))
            {
                IsProjectionValid = false;
                return false;
            }

            RectTransform anchorParent = uiAnchor.parent as RectTransform;
            if (anchorParent == canvasRect)
            {
                uiAnchor.anchoredPosition = localPoint;
            }
            else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, LastScreenPosition, eventCamera, out Vector3 worldPoint))
            {
                uiAnchor.position = worldPoint;
            }
            else
            {
                IsProjectionValid = false;
            }

            return IsProjectionValid;
        }

        private void ResolveReferences()
        {
            if (uiAnchor == null)
                uiAnchor = transform as RectTransform;
            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();
            if (targetCamera == null)
                targetCamera = parentCanvas != null && parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
            canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        }
    }
}
