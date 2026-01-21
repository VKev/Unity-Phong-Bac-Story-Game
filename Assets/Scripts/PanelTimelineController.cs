using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PanelTimelineController : MonoBehaviour
{
    [System.Serializable]
    private class SceneButtonConfig
    {
        public string sceneName = string.Empty;
        public string label = string.Empty;
    }

    [SerializeField] private Image panel;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<PanelEvent> events = new List<PanelEvent>();
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private bool hideSettingsOnStart = true;
    [SerializeField] private bool toggleSettingsWithEscape = true;
    private CanvasGroup settingsCanvasGroup;
    private Canvas settingsCanvas;
    private UnityEngine.UI.GraphicRaycaster settingsRaycaster;
    [Header("Scene Buttons")]
    [SerializeField] private Button sceneButtonPrefab;
    [SerializeField] private Transform sceneButtonContainer;
    [SerializeField] private List<SceneButtonConfig> sceneButtons = new List<SceneButtonConfig>();
    [SerializeField] private bool autoLayoutSceneButtons = true;
    [SerializeField] private int sceneButtonColumns = 3;
    [SerializeField] private Vector2 sceneButtonSpacing = new Vector2(8f, 8f);
    [SerializeField] private Vector2 sceneButtonCellSize = new Vector2(200f, 60f);

  [SerializeField]  public  static int globalVoiceId = 0;
    public static int GlobalVoiceId => globalVoiceId;

    private static bool isLoadingScene;
    private static string pendingSceneName = string.Empty;
    private static readonly List<string> buildSceneNames = new List<string>();

    public static void ResetGlobalVoiceId()
    {
        globalVoiceId = 0;
    }

    public static void AdvanceGlobalVoiceId()
    {
        globalVoiceId++;
    }

    private readonly Dictionary<string, bool> eventStates = new Dictionary<string, bool>();
    private readonly Dictionary<PanelAction, IPanelTimelineActionHandler> actionHandlers =
        new Dictionary<PanelAction, IPanelTimelineActionHandler>();
    private readonly List<Button> spawnedSceneButtons = new List<Button>();
    private PanelTimelineContext context;

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<Image>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (panel != null)
            panel.raycastTarget = false; // avoid blocking UI clicks with the fade panel

        context = new PanelTimelineContext(panel, audioSource, eventStates);
        actionHandlers[PanelAction.Transparent] = new TransparentActionHandler();
        actionHandlers[PanelAction.Black] = new BlackActionHandler();
        actionHandlers[PanelAction.Sound] = new SoundActionHandler();
        actionHandlers[PanelAction.Text] = new TextActionHandler();
        actionHandlers[PanelAction.SwitchScene] = new SwitchSceneActionHandler();
        actionHandlers[PanelAction.LockPlayerCamera] = new LockPlayerCameraActionHandler();
        actionHandlers[PanelAction.UnlockPlayerCamera] = new UnlockPlayerCameraActionHandler();
        actionHandlers[PanelAction.RotatePlayerCamera] = new RotatePlayerCameraActionHandler();
        actionHandlers[PanelAction.RotateCameraToTarget] = new RotateCameraToTargetActionHandler();
        actionHandlers[PanelAction.LockPlayerPosition] = new LockPlayerPositionActionHandler();
        actionHandlers[PanelAction.UnlockPlayerPosition] = new UnlockPlayerPositionActionHandler();
        actionHandlers[PanelAction.AfterCredit] = new AfterCreditActionHandler();
    }

    private void Start()
    {
        ResetGlobalVoiceId();
        BuildEventStateLookup();
        PrepareAfterCreditPanels();
        EnsureDefaultSceneButtonEntry();
        InitializeSettingsPanel();
        BuildSceneButtons();

        foreach (PanelEvent panelEvent in events)
            StartCoroutine(RunEvent(panelEvent));
    }

    private void Update()
    {
        if (!toggleSettingsWithEscape || settingsPanel == null)
            return;

        if (IsEscapePressed())
            ToggleSettingsPanel();
    }

    private void BuildEventStateLookup()
    {
        eventStates.Clear();

        foreach (PanelEvent panelEvent in events)
        {
            if (panelEvent == null)
                continue;

            string id = PanelTimelineIdUtility.NormalizeId(panelEvent.eventId);
            if (string.IsNullOrEmpty(id))
                continue;

            if (eventStates.ContainsKey(id))
            {
                Debug.LogWarning($"PanelTimelineController: Duplicate event id '{panelEvent.eventId}'. Using first instance.");
                continue;
            }

            eventStates.Add(id, false);
        }
    }

    private void PrepareAfterCreditPanels()
    {
        if (events == null || events.Count == 0)
            return;

        foreach (PanelEvent panelEvent in events)
        {
            if (panelEvent == null || panelEvent.action != PanelAction.AfterCredit)
                continue;

            GameObject panelObject = panelEvent.afterCreditPanel;
            if (panelObject == null)
                continue;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            if (group == null)
                group = panelObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    private void InitializeSettingsPanel()
    {
        if (settingsPanel == null)
            return;

        EnsureSettingsCanvas();
        EnsureSettingsRaycaster();
        EnsureSettingsCanvasGroup();
        DisableSettingsBackgroundRaycast();
        ApplySettingsPanelVisibility(!hideSettingsOnStart);
    }

    private void BuildSceneButtons()
    {
        if (sceneButtonPrefab == null || sceneButtonContainer == null)
        {
            Debug.LogWarning("PanelTimelineController: Scene buttons not built because prefab or container is missing.");
            return;
        }

        if (sceneButtons == null || sceneButtons.Count == 0)
            return;

        EnsureSceneButtonLayout();
        ClearSpawnedButtons();

        foreach (SceneButtonConfig config in sceneButtons)
        {
            if (config == null)
                continue;

            string sceneName = (config.sceneName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(sceneName))
                continue;

            Button button = Instantiate(sceneButtonPrefab, sceneButtonContainer);
            if (button == null)
                continue;

            spawnedSceneButtons.Add(button);

            string label = string.IsNullOrEmpty(config.label) ? sceneName : config.label;
            ApplyButtonLabel(button, label);

            button.onClick.AddListener(() => LoadSceneByName(sceneName));
        }

        Debug.Log($"PanelTimelineController: Built {spawnedSceneButtons.Count} scene button(s) on '{name}'.");
    }

    private void ClearSpawnedButtons()
    {
        if (spawnedSceneButtons.Count == 0)
            return;

        foreach (Button button in spawnedSceneButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedSceneButtons.Clear();
    }

    private void ApplyButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = label;
    }

    private void LoadSceneByName(string sceneName)
    {
        TryLoadScene(sceneName, "scene button");
    }

    private void EnsureSceneButtonLayout()
    {
        if (!autoLayoutSceneButtons || sceneButtonContainer == null)
            return;

        if (sceneButtonContainer.GetComponent<LayoutGroup>() != null)
            return;

        GridLayoutGroup grid = sceneButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
        Vector2 cellSize = sceneButtonCellSize;
        if (cellSize == Vector2.zero && sceneButtonPrefab != null)
        {
            RectTransform prefabRect = sceneButtonPrefab.GetComponent<RectTransform>();
            if (prefabRect != null && prefabRect.sizeDelta != Vector2.zero)
                cellSize = prefabRect.sizeDelta;
        }

        if (cellSize == Vector2.zero)
            cellSize = new Vector2(200f, 60f);

        grid.cellSize = cellSize;
        grid.spacing = sceneButtonSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        if (sceneButtonColumns > 0)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = sceneButtonColumns;
        }
        else
        {
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
        }
    }

    private void EnsureDefaultSceneButtonEntry()
    {
        const string defaultSceneName = "Chapter 2";
        const string defaultLabel = "Chương 4";
        const string defaultSceneName2 = "Chapter 3";
        const string defaultLabel2 = "Chương 5";

        if (sceneButtons == null)
            sceneButtons = new List<SceneButtonConfig>();

        bool exists = sceneButtons.Exists(cfg =>
            cfg != null && string.Equals((cfg.sceneName ?? string.Empty).Trim(), defaultSceneName, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            sceneButtons.Add(new SceneButtonConfig
            {
                sceneName = defaultSceneName,
                label = defaultLabel
            });
        }

        bool exists2 = sceneButtons.Exists(cfg =>
            cfg != null && string.Equals((cfg.sceneName ?? string.Empty).Trim(), defaultSceneName2, StringComparison.OrdinalIgnoreCase));

        if (!exists2)
        {
            sceneButtons.Add(new SceneButtonConfig
            {
                sceneName = defaultSceneName2,
                label = defaultLabel2
            });
        }
    }

    public void ToggleSettingsPanel()
    {
        ToggleSettingsPanelInternal("ToggleSettingsPanel()");
    }

    // Exposed for UI Button OnClick. Keeps inspector wiring simple.
    public void OnSettingsButtonClicked()
    {
        ToggleSettingsPanelInternal("UI button");
    }

    private void ToggleSettingsPanelInternal(string source)
    {
        if (settingsPanel == null)
            return;

        bool show = !settingsPanel.activeSelf;
        ApplySettingsPanelVisibility(show);
        Debug.Log($"PanelTimelineController: Settings panel set to {(show ? "visible" : "hidden")} via {source}.");
    }

    private void EnsureSettingsCanvasGroup()
    {
        if (settingsPanel == null)
            return;

        if (settingsCanvasGroup == null)
            settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (settingsCanvasGroup == null)
            settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();

        settingsCanvasGroup.ignoreParentGroups = true;
    }

    private void ApplySettingsPanelVisibility(bool visible)
    {
        EnsureSettingsCanvasGroup();
        EnsureSceneButtonsCanvasGroup();

        settingsPanel.SetActive(visible);
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = visible ? 1f : 0f;
            settingsCanvasGroup.interactable = visible;
            settingsCanvasGroup.blocksRaycasts = visible;
        }

        if (sceneButtonContainer != null)
        {
            CanvasGroup buttonsGroup = sceneButtonContainer.GetComponent<CanvasGroup>();
            if (buttonsGroup != null)
            {
                buttonsGroup.interactable = visible;
                buttonsGroup.blocksRaycasts = visible;
            }
        }
    }

    private void EnsureSettingsCanvas()
    {
        if (settingsPanel == null)
            return;

        if (settingsCanvas == null)
            settingsCanvas = settingsPanel.GetComponent<Canvas>();
        if (settingsCanvas == null)
            settingsCanvas = settingsPanel.AddComponent<Canvas>();

        settingsCanvas.overrideSorting = true;
        if (settingsCanvas.sortingOrder < 1000)
            settingsCanvas.sortingOrder = 1000; // keep above other UI layers like choices/prompts
    }

    private void EnsureSettingsRaycaster()
    {
        if (settingsPanel == null)
            return;

        if (settingsRaycaster == null)
            settingsRaycaster = settingsPanel.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (settingsRaycaster == null)
            settingsRaycaster = settingsPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    private bool IsEscapePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame))
            return true;

        return false;
    }

    private void DisableSettingsBackgroundRaycast()
    {
        if (settingsPanel == null)
            return;

        Image image = settingsPanel.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }

    private void EnsureSceneButtonsCanvasGroup()
    {
        if (sceneButtonContainer == null)
            return;

        CanvasGroup group = sceneButtonContainer.GetComponent<CanvasGroup>();
        if (group == null)
            group = sceneButtonContainer.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;
        group.interactable = settingsPanel == null || settingsPanel.activeSelf;
        group.blocksRaycasts = group.interactable;
    }

    private IEnumerator RunEvent(PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        string eventId = PanelTimelineIdUtility.NormalizeId(panelEvent.eventId);

        yield return WaitForStart(panelEvent);

        if (actionHandlers.TryGetValue(panelEvent.action, out IPanelTimelineActionHandler handler) && handler != null)
            yield return handler.Execute(context, panelEvent);
        else
            Debug.LogWarning($"PanelTimelineController: No handler for action '{panelEvent.action}'.");

        if (!string.IsNullOrEmpty(eventId))
            eventStates[eventId] = true;
    }

    private IEnumerator WaitForStart(PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        if (panelEvent.useStartVoiceId)
        {
            int startVoiceId = panelEvent.startVoiceId;
            if (startVoiceId < 0)
            {
                Debug.LogWarning("PanelTimelineController: startVoiceId is negative. Starting immediately.");
            }
            else if (GlobalVoiceId <= startVoiceId)
            {
                yield return new WaitUntil(() => GlobalVoiceId > startVoiceId);
            }
        }
        else if (panelEvent.useTimeStartEventId)
        {
            string startId = PanelTimelineIdUtility.NormalizeId(panelEvent.timeStartEventId);
            if (!string.IsNullOrEmpty(startId))
            {
                string currentId = PanelTimelineIdUtility.NormalizeId(panelEvent.eventId);
                if (startId == currentId)
                {
                    Debug.LogWarning("PanelTimelineController: timeStartEventId matches its own eventId. Starting immediately.");
                }
                else if (eventStates.ContainsKey(startId))
                {
                    yield return new WaitUntil(() => eventStates.TryGetValue(startId, out bool done) && done);
                }
                else
                {
                    Debug.LogWarning($"PanelTimelineController: timeStartEventId '{panelEvent.timeStartEventId}' not found. Starting immediately.");
                }
            }
            else
            {
                Debug.LogWarning("PanelTimelineController: timeStartEventId is empty. Starting immediately.");
            }

            if (panelEvent.timeStartEventDelay > 0f)
                yield return new WaitForSeconds(panelEvent.timeStartEventDelay);
        }
        else if (panelEvent.timeStart > 0f)
            yield return new WaitForSeconds(panelEvent.timeStart);

        if (panelEvent.useStartChoiceId)
        {
            int startChoiceId = panelEvent.startChoiceId;
            yield return new WaitUntil(() => InteractionUITrigger.GlobalChoiceIndex == startChoiceId);
        }
    }

    internal static bool TryLoadScene(string sceneName, string source)
    {
        string trimmed = (sceneName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            Debug.LogWarning($"PanelTimelineController: Ignoring empty scene name from {source}.");
            return false;
        }

        if (isLoadingScene)
        {
            Debug.LogWarning($"PanelTimelineController: Already loading scene '{pendingSceneName}'. Ignoring {source} request for '{trimmed}'.");
            return false;
        }

        string resolvedName = ResolveSceneName(trimmed);
        if (string.IsNullOrEmpty(resolvedName))
        {
            Debug.LogWarning($"PanelTimelineController: Scene '{trimmed}' not found in build settings. Available scenes: {DescribeBuildScenes()}");
            return false;
        }

        Debug.Log($"PanelTimelineController: Loading scene '{resolvedName}' (requested by {source}).");
        AsyncOperation operation = SceneManager.LoadSceneAsync(resolvedName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogWarning($"PanelTimelineController: SceneManager.LoadSceneAsync returned null for '{resolvedName}'.");
            return false;
        }

        pendingSceneName = resolvedName;
        isLoadingScene = true;
        operation.completed += _ =>
        {
            isLoadingScene = false;
            pendingSceneName = string.Empty;
        };

        return true;
    }

    private static string ResolveSceneName(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            return sceneName;

        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        if (Application.CanStreamedLevelBeLoaded(scenePath))
            return scenePath;

        int buildCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < buildCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
                continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(fileName, sceneName, StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return string.Empty;
    }

    private static string DescribeBuildScenes()
    {
        buildSceneNames.Clear();

        int buildCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < buildCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
                continue;

            string name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(name))
                continue;

            bool exists = false;
            for (int j = 0; j < buildSceneNames.Count; j++)
            {
                if (string.Equals(buildSceneNames[j], name, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                buildSceneNames.Add(name);
        }

        if (buildSceneNames.Count == 0)
            return "<none>";

        return string.Join(", ", buildSceneNames);
    }
}
