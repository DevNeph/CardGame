using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;



public static class LevelSelection
{
    public static LevelDefinition selectedLevel;
}

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentPanel;           // Butonların oluşturulacağı panel
    public GameObject levelButtonPrefab;     // Seviye butonu prefab'ı

    [Header("Level Data")]
    public LevelDefinition[] levelDefinitions; // Tüm seviye tanımlamaları
    public CardDealer cardDealer;

    void Start()
    {
        Debug.Log("Total levels assigned: " + levelDefinitions.Length);
        PopulateLevelButtons();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) // R tuşuna basıldığında reset
        {
            ResetPlayerData();
        }
    }

    void PopulateLevelButtons()
    {
        Debug.Log("Populating level buttons...");

        // Mevcut butonları temizle
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // En yüksek tamamlanan seviye
        int highestCompleted = PlayerPrefs.GetInt("HighestCompletedLevel", 0);

        // Her level için bir buton oluştur
        foreach (var levelDef in levelDefinitions)
        {
            Debug.Log("Creating button for: " + levelDef.levelName);

            GameObject newButton = Instantiate(levelButtonPrefab, contentPanel);

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = levelDef.levelName;
            }
            else
            {
                Debug.LogWarning("TextMeshProUGUI component not found on button prefab");
            }

            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                // Eğer seviye tamamlandıysa butonu devre dışı bırak
                if (levelDef.levelNumber <= highestCompleted)
                {
                    btn.interactable = false;
                }
                else
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() => OnLevelButtonClicked(levelDef));
                }
            }
            else
            {
                Debug.LogWarning("Button component not found on button prefab");
            }
        }
    }

    public void OnLevelButtonClicked(LevelDefinition levelDefinition)
    {
        Debug.Log("Selected Level: " + levelDefinition.levelName);

        // Seçilen leveli static değişkene aktar
        LevelSelection.selectedLevel = levelDefinition;

        // Ana oyun sahnesini yükle
        SceneManager.LoadScene("MainScene");
    }

    public void ResetPlayerData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Player data has been reset.");
    }
}
