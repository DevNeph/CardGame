using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardDealer : MonoBehaviour
{
    #region Inspector Fields
    [Header("Required References")]
    [SerializeField] public CardDataList cardDataList;
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
    private bool IsValidCardID(int cardID)
    {
        return cardID >= 0 && cardDataList.GetDataByID(cardID) != null;
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

        List<(Vector3 position, bool isHidden, int layerIndex)> allPositions = GetAllPositions();
        int totalPositions = allPositions.Count;

        List<int> remainingCards = new List<int>(workingDeck);
        List<int> targetCards = GetTargetCards();

        // Target kartları remainingCards'tan çıkar
        foreach (var cardId in targetCards)
        {
            if (remainingCards.Contains(cardId))
            {
                remainingCards.Remove(cardId);
            }
        }

        // Target kartları ilk pozisyonlara yerleştir
        for (int i = 0; i < targetCards.Count && i < allPositions.Count; i++)
        {
            int cardID = targetCards[i];
            var (position, isHidden, layerIndex) = allPositions[i];
            SpawnCard(cardID, position, isHidden, layerIndex);
        }

        // Kalan pozisyonları remainingCards ile doldur
        int remainingPositions = totalPositions - targetCards.Count;
        if (remainingPositions > 0 && remainingCards.Count > 0)
        {
            FillRemainingPositions(
                remainingPositions, 
                remainingCards, 
                allPositions.Skip(targetCards.Count).ToList()
            );
        }

        GameManager.Instance?.UpdateAllCardsAppearance();
    }


    private List<(Vector3, bool, int)> GetAllPositions()
    {
        List<(Vector3 position, bool isHidden, int layerIndex)> allPositions = new List<(Vector3, bool, int)>();

        for (int i = currentLayout.layers.Count - 1; i >= 0; i--)
        {
            var layer = currentLayout.layers[i];
            foreach (var pos in layer.positions)
            {
                float zPos = (currentLayout.layers.Count - 1 - i) * -0.1f;
                Vector3 worldPos = new Vector3(pos.x, pos.y, zPos);
                allPositions.Add((worldPos, pos.isHidden, i));
            }
        }
        return allPositions;
    }

    private List<int> GetTargetCards()
    {
        List<int> targetCards = new List<int>();

        if (currentStage?.objectives != null)
        {
            foreach (var objective in currentStage.objectives)
            {
                if (objective.type == ObjectiveType.CollectSpecificCards && objective.specificCardID >= 0)
                {
                    int requiredCount = objective.targetAmount;
                    for (int i = 0; i < requiredCount; i++)
                    {
                        targetCards.Add(objective.specificCardID);
                    }
                }
            }
        }

        return targetCards;
    }

    private void FillRemainingPositions(int positionCount, List<int> availableCards, List<(Vector3 position, bool isHidden, int layerIndex)> positions)
    {
        if (availableCards.Count == 0) return;

        List<int> shuffledCards = new List<int>(availableCards);
        for (int i = shuffledCards.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            (shuffledCards[i], shuffledCards[randIndex]) = (shuffledCards[randIndex], shuffledCards[i]);
        }

        int cardIndex = 0;
        for (int i = 0; i < positionCount && i < positions.Count; i++)
        {
            var (position, isHidden, layerIndex) = positions[i];
            
            if (cardIndex >= shuffledCards.Count)
            {
                cardIndex = 0;
            }

            SpawnCard(shuffledCards[cardIndex], position, isHidden, layerIndex);
            cardIndex++;
        }
    }

    private bool ValidateLayout()
    {
        if (currentLayout == null || currentLayout.layers == null || currentLayout.layers.Count == 0)
        {
            Debug.LogError("No valid layout data!");
            return false;
        }

        int totalPositions = currentLayout.layers.Sum(layer => layer.positions.Count);
        if (totalPositions == 0)
        {
            Debug.LogError("No positions defined in layout!");
            return false;
        }

        return true;
    }

    private void SpawnCard(int cardID, Vector3 position, bool isHidden, int layerIndex)
    {
        var cardData = cardDataList.GetDataByID(cardID);
        if (cardData == null) return;

        var cardObj = Instantiate(cardPrefab, position, Quaternion.identity);
        var card = cardObj.GetComponent<Card>();

        if (card != null)
        {
            card.SetupCard(cardID, cardData.cardSprite, isHidden);
            card.SetLayerIndex(layerIndex);
            
            // Box Collider ayarları
            BoxCollider2D boxCollider = cardObj.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector2(3f, 3.9f);
                boxCollider.offset = Vector2.zero;
            }
            
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
            var totalPositions = layout.layers.Sum(layer => layer.positions.Count);
            Debug.Log($"Layout set: {layout.layoutName} with {totalPositions} positions in {layout.layers.Count} layers");
        }
    }

    public void ClearCurrentCards()
    {
        var existingCards = Object.FindObjectsByType<Card>(FindObjectsSortMode.None);
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
