using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MarkerPanelDemoBuilder
{
    private static readonly Vector2 Display4KResolution = new Vector2(3840f, 2160f);
    private static readonly Vector2 CombinedResolution = new Vector2(7680f, 2160f);
    private const string ScenePath = "Assets/Scenes/MarkerPanelDemo.unity";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = PrefabFolder + "/MarkerPanelGroup.prefab";
    private const string PanelSpriteSheetPath = "Assets/UI/资源 1.png";
    private static readonly string[] PanelSpriteNames = { "资源 1_0", "资源 1_1", "资源 1_2" };
    private const float MarkerPrefabScale = 1.35f;
    private const float PanelHeight = 300f;
    private static readonly Color Cyan = new Color(0.06f, 1f, 0.72f, 1f);

    [MenuItem("Tools/Marker Detect/Rebuild Panel Growth Demo")]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas display1Canvas = CreateCanvas("Canvas_Display1", 0);
        Canvas display2Canvas = CreateCanvas("Canvas_Display2", 1);
        // 两个独立 Display 视觉上拼成一块连续宽屏：标题只出现在最右侧。
        CreateBackground(display1Canvas.transform, false);
        CreateBackground(display2Canvas.transform, true);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        new GameObject("MultiDisplayController", typeof(MultiDisplayActivator));
        // 与官方 SampleScene 保持一致：场景中只保留一个真实三点触控检测器。
        new GameObject("MarkerDetectRuntime", typeof(ObjectDetect));

        GameObject prefab = CreatePrefab();
        // ObjectDetect 直接使用 Touch.position，坐标原点在整块 7680x2160 屏幕左下角。
        // ID 5/6 靠近中缝，用于演示跨屏标注。
        Vector2[] combinedPositions =
        {
            new Vector2(900f, 1540f),
            new Vector2(2600f, 1540f),
            new Vector2(1750f, 580f),
            new Vector2(650f, 420f),
            new Vector2(3700f, 1180f),
            new Vector2(3980f, 900f),
            new Vector2(5080f, 1540f),
            new Vector2(6780f, 1540f),
            new Vector2(5930f, 580f),
            new Vector2(7080f, 420f)
        };
        CreateTrackedViews(display1Canvas, prefab, combinedPositions, 0);
        CreateTrackedViews(display2Canvas, prefab, combinedPositions, 1);
        CreateSimulators(combinedPositions);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Marker panel dual-display demo rebuilt: " + ScenePath);
    }

    private static void CreateTrackedViews(Canvas canvas, GameObject prefab, Vector2[] combinedPositions, int displayIndex)
    {
        GameObject viewRoot = UIObject($"TrackedMarkers_Display{displayIndex + 1}", canvas.transform);
        Stretch(viewRoot.GetComponent<RectTransform>());
        viewRoot.AddComponent<RectMask2D>();

        for (int i = 0; i < combinedPositions.Length; i++)
        {
            int objectID = i + 1;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, viewRoot.transform);
            instance.name = $"TrackedMarker_{objectID}_Display{displayIndex + 1}";
            RectTransform root = instance.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = CombinedToDisplayLocal(combinedPositions[i], displayIndex);

            SerializedObject markerEvents = new SerializedObject(instance.GetComponent<MarKActions>());
            markerEvents.FindProperty("mObjectID").intValue = objectID;
            markerEvents.ApplyModifiedPropertiesWithoutUndo();

            MarkerPanelPresenter presenter = instance.GetComponent<MarkerPanelPresenter>();
            presenter.ConfigureCombinedDisplay(displayIndex, CombinedResolution, Display4KResolution.x);
        }
    }

    private static void CreateSimulators(Vector2[] combinedPositions)
    {
        GameObject simulationRoot = new GameObject("ObjectDetectDictionarySimulators");
        for (int i = 0; i < combinedPositions.Length; i++)
        {
            int objectID = i + 1;
            GameObject simulator = new GameObject($"ObjectDetectDictionarySimulator_{objectID}",
                typeof(MarkerDetectSimulationDriver));
            simulator.transform.SetParent(simulationRoot.transform, false);
            SerializedObject simulation = new SerializedObject(simulator.GetComponent<MarkerDetectSimulationDriver>());
            simulation.FindProperty("objectID").intValue = objectID;
            simulation.FindProperty("startPosition").vector2Value = combinedPositions[i];
            simulation.FindProperty("moveOffset").vector2Value = new Vector2(30f, 16f);
            simulation.FindProperty("initialDelay").floatValue = (i % 5) * 1.34f;
            simulation.FindProperty("moveSeconds").floatValue = 2.5f;
            simulation.FindProperty("endSeconds").floatValue = 0.5f;
            simulation.FindProperty("undetectSeconds").floatValue = 2.5f;
            simulation.FindProperty("playOnStart").boolValue = true;
            simulation.FindProperty("loop").boolValue = true;
            simulation.ApplyModifiedPropertiesWithoutUndo();
        }

        // 默认必须关闭，否则模拟数据会覆盖真实 ObjectDetect 的识别字典。
        simulationRoot.SetActive(false);
    }

    private static Vector2 CombinedToDisplayLocal(Vector2 combinedPosition, int displayIndex)
    {
        return new Vector2(
            combinedPosition.x - displayIndex * Display4KResolution.x - Display4KResolution.x * 0.5f,
            combinedPosition.y - Display4KResolution.y * 0.5f);
    }

    private static Canvas CreateCanvas(string name, int targetDisplay)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Display4KResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateBackground(Transform parent, bool showTitle)
    {
        GameObject background = UIObject("Background", parent);
        Stretch(background.GetComponent<RectTransform>());
        background.AddComponent<Image>().color = new Color(0.012f, 0.035f, 0.031f, 1f);
        if (showTitle)
            CreateText("Title", background.transform, "模型识别互动台", 60, new Vector2(-70f, -50f),
                new Vector2(0.5f, 1f), Vector2.one, Vector2.one, new Vector2(-60f, 110f), TextAnchor.MiddleRight);
    }

    private static GameObject CreatePrefab()
    {
        GameObject root = UIObject("MarkerPanelGroup", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = Vector2.zero;
        rootRect.localScale = Vector3.one * MarkerPrefabScale;
        CanvasGroup rootGroup = root.AddComponent<CanvasGroup>();
        MarKActions eventsSource = root.AddComponent<MarKActions>();
        MarkerPanelPresenter presenter = root.AddComponent<MarkerPanelPresenter>();

        GameObject linesObject = UIObject("PanelLines", root.transform);
        RectTransform linesRect = linesObject.GetComponent<RectTransform>();
        linesRect.anchorMin = linesRect.anchorMax = new Vector2(0.5f, 0.5f);
        linesRect.sizeDelta = new Vector2(2000f, 1000f);
        RadialPanelLines lines = linesObject.AddComponent<RadialPanelLines>();
        lines.color = Cyan;
        SerializedObject lineSettings = new SerializedObject(lines);
        lineSettings.FindProperty("lineWidth").floatValue = 6f;
        lineSettings.FindProperty("diagonalLength").floatValue = 58f;
        lineSettings.FindProperty("minimumHorizontalLength").floatValue = 48f;
        lineSettings.ApplyModifiedPropertiesWithoutUndo();

        GameObject center = UIObject("CenterScan", root.transform);
        RectTransform centerRect = center.GetComponent<RectTransform>();
        centerRect.anchorMin = centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(168f, 168f);
        CenterScanPulse pulse = center.AddComponent<CenterScanPulse>();
        ScanRingGraphic dash = CreateRing("DashRing", center.transform, 168f, false, Cyan, 40, 6f);
        ScanRingGraphic white = CreateRing("WhiteRing", center.transform, 134f, true, Color.white, 56, 8f);
        ScanRingGraphic red = CreateRing("RedPulseRing", center.transform, 108f, true, new Color(1f, 0.03f, 0.04f, 1f), 56, 8f);
        ScanRingGraphic green = CreateRing("InnerRing", center.transform, 82f, true, Cyan, 56, 4f);
        pulse.Configure(dash.rectTransform, red);
        white.raycastTarget = red.raycastTarget = green.raycastTarget = false;

        // 左一右二的紧凑扇形：卡片靠近圆环，但仍保留较长的水平线段。
        Vector2[] offsets = { new Vector2(-600f, 0f), new Vector2(600f, 210f), new Vector2(600f, -210f) };
        string[] names = { "材料特性", "产品应用效果", "航天与国防" };
        RectTransform[] targets = new RectTransform[3];
        CanvasGroup[] panels = new CanvasGroup[3];
        for (int i = 0; i < 3; i++)
        {
            Sprite sprite = LoadPanelSprite(PanelSpriteNames[i]);
            GameObject panel = CreatePanel($"PanelImage_{i + 1}", root.transform, names[i], sprite);
            targets[i] = panel.GetComponent<RectTransform>();
            targets[i].anchoredPosition = offsets[i];
            panels[i] = panel.GetComponent<CanvasGroup>();
        }

        lines.Configure(centerRect, targets);
        presenter.Configure(eventsSource, lines, null, panels);
        SerializedObject presenterSettings = new SerializedObject(presenter);
        // 面板位置固定，避免多个 Marker 靠近时自适应避让导致连线来回跳动。
        presenterSettings.FindProperty("autoArrangeLeaves").boolValue = false;
        presenterSettings.FindProperty("equalizeLeafAngles").boolValue = false;
        presenterSettings.FindProperty("minimumLeafAngle").floatValue = 44f;
        presenterSettings.FindProperty("maxLeafAngleAdjustment").floatValue = 68f;
        presenterSettings.FindProperty("leafOverlapPadding").floatValue = 48f;
        presenterSettings.FindProperty("maxLeafOffset").floatValue = 260f;
        presenterSettings.FindProperty("leafLayoutSpeed").floatValue = 10f;
        presenterSettings.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreatePanel(string name, Transform parent, string title, Sprite sprite)
    {
        GameObject panel = UIObject(name, parent);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float aspect = sprite != null && sprite.rect.height > 0f
            ? sprite.rect.width / sprite.rect.height
            : 2f;
        rt.sizeDelta = new Vector2(PanelHeight * aspect, PanelHeight);
        Image image = panel.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.65f, 1f, 0.88f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        PanelImageZoomToggle zoom = panel.AddComponent<PanelImageZoomToggle>();
        SerializedObject zoomSettings = new SerializedObject(zoom);
        zoomSettings.FindProperty("moveAwayFromCenter").boolValue = true;
        zoomSettings.FindProperty("centerClearance").floatValue = 24f;
        zoomSettings.ApplyModifiedPropertiesWithoutUndo();
        Text label = CreateText("Label", panel.transform, title, 30, new Vector2(24f, -17f), Vector2.zero,
            Vector2.one, new Vector2(0f, 1f), new Vector2(-42f, -32f), TextAnchor.UpperLeft);
        label.color = Cyan;
        label.gameObject.SetActive(false);
        Text placeholder = CreateText("Placeholder", panel.transform, "图片占位区域", 21,
            new Vector2(24f, -66f), Vector2.zero, Vector2.one, new Vector2(0f, 1f),
            new Vector2(-42f, -80f), TextAnchor.UpperLeft);
        placeholder.color = Color.white;
        placeholder.gameObject.SetActive(false);
        group.alpha = 0f;
        return panel;
    }

    private static Sprite LoadPanelSprite(string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PanelSpriteSheetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName) return sprite;
        }

        Debug.LogWarning($"Panel sprite '{spriteName}' was not found in '{PanelSpriteSheetPath}'.");
        return null;
    }

    private static ScanRingGraphic CreateRing(string name, Transform parent, float size, bool solid,
        Color color, int count, float width)
    {
        GameObject go = UIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        ScanRingGraphic ring = go.AddComponent<ScanRingGraphic>();
        ring.Configure(solid, count, width, 0.52f);
        ring.color = color;
        return ring;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, Vector2 position,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, TextAnchor alignment)
    {
        GameObject go = UIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = sizeDelta;
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
