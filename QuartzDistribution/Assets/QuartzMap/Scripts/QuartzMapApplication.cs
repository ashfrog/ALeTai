using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuartzDistribution
{
    [DisallowMultipleComponent]
    public sealed class QuartzMapApplication : MonoBehaviour
    {
        private readonly HashSet<string> selectedTypeIds = new HashSet<string>();
        private readonly List<MarkerHighlightAnimator> nationalMarkers = new List<MarkerHighlightAnimator>();
        private readonly List<MarkerHighlightAnimator> altayMarkers = new List<MarkerHighlightAnimator>();
        private readonly List<LegendItemView> legends = new List<LegendItemView>();
        private ResourceTypeCollection resourceTypes;
        private Font font;
        private RectTransform mapRect;
        private GameObject nationalLayer;
        private GameObject altayLayer;
        private Image nationalTab;
        private Image altayTab;
        private Text debugText;
        private Text infoText;
        private CanvasGroup infoGroup;
        private bool debugVisible;
        private bool showingAltay;

        public event Action<IReadOnlyCollection<string>> OnLegendSelectionChanged;

        private static readonly Color Cyan = ParseHexColor("#00BDF2");

        private void Awake()
        {
            Rebuild();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                debugVisible = !debugVisible;
                if (debugText != null) debugText.transform.parent.gameObject.SetActive(debugVisible);
            }

            if (!debugVisible || mapRect == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out local)) return;
            Vector2 size = mapRect.rect.size;
            float x = local.x + size.x * mapRect.pivot.x;
            float y = size.y * (1f - mapRect.pivot.y) - local.y;
            MapConfigData config = QuartzDataLoader.LoadMap(showingAltay ? "altay" : "national");
            if (config == null || config.mapImageSize == null) return;
            x = x / Mathf.Max(1f, size.x) * config.mapImageSize.width;
            y = y / Mathf.Max(1f, size.y) * config.mapImageSize.height;
            debugText.text = string.Format("锚点坐标  /  ANCHOR\nX: {0:F1} px\nY: {1:F1} px\nF 隐藏 · 点击复制", x, y);
            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(mapRect, Input.mousePosition))
            {
                string value = string.Format("{0:F1}, {1:F1}", x, y);
                GUIUtility.systemCopyBuffer = value;
                Debug.Log("[QuartzMap] 已复制锚点坐标: " + value);
            }
        }

        [ContextMenu("重建全国石英资源分布界面")]
        public void Rebuild()
        {
            Transform old = transform.Find("QuartzMapUI");
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            }

            nationalMarkers.Clear();
            altayMarkers.Clear();
            legends.Clear();
            selectedTypeIds.Clear();
            resourceTypes = QuartzDataLoader.LoadResourceTypes();
            if (resourceTypes == null || resourceTypes.resourceTypes == null) return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildInterface();
            SetMap("national", true);
        }

        private void BuildInterface()
        {
            GameObject root = Node("QuartzMapUI", transform);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            RawImage background = UI<RawImage>("BG_Panel", root.transform, Vector2.zero, Vector2.one);
            background.color = Color.white;
            background.raycastTarget = false;
            background.texture = Resources.Load<Texture2D>("QuartzMap/EffectReference");
            AspectRatioFitter fit = background.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1307f / 738f;

            BuildTabs(root.transform);
            BuildLegend(root.transform);
            BuildMap(root.transform);
            BuildInfo(root.transform);
            BuildDebug(root.transform);
        }

        private void BuildTabs(Transform root)
        {
            nationalTab = Panel("Tab_National", root, new Vector2(0.607f, 0.876f), new Vector2(0.792f, 0.938f), new Color(0f, .55f, 1f, .28f));
            Button n = nationalTab.gameObject.AddComponent<Button>();
            n.targetGraphic = nationalTab;
            n.onClick.AddListener(delegate { SetMap("national", false); });
            Label("全国资源分布", nationalTab.transform, 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            altayTab = Panel("Tab_Altay", root, new Vector2(0.801f, 0.876f), new Vector2(0.985f, 0.938f), new Color(0f, .15f, .28f, .45f));
            Button a = altayTab.gameObject.AddComponent<Button>();
            a.targetGraphic = altayTab;
            a.onClick.AddListener(delegate { SetMap("altay", false); });
            Label("◆  阿勒泰资源分布", altayTab.transform, 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        }

        private void BuildLegend(Transform root)
        {
            RectTransform panel = Rect("LeftLegendPanel", root, new Vector2(.03f, .055f), new Vector2(.262f, .815f));
            float gap = 0.012f;
            float itemHeight = (1f - gap * 6f) / 7f;
            for (int i = 0; i < resourceTypes.resourceTypes.Length; i++)
            {
                ResourceTypeData data = resourceTypes.resourceTypes[i];
                float top = 1f - i * (itemHeight + gap);
                Image hit = Panel("LegendItem_" + data.id, panel, new Vector2(0, top - itemHeight), new Vector2(1, top), new Color(.015f, .075f, .11f, .92f));
                Outline outline = hit.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, .75f, 1f, .35f);
                outline.effectDistance = new Vector2(2, -2);
                Toggle toggle = hit.gameObject.AddComponent<Toggle>();
                toggle.targetGraphic = hit;
                toggle.graphic = null;
                toggle.isOn = false;
                toggle.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = toggle.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(.2f, .8f, 1f, 1f);
                colors.pressedColor = new Color(.1f, .65f, 1f, 1f);
                colors.selectedColor = colors.highlightedColor;
                toggle.colors = colors;

                Color markerColor = ParseHexColor(data.colorHex);
                RawImage thumb = UI<RawImage>("Thumbnail", hit.transform, new Vector2(.02f, .08f), new Vector2(.26f, .92f));
                thumb.texture = Resources.Load<Texture2D>("QuartzMap/EffectReference");
                thumb.uvRect = LegendThumbUv(i);
                thumb.raycastTarget = false;
                int titleSize = data.name.Length > 8 ? 15 : 18;
                Label(data.name, hit.transform, titleSize, FontStyle.Bold, new Color(.1f, .84f, 1f), TextAnchor.LowerLeft, new Vector2(.3f, .49f), new Vector2(.84f, .88f));
                Label(data.description, hit.transform, 11, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, new Vector2(.3f, .08f), new Vector2(.84f, .49f));
                Image rail = Panel("SelectedRail", hit.transform, new Vector2(0, 0), new Vector2(.018f, 1), markerColor);
                rail.gameObject.SetActive(false);
                DiamondGraphic swatch = UI<DiamondGraphic>("ColorDiamond", hit.transform, new Vector2(.87f, .35f), new Vector2(.95f, .65f));
                swatch.color = markerColor;
                swatch.raycastTarget = false;
                Image selectedFill = Panel("SelectedFill", hit.transform, Vector2.zero, Vector2.one, new Color(0, .55f, 1f, .14f));
                selectedFill.raycastTarget = false;
                selectedFill.gameObject.SetActive(false);

                LegendItemView view = new LegendItemView(toggle, rail.gameObject, selectedFill.gameObject, outline, data.id);
                legends.Add(view);
                toggle.onValueChanged.AddListener(value => OnLegendChanged(view, value));
            }
        }

        private void BuildMap(Transform root)
        {
            mapRect = Rect("MapPanel", root, new Vector2(.29f, .055f), new Vector2(.985f, .855f));
            nationalLayer = Node("MarkerLayer_National", mapRect);
            Stretch(nationalLayer.GetComponent<RectTransform>());
            altayLayer = Node("MarkerLayer_Altay", mapRect);
            Stretch(altayLayer.GetComponent<RectTransform>());

            Image shade = Panel("AltayMapShade", altayLayer.transform, Vector2.zero, Vector2.one, new Color(.015f, .055f, .09f, .94f));
            shade.raycastTarget = false;
            TechRegionGraphic region = UI<TechRegionGraphic>("AltayRegionGraphic", altayLayer.transform, new Vector2(.08f, .08f), new Vector2(.95f, .95f));
            region.color = new Color(0f, .65f, 1f, .82f);
            region.raycastTarget = false;
            Label("阿 勒 泰 地 区 石 英 资 源 分 布", altayLayer.transform, 28, FontStyle.Bold, new Color(.55f, .9f, 1f), TextAnchor.UpperCenter, new Vector2(.15f, .88f), new Vector2(.9f, .97f));

            SpawnMarkers(QuartzDataLoader.LoadMap("national"), nationalLayer.transform, nationalMarkers);
            SpawnMarkers(QuartzDataLoader.LoadMap("altay"), altayLayer.transform, altayMarkers);
        }

        private void SpawnMarkers(MapConfigData config, Transform parent, List<MarkerHighlightAnimator> list)
        {
            if (config == null || config.markers == null || config.mapImageSize == null) return;
            foreach (MapMarkerData marker in config.markers)
            {
                ResourceTypeData type = FindType(marker.resourceTypeId);
                if (type == null) continue;
                float x = marker.anchorPx.x / config.mapImageSize.width;
                float y = 1f - marker.anchorPx.y / config.mapImageSize.height;
                GameObject item = Node("Marker_" + marker.province + "_" + marker.resourceTypeId, parent);
                RectTransform rt = item.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.sizeDelta = new Vector2(20, 20);
                item.AddComponent<CanvasRenderer>();
                DiamondGraphic diamond = item.AddComponent<DiamondGraphic>();
                diamond.color = ParseHexColor(type.colorHex);
                diamond.raycastTarget = true;
                Outline glow = item.AddComponent<Outline>();
                glow.effectColor = new Color(1f, 1f, 1f, .75f);
                glow.effectDistance = new Vector2(2.5f, -2.5f);
                Button button = item.AddComponent<Button>();
                button.targetGraphic = diamond;
                MarkerHighlightAnimator animator = item.AddComponent<MarkerHighlightAnimator>();
                animator.Configure(marker.resourceTypeId, diamond, this);
                list.Add(animator);
                MapMarkerData capturedMarker = marker;
                ResourceTypeData capturedType = type;
                button.onClick.AddListener(delegate { ShowInfo(capturedMarker, capturedType); });
            }
        }

        private void BuildInfo(Transform root)
        {
            Image card = Panel("InfoCardPopup", root, new Vector2(.67f, .12f), new Vector2(.965f, .25f), new Color(.015f, .09f, .15f, .97f));
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Cyan;
            outline.effectDistance = new Vector2(2, -2);
            infoGroup = card.gameObject.AddComponent<CanvasGroup>();
            infoGroup.alpha = 0;
            infoGroup.blocksRaycasts = false;
            infoText = Label("点击地图标记查看资源详情", card.transform, 24, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft, new Vector2(.05f, .08f), new Vector2(.95f, .92f));
        }

        private void BuildDebug(Transform root)
        {
            Image panel = Panel("DebugAnchorPanel", root, new Vector2(.39f, .68f), new Vector2(.56f, .79f), new Color(.02f, .08f, .12f, .92f));
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = Cyan;
            debugText = Label("锚点坐标 / ANCHOR\nX: 0 px\nY: 0 px", panel.transform, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, new Vector2(.08f, .08f), new Vector2(.92f, .92f));
            panel.gameObject.SetActive(false);
        }

        private void SetMap(string mapId, bool immediate)
        {
            showingAltay = mapId == "altay";
            if (nationalLayer == null || altayLayer == null) return;
            nationalLayer.SetActive(!showingAltay);
            altayLayer.SetActive(showingAltay);
            nationalTab.color = showingAltay ? new Color(0, .12f, .24f, .55f) : new Color(0, .55f, 1f, .48f);
            altayTab.color = showingAltay ? new Color(0, .55f, 1f, .48f) : new Color(0, .12f, .24f, .55f);
            RefreshMarkers();
            if (!immediate)
            {
                CanvasGroup group = (showingAltay ? altayLayer : nationalLayer).GetComponent<CanvasGroup>();
                if (group == null) group = (showingAltay ? altayLayer : nationalLayer).AddComponent<CanvasGroup>();
                group.alpha = 0;
                group.DOFade(1f, .35f).SetUpdate(true).SetId(this);
            }
        }

        private void OnLegendChanged(LegendItemView view, bool selected)
        {
            if (selected) selectedTypeIds.Add(view.TypeId); else selectedTypeIds.Remove(view.TypeId);
            view.Rail.SetActive(selected);
            view.Fill.SetActive(selected);
            view.Outline.effectColor = selected ? Cyan : new Color(0, .75f, 1f, .35f);
            RefreshMarkers();
            if (OnLegendSelectionChanged != null) OnLegendSelectionChanged(selectedTypeIds);
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

        private void ShowInfo(MapMarkerData marker, ResourceTypeData type)
        {
            infoText.text = string.Format("<b>{0}</b>  ·  {1}\n{2}", marker.province, type.name, string.IsNullOrEmpty(marker.note) ? type.description : marker.note);
            infoGroup.DOKill();
            infoGroup.blocksRaycasts = true;
            infoGroup.DOFade(1, .22f).SetId(this);
            infoGroup.transform.DOPunchScale(Vector3.one * .05f, .3f, 4, .4f).SetId(this);
        }

        private ResourceTypeData FindType(string id)
        {
            foreach (ResourceTypeData item in resourceTypes.resourceTypes) if (item.id == id) return item;
            return null;
        }

        private static GameObject Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rt = Node(name, parent).GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static T UI<T>(string name, Transform parent, Vector2 min, Vector2 max) where T : Graphic
        {
            RectTransform rt = Rect(name, parent, min, max);
            return rt.gameObject.AddComponent<T>();
        }

        private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            Image image = UI<Image>(name, parent, min, max);
            image.color = color;
            return image;
        }

        private Text Label(string value, Transform parent, int size, FontStyle style, Color color, TextAnchor align, Vector2 min, Vector2 max)
        {
            Text text = UI<Text>("Text", parent, min, max);
            text.text = value; text.font = font; text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static Color ParseHexColor(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
        }

        private static Rect LegendThumbUv(int index)
        {
            const float imageWidth = 1307f;
            const float imageHeight = 738f;
            float topPx = 139f + index * 82.5f;
            float leftPx = 39f;
            float widthPx = 78f;
            float heightPx = 70f;
            return new Rect(leftPx / imageWidth, 1f - (topPx + heightPx) / imageHeight, widthPx / imageWidth, heightPx / imageHeight);
        }

        private sealed class LegendItemView
        {
            public readonly Toggle Toggle;
            public readonly GameObject Rail;
            public readonly GameObject Fill;
            public readonly Outline Outline;
            public readonly string TypeId;

            public LegendItemView(Toggle toggle, GameObject rail, GameObject fill, Outline outline, string typeId)
            {
                Toggle = toggle; Rail = rail; Fill = fill; Outline = outline; TypeId = typeId;
            }
        }
    }

    public sealed class MarkerHighlightAnimator : MonoBehaviour
    {
        public string ResourceTypeId { get; private set; }
        private Graphic graphic;
        private Color normal;
        private object tweenId;

        public void Configure(string resourceTypeId, Graphic target, object owner)
        {
            ResourceTypeId = resourceTypeId;
            graphic = target;
            normal = target.color;
            tweenId = owner;
            ShowNormal();
        }

        public void PlayHighlight()
        {
            transform.DOKill();
            graphic.DOKill();
            graphic.color = normal;
            transform.localScale = Vector3.one;
            transform.DOScale(1.65f, .55f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetId(tweenId);
            graphic.DOFade(.55f, .55f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetId(tweenId);
        }

        public void ShowDimmed()
        {
            StopTween();
            Color c = normal; c.a = .2f; graphic.color = c;
            transform.localScale = Vector3.one * .82f;
        }

        public void ShowNormal()
        {
            StopTween();
            Color c = normal; c.a = .88f; graphic.color = c;
            transform.localScale = Vector3.one;
        }

        private void StopTween()
        {
            transform.DOKill();
            if (graphic != null) graphic.DOKill();
        }
    }

    public sealed class DiamondGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2 center = r.center;
            float half = Mathf.Min(r.width, r.height) * .44f;
            UIVertex v = UIVertex.simpleVert; v.color = color;
            v.position = center + Vector2.up * half; vh.AddVert(v);
            v.position = center + Vector2.right * half; vh.AddVert(v);
            v.position = center + Vector2.down * half; vh.AddVert(v);
            v.position = center + Vector2.left * half; vh.AddVert(v);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
        }
    }

    public sealed class TechGridGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            for (float x = r.xMin; x <= r.xMax; x += 28f)
                for (float y = r.yMin; y <= r.yMax; y += 28f)
                    Quad(vh, new Rect(x, y, 2, 2), color);
        }

        private static void Quad(VertexHelper vh, Rect r, Color color)
        {
            int i = vh.currentVertCount; UIVertex v = UIVertex.simpleVert; v.color = color;
            v.position = new Vector2(r.xMin, r.yMin); vh.AddVert(v); v.position = new Vector2(r.xMin, r.yMax); vh.AddVert(v);
            v.position = new Vector2(r.xMax, r.yMax); vh.AddVert(v); v.position = new Vector2(r.xMax, r.yMin); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i + 2, i + 3, i);
        }
    }

    public sealed class TechRegionGraphic : MaskableGraphic
    {
        private static readonly Vector2[] Shape = { new Vector2(.08f,.38f), new Vector2(.18f,.68f), new Vector2(.38f,.78f), new Vector2(.57f,.68f), new Vector2(.82f,.82f), new Vector2(.93f,.58f), new Vector2(.82f,.33f), new Vector2(.62f,.18f), new Vector2(.37f,.24f), new Vector2(.18f,.18f) };
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect; Color fill = color; fill.a *= .14f;
            Vector2 center = r.center; UIVertex v = UIVertex.simpleVert; v.color = fill; v.position = center; vh.AddVert(v);
            for (int i = 0; i < Shape.Length; i++) { v.position = new Vector2(r.xMin + Shape[i].x * r.width, r.yMin + Shape[i].y * r.height); vh.AddVert(v); }
            for (int i = 0; i < Shape.Length; i++) vh.AddTriangle(0, i + 1, ((i + 1) % Shape.Length) + 1);
            for (int i = 0; i < Shape.Length; i++) Line(vh, r, Shape[i], Shape[(i + 1) % Shape.Length], color, 4f);
            Line(vh, r, Shape[1], Shape[6], color * new Color(1,1,1,.45f), 2f);
            Line(vh, r, Shape[2], Shape[8], color * new Color(1,1,1,.45f), 2f);
            Line(vh, r, Shape[4], Shape[9], color * new Color(1,1,1,.45f), 2f);
        }
        private static void Line(VertexHelper vh, Rect r, Vector2 a, Vector2 b, Color c, float w)
        {
            Vector2 p = new Vector2(r.xMin + a.x * r.width, r.yMin + a.y * r.height), q = new Vector2(r.xMin + b.x * r.width, r.yMin + b.y * r.height);
            Vector2 n = new Vector2(-(q-p).y, (q-p).x).normalized * w; int i = vh.currentVertCount; UIVertex v = UIVertex.simpleVert; v.color = c;
            v.position=p+n;vh.AddVert(v);v.position=q+n;vh.AddVert(v);v.position=q-n;vh.AddVert(v);v.position=p-n;vh.AddVert(v);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i+2,i+3,i);
        }
    }
}
