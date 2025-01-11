using System.Collections.Generic;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    public LevelDefinition currentLevel;
    public CardDataList cardDataList;
    public GameObject cardPrefab;

    private List<int> workingDeck;

    void Start()
    {
        InitDeck();
        ShuffleDeck();
        PlaceCardsAccordingToLayout();
    }

    void InitDeck()
    {
        int totalCards = currentLevel.totalCards;
        var allowedIDs = currentLevel.allowedCardIDs;
        workingDeck = new List<int>();

        if (allowedIDs.Count == 0)
        {
            Debug.LogError("No allowed IDs for this level!");
            return;
        }

        // Spesifik kart hedeflerini kontrol et ve ekle
        if ((currentLevel.completionType == LevelCompletionType.SpecificCards || 
             currentLevel.completionType == LevelCompletionType.BothConditions) && 
            currentLevel.collectionTargets != null)
        {
            int totalSpecificCards = 0;
            
            foreach (var target in currentLevel.collectionTargets)
            {
                // Gerekli set sayısını hesapla (3'lü gruplar için)
                int requiredSets = Mathf.CeilToInt(target.requiredCount / 3f);
                int cardsToAdd = requiredSets * 3;
                totalSpecificCards += cardsToAdd;

                // Hedef kartları ekle
                for (int i = 0; i < cardsToAdd; i++)
                {
                    workingDeck.Add(target.cardID);
                }

                Debug.Log($"Added {cardsToAdd} cards of ID {target.cardID} (need {target.requiredCount})");
            }

            // Toplam kart sayısını kontrol et ve güncelle
            if (totalSpecificCards > totalCards)
            {
                Debug.LogWarning($"Required specific cards ({totalSpecificCards}) exceed total cards limit ({totalCards}). Adjusting total cards.");
                totalCards = totalSpecificCards;
                currentLevel.totalCards = totalCards;
            }
        }

        // Kalan kartları diğer ID'lerden rastgele ekle
        while (workingDeck.Count < totalCards)
        {
            // Kullanılabilir ID'lerden rastgele seç
            List<int> availableIDs = new List<int>();
            foreach (int id in allowedIDs)
            {
                // Eğer bu ID bir hedef kart değilse, kullanılabilir ID'lere ekle
                bool isTargetCard = false;
                if (currentLevel.collectionTargets != null)
                {
                    foreach (var target in currentLevel.collectionTargets)
                    {
                        if (target.cardID == id)
                        {
                            isTargetCard = true;
                            break;
                        }
                    }
                }
                if (!isTargetCard)
                {
                    availableIDs.Add(id);
                }
            }

            // Eğer kullanılabilir ID kalmadıysa, tüm ID'leri kullan
            if (availableIDs.Count == 0)
            {
                availableIDs = new List<int>(allowedIDs);
            }

            // Rastgele bir ID seç ve 3'lü grup olarak ekle
            int randomID = availableIDs[Random.Range(0, availableIDs.Count)];
            for (int i = 0; i < 3 && workingDeck.Count < totalCards; i++)
            {
                workingDeck.Add(randomID);
            }
        }

        // Debug bilgisi
        Dictionary<int, int> cardCounts = new Dictionary<int, int>();
        foreach (int cardID in workingDeck)
        {
            if (!cardCounts.ContainsKey(cardID))
                cardCounts[cardID] = 0;
            cardCounts[cardID]++;
        }

        string debugInfo = "Final deck composition:\n";
        foreach (var kvp in cardCounts)
        {
            debugInfo += $"Card ID {kvp.Key}: {kvp.Value} cards\n";
        }
        Debug.Log(debugInfo);
    }

    /// <summary>
    /// Deste karıştırılır.
    /// </summary>
    void ShuffleDeck()
    {
        for (int i = 0; i < workingDeck.Count; i++)
        {
            int randIndex = Random.Range(i, workingDeck.Count);
            int temp = workingDeck[i];
            workingDeck[i] = workingDeck[randIndex];
            workingDeck[randIndex] = temp;
        }
    }

    /// <summary>
    /// Kartlar layout'a göre yerleştirilir.
    /// </summary>
    void PlaceCardsAccordingToLayout()
    {
        var layout = currentLevel.layoutData;
        var positions = layout.positions;

        int spawnCount = Mathf.Min(positions.Count, workingDeck.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            int cardID = workingDeck[i];
            var cardData = cardDataList.GetDataByID(cardID);
            if (cardData == null) continue;

            var pos = positions[i];
            var newCardObj = Instantiate(cardPrefab, pos.position, Quaternion.Euler(0, 0, pos.rotation));

            var card = newCardObj.GetComponent<Card>();
            card.SetupCard(cardID, cardData.cardSprite, pos.isHidden);

            // BoxCollider2D boyutunu sprite'a göre ayarla
            var boxCollider = newCardObj.GetComponent<BoxCollider2D>();
            if (boxCollider != null && cardData.cardSprite != null)
            {
                boxCollider.size = cardData.cardSprite.bounds.size;
            }

            // Kartı layer'a göre sahnede doğru sırada göster
            newCardObj.transform.position += Vector3.forward * -pos.layer;
        }

        GameObject.FindObjectOfType<GameManager>()?.UpdateAllCardsAppearance();
    }
}
