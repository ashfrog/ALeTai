using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仅在 isOn==true 时呼吸，彻底杜绝取消选中后还在呼吸的情况。
/// 依赖 ToggleButtonWithImage.onValueChanged 事件。
/// </summary>
[RequireComponent(typeof(ToggleButtonWithImage))]
public class ToggleBreathingEffect : MonoBehaviour
{
    [Header("呼吸灯效果设置")]
    [SerializeField] private float breathPeriod = 2f;
    [SerializeField] private float minIntensity = 0.3f;
    [SerializeField] private float maxIntensity = 0.8f;
    [SerializeField] private Color breathColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private AnimationCurve breathCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private ToggleButtonWithImage toggleButton;
    private Image buttonImage;
    private Image overlayImage;
    private GameObject breathingOverlay;
    private Coroutine breathCo;

    void Start()
    {
        toggleButton = GetComponent<ToggleButtonWithImage>();
        buttonImage = GetComponent<Image>();
        CreateBreathingOverlay();

        // 监听 isOn 变化
        toggleButton.onValueChanged.AddListener(OnToggleChanged);
        // 初始化
        OnToggleChanged(toggleButton.IsOn);
    }

    void OnDestroy()
    {
        if (toggleButton)
            toggleButton.onValueChanged.RemoveListener(OnToggleChanged);
        if (breathingOverlay)
            DestroyImmediate(breathingOverlay);
    }

    void CreateBreathingOverlay()
    {
        breathingOverlay = new GameObject("BreathingOverlay");
        breathingOverlay.transform.SetParent(transform, false);
        overlayImage = breathingOverlay.AddComponent<Image>();

        if (buttonImage && buttonImage.sprite)
        {
            overlayImage.sprite = buttonImage.sprite;
            overlayImage.type = buttonImage.type;
            overlayImage.preserveAspect = buttonImage.preserveAspect;
        }

        overlayImage.color = new Color(breathColor.r, breathColor.g, breathColor.b, 0);

        var overlayRect = breathingOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;

        SetOverlayOrder();
        breathingOverlay.SetActive(false);
    }

    void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            breathingOverlay.SetActive(true);
            if (breathCo != null) StopCoroutine(breathCo);
            breathCo = StartCoroutine(Breathing());
        }
        else
        {
            if (breathCo != null)
            {
                StopCoroutine(breathCo);
                breathCo = null;
            }
            breathingOverlay.SetActive(false);
        }
    }

    System.Collections.IEnumerator Breathing()
    {
        while (true)
        {
            float t = (Time.time % breathPeriod) / breathPeriod;
            float curve = breathCurve.Evaluate((Mathf.Sin(t * 2f * Mathf.PI) + 1f) * 0.5f);
            float a = Mathf.Lerp(minIntensity, maxIntensity, curve);
            var c = breathColor;
            c.a = a;
            overlayImage.color = c;
            yield return null;
        }
    }

    void SetOverlayOrder()
    {
        var tmpText = GetComponentInChildren<TMPro.TMP_Text>();
        if (tmpText)
            breathingOverlay.transform.SetSiblingIndex(Mathf.Max(0, tmpText.transform.GetSiblingIndex() - 1));
        else
            breathingOverlay.transform.SetAsFirstSibling();
    }
}