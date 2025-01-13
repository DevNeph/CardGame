using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Slot Configuration")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotParent;
    
    private readonly Vector3[] slotPositions = new Vector3[]
    {
        new Vector3(-2.4f, -4f, 0f),
        new Vector3(-1.6f, -4f, 0f),
        new Vector3(-0.8f, -4f, 0f),
        new Vector3(0f, -4f, 0f),
        new Vector3(0.8f, -4f, 0f),
        new Vector3(1.6f, -4f, 0f),
        new Vector3(2.5f, -4f, 0f)
    };

    private Slot[] slots;
    private bool[] slotOccupied;
    private int removedCardCount = 0;
    private Dictionary<int, int> collectedCards = new Dictionary<int, int>();
    private LevelManager levelManager;
    private CardDealer cardDealer;

    public static bool IsPopupActive { get; set; }

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("[GameManager] Instance already exists. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        levelManager = GetComponent<LevelManager>();
        cardDealer = GetComponent<CardDealer>();

        if (levelManager == null || cardDealer == null)
        {
            Debug.LogError("[GameManager] Required components missing!");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("[GameManager] Slot prefab is not assigned!");
            return;
        }

        if (slotParent == null)
        {
            GameObject parentObj = new GameObject("Slots");
            parentObj.transform.SetParent(transform);
            slotParent = parentObj.transform;
        }

        CreateSlots();
        Debug.Log("[GameManager] Initialized successfully.");
    }

    private void Start()
    {
        InitializeSlots();
        SetupEventListeners();
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            RemoveEventListeners();
        }
    }

    #endregion

    #region Slots
    private void CreateSlots()
    {
        slots = new Slot[slotPositions.Length];
        slotOccupied = new bool[slotPositions.Length];

        for (int i = 0; i < slotPositions.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotPositions[i], Quaternion.identity, slotParent);
            slotObj.name = $"Slot_{i}";

            Slot slot = slotObj.GetComponent<Slot>();
            if (slot == null)
            {
                Debug.LogError("[GameManager] Slot component missing on prefab!");
                continue;
            }

            slots[i] = slot;
            slotOccupied[i] = false;
            slotObj.transform.localPosition = slotPositions[i];
        }
    }

    public bool IsSlotAvailable(int index)
    {
        return index >= 0 && index < slotOccupied.Length && !slotOccupied[index];
    }

    public Slot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Length)
            return slots[index];
        return null;
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index >= 0 && index < slotPositions.Length)
            return slotPositions[index];
        return Vector3.zero;
    }

    public int FindEmptySlotIndex()
    {
        for (int i = 0; i < slotOccupied.Length; i++)
        {
            if (!slotOccupied[i]) return i;
        }
        return -1;
    }
    #endregion

    #region Initialization

    private void InitializeSlots()
    {
        removedCardCount = 0;
        slotOccupied = new bool[slotPositions.Length];
        collectedCards.Clear();

        UIManager.Instance?.UpdateRemovedCardText(removedCardCount);
    }

    private void SetupEventListeners()
    {
        if (levelManager != null)
        {
            levelManager.onStageStart.AddListener(OnStageStart);
            levelManager.onStageComplete.AddListener(OnStageComplete);
            levelManager.onLevelComplete.AddListener(OnLevelComplete);
            levelManager.onObjectiveProgress.AddListener(OnObjectiveProgress);
        }
    }

    private void RemoveEventListeners()
    {
        levelManager.onStageStart.RemoveListener(OnStageStart);
        levelManager.onStageComplete.RemoveListener(OnStageComplete);
        levelManager.onLevelComplete.RemoveListener(OnLevelComplete);
        levelManager.onObjectiveProgress.RemoveListener(OnObjectiveProgress);
    }

    #endregion

    #region Game Flow Control

    public async Task StartGame(LevelDefinition level)
    {
        if (level == null)
        {
            Debug.LogError("Cannot start game: Level is null!");
            return;
        }

        IsPopupActive = false;
        InitializeSlots();

        if (levelManager != null)
        {
            await Task.Yield();
            levelManager.currentLevel = level;
            levelManager.StartGame();
        }
    }

    public void ResetGame()
    {
        IsPopupActive = false;
        ClearAllCards();
        InitializeSlots();
        collectedCards.Clear();
        removedCardCount = 0;

        levelManager?.ResetLevel();
    }

    #endregion

    #region Card Management

    public void OnCardClicked(Card clickedCard)
    {
        if (IsPopupActive || clickedCard == null)
        {
            Debug.Log("Card click ignored: popup active or card null");
            return;
        }

        int slotIndex = FindFirstEmptySlotIndex();
        if (slotIndex == -1)
        {
            Debug.Log("No empty slots available!");
            UIManager.Instance?.ShowLevelFailedPanel();
            return;
        }

        PlaceCardInSlot(clickedCard, slotIndex);
    }

    private void PlaceCardInSlot(Card card, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            Slot slot = slots[slotIndex];
            card.transform.position = slotPositions[slotIndex];
            slot.PlaceCard(card);
            card.PlaceInSlot();
            slotOccupied[slotIndex] = true;

            bool matchFound = CheckAndRemoveMatches(card.cardID);

            if (slotIndex == slots.Length - 1 && !matchFound)
            {
                UIManager.Instance?.ShowLevelFailedPanel();
            }

            UpdateAllCardsAppearance();
        }
    }

    private bool CheckAndRemoveMatches(int cardID)
    {
        int count = CountMatchingCards(cardID);

        if (count >= 3)
        {
            RemoveMatchingCards(cardID);
            ShiftCardsLeft();
            levelManager?.OnCardMatched(cardID);
            return true;
        }

        return false;
    }

    private int CountMatchingCards(int cardID)
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isOccupied && 
                slots[i].occupantCard != null && 
                slots[i].occupantCard.cardID == cardID)
            {
                count++;
            }
        }
        return count;
    }

    private void RemoveMatchingCards(int cardID)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isOccupied && slots[i].occupantCard != null && slots[i].occupantCard.cardID == cardID)
            {
                Destroy(slots[i].occupantCard.gameObject);
                slots[i].ClearSlot();
                slotOccupied[i] = false;
            }
        }

        // Removed card count güncellemesi
        int removedCount = CountMatchingCards(cardID);
        AddRemovedCard(removedCount);
    }

    #endregion

    #region Helper Methods

    public void UpdateAllCardsAppearance()
    {
        var allCards = Object.FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            card?.UpdateCardAppearance();
        }
    }

    private void ShiftCardsLeft()
    {
        bool shifted;
        do
        {
            shifted = false;
            for (int i = 0; i < slots.Length - 1; i++)
            {
                if (!slots[i].isOccupied && slots[i + 1].isOccupied)
                {
                    MoveCardToSlot(i + 1, i);
                    shifted = true;
                }
            }
        } while (shifted);

        UpdateAllCardsAppearance();
    }

    private void MoveCardToSlot(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= slots.Length || toIndex < 0 || toIndex >= slots.Length)
            return;

        Card movingCard = slots[fromIndex].occupantCard;
        if (movingCard == null) return;

        slots[fromIndex].ClearSlot();
        slotOccupied[fromIndex] = false;

        slots[toIndex].PlaceCard(movingCard);
        slotOccupied[toIndex] = true;
        movingCard.transform.position = slotPositions[toIndex];
    }

    private int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < slotOccupied.Length; i++)
        {
            if (!slotOccupied[i]) return i;
        }
        return -1;
    }

    private void ClearAllCards()
    {
        var allCards = Object.FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            Destroy(card.gameObject);
        }

        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }
    }

    #endregion

    #region Event Handlers

    private void OnStageStart(StageDefinition stage)
    {
        Debug.Log($"Starting Stage {levelManager.currentStageIndex + 1}");
        InitializeSlots();
        
        cardDealer?.InitializeStage(stage);
        UIManager.Instance?.UpdateRemovedCardText(removedCardCount);
        UIManager.Instance?.UpdateObjectiveUI(stage);
    }

    private void OnStageComplete(StageDefinition stage)
    {
        Debug.Log($"Stage {levelManager.currentStageIndex + 1} completed!");
        ClearAllCards();
        InitializeSlots();
    }

    private void OnLevelComplete(LevelDefinition level)
    {
        Debug.Log($"Level {level.levelName} completed!");
        UIManager.Instance?.ShowLevelCompletePanel();
        SaveProgress(level);
    }

    private void OnObjectiveProgress(StageObjective objective)
    {
        UIManager.Instance?.UpdateObjectiveProgress(objective);
    }

    #endregion

    #region Progress Management

    public void AddRemovedCard(int count)
    {
        removedCardCount += count;
        Debug.Log($"Removed card count updated to: {removedCardCount}");
        UIManager.Instance?.UpdateRemovedCardText(removedCardCount);
    }

    private void SaveProgress(LevelDefinition level)
    {
        if (level != null)
        {
            int currentLevelNumber = level.levelNumber;
            int highestCompleted = PlayerPrefs.GetInt("HighestCompletedLevel", 0);
            
            if (currentLevelNumber > highestCompleted)
            {
                PlayerPrefs.SetInt("HighestCompletedLevel", currentLevelNumber);
                PlayerPrefs.Save();
            }
        }
    }

    #endregion
}
