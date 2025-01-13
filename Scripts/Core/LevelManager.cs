using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Linq;

[System.Serializable]
public class StageEvent : UnityEvent<StageDefinition> { }

[System.Serializable]
public class LevelEvent : UnityEvent<LevelDefinition> { }

[System.Serializable]
public class ObjectiveEvent : UnityEvent<StageObjective> { }

public class LevelManager : MonoBehaviour
{
    private static LevelManager _instance;
    public static LevelManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    #region Inspector Fields
    [Header("Level Data")]
    public LevelDefinition currentLevel;
    public int currentStageIndex = 0;
    
    [Header("Events")]
    public StageEvent onStageStart = new StageEvent();
    public StageEvent onStageComplete = new StageEvent();
    public LevelEvent onLevelComplete = new LevelEvent();
    public ObjectiveEvent onObjectiveProgress = new ObjectiveEvent();

    [Header("Settings")]
    [SerializeField] private float stageTransitionDelay = 1.5f;
    #endregion

    #region Properties
    public StageEvent OnStageStart => onStageStart;
    public StageEvent OnStageComplete => onStageComplete;
    public LevelEvent OnLevelComplete => onLevelComplete;
    public ObjectiveEvent OnObjectiveProgress => onObjectiveProgress;
    #endregion

    private CardDealer cardDealer;
    private GameManager gameManager;

    private bool isInitialized = false;
    private bool isGameStarted = false;

    #region Unity Lifecycle
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        cardDealer = GetComponent<CardDealer>();
        gameManager = GetComponent<GameManager>();

        if (cardDealer == null || gameManager == null)
        {
            Debug.LogError("[LevelManager] Required components missing!");
            return;
        }

        InitializeEvents();
        // Başlangıçta component'i deaktif et
        enabled = false;
        
        Debug.Log("[LevelManager] Initialized successfully.");
    }

    public void StartGame()
    {
        if (!isGameStarted)
        {
            isGameStarted = true;
            enabled = true;
            
            if (currentLevel == null)
            {
                Debug.LogError("No level data set before StartGame!");
                return;
            }
            
            Debug.Log($"Starting game with level: {currentLevel.levelName}");
            InitializeLevel();
        }
    }

    public void LoadLevelFromPlayerPrefs()
    {
        if (!isGameStarted) return;
        
        int levelIndex = PlayerPrefs.GetInt("CurrentLevel", 1);
        LoadLevel(levelIndex);
    }

    public void LoadLevel(int levelIndex)
    {
        if (!isGameStarted) return;

        currentLevel = LevelContainer.Instance.GetLevel(levelIndex);

        if (currentLevel == null)
        {
            Debug.LogError($"[LevelManager] Could not load level {levelIndex}");
            return;
        }

        InitializeLevel();
        Debug.Log($"[LevelManager] Loaded level {levelIndex}");
    }

    private void Start() { }
    #endregion

    #region Initialization

    public void Initialize()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            Debug.Log("LevelManager initialized");
        }
    }

    private void InitializeEvents()
    {
        onStageStart ??= new StageEvent();
        onStageComplete ??= new StageEvent();
        onLevelComplete ??= new LevelEvent();
        onObjectiveProgress ??= new ObjectiveEvent();
    }

    private void ValidateReferences()
    {
        // Yeni yöntem kullanarak referansları doğrula
        if (cardDealer == null)
            cardDealer = Object.FindFirstObjectByType<CardDealer>();
        
        if (gameManager == null)
            gameManager = Object.FindFirstObjectByType<GameManager>();

        if (cardDealer == null || gameManager == null)
        {
            Debug.LogError($"Missing required components! CardDealer: {cardDealer != null}, GameManager: {gameManager != null}");
        }
    }

    private void InitializeLevel()
    {
        if (currentLevel == null)
        {
            Debug.LogError("[LevelManager] No level data available!");
            return;
        }

        Debug.Log($"[LevelManager] Starting level {currentLevel.levelName} with {currentLevel.stages.Count} stages");
        
        PlayerPrefs.SetInt("CurrentLevel", currentLevel.levelNumber);
        PlayerPrefs.Save();
        
        currentStageIndex = 0;
        StartStage(0);
    }
    #endregion

    #region Stage Management
    public void StartStage(int stageIndex)
    {
        if (!ValidateStageIndex(stageIndex)) return;

        currentStageIndex = stageIndex;
        var stage = currentLevel.stages[currentStageIndex];
        
        Debug.Log($"Starting stage {currentStageIndex + 1} of level {currentLevel.levelName}");
        
        LoadLayout(stage.layoutData);
        ResetObjectives(stage);
        onStageStart?.Invoke(stage);
    }

    private bool ValidateStageIndex(int stageIndex)
    {
        if (currentLevel == null)
        {
            Debug.LogError("No level is currently set!");
            return false;
        }

        if (stageIndex >= currentLevel.stages.Count)
        {
            Debug.LogError($"Invalid stage index: {stageIndex}. Total stages: {currentLevel.stages.Count}");
            return false;
        }

        return true;
    }

    private void CompleteStage()
    {
        var currentStage = currentLevel.stages[currentStageIndex];
        currentStage.isCompleted = true;
        onStageComplete?.Invoke(currentStage);

        Debug.Log($"Stage {currentStageIndex + 1} completed. Total stages: {currentLevel.stages.Count}");

        if (currentStageIndex < currentLevel.stages.Count - 1)
        {
            Debug.Log($"Moving to next stage {currentStageIndex + 2}");
            StartCoroutine(TransitionToNextStage());
        }
        else
        {
            Debug.Log("This was the last stage, completing level");
            CompleteLevel();
        }
    }

    private IEnumerator TransitionToNextStage()
    {
        Debug.Log("Starting transition to next stage...");
        
        cardDealer?.ClearCurrentCards();
        yield return new WaitForSeconds(stageTransitionDelay);
        
        currentStageIndex++;
        Debug.Log($"Transitioning to stage {currentStageIndex + 1}");
        
        StartStage(currentStageIndex);
    }
    #endregion

    #region Objective Management
    public void OnCardMatched(int cardID)
    {
        Debug.Log($"Card matched - ID: {cardID}");
        var stage = currentLevel.stages[currentStageIndex];

        foreach (var objective in stage.objectives)
        {
            UpdateObjectiveForCard(objective, cardID);
            Debug.Log($"Objective Progress - Type: {objective.type}, Current: {objective.currentAmount}, Target: {objective.targetAmount}");
        }
        
        CheckStageProgress();
    }

    private void UpdateObjectiveForCard(StageObjective objective, int cardID)
    {
        switch (objective.type)
        {
            case ObjectiveType.CollectSpecificCards:
                if (cardID == objective.specificCardID)
                {
                    objective.currentAmount += 3;
                    Debug.Log($"Added 3 specific cards (ID: {cardID}). Current: {objective.currentAmount}/{objective.targetAmount}");
                }
                break;
            case ObjectiveType.CollectCardAmount:
                objective.currentAmount += 3;
                Debug.Log($"Added 3 cards. Current: {objective.currentAmount}/{objective.targetAmount}");
                break;
            case ObjectiveType.MatchPairs:
                objective.currentAmount += 1;
                Debug.Log($"Added 1 match. Current: {objective.currentAmount}/{objective.targetAmount}");
                break;
        }

        if (objective.currentAmount >= objective.targetAmount)
        {
            objective.isCompleted = true;
            Debug.Log($"Objective completed: {objective.type}");
        }

        onObjectiveProgress?.Invoke(objective);
    }

    public void CheckStageProgress()
    {
        var currentStage = currentLevel.stages[currentStageIndex];
        int totalObjectives = currentStage.objectives.Count;
        int completedObjectives = 0;

        foreach(var objective in currentStage.objectives)
        {
            if(objective.CheckCompletion())
            {
                completedObjectives++;
            }
            Debug.Log($"Objective: {objective.type}, Progress: {objective.currentAmount}/{objective.targetAmount}, Completed: {objective.isCompleted}");
        }

        Debug.Log($"Stage Progress: {completedObjectives}/{totalObjectives} objectives completed");

        if (completedObjectives == totalObjectives)
        {
            Debug.Log("All objectives completed, completing stage");
            CompleteStage();
        }
    }

    public void ResetObjectives(StageDefinition stage)
    {
        if (stage == null) return;

        foreach (var objective in stage.objectives)
        {
            objective.currentAmount = 0;
            objective.isCompleted = false;
        }
    }
    #endregion

    #region Layout Management
    private void LoadLayout(LayoutData layout)
    {
        if (layout == null)
        {
            Debug.LogError("Layout data is null!");
            return;
        }

        var totalPositions = layout.layers.Sum(layer => layer.positions.Count);
        Debug.Log($"Loading layout: {layout.layoutName} with {totalPositions} positions in {layout.layers.Count} layers");

        if (cardDealer != null)
        {
            cardDealer.ClearCurrentCards();
            cardDealer.SetLayout(layout);
            
            var currentStage = currentLevel.stages[currentStageIndex];
            cardDealer.InitializeStage(currentStage);
            
            Debug.Log($"Stage {currentStageIndex + 1} initialized with layout: {layout.layoutName}");
        }
        else
        {
            Debug.LogError("CardDealer not found! Make sure CardDealer component exists in the scene.");
        }
    }
    #endregion

    #region Level Management
    private void CompleteLevel()
    {
        Debug.Log($"Completing level: {currentLevel.levelName}");
        onLevelComplete?.Invoke(currentLevel);
    }

    public void ResetLevel()
    {
        isGameStarted = false;
        enabled = false;
        
        cardDealer?.ClearCurrentCards();
        currentStageIndex = 0;
        
        if (currentLevel != null)
        {
            foreach (var stage in currentLevel.stages)
            {
                stage.isCompleted = false;
                ResetObjectives(stage);
            }
        }

        ClearEventListeners();
        currentLevel = null;
        Debug.Log("Level Manager reset completed");
    }

    private void ClearEventListeners()
    {
        onStageStart?.RemoveAllListeners();
        onStageComplete?.RemoveAllListeners();
        onLevelComplete?.RemoveAllListeners();
        onObjectiveProgress?.RemoveAllListeners();
    }
    #endregion
}
