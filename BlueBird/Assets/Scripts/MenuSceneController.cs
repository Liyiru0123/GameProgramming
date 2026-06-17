using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Values are populated from the menu scene by the generator/editor wiring.
#pragma warning disable CS0649
[DisallowMultipleComponent]
public class MenuSceneController : MonoBehaviour
{
    private enum SlotMode
    {
        NewGame,
        Continue
    }

    [Serializable]
    private class BindingEntry
    {
        public string label;
        public GameInput.ActionName action;
        public Button button;
        public Text valueText;
    }

    private const string GameplaySceneName = "SampleScene";
    private const string MasterVolumeKey = "master_volume";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject slotPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button startNewGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button slotBackButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Status")]
    [SerializeField] private Text slotTitleText;
    [SerializeField] private Text statusText;

    [Header("Presentation")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private AudioClip menuMusicClip;

    [Header("Slots")]
    [SerializeField] private Button[] slotButtons = Array.Empty<Button>();
    [SerializeField] private Text[] slotButtonTexts = Array.Empty<Text>();

    [Header("Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private ScrollRect settingsScrollRect;
    [SerializeField] private BindingEntry[] bindingEntries = Array.Empty<BindingEntry>();

    private readonly Dictionary<GameInput.ActionName, BindingEntry> bindingLookup =
        new Dictionary<GameInput.ActionName, BindingEntry>();

    private bool isTransitioning;
    private SlotMode currentSlotMode;

    private void Awake()
    {
        CacheBindingLookup();
        WireButtons();
        ApplySavedVolume();
        RefreshSettingsLabels();
        ShowMainPanel();
    }

    private void Start()
    {
        PlayMenuMusic();

        if (fadeOverlay == null)
        {
            return;
        }

        SetFadeAlpha(1f);
        StartCoroutine(FadeOverlayRoutine(0f, 0.45f));
    }

    private void CacheBindingLookup()
    {
        bindingLookup.Clear();
        foreach (BindingEntry entry in bindingEntries)
        {
            if (entry == null)
            {
                continue;
            }

            bindingLookup[entry.action] = entry;
        }
    }

    private void WireButtons()
    {
        WireButton(startNewGameButton, OnStartNewGame);
        WireButton(continueButton, OnOpenContinueSlots);
        WireButton(openSettingsButton, OnOpenSettings);
        WireButton(exitButton, OnExitGame);
        WireButton(slotBackButton, OnBackToMain);
        WireButton(settingsBackButton, OnBackToMain);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;
            if (slotButtons[i] != null)
            {
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => HandleSlotSelected(slotIndex));
            }
        }

        foreach (BindingEntry entry in bindingEntries)
        {
            if (entry == null || entry.button == null)
            {
                continue;
            }

            GameInput.ActionName action = entry.action;
            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => BeginRebind(action));
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void OnStartNewGame()
    {
        currentSlotMode = SlotMode.NewGame;
        slotTitleText.text = "Choose Slot For New Game";
        ShowSlotPanel();
    }

    public void OnOpenContinueSlots()
    {
        currentSlotMode = SlotMode.Continue;
        slotTitleText.text = "Choose Slot To Continue";
        ShowSlotPanel();
    }

    public void OnOpenSettings()
    {
        ShowSettingsPanel();
    }

    public void OnBackToMain()
    {
        ShowMainPanel();
    }

    public void OnExitGame()
    {
        StartCoroutine(TransitionOut(ExitGame));
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (slotPanel != null) slotPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (statusText != null) statusText.text = string.Empty;
        RefreshSlotButtons();
        RefreshSettingsLabels();
    }

    private void ShowSlotPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (slotPanel != null) slotPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (statusText != null) statusText.text = string.Empty;
        RefreshSlotButtons();
    }

    private void ShowSettingsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (slotPanel != null) slotPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (settingsScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            settingsScrollRect.verticalNormalizedPosition = 1f;
        }
        if (statusText != null) statusText.text = string.Empty;
        RefreshSettingsLabels();
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

        if (statusText != null)
        {
            statusText.text = $"Slot {slotIndex + 1} is empty.";
        }
    }

    private void LoadGameplayScene()
    {
        StartCoroutine(TransitionOut(() =>
        {
            Time.timeScale = 1f;
            GameSession.SetGameplayInputBlocked(false);
            SceneManager.LoadScene(GameplaySceneName);
        }));
    }

    private void RefreshSlotButtons()
    {
        SaveGameSystem saveSystem = SaveGameSystem.Instance;

        for (int i = 0; i < slotButtons.Length && i < slotButtonTexts.Length; i++)
        {
            bool hasData = saveSystem != null && saveSystem.HasSlotData(i);
            string summary = saveSystem != null ? saveSystem.GetSlotSummary(i) : "Empty";

            if (slotButtons[i] != null)
            {
                slotButtons[i].interactable = currentSlotMode == SlotMode.NewGame || hasData;
            }

            if (slotButtonTexts[i] != null)
            {
                slotButtonTexts[i].text = $"Slot {i + 1}\n{summary}";
            }
        }
    }

    private void RefreshSettingsLabels()
    {
        GameInput input = GameInput.Instance;
        if (input == null)
        {
            return;
        }

        foreach (BindingEntry entry in bindingEntries)
        {
            if (entry == null || entry.valueText == null)
            {
                continue;
            }

            entry.valueText.text = input.GetBindingLabel(entry.action);
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }
    }

    private void BeginRebind(GameInput.ActionName action)
    {
        GameInput input = GameInput.Instance;
        if (input == null || !bindingLookup.TryGetValue(action, out BindingEntry entry))
        {
            return;
        }

        if (entry.valueText != null)
        {
            entry.valueText.text = "Press a key...";
        }

        if (statusText != null)
        {
            statusText.text = $"Waiting for new key: {entry.label}";
        }

        input.BeginRebind(action, _ =>
        {
            RefreshSettingsLabels();
            if (statusText != null)
            {
                statusText.text = $"{entry.label} -> {input.GetBindingLabel(action)}";
            }
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

    private void PlayMenuMusic()
    {
        if (menuMusicSource == null || menuMusicClip == null)
        {
            return;
        }

        menuMusicSource.clip = menuMusicClip;
        menuMusicSource.loop = true;
        menuMusicSource.playOnAwake = false;
        menuMusicSource.volume = 0.18f;

        if (!menuMusicSource.isPlaying)
        {
            menuMusicSource.Play();
        }
    }

    private IEnumerator TransitionOut(Action onComplete)
    {
        if (isTransitioning)
        {
            yield break;
        }

        isTransitioning = true;

        if (fadeOverlay != null)
        {
            yield return FadeOverlayRoutine(1f, 0.28f);
        }

        onComplete?.Invoke();
    }

    private IEnumerator FadeOverlayRoutine(float targetAlpha, float duration)
    {
        if (fadeOverlay == null)
        {
            yield break;
        }

        Color color = fadeOverlay.color;
        float startAlpha = color.a;
        float elapsed = 0f;
        fadeOverlay.raycastTarget = true;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeOverlay.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeOverlay.color = color;
        fadeOverlay.raycastTarget = targetAlpha > 0.01f;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
        {
            return;
        }

        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
        fadeOverlay.raycastTarget = alpha > 0.01f;
    }

    private static void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
#pragma warning restore CS0649
