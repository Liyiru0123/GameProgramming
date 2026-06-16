using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RuntimeGameUI : MonoBehaviour
{
    private enum SlotMode
    {
        NewGame,
        Continue
    }

    private const string MasterVolumeKey = "master_volume";
    private const string MenuSceneName = "menu";
    private const string GameplaySceneName = "SampleScene";

    private static RuntimeGameUI instance;

    private readonly List<Button> slotButtons = new List<Button>();
    private readonly List<Text> slotButtonTexts = new List<Text>();
    private readonly Dictionary<GameInput.ActionName, Text> bindingValueTexts = new Dictionary<GameInput.ActionName, Text>();

    private Canvas canvas;
    private GameObject overlayRoot;
    private GameObject titlePanel;
    private GameObject mainPanel;
    private GameObject slotPanel;
    private GameObject settingsPanel;
    private Text slotTitleText;
    private Text statusText;
    private Text tutorialText;
    private CanvasGroup tutorialGroup;
    private Slider volumeSlider;

    private Coroutine tutorialCoroutine;
    private bool settingsOpenedFromGameplay;
    private SlotMode currentSlotMode;

    public static RuntimeGameUI Instance => instance;

    private bool IsMenuScene =>
        string.Equals(SceneManager.GetActiveScene().name, MenuSceneName, System.StringComparison.OrdinalIgnoreCase);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySavedVolume();
        BuildUi();
        RefreshForCurrentScene();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Update()
    {
        if (GameInput.Instance != null && GameInput.Instance.AwaitingRebind)
        {
            return;
        }

        if (IsMenuScene)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (settingsPanel.activeSelf && settingsOpenedFromGameplay)
        {
            ResumeGameplay();
            return;
        }

        if (!overlayRoot.activeSelf)
        {
            settingsOpenedFromGameplay = true;
            ShowSettings(fromGameplay: true);
        }
    }

    public void ShowTutorialPrompt(string message, float duration = 2.5f)
    {
        if (tutorialText == null || tutorialGroup == null)
        {
            return;
        }

        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
        }

        tutorialCoroutine = StartCoroutine(TutorialRoutine(ResolveBindingPlaceholders(message), duration));
    }

    public static string ResolveBindingPlaceholders(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        GameInput input = GameInput.Instance;
        if (input == null)
        {
            return text;
        }

        string resolved = text;
        resolved = resolved.Replace("{Left}", input.GetBindingLabel(GameInput.ActionName.Left));
        resolved = resolved.Replace("{Right}", input.GetBindingLabel(GameInput.ActionName.Right));
        resolved = resolved.Replace("{Up}", input.GetBindingLabel(GameInput.ActionName.Up));
        resolved = resolved.Replace("{Down}", input.GetBindingLabel(GameInput.ActionName.Down));
        resolved = resolved.Replace("{Jump}", input.GetBindingLabel(GameInput.ActionName.Jump));
        resolved = resolved.Replace("{Dash}", input.GetBindingLabel(GameInput.ActionName.Dash));
        resolved = resolved.Replace("{Grab}", input.GetBindingLabel(GameInput.ActionName.Grab));
        return resolved;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForCurrentScene();
    }

    private void RefreshForCurrentScene()
    {
        settingsOpenedFromGameplay = false;
        RefreshSettingsLabels();

        if (IsMenuScene)
        {
            ShowMainMenu();
            return;
        }

        overlayRoot.SetActive(false);
        titlePanel.SetActive(false);
        mainPanel.SetActive(false);
        slotPanel.SetActive(false);
        settingsPanel.SetActive(false);
        statusText.text = string.Empty;
        GameSession.SetGameplayInputBlocked(false);
        Time.timeScale = 1f;
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("RuntimeCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        overlayRoot = CreateUiObject("OverlayRoot", canvas.transform);
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image dim = overlayRoot.AddComponent<Image>();
        dim.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);

        titlePanel = CreateUiObject("TitlePanel", overlayRoot.transform);
        RectTransform titleRect = titlePanel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(1200f, 220f);

        CreateMenuTitle(titlePanel.transform, "BLUE BIRD", 78, new Vector2(0f, -20f));
        CreateMenuTitle(titlePanel.transform, "2D PLATFORMER MENU", 24, new Vector2(0f, -110f));

        mainPanel = CreatePanel("MainPanel", overlayRoot.transform, new Vector2(560f, 580f));
        CreateMenuBody(mainPanel.transform, "Main Menu");
        CreateActionButton(mainPanel.transform, "Start New Game", () =>
        {
            currentSlotMode = SlotMode.NewGame;
            ShowSlotSelection();
        });
        CreateActionButton(mainPanel.transform, "Continue From Slot", () =>
        {
            currentSlotMode = SlotMode.Continue;
            ShowSlotSelection();
        });
        CreateActionButton(mainPanel.transform, "Settings", () => ShowSettings(fromGameplay: false));

        slotPanel = CreatePanel("SlotPanel", overlayRoot.transform, new Vector2(640f, 620f));
        slotTitleText = CreateMenuBody(slotPanel.transform, "Choose Slot");
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i;
            Button slotButton = CreateActionButton(slotPanel.transform, string.Empty, () => HandleSlotSelected(slotIndex));
            slotButtons.Add(slotButton);
            slotButtonTexts.Add(slotButton.GetComponentInChildren<Text>());
        }

        CreateActionButton(slotPanel.transform, "Back", ShowMainMenu);

        settingsPanel = CreatePanel("SettingsPanel", overlayRoot.transform, new Vector2(760f, 760f));
        CreateMenuBody(settingsPanel.transform, "Settings");
        volumeSlider = CreateSliderRow(settingsPanel.transform, "Master Volume", OnVolumeChanged);
        CreateBindingRow(settingsPanel.transform, "Move Left", GameInput.ActionName.Left);
        CreateBindingRow(settingsPanel.transform, "Move Right", GameInput.ActionName.Right);
        CreateBindingRow(settingsPanel.transform, "Move Up", GameInput.ActionName.Up);
        CreateBindingRow(settingsPanel.transform, "Move Down", GameInput.ActionName.Down);
        CreateBindingRow(settingsPanel.transform, "Jump", GameInput.ActionName.Jump);
        CreateBindingRow(settingsPanel.transform, "Dash", GameInput.ActionName.Dash);
        CreateBindingRow(settingsPanel.transform, "Grab / Climb", GameInput.ActionName.Grab);
        CreateActionButton(settingsPanel.transform, "Back", BackFromSettings);

        GameObject statusObject = CreateUiObject("StatusText", overlayRoot.transform);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 36f);
        statusRect.sizeDelta = new Vector2(1100f, 80f);

        statusText = statusObject.AddComponent<Text>();
        statusText.font = GetDefaultFont();
        statusText.fontSize = 26;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(0.92f, 0.96f, 1f, 1f);

        GameObject tutorialObject = CreateUiObject("TutorialText", canvas.transform);
        RectTransform tutorialRect = tutorialObject.GetComponent<RectTransform>();
        tutorialRect.anchorMin = new Vector2(0.5f, 1f);
        tutorialRect.anchorMax = new Vector2(0.5f, 1f);
        tutorialRect.pivot = new Vector2(0.5f, 1f);
        tutorialRect.anchoredPosition = new Vector2(0f, -52f);
        tutorialRect.sizeDelta = new Vector2(1240f, 120f);

        tutorialGroup = tutorialObject.AddComponent<CanvasGroup>();
        tutorialGroup.alpha = 0f;

        tutorialText = tutorialObject.AddComponent<Text>();
        tutorialText.font = GetDefaultFont();
        tutorialText.fontSize = 30;
        tutorialText.alignment = TextAnchor.MiddleCenter;
        tutorialText.color = new Color(1f, 0.97f, 0.82f, 1f);

        overlayRoot.SetActive(false);
        titlePanel.SetActive(false);
        mainPanel.SetActive(false);
        slotPanel.SetActive(false);
        settingsPanel.SetActive(false);
        RefreshSettingsLabels();
    }

    private void ShowMainMenu()
    {
        settingsOpenedFromGameplay = false;
        SetOverlayState(true);
        titlePanel.SetActive(true);
        mainPanel.SetActive(true);
        slotPanel.SetActive(false);
        settingsPanel.SetActive(false);
        statusText.text = string.Empty;
        RefreshSlotButtons();
        RefreshSettingsLabels();
    }

    private void ShowSlotSelection()
    {
        SetOverlayState(true);
        titlePanel.SetActive(true);
        mainPanel.SetActive(false);
        slotPanel.SetActive(true);
        settingsPanel.SetActive(false);
        slotTitleText.text = currentSlotMode == SlotMode.NewGame ? "Choose Slot For New Game" : "Choose Slot To Continue";
        statusText.text = string.Empty;
        RefreshSlotButtons();
    }

    private void ShowSettings(bool fromGameplay)
    {
        settingsOpenedFromGameplay = fromGameplay;
        SetOverlayState(true);
        titlePanel.SetActive(IsMenuScene);
        mainPanel.SetActive(false);
        slotPanel.SetActive(false);
        settingsPanel.SetActive(true);
        statusText.text = string.Empty;
        RefreshSettingsLabels();
    }

    private void BackFromSettings()
    {
        if (settingsOpenedFromGameplay)
        {
            ResumeGameplay();
            return;
        }

        ShowMainMenu();
    }

    private void ResumeGameplay()
    {
        settingsOpenedFromGameplay = false;
        overlayRoot.SetActive(false);
        titlePanel.SetActive(false);
        mainPanel.SetActive(false);
        slotPanel.SetActive(false);
        settingsPanel.SetActive(false);
        statusText.text = string.Empty;
        GameSession.SetGameplayInputBlocked(false);
        Time.timeScale = 1f;
    }

    private void SetOverlayState(bool visible)
    {
        overlayRoot.SetActive(visible);
        GameSession.SetGameplayInputBlocked(visible && !IsMenuScene);
        Time.timeScale = visible && !IsMenuScene ? 0f : 1f;
    }

    private void HandleSlotSelected(int slotIndex)
    {
        if (currentSlotMode == SlotMode.NewGame)
        {
            SaveGameSystem.Instance?.StartNewGameInSlot(slotIndex);
            LoadGameplayScene();
            return;
        }

        if (SaveGameSystem.Instance != null && SaveGameSystem.Instance.ContinueFromSlot(slotIndex))
        {
            LoadGameplayScene();
            return;
        }

        statusText.text = $"Slot {slotIndex + 1} is empty.";
        RefreshSlotButtons();
    }

    private void LoadGameplayScene()
    {
        Time.timeScale = 1f;
        GameSession.SetGameplayInputBlocked(false);
        SceneManager.LoadScene(GameplaySceneName);
    }

    private void RefreshSlotButtons()
    {
        SaveGameSystem saveSystem = SaveGameSystem.Instance;
        for (int i = 0; i < slotButtons.Count; i++)
        {
            string summary = saveSystem != null ? saveSystem.GetSlotSummary(i) : "Empty";
            bool hasData = saveSystem != null && saveSystem.HasSlotData(i);
            slotButtons[i].interactable = currentSlotMode == SlotMode.NewGame || hasData;
            slotButtonTexts[i].text = $"Slot {i + 1}\n{summary}";
        }
    }

    private void RefreshSettingsLabels()
    {
        GameInput input = GameInput.Instance;
        if (input == null)
        {
            return;
        }

        foreach (KeyValuePair<GameInput.ActionName, Text> pair in bindingValueTexts)
        {
            if (pair.Value != null)
            {
                pair.Value.text = input.GetBindingLabel(pair.Key);
            }
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }
    }

    private void BeginRebind(GameInput.ActionName action)
    {
        GameInput input = GameInput.Instance;
        if (input == null)
        {
            return;
        }

        if (bindingValueTexts.TryGetValue(action, out Text valueText) && valueText != null)
        {
            valueText.text = "Press a key...";
        }

        statusText.text = $"Waiting for new key: {GetActionDisplayName(action)}";
        input.BeginRebind(action, _ =>
        {
            statusText.text = $"{GetActionDisplayName(action)} -> {input.GetBindingLabel(action)}";
            RefreshSettingsLabels();
        });
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplySavedVolume()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
    }

    private IEnumerator TutorialRoutine(string message, float duration)
    {
        tutorialText.text = message;
        tutorialGroup.alpha = 1f;

        float holdDuration = Mathf.Max(0.1f, duration);
        yield return new WaitForSecondsRealtime(holdDuration);

        float fadeDuration = 0.35f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            tutorialGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        tutorialGroup.alpha = 0f;
        tutorialCoroutine = null;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(eventSystemObject);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.AddComponent<RectTransform>();
        return gameObject;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = CreateUiObject(name, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.14f, 0.2f, 0.96f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return panel;
    }

    private static Text CreateMenuTitle(Transform parent, string text, int fontSize, Vector2 anchoredPosition)
    {
        GameObject textObject = CreateUiObject(text.Replace(" ", "_"), parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1200f, 80f);

        Text uiText = textObject.AddComponent<Text>();
        uiText.font = GetDefaultFont();
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.fontStyle = FontStyle.Bold;
        return uiText;
    }

    private static Text CreateMenuBody(Transform parent, string text)
    {
        Text body = CreateText(parent, text, 28, TextAnchor.MiddleCenter);
        body.color = new Color(0.9f, 0.95f, 1f, 1f);
        LayoutElement layout = body.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 50f;
        return body;
    }

    private Button CreateActionButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject($"{text}_Button", parent);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = 72f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.38f, 0.56f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.5f, 0.7f, 1f);
        colors.pressedColor = new Color(0.16f, 0.28f, 0.42f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.24f, 0.7f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Text label = CreateText(buttonObject.transform, text, 28, TextAnchor.MiddleCenter);
        label.color = Color.white;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 8f);
        labelRect.offsetMax = new Vector2(-16f, -8f);

        return button;
    }

    private Slider CreateSliderRow(Transform parent, string label, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = CreateUiObject($"{label}_Row", parent);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 88f;

        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 20f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;

        Text rowLabel = CreateText(row.transform, label, 28, TextAnchor.MiddleLeft);
        LayoutElement labelLayout = rowLabel.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 220f;

        GameObject sliderRoot = CreateUiObject($"{label}_SliderRoot", row.transform);
        LayoutElement sliderLayout = sliderRoot.AddComponent<LayoutElement>();
        sliderLayout.preferredWidth = 420f;
        sliderLayout.minHeight = 48f;

        Slider slider = sliderRoot.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(onChanged);

        GameObject background = CreateUiObject("Background", sliderRoot.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.25f, 1f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.25f);
        backgroundRect.anchorMax = new Vector2(1f, 0.75f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = CreateUiObject("FillArea", sliderRoot.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.5f, 0.8f, 0.95f, 1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handle = CreateUiObject("Handle", sliderRoot.transform);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.97f, 0.9f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26f, 54f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private void CreateBindingRow(Transform parent, string label, GameInput.ActionName action)
    {
        GameObject row = CreateUiObject($"{action}_Row", parent);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 68f;

        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 18f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;

        Text rowLabel = CreateText(row.transform, label, 26, TextAnchor.MiddleLeft);
        LayoutElement labelLayout = rowLabel.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 260f;

        Button button = CreateActionButton(row.transform, string.Empty, () => BeginRebind(action));
        LayoutElement buttonLayout = button.gameObject.GetComponent<LayoutElement>();
        buttonLayout.minHeight = 58f;

        Text valueText = button.GetComponentInChildren<Text>();
        bindingValueTexts[action] = valueText;
    }

    private static Text CreateText(Transform parent, string text, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject("Text", parent);
        Text uiText = textObject.AddComponent<Text>();
        uiText.font = GetDefaultFont();
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        return uiText;
    }

    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static string GetActionDisplayName(GameInput.ActionName action)
    {
        switch (action)
        {
            case GameInput.ActionName.Left:
                return "Move Left";
            case GameInput.ActionName.Right:
                return "Move Right";
            case GameInput.ActionName.Up:
                return "Move Up";
            case GameInput.ActionName.Down:
                return "Move Down";
            case GameInput.ActionName.Jump:
                return "Jump";
            case GameInput.ActionName.Dash:
                return "Dash";
            case GameInput.ActionName.Grab:
                return "Grab / Climb";
            default:
                return action.ToString();
        }
    }
}
