using System;
using System.Collections.Generic;
using DG.Tweening;
using RenderHeads.Media.AVProVideo;
using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution
{
    [DisallowMultipleComponent]
    public sealed class QuartzMapApplication : MonoBehaviour
    {
        [Header("场景节点")]
        [SerializeField] private RectTransform mapRect;
        [SerializeField] private GameObject nationalLayer;
        [SerializeField] private GameObject altayLayer;
        [SerializeField] private Button nationalTabButton;
        [SerializeField] private Button altayTabButton;
        [SerializeField] private Image nationalTabBackground;
        [SerializeField] private Image altayTabBackground;
        [SerializeField] private Sprite nationalTabSelectedSprite;
        [SerializeField] private Sprite nationalTabDefaultSprite;
        [SerializeField] private Sprite altayTabSelectedSprite;
        [SerializeField] private Sprite altayTabDefaultSprite;
        [SerializeField] private GameObject nationalLegendPanel;
        [SerializeField] private GameObject altayLegendPanel;
        [SerializeField] private GameObject nationalPageTitle;
        [SerializeField] private GameObject altayPageTitle;
        [SerializeField] private GameObject altaySupplementalUi;
        [SerializeField] private CanvasGroup infoCard;
        [SerializeField] private Text infoText;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Text debugText;

        [Header("背景视频")]
        [SerializeField] private MediaPlayer backgroundMediaPlayer;
        [SerializeField] private DisplayUGUI backgroundVideoDisplay;
        [SerializeField] private string nationalVideoPath = "石英的分布_全国.mp4";
        [SerializeField] private string altayVideoPath = "石英的分布_阿勒泰.mp4";

        [Header("锚点坐标")]
        [SerializeField] private Vector2 nationalReferenceSize = new Vector2(1410f, 925f);
        [SerializeField] private Vector2 altayReferenceSize = new Vector2(1410f, 925f);
        [SerializeField] private KeyCode debugToggleKey = KeyCode.F;

        private readonly HashSet<string> selectedTypeIds = new HashSet<string>();
        private readonly List<QuartzLegendItem> legendItems = new List<QuartzLegendItem>();
        private readonly List<MarkerHighlightAnimator> nationalMarkers = new List<MarkerHighlightAnimator>();
        private readonly List<MarkerHighlightAnimator> altayMarkers = new List<MarkerHighlightAnimator>();
        private bool showingAltay;
        private bool debugVisible;
        private string currentBackgroundVideoPath;

        public event Action<IReadOnlyCollection<string>> OnLegendSelectionChanged;

        private void Awake()
        {
            BindSceneNodes();
            SetMap(false, true);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (backgroundMediaPlayer != null) backgroundMediaPlayer.CloseMedia();
        }

        private void Update()
        {
            if (Input.GetKeyDown(debugToggleKey))
            {
                debugVisible = !debugVisible;
                if (debugPanel != null) debugPanel.SetActive(debugVisible);
            }

            if (!debugVisible || mapRect == null || debugText == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out local)) return;
            Vector2 size = mapRect.rect.size;
            Vector2 referenceSize = showingAltay ? altayReferenceSize : nationalReferenceSize;
            float x = (local.x + size.x * mapRect.pivot.x) / Mathf.Max(1f, size.x) * referenceSize.x;
            float y = (size.y * (1f - mapRect.pivot.y) - local.y) / Mathf.Max(1f, size.y) * referenceSize.y;
            debugText.text = string.Format("锚点坐标  /  ANCHOR\nX: {0:F1} px\nY: {1:F1} px\nF 隐藏 · 点击复制", x, y);
            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(mapRect, Input.mousePosition))
            {
                GUIUtility.systemCopyBuffer = string.Format("{0:F1}, {1:F1}", x, y);
            }
        }

        public void BindSceneNodes()
        {
            UnbindSceneNodes();
            legendItems.AddRange(GetComponentsInChildren<QuartzLegendItem>(true));
            MarkerHighlightAnimator[] allMarkers = GetComponentsInChildren<MarkerHighlightAnimator>(true);
            foreach (MarkerHighlightAnimator marker in allMarkers)
            {
                marker.Initialize(this);
                if (marker.transform.IsChildOf(altayLayer.transform)) altayMarkers.Add(marker);
                else nationalMarkers.Add(marker);
            }

            foreach (QuartzLegendItem item in legendItems)
            {
                item.Initialize();
                QuartzLegendItem captured = item;
                item.Toggle.onValueChanged.AddListener(value => OnLegendChanged(captured, value));
                if (item.Toggle.isOn) selectedTypeIds.Add(item.ResourceTypeId);
            }

            if (nationalTabButton != null) nationalTabButton.onClick.AddListener(ShowNational);
            if (altayTabButton != null) altayTabButton.onClick.AddListener(ShowAltay);
            if (debugPanel != null) debugPanel.SetActive(false);
            if (infoCard != null)
            {
                infoCard.alpha = 0f;
                infoCard.blocksRaycasts = false;
            }
        }

        public void ShowNational()
        {
            SetMap(false, false);
        }

        public void ShowAltay()
        {
            SetMap(true, false);
        }

        public void ShowMarkerInfo(MarkerHighlightAnimator marker)
        {
            if (infoCard == null || infoText == null) return;
            infoText.text = string.Format(
                "<size=34><color=#72E6FF><b>{0}</b></color></size>\n<size=24><color=#E4FAFF>{1}</color></size>",
                marker.ResourceDisplayName,
                marker.Note);
            infoCard.DOKill();
            infoCard.transform.DOKill();
            infoCard.alpha = 0f;
            infoCard.blocksRaycasts = false;
            infoCard.transform.localScale = Vector3.one * .96f;

            Sequence sequence = DOTween.Sequence().SetTarget(infoCard).SetId(this);
            sequence.Append(infoCard.DOFade(1f, .18f));
            sequence.Join(infoCard.transform.DOScale(1f, .22f).SetEase(Ease.OutCubic));
            sequence.AppendInterval(2f);
            sequence.Append(infoCard.DOFade(0f, .5f).SetEase(Ease.InQuad));
        }

        private void UnbindSceneNodes()
        {
            foreach (QuartzLegendItem item in legendItems)
                if (item != null && item.Toggle != null) item.Toggle.onValueChanged.RemoveAllListeners();
            if (nationalTabButton != null) nationalTabButton.onClick.RemoveAllListeners();
            if (altayTabButton != null) altayTabButton.onClick.RemoveAllListeners();
            legendItems.Clear();
            nationalMarkers.Clear();
            altayMarkers.Clear();
            selectedTypeIds.Clear();
        }

        private void OnLegendChanged(QuartzLegendItem item, bool selected)
        {
            if (selected) selectedTypeIds.Add(item.ResourceTypeId);
            else selectedTypeIds.Remove(item.ResourceTypeId);
            item.SetSelected(selected);
            RefreshMarkers();
            if (OnLegendSelectionChanged != null) OnLegendSelectionChanged(selectedTypeIds);
        }

        private void SetMap(bool altay, bool immediate)
        {
            showingAltay = altay;
            SetBackgroundVideo(altay ? altayVideoPath : nationalVideoPath);
            if (nationalLayer != null) nationalLayer.SetActive(!altay);
            if (altayLayer != null) altayLayer.SetActive(altay);
            if (nationalLegendPanel != null) nationalLegendPanel.SetActive(!altay);
            if (altayLegendPanel != null) altayLegendPanel.SetActive(altay);
            if (nationalPageTitle != null) nationalPageTitle.SetActive(!altay);
            if (altayPageTitle != null) altayPageTitle.SetActive(altay);
            if (altaySupplementalUi != null) altaySupplementalUi.SetActive(altay);
            SetTabSprite(nationalTabBackground, altay ? nationalTabDefaultSprite : nationalTabSelectedSprite);
            SetTabSprite(altayTabBackground, altay ? altayTabSelectedSprite : altayTabDefaultSprite);
            RefreshMarkers();

            GameObject active = altay ? altayLayer : nationalLayer;
            if (!immediate && active != null)
            {
                CanvasGroup group = active.GetComponent<CanvasGroup>();
                if (group == null) group = active.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.DOFade(1f, .35f).SetUpdate(true).SetId(this);
            }
        }

        private void SetBackgroundVideo(string relativePath)
        {
            if (backgroundMediaPlayer == null || string.IsNullOrEmpty(relativePath)) return;

            backgroundMediaPlayer.Loop = true;
            if (currentBackgroundVideoPath == relativePath)
            {
                backgroundMediaPlayer.Play();
                return;
            }

            currentBackgroundVideoPath = relativePath;
            backgroundMediaPlayer.OpenMedia(
                MediaPathType.RelativeToStreamingAssetsFolder,
                relativePath,
                true);
        }

        private void RefreshMarkers()
        {
            bool none = selectedTypeIds.Count == 0;
            List<MarkerHighlightAnimator> active = showingAltay ? altayMarkers : nationalMarkers;
            foreach (MarkerHighlightAnimator marker in active)
            {
                if (none) marker.ShowNormal();
                else if (selectedTypeIds.Contains(marker.ResourceTypeId)) marker.PlayHighlight();
                else marker.ShowDimmed();
            }
        }

        private static void SetTabSprite(Image background, Sprite sprite)
        {
            if (background == null) return;
            if (sprite != null) background.sprite = sprite;
            background.color = Color.white;
        }

#if UNITY_EDITOR
        public void EditorAssign(RectTransform map, GameObject national, GameObject altay, Button nationalButton,
            Button altayButton, Image nationalBackground, Image altayBackground, CanvasGroup card, Text cardText,
            GameObject anchorPanel, Text anchorText, Sprite nationalSelected, Sprite nationalDefault,
            Sprite altaySelected, Sprite altayDefault)
        {
            mapRect = map;
            nationalLayer = national;
            altayLayer = altay;
            nationalTabButton = nationalButton;
            altayTabButton = altayButton;
            nationalTabBackground = nationalBackground;
            altayTabBackground = altayBackground;
            nationalTabSelectedSprite = nationalSelected;
            nationalTabDefaultSprite = nationalDefault;
            altayTabSelectedSprite = altaySelected;
            altayTabDefaultSprite = altayDefault;
            infoCard = card;
            infoText = cardText;
            debugPanel = anchorPanel;
            debugText = anchorText;
        }
#endif
    }

}
