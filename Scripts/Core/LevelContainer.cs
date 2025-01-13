using UnityEngine;
using System.Collections.Generic;

public class LevelContainer : MonoBehaviour
{
    #region Singleton
    public static LevelContainer Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [SerializeField] private List<LevelDefinition> levels;
    #endregion

    #region Unity Lifecycle
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
    #endregion

    #region Level Access Methods
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
    #endregion

    #region Editor Utility Methods
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
    #endregion
}
