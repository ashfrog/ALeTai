using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在固定 viewport 内横向滚动过长的视频文件名。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPVideoNameMarquee : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 45f;
    [SerializeField] private float edgePause = 1f;

    private TMP_Text label;
    private RectTransform labelRect;
    private RectTransform viewportRect;
    private GameObject viewportObject;
    private Coroutine scrollCoroutine;
    private string currentValue;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        labelRect = label.rectTransform;
        CreateViewport();
    }

    private void OnDisable()
    {
        StopScrolling();
    }

    private void OnDestroy()
    {
        if (viewportObject != null)
        {
            Destroy(viewportObject);
        }
    }

    public void SetText(string value)
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
            labelRect = label.rectTransform;
        }

        currentValue = string.IsNullOrEmpty(value) ? "无视频" : value;
        label.text = currentValue;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        RestartScrolling();
    }

    private void CreateViewport()
    {
        if (viewportRect != null)
        {
            return;
        }

        Transform originalParent = labelRect.parent;
        int siblingIndex = labelRect.GetSiblingIndex();
        Vector2 oldAnchorMin = labelRect.anchorMin;
        Vector2 oldAnchorMax = labelRect.anchorMax;
        Vector2 oldPosition = labelRect.anchoredPosition;
        Vector2 oldSize = labelRect.sizeDelta;
        Vector2 oldPivot = labelRect.pivot;

        viewportObject = new GameObject(label.name + "Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(originalParent, false);
        viewportObject.transform.SetSiblingIndex(siblingIndex);

        viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = oldAnchorMin;
        viewportRect.anchorMax = oldAnchorMax;
        viewportRect.anchoredPosition = oldPosition;
        viewportRect.sizeDelta = oldSize;
        viewportRect.pivot = oldPivot;

        labelRect.SetParent(viewportRect, false);
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(oldSize.x, oldSize.y);
    }

    private void RestartScrolling()
    {
        StopScrolling();
        if (!isActiveAndEnabled || viewportRect == null || label == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollCoroutine = StartCoroutine(ScrollWhenNeeded());
    }

    private void StopScrolling()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        if (labelRect != null)
        {
            labelRect.anchoredPosition = Vector2.zero;
        }
    }

    private IEnumerator ScrollWhenNeeded()
    {
        while (isActiveAndEnabled && label != null && viewportRect != null)
        {
            Canvas.ForceUpdateCanvases();
            float viewportWidth = Mathf.Max(1f, viewportRect.rect.width);
            float contentWidth = label.GetPreferredValues(currentValue, 0f, 0f).x;
            float distance = contentWidth - viewportWidth;

            labelRect.sizeDelta = new Vector2(Mathf.Max(viewportWidth, contentWidth), labelRect.sizeDelta.y);
            labelRect.anchoredPosition = Vector2.zero;
            if (distance <= 1f)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(edgePause);
            yield return MoveTo(-distance, distance / Mathf.Max(1f, scrollSpeed));
            yield return new WaitForSecondsRealtime(edgePause);

            // LED 跑马灯效果：只向左滚动，显示完末尾后直接从第一个字重新开始。
            labelRect.anchoredPosition = Vector2.zero;
        }
    }

    private IEnumerator MoveTo(float targetX, float duration)
    {
        Vector2 start = labelRect.anchoredPosition;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration && isActiveAndEnabled)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            labelRect.anchoredPosition = new Vector2(Mathf.Lerp(start.x, targetX, t), start.y);
            yield return null;
        }

        labelRect.anchoredPosition = new Vector2(targetX, start.y);
    }
}
