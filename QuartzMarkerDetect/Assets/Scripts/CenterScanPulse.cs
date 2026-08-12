using UnityEngine;
using UnityEngine.UI;

public sealed class CenterScanPulse : MonoBehaviour
{
    [SerializeField] private RectTransform dashRing;
    [SerializeField] private Graphic redRing;
    [SerializeField] private float rotateSpeed = 25f;
    [SerializeField] private float pulseSpeed = 2f;

    private Color initialColor;

    private void Awake()
    {
        if (redRing != null) initialColor = redRing.color;
    }

    private void Update()
    {
        if (dashRing != null) dashRing.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        if (redRing == null) return;
        float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        redRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, wave);
        Color color = initialColor;
        color.a = Mathf.Lerp(0.45f, 1f, wave);
        redRing.color = color;
    }

    public void Configure(RectTransform dash, Graphic red)
    {
        dashRing = dash;
        redRing = red;
        if (redRing != null) initialColor = redRing.color;
    }
}
