using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "MyGame/Card Data List", fileName = "NewCardDataList")]
public class CardDataList : ScriptableObject
{
    public List<SetGroup> sets;
    public TrickGroup trickGroup;
    public SpecialGroup specialGroup;

    public CardData GetDataByID(int id)
    {
        // Normal kartlar
        if (sets != null)
        {
            foreach (var set in sets)
            {
                if (set.suits != null)
                {
                    foreach (var suit in set.suits)
                    {
                        if (suit.ranks != null)
                        {
                            foreach (var card in suit.ranks)
                            {
                                if (card.cardID == id)
                                    return card;
                            }
                        }
                    }
                }
            }
        }

        // Trick kartlar
        if (trickGroup != null && trickGroup.trickRanks != null)
        {
            foreach (var trickRank in trickGroup.trickRanks)
            {
                if (trickRank.trickVariations != null)
                {
                    foreach (var card in trickRank.trickVariations)
                    {
                        if (card.cardID == id)
                            return card;
                    }
                }
            }
        }

        // Special kartlar
        if (specialGroup != null && specialGroup.specialTypes != null)
        {
            foreach (var specialType in specialGroup.specialTypes)
            {
                if (specialType.cards != null)
                {
                    foreach (var card in specialType.cards)
                    {
                        if (card.cardID == id)
                            return card;
                    }
                }
            }
        }

        // Kart bulunamadı
        return null;
    }
}

[System.Serializable]
public class SetGroup
{
    public string setName;
    public List<SuitGroup> suits;
}

[System.Serializable]
public class SuitGroup
{
    public string suitName;
    public List<CardData> ranks;
}

[System.Serializable]
public class CardData
{
    public int cardID;
    public string cardName;
    public Sprite cardSprite;
    public int rank;
    public bool isTrick;
    // Yeni alanlar ekleyebilirsiniz, örneğin:
    // public bool isJoker;
    // public bool isEmpty;
}

[System.Serializable]
public class TrickGroup
{
    public string groupName = "Trick";
    public List<TrickRankGroup> trickRanks;
}

[System.Serializable]
public class TrickRankGroup
{
    public int rank;
    public List<CardData> trickVariations;
}

[System.Serializable]
public class SpecialGroup
{
    public string groupName = "Special";
    public List<SpecialTypeGroup> specialTypes;
}

[System.Serializable]
public class SpecialTypeGroup
{
    public string typeName; // "Joker", "Empty", vb.
    public List<CardData> cards;
}
