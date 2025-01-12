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
    [SerializeField] private Transform slotParent; // Slot'ların parent'ı (opsiyonel)
    
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

        // Componentleri al
        levelManager = GetComponent<LevelManager>();
        cardDealer = GetComponent<CardDealer>();

        if (levelManager == null || cardDealer == null)
        {
            Debug.LogError("[GameManager] Required components missing!");
            return;
        }

        // Slot prefab kontrolü
        if (slotPrefab == null)
        {
            Debug.LogError("[GameManager] Slot prefab is not assigned!");
            return;
        }

        // Parent yoksa oluştur
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
            // Slot'u oluştur
            GameObject slotObj = Instantiate(slotPrefab, slotPositions[i], Quaternion.identity, slotParent);
            slotObj.name = $"Slot_{i}";

            // Slot component'ini al
            Slot slot = slotObj.GetComponent<Slot>();
            if (slot == null)
            {
                Debug.LogError($"[GameManager] Slot component missing on prefab!");
                continue;
            }

            // Slot'u kaydet ve initialize et
            slots[i] = slot;
            slotOccupied[i] = false;
            
            // Pozisyonu ayarla
            slotObj.transform.localPosition = slotPositions[i];
        }
    }

        // Slot işlemleri için yardımcı metodlar
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

    private void InitializeComponents()
    {
        // Component referanslarını kontrol et
        if (levelManager == null) levelManager = GetComponent<LevelManager>();
        if (cardDealer == null) cardDealer = GetComponent<CardDealer>();

        // Null check
        if (levelManager == null || cardDealer == null)
        {
            Debug.LogError($"Missing required components! LevelManager: {levelManager != null}, CardDealer: {cardDealer != null}");
        }

        // Slot kontrolü
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("No slots assigned!");
        }

        if (slotPositions == null || slotPositions.Length == 0)
        {
            Debug.LogError("No slot positions assigned!");
        }

        if (slots.Length != slotPositions.Length)
        {
            Debug.LogError($"Slot count ({slots.Length}) does not match position count ({slotPositions.Length})!");
        }
    }

    private void InitializeSlots()
    {
        removedCardCount = 0;
        slotOccupied = new bool[slotPositions.Length];
        collectedCards.Clear();
        
        // UI güncelleme
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRemovedCardText(removedCardCount);
        }
    }

    private void SetupEventListeners()
    {
        if (levelManager != null)
        {
            // Event listener'ları ekle
            levelManager.onStageStart.AddListener(OnStageStart);
            levelManager.onStageComplete.AddListener(OnStageComplete);
            levelManager.onLevelComplete.AddListener(OnLevelComplete);
            levelManager.onObjectiveProgress.AddListener(OnObjectiveProgress);
        }
    }

    private void RemoveEventListeners()
    {
        // Event listener'ları temizle
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
            await Task.Yield(); // Frame geçişi için bekle
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

        if (levelManager != null)
        {
            levelManager.ResetLevel();
        }
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

        // Coroutine yerine direkt metod çağrısı
        PlaceCardInSlot(clickedCard, slotIndex);
    }


    private IEnumerator PlaceCardInSlotCoroutine(Card card, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            Slot slot = slots[slotIndex];
            Vector3 startPos = card.transform.position;
            Vector3 targetPos = slotPositions[slotIndex];
            float duration = 0;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                card.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // Final pozisyonu garantile
            card.transform.position = targetPos;
            
            // Kartı slota yerleştir
            slot.PlaceCard(card);
            card.PlaceInSlot();
            slotOccupied[slotIndex] = true;

            // Eşleşmeleri kontrol et
            bool matchFound = CheckAndRemoveMatches(card.cardID);

            if (slotIndex == slots.Length - 1 && !matchFound)
            {
                UIManager.Instance?.ShowLevelFailedPanel();
            }

            UpdateAllCardsAppearance();
        }
    }

    private void PlaceCardInSlot(Card card, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            Slot slot = slots[slotIndex];
            
            // Kartı direkt olarak hedef pozisyona yerleştir
            card.transform.position = slotPositions[slotIndex];
            
            // Kartı slota yerleştir
            slot.PlaceCard(card);
            card.PlaceInSlot();
            slotOccupied[slotIndex] = true;

            // Eşleşmeleri kontrol et
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
        // Eşleşen kartları say
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
        int destroyed = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isOccupied && slots[i].occupantCard != null && slots[i].occupantCard.cardID == cardID)
            {
                Destroy(slots[i].occupantCard.gameObject);
                slots[i].ClearSlot();
                slotOccupied[i] = false;
                destroyed++;
            }
        }

        AddRemovedCard(destroyed);
    }

    #endregion

    #region Helper Methods

    public void UpdateAllCardsAppearance()
    {
        // Tüm kartları bul
        var allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            if (card != null)
            {
                card.UpdateCardAppearance();
            }
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
        if (fromIndex < 0 || fromIndex >= slots.Length || 
            toIndex < 0 || toIndex >= slots.Length)
            return;

        Card movingCard = slots[fromIndex].occupantCard;
        if (movingCard == null) return;

        // Önce eski slotu temizle
        slots[fromIndex].ClearSlot();
        slotOccupied[fromIndex] = false;

        // Yeni slota yerleştir
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
        var allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
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