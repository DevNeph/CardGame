using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private GameObject mainMenuUIPrefab;  
    [SerializeField] private GameObject gameplayUIPrefab;

    private GameObject currentMainMenuUI;
    private GameObject currentGameplayUI;

    // UI References - MainMenu
    private TMP_Text[] levelTexts;
    private TMP_Text levelTitleText;    // Level 1, Level 2 vs.
    private TMP_Text stageCountText;    // Stage: 1/3 gibi
    private TMP_Text levelObjectivesText; // Sadece objectives bilgisi
    private Button playButton;

    // Level textleri için property'ler
    private TMP_Text PreviousLevel2Text => levelTexts[0];
    private TMP_Text PreviousLevel1Text => levelTexts[1];
    private TMP_Text CurrentLevelText => levelTexts[2];
    private TMP_Text NextLevel1Text => levelTexts[3];
    private TMP_Text NextLevel2Text => levelTexts[4];

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
    private int currentLevelIndex = 0;

    private async void Start()
    {
        Debug.Log("UIManager Start called");
        
        // Önce referansları al
        levelManager = FindFirstObjectByType<LevelManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        
        // LevelManager'ı initialize et ama level yükleme
        if (levelManager != null)
        {
            levelManager.Initialize();
        }
        
        await InitializeUI();
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ana Canvas'ı bul veya oluştur
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObj);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async Task InitializeUI()
    {
        Debug.Log("Initializing UI...");
        
        // Loading Screen'i oluştur
        if (loadingScreen == null)
        {
            GameObject loadingScreenObj = Instantiate(loadingScreenPrefab);
            loadingScreen = loadingScreenObj.GetComponent<LoadingScreen>();
            
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                loadingScreenObj.transform.SetParent(mainCanvas.transform, false);
            }
        }

        try
        {
            // İlk yüklemeyi başlat
            if (loadingScreen != null)
            {
                await loadingScreen.LoadGameContent();
            }

            // Gameplay UI'ı gizli olarak oluştur
            if (currentGameplayUI != null)
            {
                Destroy(currentGameplayUI);
            }
            currentGameplayUI = Instantiate(gameplayUIPrefab);
            currentGameplayUI.SetActive(false);

            // Ana menüyü göster
            ShowMainMenu();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during UI initialization: {e.Message}");
        }
    }

    public void ShowMainMenu()
    {
        if (currentGameplayUI != null)
        {
            Destroy(currentGameplayUI);
            currentGameplayUI = null;
        }
        SetupMainMenuUI();
    }

    public async Task StartGame()
    {
        Debug.Log("StartGame method called");
        
        if (currentMainMenuUI != null)
        {
            currentMainMenuUI.SetActive(false);  // Ana menüyü gizle
        }
        
        // Önce GameplayUI'ı hazırla
        SetupGameplayUI();
        
        // Level'ı yükle ve oyunu başlat
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        LevelDefinition levelToLoad = LevelContainer.Instance.GetLevel(currentLevel);
        
        if (gameManager != null && levelToLoad != null)
        {
            currentGameplayUI.SetActive(true);  // GameplayUI'ı göster
            await gameManager.StartGame(levelToLoad);
        }
        else
        {
            Debug.LogError("GameManager or Level is null!");
            ReturnToMainMenu(); // Hata durumunda ana menüye dön
        }
    }

    private void SetupMainMenuUI()
    {
        Debug.Log("Setting up MainMenuUI");
        
        try
        {
            if (currentMainMenuUI == null && mainMenuUIPrefab != null)
            {
                currentMainMenuUI = Instantiate(mainMenuUIPrefab);
                
                Canvas mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    currentMainMenuUI.transform.SetParent(mainCanvas.transform, false);
                }
                
                // UI elementlerinin referanslarını al
                SetupMainMenuReferences();
            }
            
            if (currentMainMenuUI != null)
            {
                currentMainMenuUI.SetActive(true);
            }
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
            // Önce LevelSelectionPanel'i bul
            Transform levelSelectionPanel = currentMainMenuUI.transform.Find("LevelSelectionPanel");
            if (levelSelectionPanel == null)
            {
                Debug.LogError("LevelSelectionPanel not found. Please check MainMenuUI prefab structure.");
                return;
            }

            // Sonra LevelDisplay'i LevelSelectionPanel altında ara
            Transform levelDisplay = levelSelectionPanel.Find("LevelDisplay");
            if (levelDisplay == null)
            {
                Debug.LogError("LevelDisplay not found under LevelSelectionPanel.");
                return;
            }

            levelTexts = new TMP_Text[5];
            levelTexts[0] = levelDisplay.Find("Level-2Text")?.GetComponent<TMP_Text>();
            levelTexts[1] = levelDisplay.Find("Level-1Text")?.GetComponent<TMP_Text>();
            levelTexts[2] = levelDisplay.Find("CurrentLevelText")?.GetComponent<TMP_Text>();
            levelTexts[3] = levelDisplay.Find("Level+1Text")?.GetComponent<TMP_Text>();
            levelTexts[4] = levelDisplay.Find("Level+2Text")?.GetComponent<TMP_Text>();

            // Level info panel referanslarını al
            Transform levelInfoPanel = levelSelectionPanel.Find("LevelInfoPanel");
            if (levelInfoPanel == null)
            {
                Debug.LogError("LevelInfoPanel not found under LevelSelectionPanel.");
                return;
            }

            // Yeni eklenen referanslar
            levelTitleText = levelInfoPanel.Find("LevelTitleText")?.GetComponent<TMP_Text>();
            stageCountText = levelInfoPanel.Find("StageCountText")?.GetComponent<TMP_Text>();
            levelObjectivesText = levelInfoPanel.Find("LevelObjectivesText")?.GetComponent<TMP_Text>();
            playButton = levelInfoPanel.Find("PlayButton")?.GetComponent<Button>();

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => OnPlayButtonClicked());
            }

            // Level display'i güvenli bir şekilde güncelle
            if (levelTexts[2] != null)
            {
                UpdateLevelDisplay();
            }

            // Level bilgilerini güncelle
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
        
        // Top panel referanslarını al
        var topPanel = currentGameplayUI.transform.Find("TopPanel");
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

        // Objectives panel referansını al
        var objectivesPanel = currentGameplayUI.transform.Find("ObjectivesPanel");
        if (objectivesPanel != null)
        {
            objectivesText = objectivesPanel.Find("ObjectivesText")?.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogError("ObjectivesPanel not found in GameplayUI");
        }

        // Panel referanslarını al
        stageTransitionPanel = currentGameplayUI.transform.Find("StageTransitionPanel")?.gameObject;
        levelCompletePanel = currentGameplayUI.transform.Find("LevelCompletePanel")?.gameObject;
        levelFailedPanel = currentGameplayUI.transform.Find("LevelFailedPanel")?.gameObject;

        // Game Manager ve Level Manager referanslarını al
        levelManager = FindFirstObjectByType<LevelManager>();
        gameManager = FindFirstObjectByType<GameManager>();

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
        
        if (currentGameplayUI == null)
        {
            currentGameplayUI = Instantiate(gameplayUIPrefab);
            Debug.Log("GameplayUI instantiated");
            
            // Parent'ını Canvas yapalım
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                currentGameplayUI.transform.SetParent(mainCanvas.transform, false);
            }
            
            // UI elementlerinin referanslarını al
            SetupGameplayUIReferences();
        }
    }
    
    public void UpdateLevelDisplay()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        int totalLevels = LevelContainer.Instance.GetTotalLevelCount();
        
        // Level -2
        if (currentLevel >= 3)
        {
            PreviousLevel2Text.text = (currentLevel - 2).ToString();
            PreviousLevel2Text.gameObject.SetActive(true);
        }
        else
        {
            PreviousLevel2Text.gameObject.SetActive(false);
        }

        // Level -1
        if (currentLevel >= 2)
        {
            PreviousLevel1Text.text = (currentLevel - 1).ToString();
            PreviousLevel1Text.gameObject.SetActive(true);
        }
        else
        {
            PreviousLevel1Text.gameObject.SetActive(false);
        }

        // Mevcut Level
        CurrentLevelText.text = currentLevel.ToString();
        CurrentLevelText.gameObject.SetActive(true);

        // Level +1
        if (currentLevel < totalLevels)
        {
            NextLevel1Text.text = (currentLevel + 1).ToString();
            NextLevel1Text.gameObject.SetActive(true);
        }
        else
        {
            NextLevel1Text.gameObject.SetActive(false);
        }

        // Level +2
        if (currentLevel < totalLevels - 1)
        {
            NextLevel2Text.text = (currentLevel + 2).ToString();
            NextLevel2Text.gameObject.SetActive(true);
        }
        else
        {
            NextLevel2Text.gameObject.SetActive(false);
        }

        // Level bilgilerini güncelle
        UpdateLevelInfo();
    }

    private void UpdateLevelInfo()
    {
        int currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 1);
        LevelDefinition level = LevelContainer.Instance.GetLevel(currentLevelIndex);
        
        if (level == null) return;

        // Level başlığını güncelle
        if (levelTitleText != null)
        {
            levelTitleText.text = $"Level {level.levelNumber}";
        }

        // Stage sayısını güncelle
        if (stageCountText != null)
        {
            stageCountText.text = $"Stages: {level.stages.Count}";
        }

        // Objectives bilgisini güncelle
        if (levelObjectivesText != null)
        {
            string objectives = "Objectives:\n\n";
            
            foreach (var stage in level.stages)
            {
                objectives += $"Stage {level.stages.IndexOf(stage) + 1}:\n";
                foreach (var objective in stage.objectives)
                {
                    switch (objective.type)
                    {
                        case ObjectiveType.CollectSpecificCards:
                            objectives += $"• Collect {objective.targetAmount} of Card {objective.specificCardID}\n";
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
        {
            targetCardsText.text = $"{objective.currentAmount}/{objective.targetAmount}";
        }
        if (levelManager != null && levelManager.currentLevel != null)
        {
            UpdateObjectivesText(levelManager.currentLevel.stages[levelManager.currentStageIndex]);
        }
    }

    public void UpdateRemovedCardText(int count)
    {
        if (removedCardsText != null)
        {
            removedCardsText.text = $"Toplam: {count}";
        }
    }

    public void UpdateObjectiveUI(StageDefinition stage)
    {
        if (stage.objectives != null && stage.objectives.Count > 0)
        {
            var firstObjective = stage.objectives[0];
            
            if (targetCardsText != null)
            {
                targetCardsText.text = $"0/{firstObjective.targetAmount}";
            }

            if (cardIDText != null)
            {
                switch (firstObjective.type)
                {
                    case ObjectiveType.CollectSpecificCards:
                        cardIDText.text = $"Hedef: Kart {firstObjective.specificCardID} Topla";
                        break;
                    case ObjectiveType.CollectCardAmount:
                        cardIDText.text = "Hedef: Kart Topla";
                        break;
                    case ObjectiveType.MatchPairs:
                        cardIDText.text = "Hedef: Eşleştirme Yap";
                        break;
                    default:
                        cardIDText.text = $"Hedef: {firstObjective.type}";
                        break;
                }
            }
        }
    }

    public void UpdateObjectivesText(StageDefinition stage)
    {
        if (objectivesText != null)
        {
            string objectives = "";
            foreach (var objective in stage.objectives)
            {
                string progress = $"{objective.currentAmount}/{objective.targetAmount}";
                string status = objective.isCompleted ? "✓" : "";
                objectives += $"{objective.description} {progress} {status}\n";
            }
            objectivesText.text = objectives;
        }
    }

    private void OnStageStart(StageDefinition stage)
    {
        if (stageNameText != null)
        {
            stageNameText.text = $"Stage {levelManager.currentStageIndex + 1}: {stage.stageName}";
        }
        
        // Stage transition panelini kapat
        if (stageTransitionPanel != null)
        {
            stageTransitionPanel.SetActive(false);
        }
        
        UpdateObjectiveUI(stage);
        UpdateObjectivesText(stage);
    }

    private void OnStageComplete(StageDefinition stage)
    {
        // Son aşama değilse aşama geçiş panelini göster
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
        
        // Bir sonraki leveli kaydet
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

    public async void OnPlayButtonClicked()
    {
        Debug.Log("Play button clicked");
        
        if (currentMainMenuUI != null)
        {
            currentMainMenuUI.SetActive(false);
        }

        SetupGameplayUI();
        currentGameplayUI.SetActive(true);

        // Mevcut level'ı al
        int currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 1);
        LevelDefinition levelToLoad = LevelContainer.Instance.GetLevel(currentLevelIndex);

        Debug.Log($"Loading level: {levelToLoad?.levelName ?? "null"}");

        // Level kontrolü
        if (levelToLoad == null)
        {
            Debug.LogError("Could not load level data!");
            ReturnToMainMenu();
            return;
        }

        // GameManager ve LevelManager'ı başlat
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
        if (levelManager != null)
        {
            levelManager.ResetLevel();
        }
        
        if (gameManager != null)
        {
            gameManager.ResetGame();
        }
        
        ShowMainMenu();
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

    public void SetLevel(int levelIndex)
    {
        PlayerPrefs.SetInt("CurrentLevel", levelIndex);
        PlayerPrefs.Save();
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(levelIndex);
        }
    }

    public void LoadCurrentLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevelFromPlayerPrefs();
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R)) // R tuşuna basıldığında reset
        {
            ResetPlayerData();
        }
    }

        public void ResetPlayerData()
    {
        // Belirli bir anahtarı silmek için:
        // PlayerPrefs.DeleteKey("HighestCompletedLevel");

        // Tüm PlayerPrefs verilerini temizlemek için:
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("Player data has been reset.");
    }
}