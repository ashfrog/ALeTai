using System.IO;
using QuartzDistribution.HotspotAnnotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuartzDistribution.HotspotAnnotations.Editor
{
    public static class HotspotDemoSceneBuilder
    {
        private const string RootFolder = "Assets/HotspotAnnotations";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string PrefabPath = PrefabFolder + "/FlatAnnotationGroup.prefab";
        private const string ScenePath = "Assets/Scenes/FlatHotspotDemo.unity";

        private static readonly Color Cyan = new Color(0.05f, 0.96f, 0.73f, 1f);
        private static readonly Color Panel = new Color(0.035f, 0.09f, 0.085f, 0.9f);

        [MenuItem("Tools/Quartz Distribution/Rebuild Flat Hotspot Demo")]
        public static void Build()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(PrefabFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Canvas canvas = CreateCanvas();
            CreateEventSystem();
            CreateBackground(canvas.transform);

            GameObject prefab = CreateOrReplaceAnnotationPrefab();
            CreateGroup(prefab, canvas, 1, "TrackingGroup_1", new Vector2(920f, 650f),
                new[] { "材料特性", "产品应用效果", "航天与国防" },
                new[] { new Vector2(390f, 690f), new Vector2(1170f, 780f), new Vector2(1110f, 485f) });
            CreateGroup(prefab, canvas, 2, "TrackingGroup_2", new Vector2(640f, 265f),
                new[] { "材料特性", "光纤预制棒", "产品应用效果" },
                new[] { new Vector2(110f, 330f), new Vector2(860f, 250f), new Vector2(70f, 70f) });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("QuartzDistribution flat hotspot demo rebuilt: " + ScenePath);
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("FlatInteractionCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void CreateBackground(Transform parent)
        {
            GameObject background = CreateUIObject("Background", parent);
            Stretch(background.GetComponent<RectTransform>());
            Image image = background.AddComponent<Image>();
            image.color = new Color(0.015f, 0.035f, 0.032f, 1f);

            GameObject topLine = CreateUIObject("TopLine", background.transform);
            Image topLineImage = topLine.AddComponent<Image>();
            topLineImage.color = new Color(0.55f, 0.65f, 0.62f, 0.4f);
            SetRect(topLine.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -82f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f));

            CreateText("Header", background.transform, "模型识别互动台", 38, FontStyle.Bold, TextAnchor.MiddleRight,
                Color.white, new Vector2(-90f, -42f), new Vector2(0.4f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-40f, 72f));

            ProceduralRingGraphic arc = CreateRing("RightDecoration", background.transform, 700f,
                ProceduralRingGraphic.RingStyle.Ticks, new Color(0.04f, 0.9f, 0.67f, 0.24f), 64, 5f, 0.18f);
            RectTransform arcRect = arc.rectTransform;
            arcRect.anchorMin = arcRect.anchorMax = new Vector2(1f, 0.5f);
            arcRect.anchoredPosition = new Vector2(180f, -20f);
        }

        private static GameObject CreateOrReplaceAnnotationPrefab()
        {
            GameObject controller = CreateUIObject("FlatAnnotationGroup", null);
            Stretch(controller.GetComponent<RectTransform>());
            MarKActions actions = controller.AddComponent<MarKActions>();

            GameObject visual = CreateUIObject("VisualRoot", controller.transform);
            Stretch(visual.GetComponent<RectTransform>());
            CanvasGroup canvasGroup = visual.AddComponent<CanvasGroup>();

            GameObject scanRoot = CreateUIObject("ScanRingRoot", visual.transform);
            RectTransform scanRect = scanRoot.GetComponent<RectTransform>();
            SetRect(scanRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(170f, 170f), new Vector2(0.5f, 0.5f));
            ScanRingPulse pulse = scanRoot.AddComponent<ScanRingPulse>();
            ProceduralRingGraphic dashes = CreateRing("OuterDashRing", scanRoot.transform, 170f,
                ProceduralRingGraphic.RingStyle.Dashes, new Color(0.16f, 1f, 0.77f, 0.88f), 48, 5f, 0.54f);
            ProceduralRingGraphic ticks = CreateRing("TickRing", scanRoot.transform, 142f,
                ProceduralRingGraphic.RingStyle.Ticks, Color.white, 48, 4f, 0.35f);
            ProceduralRingGraphic glow = CreateRing("RedGlowRing", scanRoot.transform, 112f,
                ProceduralRingGraphic.RingStyle.Solid, new Color(1f, 0.03f, 0.06f, 0.3f), 72, 13f, 1f);
            ProceduralRingGraphic red = CreateRing("RedPulseRing", scanRoot.transform, 104f,
                ProceduralRingGraphic.RingStyle.Solid, new Color(1f, 0.02f, 0.04f, 1f), 72, 5f, 1f);
            ProceduralRingGraphic inner = CreateRing("InnerGreenRing", scanRoot.transform, 78f,
                ProceduralRingGraphic.RingStyle.Solid, Cyan, 72, 2f, 1f);
            pulse.Configure(dashes.rectTransform, ticks.rectTransform, red, glow);
            inner.raycastTarget = false;

            OrthogonalLiveLine[] lines = new OrthogonalLiveLine[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject lineObject = CreateUIObject($"ConnectLine_{i + 1}", visual.transform);
                Stretch(lineObject.GetComponent<RectTransform>());
                lines[i] = lineObject.AddComponent<OrthogonalLiveLine>();
                lines[i].color = Cyan;
                GameObject card = CreateCard($"InfoPanel_{i + 1}", visual.transform);
                lines[i].Configure(scanRect, card.GetComponent<RectTransform>(), null);
            }

            actions.Configure(1, scanRect, null, visual, canvasGroup, pulse, lines);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(controller, PrefabPath);
            Object.DestroyImmediate(controller);
            return prefab;
        }

        private static void CreateGroup(GameObject prefab, Canvas canvas, int id, string name,
            Vector2 simulatedPosition, string[] titles, Vector2[] cardPositions)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            instance.name = name;
            Stretch(instance.GetComponent<RectTransform>());
            GameObject visual = instance.transform.Find("VisualRoot").gameObject;
            RectTransform scan = visual.transform.Find("ScanRingRoot").GetComponent<RectTransform>();
            OrthogonalLiveLine[] lines = new OrthogonalLiveLine[3];

            for (int i = 0; i < 3; i++)
            {
                RectTransform card = visual.transform.Find($"InfoPanel_{i + 1}").GetComponent<RectTransform>();
                card.anchorMin = card.anchorMax = Vector2.zero;
                card.pivot = Vector2.zero;
                card.anchoredPosition = cardPositions[i];
                card.Find("Title").GetComponent<Text>().text = titles[i];
                card.Find("Body").GetComponent<Text>().text = GetBody(titles[i]);
                lines[i] = visual.transform.Find($"ConnectLine_{i + 1}").GetComponent<OrthogonalLiveLine>();
                lines[i].Configure(scan, card, canvas);
            }

            MarKActions actions = instance.GetComponent<MarKActions>();
            actions.Configure(id, scan, canvas, visual, visual.GetComponent<CanvasGroup>(), scan.GetComponent<ScanRingPulse>(), lines);
            SerializedObject serialized = new SerializedObject(actions);
            serialized.FindProperty("simulateTrackingData").boolValue = true;
            serialized.FindProperty("simulatedDetected").boolValue = true;
            serialized.FindProperty("simulatedPosition").vector2Value = simulatedPosition;
            serialized.FindProperty("simulationReferenceResolution").vector2Value = new Vector2(1920f, 1080f);
            serialized.FindProperty("simulateMotion").boolValue = true;
            serialized.FindProperty("simulatedMotionRadius").vector2Value = id == 1 ? new Vector2(85f, 36f) : new Vector2(55f, 28f);
            serialized.FindProperty("simulatedMotionSpeed").floatValue = id == 1 ? 0.16f : 0.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateCard(string name, Transform parent)
        {
            GameObject card = CreateUIObject(name, parent);
            RectTransform rt = card.GetComponent<RectTransform>();
            SetRect(rt, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(370f, 145f), Vector2.zero);
            Image image = card.AddComponent<Image>();
            image.color = Panel;
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.62f, 1f, 0.88f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateText("Icon", card.transform, "⊖", 25, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
                new Vector2(20f, -18f), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(34f, 34f));
            CreateText("Title", card.transform, "材料特性", 25, FontStyle.Bold, TextAnchor.UpperLeft, Cyan,
                new Vector2(55f, -18f), Vector2.zero, new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-70f, -50f));
            CreateText("Body", card.transform, "高纯石英玻璃 · 低热膨胀 · 高透过率", 16, FontStyle.Normal,
                TextAnchor.UpperLeft, new Color(0.78f, 0.9f, 0.84f, 1f), new Vector2(24f, -68f),
                Vector2.zero, new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-46f, -82f));
            return card;
        }

        private static string GetBody(string title)
        {
            switch (title)
            {
                case "产品应用效果": return "航天器光学窗口、卫星遥感镜头与精密光学系统";
                case "航天与国防": return "适用于极端温度、辐射环境下的高可靠关键部件";
                case "光纤预制棒": return "高纯度石英材料，支持低损耗光纤拉制与稳定传输";
                default: return "高纯度 · 耐高温 · 低热膨胀 · 宽波段高透过";
            }
        }

        private static ProceduralRingGraphic CreateRing(string name, Transform parent, float size,
            ProceduralRingGraphic.RingStyle style, Color color, int count, float thickness, float fill)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size, size), new Vector2(0.5f, 0.5f));
            ProceduralRingGraphic ring = go.AddComponent<ProceduralRingGraphic>();
            ring.Style = style;
            ring.ElementCount = count;
            ring.Thickness = thickness;
            ring.FillRatio = fill;
            ring.color = color;
            return ring;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style,
            TextAnchor alignment, Color color, Vector2 position, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta)
        {
            GameObject go = CreateUIObject(name, parent);
            Text text = go.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, position, sizeDelta, pivot);
            return text;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            SetRect(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Vector2 sizeDelta, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = sizeDelta;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
