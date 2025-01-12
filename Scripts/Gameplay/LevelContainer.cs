using UnityEngine;
using System.Collections.Generic; // List için gerekli

public class LevelContainer : MonoBehaviour
{
    public static LevelContainer Instance { get; private set; }
    
    [SerializeField] private List<LevelDefinition> levels;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public LevelDefinition GetLevel(int levelIndex)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("No levels defined in LevelContainer!");
            return null;
        }

        // Level index 1'den başlıyorsa
        int index = levelIndex - 1;
        
        if (index >= 0 && index < levels.Count)
        {
            return levels[index];
        }
        
        Debug.LogError($"Level index {levelIndex} out of range! Total levels: {levels.Count}");
        return null;
    }

    public int GetTotalLevelCount()
    {
        return levels?.Count ?? 0;
    }

    // Editor'da level eklemeyi kolaylaştırmak için
    public void AddLevel(LevelDefinition level)
    {
        if (levels == null)
        {
            levels = new List<LevelDefinition>();
        }

        if (level != null)
        {
            levels.Add(level);
            Debug.Log($"Added level: {level.levelName}");
        }
    }

    // Editor'da level silmeyi kolaylaştırmak için
    public void RemoveLevel(LevelDefinition level)
    {
        if (levels != null && level != null)
        {
            levels.Remove(level);
            Debug.Log($"Removed level: {level.levelName}");
        }
    }
}