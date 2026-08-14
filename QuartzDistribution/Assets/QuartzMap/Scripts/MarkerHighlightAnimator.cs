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
        [SerializeField] private Button markerButton;

        private QuartzMapApplication application;

        public string Province { get { return province; } }
        public string ResourceTypeId { get { return resourceTypeId; } }
        public string ResourceDisplayName { get { return resourceDisplayName; } }
        public string Note { get { return note; } }

        public void Initialize(QuartzMapApplication owner)
        {
            application = owner;
            markerGraphic.color = markerColor;
            markerButton.onClick.RemoveAllListeners();
            markerButton.onClick.AddListener(ShowInfo);
            ShowNormal();
        }

        public void PlayHighlight()
        {
            StopTween();
            markerGraphic.color = markerColor;
            transform.localScale = Vector3.one;
            transform.DOScale(1.65f, .55f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetId(application);
            markerGraphic.DOFade(.55f, .55f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetId(application);
        }

        public void ShowDimmed()
        {
            StopTween();
            Color color = markerColor;
            color.a = .2f;
            markerGraphic.color = color;
            transform.localScale = Vector3.one * .82f;
        }

        public void ShowNormal()
        {
            StopTween();
            Color color = markerColor;
            color.a = .88f;
            markerGraphic.color = color;
            transform.localScale = Vector3.one;
        }

        private void ShowInfo()
        {
            if (application != null) application.ShowMarkerInfo(this);
        }

        private void StopTween()
        {
            transform.DOKill();
            if (markerGraphic != null) markerGraphic.DOKill();
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
        }
#endif
    }
}
