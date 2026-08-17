using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution
{
    public sealed class MarkerHighlightAnimator : MonoBehaviour
    {
        [Header("直接在场景中编辑")]
        [SerializeField] private string province;
        [SerializeField] private string resourceTypeId;
        [SerializeField] private string resourceDisplayName;
        [TextArea(2, 4)] [SerializeField] private string note;
        [SerializeField] private Color markerColor = Color.white;
        [Header("节点引用")]
        [SerializeField] private Graphic markerGraphic;
        [SerializeField] private Outline markerOutline;
        [SerializeField] private Button markerButton;

        // Keep the marker's normal scale as the baseline. The previous 1.65 peak
        // made selected markers overpower nearby markers, so only half of that
        // enlargement is retained: 1 + (1.65 - 1) / 2 = 1.325.
        private const float HighlightMaxScale = 1.325f;
        private const float HighlightDuration = .55f;

        private QuartzMapApplication application;
        private Color normalOutlineColor;
        private bool hasNormalOutlineColor;

        public string Province { get { return province; } }
        public string ResourceTypeId { get { return resourceTypeId; } }
        public string ResourceDisplayName { get { return resourceDisplayName; } }
        public string Note { get { return note; } }

        public void Initialize(QuartzMapApplication owner)
        {
            application = owner;
            markerGraphic.color = markerColor;
            if (markerOutline != null)
            {
                normalOutlineColor = markerOutline.effectColor;
                hasNormalOutlineColor = true;
            }
            markerButton.onClick.RemoveAllListeners();
            markerButton.onClick.AddListener(ShowInfo);
            ShowNormal();
        }

        public void PlayHighlight()
        {
            StopTween();
            markerGraphic.color = markerColor;
            transform.localScale = Vector3.one;
            if (markerOutline != null)
            {
                Color outlineColor = GetHighlightOutlineColor();
                outlineColor.a = .9f;
                markerOutline.effectColor = outlineColor;
                markerOutline.DOColor(outlineColor, HighlightDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetId(application);
            }
            transform.DOScale(HighlightMaxScale, HighlightDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId(application);
        }

        public void ShowDimmed()
        {
            StopTween();
            Color color = markerColor;
            color.a = .2f;
            markerGraphic.color = color;
            RestoreNormalOutlineColor();
            transform.localScale = Vector3.one * .82f;
        }

        public void ShowNormal()
        {
            StopTween();
            Color color = markerColor;
            color.a = .88f;
            markerGraphic.color = color;
            RestoreNormalOutlineColor();
            transform.localScale = Vector3.one;
        }

        private void ShowInfo()
        {
            if (application != null && application.EnableMarkerInfoPopup) application.ShowMarkerInfo(this);
        }

        private void StopTween()
        {
            transform.DOKill();
            if (markerGraphic != null) markerGraphic.DOKill();
            if (markerOutline != null) markerOutline.DOKill();
        }

        private void RestoreNormalOutlineColor()
        {
            if (markerOutline != null && hasNormalOutlineColor)
                markerOutline.effectColor = normalOutlineColor;
        }

        private Color GetHighlightOutlineColor()
        {
            // Preserve the marker hue while capping brightness so the pulse never
            // washes out to pure white and the original fill color stays legible.
            float hue;
            float saturation;
            float value;
            Color.RGBToHSV(markerColor, out hue, out saturation, out value);
            value = Mathf.Min(.82f, Mathf.Max(value, .55f) * 1.15f);
            saturation = Mathf.Max(.55f, saturation);
            Color color = Color.HSVToRGB(hue, saturation, value);
            color.a = 1f;
            return color;
        }

#if UNITY_EDITOR
        public void EditorConfigure(string area, string typeId, string typeName, string details, Color color,
            Graphic graphic, Button button)
        {
            province = area;
            resourceTypeId = typeId;
            resourceDisplayName = typeName;
            note = details;
            markerColor = color;
            markerGraphic = graphic;
            markerButton = button;
            markerGraphic.color = color;
            markerOutline = graphic != null ? graphic.GetComponent<Outline>() : null;
        }
#endif
    }
}
