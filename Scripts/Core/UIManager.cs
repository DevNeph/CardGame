using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    #region Singleton & Serialized Fields
    public static UIManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private GameObject mainMenuUIPrefab;  
    [SerializeField] private GameObject gameplayUIPrefab;
    [SerializeField] private CardDataList cardDataList;
    #endregion

    #region Private Fields
    private Canvas mainCanvas;
    private GameObject currentMainMenuUI;
    private GameObject currentGameplayUI;

    // UI References - MainMenu
    private TMP_Text[] levelTexts;
    private TMP_Text levelTitleText;
    private TMP_Text stageCountText;
    private TMP_Text levelObjectivesText;
    private Button playButton;

    // UI References - Gameplay
    private TMP_Text removedCardsText;
    private TMP_Text targetCardsText;
    private TMP_Text cardIDText;
    private TMP_Text stageNameText;
    private TMP_Text objectivesText;
    private GameObject stageTransitionPanel;
    private GameObject levelCompletePanel;
    private GameObject levelFailedPanel;
    private LoadingScreen loadingScreen;

    private LevelManager levelManager;
    private GameManager gameManager;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache primary references
            mainCanvas = Object.FindFirstObjectByType<Canvas>();
            if (mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObj);
            }

            gameManager = Object.FindFirstObjectByType<GameManager>();
            levelManager = Object.FindFirstObjectByType<LevelManager>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        Debug.Log("UIManager Start called");
        
        levelManager = levelManager ?? Object.FindFirstObjectByType<LevelManager>();
        gameManager = gameManager ?? Object.FindFirstObjectByType<GameManager>();
        
        levelManager?.Initialize();
        await InitializeUI();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            ResetPlayerData();
        }
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.onStageStart.RemoveListener(OnStageStart);
            levelManager.onStageComplete.RemoveListener(OnStageComplete);
            levelManager.onLevelComplete.RemoveListener(OnLevelComplete);
            levelManager.onObjectiveProgress.RemoveListener(OnObjectiveProgress);
        }
    }
    #endregion

    #region Initialization Methods
    private async Task InitializeUI()
    {
        Debug.Log("Initializing UI...");
        
        if (loadingScreen == null && loadingScreenPrefab != null)
        {
            GameObject loadingScreenObj = Instantiate(loadingScreenPrefab);
            loadingScreen = loadingScreenObj.GetComponent<LoadingScreen>();
            if (mainCanvas != null) loadingScreenObj.transform.SetParent(mainCanvas.transform, false);
        }

        try
        {
            if (loadingScreen != null)
                await loadingScreen.LoadGameContent();

            if (currentGameplayUI != null)
            {
                Destroy(currentGameplayUI);
            }
            currentGameplayUI = Instantiate(gameplayUIPrefab);
            currentGameplayUI.SetActive(false);

            ShowMainMenu();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during UI initialization: {e.Message}");
        }
    }
    #endregion

    #region UI Setup Methods
    public void ShowMainMenu()
    {
        if (currentGameplayUI != null)
        {
            Destroy(currentGameplayUI);
            currentGameplayUI = null;
        }
        SetupMainMenuUI();
    }

    private void SetupMainMenuUI()
    {
        Debug.Log("Setting up MainMenuUI");
        try
        {
            if (currentMainMenuUI == null && mainMenuUIPrefab != null)
            {
                currentMainMenuUI = Instantiate(mainMenuUIPrefab);
                if (mainCanvas != null) currentMainMenuUI.transform.SetParent(mainCanvas.transform, false);
                SetupMainMenuReferences();
            }
            currentMainMenuUI?.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up MainMenuUI: {e.Message}");
        }
    }

    private void SetupMainMenuReferences()
    {
        if (currentMainMenuUI == null) return;

        try
        {
            Transform levelSelectionPanel = currentMainMenuUI.transform.Find("LevelSelectionPanel");
            if (levelSelectionPanel == null)
            {
                Debug.LogError("LevelSelectionPanel not found.");
                return;
            }

            Transform levelDisplay = levelSelectionPanel.Find("LevelDisplay");
            if (levelDisplay == null)
            {
                Debug.LogError("LevelDisplay not found.");
                return;
            }

            levelTexts = new TMP_Text[5];
            levelTexts[0] = levelDisplay.Find("Level-2Text")?.GetComponent<TMP_Text>();
            levelTexts[1] = levelDisplay.Find("Level-1Text")?.GetComponent<TMP_Text>();
            levelTexts[2] = levelDisplay.Find("CurrentLevelText")?.GetComponent<TMP_Text>();
            levelTexts[3] = levelDisplay.Find("Level+1Text")?.GetComponent<TMP_Text>();
            levelTexts[4] = levelDisplay.Find("Level+2Text")?.GetComponent<TMP_Text>();

            Transform levelInfoPanel = levelSelectionPanel.Find("LevelInfoPanel");
            if (levelInfoPanel == null)
            {
                Debug.LogError("LevelInfoPanel not found.");
                return;
            }

            levelTitleText = levelInfoPanel.Find("LevelTitleText")?.GetComponent<TMP_Text>();
            stageCountText = levelInfoPanel.Find("StageCountText")?.GetComponent<TMP_Text>();
            levelObjectivesText = levelInfoPanel.Find("LevelObjectivesText")?.GetComponent<TMP_Text>();
            playButton = levelInfoPanel.Find("PlayButton")?.GetComponent<Button>();

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => OnPlayButtonClicked());
            }

            if (levelTexts[2] != null)
                UpdateLevelDisplay();

            UpdateLevelInfo();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SetupMainMenuReferences: {e.Message}\n{e.StackTrace}");
        }
    }

    private void SetupGameplayUIReferences()
    {
        Debug.Log("Setting up GameplayUI references");

        Transform topPanel = currentGameplayUI.transform.Find("TopPanel");
        if (topPanel != null)
        {
            removedCardsText = topPanel.Find("RemovedCardsText")?.GetComponent<TMP_Text>();
            targetCardsText = topPanel.Find("TargetCardsText")?.GetComponent<TMP_Text>();
            stageNameText = topPanel.Find("StageNameText")?.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogError("TopPanel not found in GameplayUI");
        }

        Transform objectivesPanel = currentGameplayUI.transform.Find("ObjectivesPanel");
        if (objectivesPanel != null)
        {
            objectivesText = objectivesPanel.Find("ObjectivesText")?.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogError("ObjectivesPanel not found in GameplayUI");
        }

        stageTransitionPanel = currentGameplayUI.transform.Find("StageTransitionPanel")?.gameObject;
        levelCompletePanel = currentGameplayUI.transform.Find("LevelCompletePanel")?.gameObject;
        levelFailedPanel = currentGameplayUI.transform.Find("LevelFailedPanel")?.gameObject;

        levelManager = levelManager ?? Object.FindFirstObjectByType<LevelManager>();
        gameManager = gameManager ?? Object.FindFirstObjectByType<GameManager>();

        if (levelManager != null)
        {
            levelManager.onStageStart.AddListener(OnStageStart);
            levelManager.onStageComplete.AddListener(OnStageComplete);
            levelManager.onLevelComplete.AddListener(OnLevelComplete);
            levelManager.onObjectiveProgress.AddListener(OnObjectiveProgress);
        }
        else
        {
            Debug.LogError("LevelManager not found in scene");
        }
    }

    private void SetupGameplayUI()
    {
        Debug.Log("Setting up GameplayUI");
        
        if (currentMainMenuUI != null)
        {
            Destroy(currentMainMenuUI);
            currentMainMenuUI = null;
        }

        if (currentGameplayUI != null)
        {
            Destroy(currentGameplayUI);
            currentGameplayUI = null;
        }

        currentGameplayUI = Instantiate(gameplayUIPrefab);
        Debug.Log("GameplayUI instantiated");
        
        if (mainCanvas != null) currentGameplayUI.transform.SetParent(mainCanvas.transform, false);
        
        SetupGameplayUIReferences();
    }
    #endregion

    #region Level & Objective Update Methods
    public void UpdateLevelDisplay()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        int totalLevels = LevelContainer.Instance.GetTotalLevelCount();

        if (levelTexts == null || levelTexts.Length < 5) return;

        PreviousLevel2Text(levelTexts, currentLevel);
        PreviousLevel1Text(levelTexts, currentLevel);
        SetCurrentLevelText(levelTexts[2], currentLevel);
        NextLevel1Text(levelTexts, currentLevel, totalLevels);
        NextLevel2Text(levelTexts, currentLevel, totalLevels);

        UpdateLevelInfo();
    }

    private void PreviousLevel2Text(TMP_Text[] texts, int currentLevel) {
        if (currentLevel >= 3) {
            texts[0].text = (currentLevel - 2).ToString();
            texts[0].gameObject.SetActive(true);
        } else {
            texts[0].gameObject.SetActive(false);
        }
    }

    private void PreviousLevel1Text(TMP_Text[] texts, int currentLevel) {
        if (currentLevel >= 2) {
            texts[1].text = (currentLevel - 1).ToString();
            texts[1].gameObject.SetActive(true);
        } else {
            texts[1].gameObject.SetActive(false);
        }
    }

    private void SetCurrentLevelText(TMP_Text currentText, int currentLevel) {
        currentText.text = currentLevel.ToString();
        currentText.gameObject.SetActive(true);
    }

    private void NextLevel1Text(TMP_Text[] texts, int currentLevel, int totalLevels) {
        if (currentLevel < totalLevels) {
            texts[3].text = (currentLevel + 1).ToString();
            texts[3].gameObject.SetActive(true);
        } else {
            texts[3].gameObject.SetActive(false);
        }
    }

    private void NextLevel2Text(TMP_Text[] texts, int currentLevel, int totalLevels) {
        if (currentLevel < totalLevels - 1) {
            texts[4].text = (currentLevel + 2).ToString();
            texts[4].gameObject.SetActive(true);
        } else {
            texts[4].gameObject.SetActive(false);
        }
    }

    private void UpdateLevelInfo()
    {
        int currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 1);
        LevelDefinition level = LevelContainer.Instance.GetLevel(currentLevelIndex);
        if (level == null) return;

        if (levelTitleText != null)
            levelTitleText.text = $"Level {level.levelNumber}";
        if (stageCountText != null)
            stageCountText.text = $"Stages: {level.stages.Count}";

        if (levelObjectivesText != null)
        {
            string objectives = "\n Objectives:\n\n";
            foreach (var stage in level.stages)
            {
                objectives += $"Stage {level.stages.IndexOf(stage) + 1}:\n";
                foreach (var objective in stage.objectives)
                {
                    switch (objective.type)
                    {
                        case ObjectiveType.CollectSpecificCards:
                            CardData cardData = cardDataList.GetDataByID(objective.specificCardID);
                            string cardName = cardData != null && !string.IsNullOrEmpty(cardData.cardName) 
                                ? cardData.cardName 
                                : $"Card {objective.specificCardID}";
                            objectives += $"• Collect {objective.targetAmount} of {cardName}\n";
                            break;
                        case ObjectiveType.CollectCardAmount:
                            objectives += $"• Collect {objective.targetAmount} Cards\n";
                            break;
                        case ObjectiveType.MatchPairs:
                            objectives += $"• Match {objective.targetAmount} Pairs\n";
                            break;
                    }
                }
                objectives += "\n";
            }
            levelObjectivesText.text = objectives;
        }
    }

    public void UpdateObjectiveProgress(StageObjective objective)
    {
        if (targetCardsText != null)
            targetCardsText.text = $"{objective.currentAmount}/{objective.targetAmount}";

        if (levelManager != null && levelManager.currentLevel != null)
            UpdateObjectivesText(levelManager.currentLevel.stages[levelManager.currentStageIndex]);
    }

    public void UpdateRemovedCardText(int count)
    {
        if (removedCardsText != null)
            removedCardsText.text = $"Toplam: {count}";
    }

    public void UpdateObjectiveUI(StageDefinition stage)
    {
        if (stage.objectives != null && stage.objectives.Count > 0)
        {
            var firstObjective = stage.objectives[0];
            if (targetCardsText != null)
                targetCardsText.text = $"0/{firstObjective.targetAmount}";

            if (cardIDText != null)
            {
                switch (firstObjective.type)
                {
                    case ObjectiveType.CollectSpecificCards:
                        CardData cardData = cardDataList.GetDataByID(firstObjective.specificCardID);
                        cardIDText.text = cardData != null && !string.IsNullOrEmpty(cardData.cardName)
                            ? $"Target: Collect {cardData.cardName}"
                            : $"Target: Collect Card {firstObjective.specificCardID}";
                        break;
                    case ObjectiveType.CollectCardAmount:
                        cardIDText.text = "Target: Collect Cards";
                        break;
                    case ObjectiveType.MatchPairs:
                        cardIDText.text = "Target: Match Pairs";
                        break;
                    default:
                        cardIDText.text = $"Target: {firstObjective.type}";
                        break;
                }
            }
        }
    }

    public void UpdateObjectivesText(StageDefinition stage)
    {
        if (objectivesText == null) return;

        string objectives = "";
        foreach (var objective in stage.objectives)
        {
            string progress = $"{objective.currentAmount}/{objective.targetAmount}";
            string status = objective.isCompleted ? "✓" : "";

            if (objective.type == ObjectiveType.CollectSpecificCards)
            {
                CardData cardData = cardDataList.GetDataByID(objective.specificCardID);
                if (cardData != null && !string.IsNullOrEmpty(cardData.cardName))
                {
                    objectives += $"Collect {cardData.cardName} {progress} {status}\n";
                }
                else
                {
                    Debug.LogWarning($"Card data not found for ID: {objective.specificCardID}");
                    objectives += $"Collect Card {objective.specificCardID} {progress} {status}\n";
                }
            }
            else
            {
                objectives += $"{objective.description} {progress} {status}\n";
            }
        }
        objectivesText.text = objectives;
    }
    #endregion

    #region Stage & Level Event Handlers
    private void OnStageStart(StageDefinition stage)
    {
        if (stageNameText != null)
            stageNameText.text = $"Stage {levelManager.currentStageIndex + 1}: {stage.stageName}";

        stageTransitionPanel?.SetActive(false);
        UpdateObjectiveUI(stage);
        UpdateObjectivesText(stage);
    }

    private void OnStageComplete(StageDefinition stage)
    {
        if (levelManager != null && levelManager.currentLevel != null)
        {
            bool isLastStage = levelManager.currentStageIndex == levelManager.currentLevel.stages.Count - 1;
            if (!isLastStage && stageTransitionPanel != null)
            {
                stageTransitionPanel.SetActive(true);
            }
        }
    }

    private void OnLevelComplete(LevelDefinition level)
    {
        ShowLevelCompletePanel();
        int nextLevel = level.levelNumber + 1;
        if (nextLevel <= LevelContainer.Instance.GetTotalLevelCount())
        {
            PlayerPrefs.SetInt("CurrentLevel", nextLevel);
            PlayerPrefs.Save();
        }
    }

    private void OnObjectiveProgress(StageObjective objective)
    {
        UpdateObjectiveProgress(objective);
    }
    #endregion

    #region Panel Display Methods
    public void ShowLevelCompletePanel()
    {
        GameManager.IsPopupActive = true;
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            Button button = levelCompletePanel.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    GameManager.IsPopupActive = false;
                    ReturnToMainMenu();
                });
            }
        }
    }

    public void ShowLevelFailedPanel()
    {
        GameManager.IsPopupActive = true;
        if (levelFailedPanel != null)
        {
            levelFailedPanel.SetActive(true);
            Button button = levelFailedPanel.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    GameManager.IsPopupActive = false;
                    ReturnToMainMenu();
                });
            }
        }
    }
    #endregion

    #region Gameplay Control Methods
    public async void OnPlayButtonClicked()
    {
        if (currentMainMenuUI != null)
        {
            currentMainMenuUI.SetActive(false);
            Destroy(currentMainMenuUI);
            currentMainMenuUI = null;
        }

        SetupGameplayUI();

        int currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 1);
        LevelDefinition levelToLoad = LevelContainer.Instance.GetLevel(currentLevelIndex);

        Debug.Log($"Loading level: {levelToLoad?.levelName ?? "null"}");

        if (levelToLoad == null)
        {
            Debug.LogError("Could not load level data!");
            ReturnToMainMenu();
            return;
        }

        if (gameManager != null)
        {
            gameManager.enabled = true;
            await gameManager.StartGame(levelToLoad);
        }
        else
        {
            Debug.LogError("GameManager is null!");
        }

        if (levelManager != null)
        {
            levelManager.currentLevel = levelToLoad;
            levelManager.StartGame();
        }
        else
        {
            Debug.LogError("LevelManager is null!");
        }
    }

    public void ReturnToMainMenu()
    {
        levelManager?.ResetLevel();
        gameManager?.ResetGame();

        if (currentGameplayUI != null)
        {
            Destroy(currentGameplayUI);
            currentGameplayUI = null;
        }
        
        ShowMainMenu();
    }
    #endregion

    #region Player Data Methods
    public void SetLevel(int levelIndex)
    {
        PlayerPrefs.SetInt("CurrentLevel", levelIndex);
        PlayerPrefs.Save();
        
        LevelManager.Instance?.LoadLevel(levelIndex);
    }

    public void LoadCurrentLevel()
    {
        LevelManager.Instance?.LoadLevelFromPlayerPrefs();
    }

    public void ResetPlayerData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Player data has been reset.");
    }
    #endregion
}
