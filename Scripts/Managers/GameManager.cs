using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Slots")]
    public Slot[] slots;

    [Header("UI Elements")]
    public TextMeshProUGUI removedCardsText;
    public TextMeshProUGUI targetCardsText; // Yeni eklenen UI element
    public TextMeshProUGUI cardIDText;

    [Header("Level Settings")]
    public LevelDefinition[] levelDefinitions;    
    private int currentLevelIndex = 0;            
    private LevelDefinition currentLevelDefinition; 

    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;

    [Header("Level Failed UI")]
    public GameObject levelFailedPanel;

    private int removedCardCount = 0;
    public Vector3[] slotPositions;
    private bool[] slotOccupied;
    private Dictionary<int, int> collectedCards = new Dictionary<int, int>();

    private void Awake()
    {
        Debug.Log("GameManager Awake - Oyun başlatılıyor");
    }

    private void Start()
    {
        removedCardCount = 0;
        slotOccupied = new bool[slotPositions.Length];

        // Seçilen seviyeyi kontrol et ve başlat
        if(LevelSelection.selectedLevel != null)
        {
            StartLevel(LevelSelection.selectedLevel);
            UpdateAllCardsAppearance();
        }
        else
        {
            if(levelDefinitions != null && levelDefinitions.Length > 0)
            {
                currentLevelIndex = 0;
                currentLevelDefinition = levelDefinitions[currentLevelIndex];
            }
        }

        if(levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            Button returnButton = levelCompletePanel.GetComponentInChildren<Button>();
            if(returnButton != null)
            {
                returnButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        if (targetCardsText == null)
            Debug.LogWarning("targetCardsText is not assigned!");
        if (cardIDText == null)
            Debug.LogWarning("cardIDText is not assigned!");

        InitializeCollectedCards();
        UpdateRemovedCardText();

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    public void StartLevel(LevelDefinition levelDefinition)
    {
        currentLevelDefinition = levelDefinition;
        removedCardCount = 0;
        InitializeCollectedCards();
        UpdateRemovedCardText();
        
        if(levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        
        Debug.Log("Starting Level: " + levelDefinition.levelName);
    }

    private void Update()
    {
    }

    public static bool IsPopupActive = false;

    public void OnCardClicked(Card clickedCard)
    {
        if (IsPopupActive)
        {
            Debug.Log("Popup aktif, tıklama engellendi.");
            return;
        }

        int slotIndex = FindFirstEmptySlotIndex();
        if (slotIndex == -1)
        {
            Debug.Log("Boş slot kalmadı!");
            ShowLevelFailedPanel();
            return;
        }

        Slot freeSlot = slots[slotIndex];
        freeSlot.transform.position = slotPositions[slotIndex];
        slotOccupied[slotIndex] = true;
        freeSlot.PlaceCard(clickedCard);

        bool matchFound = CheckAndRemoveMatches(clickedCard.cardID);

        if (slotIndex == slots.Length - 1 && !matchFound)
        {
            ShowLevelFailedPanel();
            return;
        }

        StartCoroutine(DelayedCheckAndRemove(clickedCard.cardID, 0.0f));

        clickedCard.PlaceInSlot();
        UpdateAllCardsAppearance();
    }

    public void UpdateAllCardsAppearance()
    {
        Card[] allCards = Object.FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card card in allCards)
        {
            card.UpdateCardAppearance();
        }
    }

    public void AddRemovedCard(int count)
    {
        removedCardCount += count;
        Debug.Log("Removed card count updated to: " + removedCardCount);
        UpdateRemovedCardText();
        CheckLevelCompletion();
    }

    private int GetLevelNumber(LevelDefinition levelDefinition)
    {
        return levelDefinition.levelNumber;
    }

    private void CheckLevelCompletion()
    {
        if (currentLevelDefinition == null) return;

        bool totalCardsCompleted = false;
        bool specificCardsCompleted = false;

        // Toplam kart hedefi kontrolü
        if (currentLevelDefinition.completionType == LevelCompletionType.TotalCards || 
            currentLevelDefinition.completionType == LevelCompletionType.BothConditions)
        {
            totalCardsCompleted = removedCardCount >= currentLevelDefinition.cardsToCollect;
        }

        // Spesifik kart hedefi kontrolü
        if (currentLevelDefinition.completionType == LevelCompletionType.SpecificCards || 
            currentLevelDefinition.completionType == LevelCompletionType.BothConditions)
        {
            specificCardsCompleted = true;
            foreach (var target in currentLevelDefinition.collectionTargets)
            {
                if (!collectedCards.ContainsKey(target.cardID) || 
                    collectedCards[target.cardID] < target.requiredCount)
                {
                    specificCardsCompleted = false;
                    break;
                }
            }
        }

        // Level tamamlanma kontrolü
        bool levelCompleted = false;
        switch (currentLevelDefinition.completionType)
        {
            case LevelCompletionType.TotalCards:
                levelCompleted = totalCardsCompleted;
                break;
            case LevelCompletionType.SpecificCards:
                levelCompleted = specificCardsCompleted;
                break;
            case LevelCompletionType.BothConditions:
                levelCompleted = totalCardsCompleted && specificCardsCompleted;
                break;
        }

        if (levelCompleted)
        {
            LevelComplete();
        }
    }

    private void UpdateRemovedCardText()
    {
        if (removedCardsText == null || currentLevelDefinition == null) return;

        string mainText = "";
        string targetText = "";
        string idText = "";

        // Toplam kart hedefi gösterimi
        if (currentLevelDefinition.completionType == LevelCompletionType.TotalCards || 
            currentLevelDefinition.completionType == LevelCompletionType.BothConditions)
        {
            mainText = $"Toplam: {removedCardCount}/{currentLevelDefinition.cardsToCollect}";
        }

        // Spesifik kart hedefi gösterimi
        if (currentLevelDefinition.completionType == LevelCompletionType.SpecificCards || 
            currentLevelDefinition.completionType == LevelCompletionType.BothConditions)
        {
            if (currentLevelDefinition.collectionTargets != null && 
                currentLevelDefinition.collectionTargets.Count > 0)
            {
                var target = currentLevelDefinition.collectionTargets[0];
                int collected = collectedCards.ContainsKey(target.cardID) ? 
                            collectedCards[target.cardID] : 0;
                
                targetText = $"{collected}/{target.requiredCount}";
                idText = $"Hedef Kart ID: {target.cardID}";
            }
        }

        // UI güncelleme
        removedCardsText.text = mainText;
        
        if (targetCardsText != null)
        {
            targetCardsText.text = targetText;
            targetCardsText.gameObject.SetActive(
                currentLevelDefinition.completionType == LevelCompletionType.SpecificCards || 
                currentLevelDefinition.completionType == LevelCompletionType.BothConditions);
        }
        
        if (cardIDText != null)
        {
            cardIDText.text = idText;
            cardIDText.gameObject.SetActive(
                currentLevelDefinition.completionType == LevelCompletionType.SpecificCards || 
                currentLevelDefinition.completionType == LevelCompletionType.BothConditions);
        }
    }

    private void InitializeCollectedCards()
    {
        collectedCards.Clear();
        if (currentLevelDefinition != null && 
            currentLevelDefinition.collectionTargets != null)
        {
            foreach (var target in currentLevelDefinition.collectionTargets)
            {
                collectedCards[target.cardID] = 0;
            }
            
            // Başlangıç UI güncellemesi
            UpdateRemovedCardText();
        }
    }

    private int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < slotOccupied.Length; i++)
        {
            if (!slotOccupied[i])
                return i;
        }
        return -1;
    }

    private Slot FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].isOccupied)
            {
                return slots[i];
            }
        }
        return null;
    }

    private bool CheckAndRemoveMatches(int cardID)
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isOccupied && slots[i].occupantCard != null)
            {
                if (slots[i].occupantCard.cardID == cardID)
                {
                    count++;
                }
            }
        }

        if (count >= 3)
        {
            Debug.Log($"ID={cardID} karttan {count} tane bulundu. Hepsini yok ediyoruz!");
            int destroyed = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].isOccupied && slots[i].occupantCard != null && 
                    slots[i].occupantCard.cardID == cardID)
                {
                    Destroy(slots[i].occupantCard.gameObject);
                    slots[i].ClearSlot();
                    slotOccupied[i] = false;
                    destroyed++;

                    // Spesifik kart sayacını güncelle
                    if (collectedCards.ContainsKey(cardID))
                    {
                        collectedCards[cardID] += 1;
                    }
                }
            }

            AddRemovedCard(destroyed);
            ShiftCardsLeft();
            return true;
        }

        return false;
    }

    private IEnumerator DelayedCheckAndRemove(int cardID, float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckAndRemoveMatches(cardID);
    }

    private void ShiftCardsLeft()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].isOccupied)
            {
                for (int j = i + 1; j < slots.Length; j++)
                {
                    if (slots[j].isOccupied)
                    {
                        Card movingCard = slots[j].occupantCard;
                        slots[i].PlaceCard(movingCard);
                        slotOccupied[i] = true;
                        slots[j].ClearSlot();
                        slotOccupied[j] = false;
                        movingCard.transform.position = slotPositions[i];
                        break;
                    }
                }
            }
        }
    }

    private void ShowLevelFailedPanel()
    {
        IsPopupActive = true;

        if (levelFailedPanel != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                GameObject panelInstance = Instantiate(levelFailedPanel, canvas.transform);

                Button button = panelInstance.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        IsPopupActive = false;
                        ReturnToMainMenu();
                    });
                }

                panelInstance.SetActive(true);
            }
        }
    }

    private void LevelComplete()
    {
        IsPopupActive = true;

        if (levelCompletePanel != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                GameObject panelInstance = Instantiate(levelCompletePanel, canvas.transform);

                Button button = panelInstance.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        IsPopupActive = false;
                        ReturnToMainMenu();
                    });
                }

                panelInstance.SetActive(true);
            }
        }

        int currentLevelNumber = GetLevelNumber(currentLevelDefinition);
        int highestCompleted = PlayerPrefs.GetInt("HighestCompletedLevel", 0);
        if (currentLevelNumber > highestCompleted)
        {
            PlayerPrefs.SetInt("HighestCompletedLevel", currentLevelNumber);
            PlayerPrefs.Save();
        }
    }

    public void ReturnToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}