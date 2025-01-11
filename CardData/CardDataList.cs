using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MyGame/Card Data List", fileName = "NewCardDataList")]
public class CardDataList : ScriptableObject
{
    public List<CardData> allCards;

    // ID'ye göre CardData bulmak için yardımcı fonksiyon
    public CardData GetDataByID(int id)
    {
        return allCards.Find(c => c.cardID == id);
    }
}

[System.Serializable]
public class CardData
{
    public int cardID;            // Örneğin 0..27 As kartlar, 28 = wild vb.
    public Sprite cardSprite;     // Kartın görseli
    public string cardName;       // İsteğe bağlı (örneğin "AS-0", "Wild", vb.)
    // Buraya ek özellikler de ekleyebilirsin (hasar, puan, vb.)
}
