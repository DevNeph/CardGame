using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MyGame/Level Definition", fileName = "NewLevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    public string levelName;
    public int levelNumber;

    [Header("Level Stages")]
    public List<StageDefinition> stages = new List<StageDefinition>();

    [Header("Level Requirements")]
    public int starsRequired; // Bu leveli açmak için gereken yıldız sayısı
    public Sprite levelThumbnail;
}

[System.Serializable]
public class StageDefinition
{
    public string stageName;
    [Header("Which layout?")]
    public LayoutData layoutData;

    [Header("Which card IDs are used?")]
    public List<int> allowedCardIDs;

    [Header("Stage Requirements")]
    public int totalCards;
    public int cardsToCollect;
    public List<StageObjective> objectives;
    public float timeLimit = -1; // -1 = sınırsız süre
    public int moveLimit = -1;   // -1 = sınırsız hamle

    [Header("Stage Rewards")]
    public int stageScore = 100;
    public List<CardReward> cardRewards;
    public bool isCompleted;
}

[System.Serializable]
public class CardReward
{
    public int cardID;
    public int amount;
}