using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MyGame/Level Definition", fileName = "NewLevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    public string levelName;
    public int levelNumber;

    [Header("Level Stages")]
    public List<StageDefinition> stages = new List<StageDefinition>();

    [Header("UI Elements")]
    public Sprite levelThumbnail;
    public int starsRequired;

    // Stage bilgilerine kolay erişim için
    public StageDefinition GetCurrentStage(int index)
    {
        return stages != null && stages.Count > index ? stages[index] : null;
    }
}

[System.Serializable]
public class StageDefinition
{
    public string stageName;

    [Header("Layout Settings")]
    public LayoutData layoutData;

    [Header("Card Settings")]
    public List<int> allowedCardIDs;
    [Tooltip("Bu stage'de toplam kaç kart olacağı")]
    public int totalCardsInStage = 9; // Minimum 9 kart
    
    [Header("Stage Completion")]
    public bool isCompleted;
    public float timeLimit = -1;
    public int moveLimit = -1;

    [Header("Stage Objectives")]
    [Tooltip("Stage'in tamamlanması için gerekli hedefler")]
    public List<StageObjective> objectives = new List<StageObjective>();

    public void InitializeObjectives()
    {
        if (objectives == null)
        {
            objectives = new List<StageObjective>();
        }

        foreach (var objective in objectives)
        {
            objective.Reset();
        }
    }

    // Toplam kart sayısının geçerli olup olmadığını kontrol et
    public bool ValidateTotalCards()
    {
        // Minimum 9 kart olmalı
        if (totalCardsInStage < 9)
        {
            Debug.LogError($"Stage {stageName}: Total cards must be at least 9!");
            return false;
        }

        // 3'ün katı olmalı
        if (totalCardsInStage % 3 != 0)
        {
            Debug.LogError($"Stage {stageName}: Total cards must be divisible by 3!");
            return false;
        }

        // Hedefler için gereken minimum kart sayısını kontrol et
        int minRequiredCards = CalculateMinimumRequiredCards();
        if (totalCardsInStage < minRequiredCards)
        {
            Debug.LogError($"Stage {stageName}: Total cards ({totalCardsInStage}) is less than required for objectives ({minRequiredCards})!");
            return false;
        }

        return true;
    }

    private int CalculateMinimumRequiredCards()
    {
        if (objectives == null || objectives.Count == 0) return 9;

        int minCards = 0;
        foreach (var objective in objectives)
        {
            switch (objective.type)
            {
                case ObjectiveType.CollectCardAmount:
                case ObjectiveType.CollectSpecificCards:
                    minCards = Mathf.Max(minCards, Mathf.CeilToInt(objective.targetAmount / 3f) * 3);
                    break;
                case ObjectiveType.MatchPairs:
                    minCards = Mathf.Max(minCards, objective.targetAmount * 3);
                    break;
            }
        }
        return Mathf.Max(9, minCards);
    }
}

public enum ObjectiveType
{
    [Tooltip("Belirli ID'li kartları topla")]
    CollectSpecificCards,
    
    [Tooltip("Belirli sayıda herhangi kart topla")]
    CollectCardAmount,
    
    [Tooltip("Belirli sayıda çift eşleştir")]
    MatchPairs,
    
    [Tooltip("Tüm kartları temizle")]
    ClearAllCards
}

[System.Serializable]
public class StageObjective
{
    [Tooltip("Hedefin tipi")]
    public ObjectiveType type;
    
    [Tooltip("Hedef açıklaması")]
    public string description;
    
    [Tooltip("Ulaşılması gereken hedef miktarı")]
    public int targetAmount;
    
    [Tooltip("Şu anki miktar")]
    public int currentAmount;
    
    [Tooltip("Belirli bir kart ID'si gerekiyorsa")]
    public int specificCardID = -1;
    
    [Tooltip("Hedef tamamlandı mı?")]
    public bool isCompleted;

    public void Reset()
    {
        currentAmount = 0;
        isCompleted = false;
    }

    public float GetProgress()
    {
        return Mathf.Clamp01((float)currentAmount / targetAmount);
    }

    public bool CheckCompletion()
    {
        isCompleted = currentAmount >= targetAmount;
        return isCompleted;
    }
}