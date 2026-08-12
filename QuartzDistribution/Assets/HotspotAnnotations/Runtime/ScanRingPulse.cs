using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution.HotspotAnnotations
{
    [DisallowMultipleComponent]
    public sealed class ScanRingPulse : MonoBehaviour
    {
        [Header("Ring references")]
        [SerializeField] private RectTransform outerDashRing;
        [SerializeField] private RectTransform tickRing;
        [SerializeField] private Graphic redPulseRing;
        [SerializeField] private Graphic redGlowRing;

        [Header("Rotation (degrees/second)")]
        [SerializeField] private float outerRingSpeed = 22f;
        [SerializeField] private float tickRingSpeed = -14f;

        [Header("Red pulse")]
        [SerializeField, Min(0.01f)] private float pulseSpeed = 1.8f;
        [SerializeField] private Vector2 pulseScaleRange = new Vector2(0.92f, 1.08f);
        [SerializeField] private Vector2 pulseAlphaRange = new Vector2(0.35f, 1f);

        private float pulseTime;
        private Vector3 redInitialScale = Vector3.one;
        private Vector3 glowInitialScale = Vector3.one;
        private Color redInitialColor = Color.white;
        private Color glowInitialColor = Color.white;

        private void Awake() => CacheInitialState();
        private void OnEnable() => CacheInitialState();

        private void Update()
        {
            float dt = Time.deltaTime;
            if (outerDashRing != null)
                outerDashRing.Rotate(0f, 0f, outerRingSpeed * dt);
            if (tickRing != null)
                tickRing.Rotate(0f, 0f, tickRingSpeed * dt);

            pulseTime += dt * pulseSpeed;
            float wave = (Mathf.Sin(pulseTime * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(pulseScaleRange.x, pulseScaleRange.y, wave);
            float alpha = Mathf.Lerp(pulseAlphaRange.x, pulseAlphaRange.y, wave);
            ApplyPulse(redPulseRing, redInitialScale, redInitialColor, scale, alpha);
            ApplyPulse(redGlowRing, glowInitialScale, glowInitialColor, Mathf.Lerp(1f, 1.18f, wave), alpha * 0.45f);
        }

        private void OnDisable()
        {
            pulseTime = 0f;
            Restore(redPulseRing, redInitialScale, redInitialColor);
            Restore(redGlowRing, glowInitialScale, glowInitialColor);
            if (outerDashRing != null) outerDashRing.localRotation = Quaternion.identity;
            if (tickRing != null) tickRing.localRotation = Quaternion.identity;
        }

        public void Configure(RectTransform dashes, RectTransform ticks, Graphic red, Graphic glow)
        {
            outerDashRing = dashes;
            tickRing = ticks;
            redPulseRing = red;
            redGlowRing = glow;
            CacheInitialState();
        }

        private void CacheInitialState()
        {
            if (redPulseRing != null)
            {
                redInitialScale = redPulseRing.rectTransform.localScale;
                redInitialColor = redPulseRing.color;
            }
            if (redGlowRing != null)
            {
                glowInitialScale = redGlowRing.rectTransform.localScale;
                glowInitialColor = redGlowRing.color;
            }
        }

        private static void ApplyPulse(Graphic graphic, Vector3 initialScale, Color initialColor, float scale, float alpha)
        {
            if (graphic == null) return;
            graphic.rectTransform.localScale = initialScale * scale;
            Color color = initialColor;
            color.a = initialColor.a * alpha;
            graphic.color = color;
        }

        private static void Restore(Graphic graphic, Vector3 initialScale, Color initialColor)
        {
            if (graphic == null) return;
            graphic.rectTransform.localScale = initialScale;
            graphic.color = initialColor;
        }
    }
}
