using System.Collections.Generic;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    public LevelDefinition currentLevel;  // Seviye ayarları (kart ID'leri, layout bilgisi)
    public CardDataList cardDataList;     // Kart veri listesi (ID -> Sprite eşleştirme)
    public GameObject cardPrefab;         // Kart prefab'ı

    private List<int> workingDeck;        // Oyun için kullanılan deste

    void Start()
    {
        InitDeck();
        ShuffleDeck();
        PlaceCardsAccordingToLayout();
    }

    /// <summary>
    /// Deste oluşturulur.
    /// </summary>
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

        int copiesPerID = totalCards / allowedIDs.Count;
        int remainder = totalCards % allowedIDs.Count;

        foreach (int id in allowedIDs)
        {
            for (int i = 0; i < copiesPerID; i++)
            {
                workingDeck.Add(id);
            }
        }

        for (int i = 0; i < remainder; i++)
        {
            workingDeck.Add(allowedIDs[i]);
        }
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
