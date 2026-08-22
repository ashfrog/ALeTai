using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>点击面板在原始尺寸与放大尺寸之间切换。</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class PanelImageZoomToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Min(1f)] private float enlargedScale = 2f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.18f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 normalScale;
    private Coroutine scaleRoutine;
    private bool enlarged;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        normalScale = rectTransform.localScale;
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
        Vector3 target = value ? normalScale * enlargedScale : normalScale;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        if (transitionDuration <= 0f)
        {
            rectTransform.localScale = target;
            scaleRoutine = null;
            return;
        }
        scaleRoutine = StartCoroutine(AnimateScale(target));
    }

    public void ResetZoom()
    {
        enlarged = false;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = null;
        rectTransform.localScale = normalScale;
    }

    private IEnumerator AnimateScale(Vector3 target)
    {
        Vector3 start = rectTransform.localScale;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = t * t * (3f - 2f * t);
            rectTransform.localScale = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }
        rectTransform.localScale = target;
        scaleRoutine = null;
    }
}
