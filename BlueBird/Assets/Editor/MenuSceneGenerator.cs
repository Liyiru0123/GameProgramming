using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneGenerator
{
    private static readonly Vector2 PanelSize = new Vector2(620f, 640f);

    public static void BuildMenuScene()
    {
        const string scenePath = "Assets/Scenes/menu.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        CleanupScene();
        Camera camera = EnsureCamera();
        if (camera != null)
        {
            camera.backgroundColor = new Color(0.12f, 0.2f, 0.33f, 1f);
            camera.orthographic = true;
        }

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        CreateEventSystem();

        Text title = CreateTitle(canvas.transform, "BLUE BIRD", 78, new Vector2(0f, -120f));
        CreateTitle(canvas.transform, "MENU SCENE", 24, new Vector2(0f, -200f));

        GameObject mainPanel = CreatePanel(canvas.transform, "MainPanel", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), PanelSize);
        GameObject slotPanel = CreatePanel(canvas.transform, "SlotPanel", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), PanelSize);
        GameObject settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(760f, 820f));

        AddHeader(mainPanel.transform, "Main Menu");
        Button startButton = CreateButton(mainPanel.transform, "New Game");
        Button continueButton = CreateButton(mainPanel.transform, "Continue From Slot");
        Button settingsButton = CreateButton(mainPanel.transform, "Settings");

        Text slotHeader = AddHeader(slotPanel.transform, "Choose Slot");
        Button[] slotButtons = new Button[3];
        Text[] slotTexts = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            Button button = CreateButton(slotPanel.transform, $"Slot {i + 1}");
            slotButtons[i] = button;
            slotTexts[i] = button.GetComponentInChildren<Text>();
        }
        Button slotBackButton = CreateButton(slotPanel.transform, "Back");

        AddHeader(settingsPanel.transform, "Settings");
        ScrollRect settingsScrollRect = CreateSettingsScrollView(settingsPanel.transform, out Transform settingsContent);
        Slider volumeSlider = CreateSliderRow(settingsContent, "Master Volume");

        (Button button, Text value)[] bindingRows = new (Button, Text)[7];
        bindingRows[0] = CreateBindingRow(settingsContent, "Move Left");
        bindingRows[1] = CreateBindingRow(settingsContent, "Move Right");
        bindingRows[2] = CreateBindingRow(settingsContent, "Move Up");
        bindingRows[3] = CreateBindingRow(settingsContent, "Move Down");
        bindingRows[4] = CreateBindingRow(settingsContent, "Jump");
        bindingRows[5] = CreateBindingRow(settingsContent, "Dash");
        bindingRows[6] = CreateBindingRow(settingsContent, "Grab / Climb");

        Button settingsBackButton = CreateButton(settingsPanel.transform, "Back");

        Text statusText = CreateStatusText(canvas.transform);

        GameObject controllerObject = new GameObject("MenuSceneController");
        MenuSceneController controller = controllerObject.AddComponent<MenuSceneController>();

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        serializedController.FindProperty("slotPanel").objectReferenceValue = slotPanel;
        serializedController.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serializedController.FindProperty("slotTitleText").objectReferenceValue = slotHeader;
        serializedController.FindProperty("statusText").objectReferenceValue = statusText;
        AssignObjectArray(serializedController.FindProperty("slotButtons"), slotButtons);
        AssignObjectArray(serializedController.FindProperty("slotButtonTexts"), slotTexts);
        serializedController.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        serializedController.FindProperty("settingsScrollRect").objectReferenceValue = settingsScrollRect;

        SerializedProperty bindingEntries = serializedController.FindProperty("bindingEntries");
        bindingEntries.arraySize = 7;
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(0), "Move Left", 0, bindingRows[0].button, bindingRows[0].value);
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(1), "Move Right", 1, bindingRows[1].button, bindingRows[1].value);
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(2), "Move Up", 2, bindingRows[2].button, bindingRows[2].value);
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(3), "Move Down", 3, bindingRows[3].button, bindingRows[3].value);
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(4), "Jump", 4, bindingRows[4].button, bindingRows[4].value);
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(5), "Dash", 5, bindingRows[5].button, bindingRows[5].value);
        SetBindingEntry(bindingEntries.GetArrayElementAtIndex(6), "Grab / Climb", 6, bindingRows[6].button, bindingRows[6].value);

        serializedController.FindProperty("startNewGameButton").objectReferenceValue = startButton;
        serializedController.FindProperty("continueButton").objectReferenceValue = continueButton;
        serializedController.FindProperty("openSettingsButton").objectReferenceValue = settingsButton;
        serializedController.FindProperty("slotBackButton").objectReferenceValue = slotBackButton;
        serializedController.FindProperty("settingsBackButton").objectReferenceValue = settingsBackButton;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        slotPanel.SetActive(false);
        settingsPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void CleanupScene()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Main Camera")
            {
                continue;
            }

            Object.DestroyImmediate(root);
        }
    }

    private static Camera EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            return camera;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(parent, false);
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.94f);
    }

    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Text CreateTitle(Transform parent, string text, int size, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(text, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1200f, 100f);

        Text uiText = textObject.GetComponent<Text>();
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = size;
        uiText.fontStyle = FontStyle.Bold;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.text = text;
        return uiText;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.11f, 0.16f, 0.22f, 0.98f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 36, 36);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return panel;
    }

    private static ScrollRect CreateSettingsScrollView(Transform parent, out Transform content)
    {
        GameObject scrollViewObject = new GameObject(
            "SettingsScrollView",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask),
            typeof(ScrollRect),
            typeof(LayoutElement));
        scrollViewObject.transform.SetParent(parent, false);

        LayoutElement layout = scrollViewObject.GetComponent<LayoutElement>();
        layout.minHeight = 560f;
        layout.preferredHeight = 560f;

        Image image = scrollViewObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.11f, 0.16f, 0.75f);

        Mask mask = scrollViewObject.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        RectTransform scrollRectTransform = scrollViewObject.GetComponent<RectTransform>();
        scrollRectTransform.sizeDelta = new Vector2(0f, 560f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollViewObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(14f, 14f);
        viewportRect.offsetMax = new Vector2(-30f, -14f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
        scrollbarObject.name = "Scrollbar Vertical";
        scrollbarObject.transform.SetParent(scrollViewObject.transform, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.sizeDelta = new Vector2(20f, 0f);
        scrollbarRect.offsetMin = new Vector2(-20f, 14f);
        scrollbarRect.offsetMax = new Vector2(0f, -14f);

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        ScrollRect scrollRect = scrollViewObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 24f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarSpacing = 8f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        content = contentObject.transform;
        return scrollRect;
    }

    private static Text AddHeader(Transform parent, string text)
    {
        GameObject header = new GameObject($"{text}_Header", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        header.transform.SetParent(parent, false);

        LayoutElement layout = header.GetComponent<LayoutElement>();
        layout.minHeight = 60f;

        Text uiText = header.GetComponent<Text>();
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = 34;
        uiText.fontStyle = FontStyle.Bold;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.text = text;
        return uiText;
    }

    private static Button CreateButton(Transform parent, string label)
    {
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = label.Replace(" ", string.Empty) + "Button";
        buttonObject.transform.SetParent(parent, false);

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = 72f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.21f, 0.39f, 0.58f, 1f);

        Text text = buttonObject.GetComponentInChildren<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
        text.color = Color.white;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 72f);
        return buttonObject.GetComponent<Button>();
    }

    private static Slider CreateSliderRow(Transform parent, string label)
    {
        GameObject row = new GameObject($"{label}_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = 90f;

        HorizontalLayoutGroup group = row.GetComponent<HorizontalLayoutGroup>();
        group.spacing = 18f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;

        CreateLabel(row.transform, label, 26, 240f);

        GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = "VolumeSlider";
        sliderObject.transform.SetParent(row.transform, false);
        sliderObject.AddComponent<LayoutElement>().preferredWidth = 360f;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    private static (Button button, Text value) CreateBindingRow(Transform parent, string label)
    {
        GameObject row = new GameObject($"{label}_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = 70f;

        HorizontalLayoutGroup group = row.GetComponent<HorizontalLayoutGroup>();
        group.spacing = 18f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;

        CreateLabel(row.transform, label, 24, 260f);

        Button button = CreateButton(row.transform, "Unbound");
        LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
        buttonLayout.minHeight = 58f;
        buttonLayout.preferredWidth = 280f;

        Text value = button.GetComponentInChildren<Text>();
        return (button, value);
    }

    private static void CreateLabel(Transform parent, string text, int fontSize, float preferredWidth)
    {
        GameObject labelObject = new GameObject(text, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.minHeight = 44f;

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = text;
    }

    private static Text CreateStatusText(Transform parent)
    {
        GameObject status = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        status.transform.SetParent(parent, false);

        RectTransform rect = status.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 34f);
        rect.sizeDelta = new Vector2(1100f, 60f);

        Text text = status.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.92f, 0.96f, 1f, 1f);
        text.text = string.Empty;
        return text;
    }

    private static void AssignObjectArray<T>(SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetBindingEntry(SerializedProperty property, string label, int actionValue, Button button, Text valueText)
    {
        property.FindPropertyRelative("label").stringValue = label;
        property.FindPropertyRelative("action").enumValueIndex = actionValue;
        property.FindPropertyRelative("button").objectReferenceValue = button;
        property.FindPropertyRelative("valueText").objectReferenceValue = valueText;
    }
}
