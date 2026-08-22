using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>点击面板在原始尺寸与放大尺寸之间切换。</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class PanelImageZoomToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Min(1f)] private float enlargedScale = 2f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.18f;
    [SerializeField] private bool moveAwayFromCenter = true;
    [SerializeField, Min(0f)] private float centerClearance = 24f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 normalScale;
    private Vector2 normalPosition;
    private Coroutine scaleRoutine;
    private bool enlarged;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        normalScale = rectTransform.localScale;
        normalPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (canvasGroup != null && canvasGroup.alpha <= 0.01f) return;
        SetEnlarged(!enlarged);
    }

    public void SetEnlarged(bool value)
    {
        enlarged = value;
        Vector3 targetScale = value ? normalScale * enlargedScale : normalScale;
        Vector2 targetPosition = value ? GetEnlargedPosition() : normalPosition;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        if (transitionDuration <= 0f)
        {
            rectTransform.localScale = targetScale;
            rectTransform.anchoredPosition = targetPosition;
            scaleRoutine = null;
            return;
        }
        scaleRoutine = StartCoroutine(AnimateTransform(targetScale, targetPosition));
    }

    public void ResetZoom()
    {
        enlarged = false;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = null;
        rectTransform.localScale = normalScale;
        rectTransform.anchoredPosition = normalPosition;
    }

    private Vector2 GetEnlargedPosition()
    {
        if (!moveAwayFromCenter) return normalPosition;

        float horizontalDirection = normalPosition.x < 0f ? -1f : 1f;
        float normalWidth = rectTransform.rect.width * Mathf.Abs(normalScale.x);
        float addedHalfWidth = normalWidth * Mathf.Max(0f, enlargedScale - 1f) * 0.5f;
        return normalPosition + Vector2.right * horizontalDirection * (addedHalfWidth + centerClearance);
    }

    private IEnumerator AnimateTransform(Vector3 targetScale, Vector2 targetPosition)
    {
        Vector3 startScale = rectTransform.localScale;
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = t * t * (3f - 2f * t);
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
            yield return null;
        }
        rectTransform.localScale = targetScale;
        rectTransform.anchoredPosition = targetPosition;
        scaleRoutine = null;
    }
}
