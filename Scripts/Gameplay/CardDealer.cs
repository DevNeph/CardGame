using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardDealer : MonoBehaviour
{
    #region Inspector Fields
    [Header("Required References")]
    [SerializeField] private CardDataList cardDataList;
    [SerializeField] private GameObject cardPrefab;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = false;
    #endregion

    #region Private Fields
    private List<int> workingDeck;
    private StageDefinition currentStage;
    private LayoutData currentLayout;
    private Dictionary<int, int> cardCounts;
    #endregion

    #region Initialize Methods
    public void InitializeStage(StageDefinition stage)
    {
        if (ValidateStageInitialization(stage))
        {
            currentStage = stage;
            ClearCurrentCards();
            InitializeDeckForStage();
            ShuffleDeck();
            PlaceCardsAccordingToLayout();
        }
    }

    private bool ValidateStageInitialization(StageDefinition stage)
    {
        if (stage == null)
        {
            Debug.LogError("CardDealer: Stage is null!");
            return false;
        }

        if (cardDataList == null)
        {
            Debug.LogError("CardDealer: CardDataList is not assigned!");
            return false;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("CardDealer: Card Prefab is not assigned!");
            return false;
        }

        return true;
    }

    private void InitializeDeckForStage()
    {
        workingDeck = new List<int>();
        cardCounts = new Dictionary<int, int>();

        AddTargetCardsToWorkingDeck();
        AddBalancedRemainingCards();

        if (showDebugLogs)
        {
            Debug.Log($"Deck initialized with {workingDeck.Count} cards");
            LogCardDistribution();
        }
    }
    #endregion

    #region Card Distribution Methods
    private void AddTargetCardsToWorkingDeck()
    {
        if (currentStage?.objectives == null) return;

        var targetCards = CalculateTargetCardRequirements();
        foreach (var targetCard in targetCards)
        {
            AddCardsOfType(targetCard.Key, targetCard.Value);
            if (showDebugLogs)
            {
                Debug.Log($"Added {targetCard.Value} cards of specific ID {targetCard.Key}");
            }
        }
    }

    private Dictionary<int, int> CalculateTargetCardRequirements()
    {
        var targetCards = new Dictionary<int, int>();

        foreach (var objective in currentStage.objectives)
        {
            if (objective.type == ObjectiveType.CollectSpecificCards && objective.specificCardID >= 0)
            {
                int setsNeeded = Mathf.CeilToInt(objective.targetAmount / 3f);
                int cardsNeeded = setsNeeded * 3;

                if (targetCards.ContainsKey(objective.specificCardID))
                {
                    targetCards[objective.specificCardID] = Mathf.Max(targetCards[objective.specificCardID], cardsNeeded);
                }
                else
                {
                    targetCards[objective.specificCardID] = cardsNeeded;
                }
            }
        }

        return targetCards;
    }

    private void AddBalancedRemainingCards()
    {
        int totalRequired = CalculateTotalRequiredCards();
        int remaining = totalRequired - workingDeck.Count;
        
        if (remaining <= 0) return;

        var availableCards = GetAvailableCards();
        if (!availableCards.Any())
        {
            Debug.LogWarning("No available cards for balanced distribution!");
            return;
        }

        DistributeRemainingCards(remaining, availableCards);
    }

    private int CalculateTotalRequiredCards()
    {
        if (currentStage == null) return 9;

        // Stage'de belirtilen toplam kart sayısını kullan
        return currentStage.totalCardsInStage;
    }

    private List<int> GetAvailableCards()
    {
        return currentStage.allowedCardIDs
            .Where(id => !currentStage.objectives.Any(obj => 
                obj.type == ObjectiveType.CollectSpecificCards && 
                obj.specificCardID == id))
            .Where(id => IsValidCardID(id))
            .ToList();
    }

    private void DistributeRemainingCards(int remainingCards, List<int> availableCards)
    {
        if (availableCards.Count == 0 || remainingCards <= 0) return;

        int totalGroups = remainingCards / 3;
        int baseGroupsPerType = totalGroups / availableCards.Count;
        int extraGroups = totalGroups % availableCards.Count;

        // Kartları tek seferde dağıt
        foreach (int cardID in availableCards)
        {
            int groupsForThisCard = baseGroupsPerType + (extraGroups > 0 ? 1 : 0);
            if (extraGroups > 0) extraGroups--;

            int cardsToAdd = groupsForThisCard * 3;
            if (cardsToAdd > 0)
            {
                AddCardsOfType(cardID, cardsToAdd);
                
                if (showDebugLogs)
                {
                    Debug.Log($"Added {cardsToAdd} cards of type {cardID}");
                }
            }
        }
    }
    #endregion

    #region Card Management Methods
    private int CalculateMaxCardsPerType(int remainingCards, int availableCardCount)
    {
        int maxCards = Mathf.CeilToInt(remainingCards / (float)availableCardCount);
        return Mathf.CeilToInt(maxCards / 3f) * 3;
    }

    private bool IsValidCardID(int cardID)
    {
        return cardID >= 0 && cardDataList.GetDataByID(cardID) != null;
    }

    private int GetLeastUsedCard(List<int> availableCards)
    {
        return availableCards.OrderBy(id => GetCardCount(id)).First();
    }

    private void AddCardsOfType(int cardID, int count)
    {
        if (!IsValidCardID(cardID))
        {
            Debug.LogError($"Invalid card ID: {cardID}");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            workingDeck.Add(cardID);
        }

        if (!cardCounts.ContainsKey(cardID))
            cardCounts[cardID] = 0;
        cardCounts[cardID] += count;
    }

    private int GetCardCount(int cardID)
    {
        return cardCounts.ContainsKey(cardID) ? cardCounts[cardID] : 0;
    }
    #endregion

    #region Layout and Card Placement
    private void ShuffleDeck()
    {
        for (int i = workingDeck.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            (workingDeck[i], workingDeck[randIndex]) = (workingDeck[randIndex], workingDeck[i]);
        }
    }

    private void PlaceCardsAccordingToLayout()
    {
        if (!ValidateLayout()) return;

        // Pozisyonları sırala, ama z pozisyonunu özel olarak ele al
        var sortedPositions = currentLayout.positions
            .OrderByDescending(p => p.layer) // Z yerine layer'a göre sırala
            .Take(workingDeck.Count)
            .ToList();

        for (int i = 0; i < sortedPositions.Count && i < workingDeck.Count; i++)
        {
            SpawnCard(workingDeck[i], sortedPositions[i], i); // index'i de gönder
        }

        GameManager.Instance?.UpdateAllCardsAppearance();
    }
    private bool ValidateLayout()
    {
        if (currentLayout == null || currentLayout.positions == null)
        {
            Debug.LogError("No valid layout data!");
            return false;
        }

        if (currentLayout.positions.Count < workingDeck.Count)
        {
            Debug.LogError($"Not enough positions ({currentLayout.positions.Count}) for cards ({workingDeck.Count})!");
            return false;
        }

        return true;
    }

    private void SpawnCard(int cardID, LayoutPosition pos, int index)
    {
        var cardData = cardDataList.GetDataByID(cardID);
        if (cardData == null) return;

        // Z pozisyonunu index'e göre ayarla
        Vector3 spawnPosition = new Vector3(
            pos.position.x, 
            pos.position.y, 
            -index * 0.01f // Negatif değer kullan (küçük Z = öne)
        );

        var cardObj = Instantiate(cardPrefab, spawnPosition, Quaternion.Euler(0, 0, pos.rotation));
        var card = cardObj.GetComponent<Card>();

        if (card != null)
        {
            card.SetupCard(cardID, cardData.cardSprite, pos.isHidden);
            card.SetLayerIndex(pos.layer);
            card.UpdateCardAppearance();
        }
    }
    #endregion

    #region Public Methods
    public void SetLayout(LayoutData layout)
    {
        currentLayout = layout ?? throw new System.ArgumentNullException(nameof(layout));
        if (showDebugLogs)
        {
            Debug.Log($"Layout set: {layout.layoutName} with {layout.positions.Count} positions");
        }
    }

    public void ClearCurrentCards()
    {
        var existingCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (var card in existingCards)
        {
            Destroy(card.gameObject);
        }
    }

    private void LogCardDistribution()
    {
        if (!showDebugLogs) return;
        
        var distribution = string.Join(", ", cardCounts.Select(kvp => 
            $"Card {kvp.Key}: {kvp.Value}"));
        Debug.Log($"Card Distribution: {distribution}");
    }
    #endregion
}