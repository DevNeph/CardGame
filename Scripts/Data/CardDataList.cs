using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MyGame/Card Data List", fileName = "NewCardDataList")]
public class CardDataList : ScriptableObject
{
    #region Fields
    public List<CardData> allCards;
    private Dictionary<int, CardData> cardDataDictionary;
    #endregion

    #region Methods
    private void OnEnable()
    {
        // ScriptableObject etkinleştirildiğinde, kart verilerini hızlıca bulmak için sözlük oluştur
        if (allCards != null)
        {
            cardDataDictionary = new Dictionary<int, CardData>();
            foreach (var card in allCards)
            {
                if (!cardDataDictionary.ContainsKey(card.cardID))
                {
                    cardDataDictionary.Add(card.cardID, card);
                }
            }
        }
    }

    // ID'ye göre CardData bulmak için yardımcı fonksiyon
    public CardData GetDataByID(int id)
    {
        // Sözlükte ara, eğer bulunmazsa listede ara
        if (cardDataDictionary != null && cardDataDictionary.TryGetValue(id, out CardData data))
        {
            return data;
        }
        return allCards?.Find(c => c.cardID == id);
    }
    #endregion
}

[System.Serializable]
public class CardData
{
    #region Fields
    public int cardID;            // Örneğin 0..27 As kartlar, 28 = wild vb.
    public Sprite cardSprite;     // Kartın görseli
    public string cardName;       // İsteğe bağlı (örneğin "AS-0", "Wild", vb.)
    #endregion
}
