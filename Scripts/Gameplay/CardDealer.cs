using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardDealer : MonoBehaviour
{
    public LevelDefinition currentLevel;
    public CardDataList cardDataList;
    public GameObject cardPrefab;

    private List<int> workingDeck;
    private int uniqueCardCounter = 0; // Her kart için unique ID

    void Start()
    {
        if (LevelSelection.selectedLevel != null)
        {
            currentLevel = LevelSelection.selectedLevel;
            Debug.Log("Loaded level: " + currentLevel.levelName);
        }
        else
        {
            Debug.LogError("No level selected! Defaulting to first level.");
        }

        InitDeck();
        ShuffleDeck();
        PlaceCardsAccordingToLayout();
    }

    void InitDeck()
    {
        if (currentLevel == null)
        {
            Debug.LogError("currentLevel is null in CardDealer!");
            return;
        }

        int totalCards = currentLevel.totalCards;
        var allowedIDs = currentLevel.allowedCardIDs;

        workingDeck = new List<int>();

        if (allowedIDs.Count == 0)
        {
            Debug.LogError("No allowed IDs in LevelDefinition!");
            return;
        }

        int copiesPerID = totalCards / allowedIDs.Count;
        foreach (int id in allowedIDs)
        {
            for (int i = 0; i < copiesPerID; i++)
            {
                workingDeck.Add(id);
            }
        }

        int remainder = totalCards % allowedIDs.Count;
        for (int i = 0; i < remainder; i++)
        {
            workingDeck.Add(allowedIDs[i]);
        }

        Debug.Log("Deck initialized with " + workingDeck.Count + " cards.");
    }

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

    void PlaceCardsAccordingToLayout()
    {
        var layout = currentLevel.layoutData;
        var posList = layout.positions;

        int spawnCount = Mathf.Min(posList.Count, workingDeck.Count);
        
        // Kartları katmanlara göre oluştur
        // Önce en alttaki katmandan başla
        int maxLayer = posList.Max(p => p.layer);
        
        for (int currentLayer = maxLayer; currentLayer >= 0; currentLayer--)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                var p = posList[i];
                
                // Sadece mevcut katmandaki kartları oluştur
                if (p.layer != currentLayer) continue;

                int cardID = workingDeck[i];
                var cData = cardDataList.GetDataByID(cardID);
                if (cData == null) continue;

                // Kartı oluştur
                GameObject newCardObj = CreateCard(p, cardID, cData);
                
                // Kart bileşenlerini ayarla
                SetupCardComponents(newCardObj, p, currentLayer);
            }
        }
    }

    private GameObject CreateCard(LayoutPosition p, int cardID, CardData cData)
    {
        // Kartın temel pozisyonunu ayarla
        Vector3 position = p.position;
        position.z = CalculateZPosition(p.layer);

        // Kartı oluştur
        GameObject newCardObj = Instantiate(cardPrefab, position, Quaternion.Euler(0, 0, p.rotation));
        
        // Unique ID ata
        uniqueCardCounter++;
        
        return newCardObj;
    }

    private void SetupCardComponents(GameObject cardObj, LayoutPosition p, int layer)
    {
        // Card bileşenini al ve ayarla
        Card cardComp = cardObj.GetComponent<Card>();
        if (cardComp != null)
        {
            cardComp.uniqueID = uniqueCardCounter;
            cardComp.SetupCard(workingDeck[uniqueCardCounter - 1], 
                cardDataList.GetDataByID(workingDeck[uniqueCardCounter - 1]).cardSprite, 
                p.isHidden);
        }

        // Z pozisyonunu ayarla
        Vector3 position = cardObj.transform.position;
        position.z = -layer * 0.1f; // Üstteki kartlar daha küçük Z değerine sahip olmalı
        cardObj.transform.position = position;

        // Rigidbody ayarları
        Rigidbody2D rb = cardObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = cardObj.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;

        // Collider ayarları
        BoxCollider2D collider = cardObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = cardObj.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;

        // SpriteRenderer sorting order ayarı
        SpriteRenderer renderer = cardObj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = layer;
        }
    }

    private float CalculateZPosition(int layer)
    {
        return -layer * 0.1f;
    }
}