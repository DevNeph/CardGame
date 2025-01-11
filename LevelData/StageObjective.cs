using UnityEngine;

public enum ObjectiveType
{
    CollectSpecificCards,    // Belirli ID'li kartları topla
    CollectCardAmount,       // Belirli sayıda herhangi kart topla
    ReachScore,             // Belirli bir skora ulaş
    ClearLayer,             // Belirli bir katmanı temizle
    MatchPairs,             // Belirli sayıda çift eşleştir
    ClearAllCards          // Tüm kartları temizle
}

[System.Serializable]
public class StageObjective
{
    public ObjectiveType type;
    public string description;
    public int targetAmount;
    public int currentAmount;
    public int specificCardID = -1; // Eğer belirli bir kart gerekiyorsa
    public int specificLayer = -1;  // Eğer belirli bir katman gerekiyorsa
    public bool isCompleted;

    public float GetProgress()
    {
        return Mathf.Clamp01((float)currentAmount / targetAmount);
    }
}