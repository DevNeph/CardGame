using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Slots")]
    public Slot[] slots;

    [Header("Removed Cards UI")]
    public TextMeshProUGUI removedCardsText;

    [Header("Level Settings")]
    public LevelDefinition[] levelDefinitions;    
    private int currentLevelIndex = 0;            
    private LevelDefinition currentLevelDefinition; 

    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;

    [Header("Level Failed UI")]
    public GameObject levelFailedPanel;

    private int removedCardCount = 0;
    public Vector3[] slotPositions;
    private bool[] slotOccupied;

    private void Awake()
    {
        Debug.Log("GameManager Awake - Oyun başlatılıyor");
    }

    private void Start()
    {
        removedCardCount = 0;
        slotOccupied = new bool[slotPositions.Length];

        // Seçilen seviyeyi kontrol et ve başlat
        if(LevelSelection.selectedLevel != null)
        {
            StartLevel(LevelSelection.selectedLevel);
        }
        else
        {
            // Eğer hiçbir seviye seçilmediyse, varsayılan olarak ilk seviyeyi ayarla
            if(levelDefinitions != null && levelDefinitions.Length > 0)
            {
                currentLevelIndex = 0;
                currentLevelDefinition = levelDefinitions[currentLevelIndex];
            }
        }

        if(levelCompletePanel != null)
        {
            // levelCompletePanel aktif değilse, aktif hale getirin (gerekirse)
            levelCompletePanel.SetActive(true);

            // Butonu bulun ve onClick eventine metod ekleyin
            Button returnButton = levelCompletePanel.GetComponentInChildren<Button>();
            if(returnButton != null)
            {
                returnButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        UpdateRemovedCardText();
        UpdateAllCardsAppearance();

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false); // Paneli başlangıçta gizli tut
        }
    }

    public void StartLevel(LevelDefinition levelDefinition)
    {
        // Yeni seviye verilerini ayarla
        currentLevelDefinition = levelDefinition;
        removedCardCount = 0;
        UpdateRemovedCardText();
        if(levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        
        // Seviye başlangıcıyla ilgili diğer ayarlar
        Debug.Log("Starting Level: " + levelDefinition.levelName);
        // levelDefinition.layoutData vb. kullanarak seviyeyi yapılandırın.
    }

    private void Update()
    {
    }

    public static bool IsPopupActive = false; // Popup kontrol değişkeni

    public void OnCardClicked(Card clickedCard)
    {
        // Eğer popup aktifse, tıklamayı engelle
        if (IsPopupActive)
        {
            Debug.Log("Popup aktif, tıklama engellendi.");
            return;
        }

        int slotIndex = FindFirstEmptySlotIndex();
        if (slotIndex == -1)
        {
            Debug.Log("Boş slot kalmadı!");
            ShowLevelFailedPanel();
            return;
        }

        Slot freeSlot = slots[slotIndex];
        freeSlot.transform.position = slotPositions[slotIndex];
        slotOccupied[slotIndex] = true;
        freeSlot.PlaceCard(clickedCard);

        // Eşleşme kontrolü yap
        bool matchFound = CheckAndRemoveMatches(clickedCard.cardID);

        if (slotIndex == slots.Length - 1 && !matchFound)
        {
            ShowLevelFailedPanel();
            return;
        }

        StartCoroutine(DelayedCheckAndRemove(clickedCard.cardID, 0.0f));

        clickedCard.PlaceInSlot();
        UpdateAllCardsAppearance();
    }

    public void UpdateAllCardsAppearance()
    {
        Card[] allCards = Object.FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card card in allCards)
        {
            card.UpdateCardAppearance();
        }
    }

    public void AddRemovedCard(int count)
    {
        removedCardCount += count;
        Debug.Log("Removed card count updated to: " + removedCardCount);
        UpdateRemovedCardText();
        CheckLevelCompletion();
    }

    private int GetLevelNumber(LevelDefinition levelDefinition)
    {
        return levelDefinition.levelNumber;
    }

    private void CheckLevelCompletion()
    {
        if(currentLevelDefinition != null)
        {
            int requiredCardCount = currentLevelDefinition.cardsToCollect;
            if(removedCardCount >= requiredCardCount)
            {
                LevelComplete();
            }
        }
    }

    private void UpdateRemovedCardText()
    {
        if (removedCardsText != null)
        {
            removedCardsText.text = removedCardCount.ToString() + " / " + currentLevelDefinition.cardsToCollect.ToString();
        }
    }

    private int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < slotOccupied.Length; i++)
        {
            if (!slotOccupied[i])
                return i;
        }
        return -1;
    }

    private Slot FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].isOccupied)
            {
                return slots[i];
            }
        }
        return null; // yoksa null döner
    }

    /// <summary>
    /// Tıklanan kartın ID'sine göre
    /// aynı ID'den 3 tane varsa, hepsini yok eden fonksiyon.
    /// </summary>
    private bool CheckAndRemoveMatches(int cardID)
    {
        // 1) Bu ID'ye sahip slotları say
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isOccupied && slots[i].occupantCard != null)
            {
                if (slots[i].occupantCard.cardID == cardID)
                {
                    count++;
                }
            }
        }

        // 2) 3 veya daha fazla eşleşme varsa, onları yok et
        if (count >= 3)
        {
            Debug.Log($"ID={cardID} karttan {count} tane bulundu. Hepsini yok ediyoruz!");
            int destroyed = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].isOccupied && slots[i].occupantCard != null && slots[i].occupantCard.cardID == cardID)
                {
                    Destroy(slots[i].occupantCard.gameObject);
                    slots[i].ClearSlot();
                    slotOccupied[i] = false;
                    destroyed++;
                }
            }

            // Kaç kart yok ettik -> removedCardCount'u güncelle
            AddRemovedCard(destroyed);
            ShiftCardsLeft(); // Kartları sola kaydır
            return true; // Eşleşme bulundu ve silindi
        }

        return false; // Eşleşme bulunamadı
    }

    private IEnumerator DelayedCheckAndRemove(int cardID, float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckAndRemoveMatches(cardID);
    }

    private void ShiftCardsLeft()
    {
        // Tüm slotları sırayla kontrol et
        for (int i = 0; i < slots.Length; i++)
        {
            // Eğer slot boşsa
            if (!slots[i].isOccupied)
            {
                // Bu boş slot için sağda bir kart arayın
                for (int j = i + 1; j < slots.Length; j++)
                {
                    if (slots[j].isOccupied)
                    {
                        // Slot j'deki kartı slot i'ye taşı
                        Card movingCard = slots[j].occupantCard;

                        // Slot i boş olduğu için kartı buraya yerleştir
                        slots[i].PlaceCard(movingCard);
                        slotOccupied[i] = true;

                        // Eski slotu temizle
                        slots[j].ClearSlot();
                        slotOccupied[j] = false;

                        // Yeni konumu ayarla: slot i'nin koordinatlarını kullan
                        movingCard.transform.position = slotPositions[i];

                        // Bulduğumuz kartı yerleştirdik, bir sonraki boş slotu aramaya geç
                        break;
                    }
                }
            }
        }
    }

    private void ShowLevelFailedPanel()
    {
        IsPopupActive = true;

        if (levelFailedPanel != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                GameObject panelInstance = Instantiate(levelFailedPanel, canvas.transform);

                // Butonu ayarla
                Button button = panelInstance.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        IsPopupActive = false; // Popup kapandığında tıklamaları geri aç
                        ReturnToMainMenu();
                    });
                }

                panelInstance.SetActive(true);
            }
        }
    }

    private void LevelComplete()
    {
        IsPopupActive = true;

        if (levelCompletePanel != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                GameObject panelInstance = Instantiate(levelCompletePanel, canvas.transform);

                // Butonu ayarla
                Button button = panelInstance.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        IsPopupActive = false; // Popup kapandığında tıklamaları geri aç
                        ReturnToMainMenu();
                    });
                }

                panelInstance.SetActive(true);
            }
        }

        // Oyuncu ilerlemesini kaydetme
        int currentLevelNumber = GetLevelNumber(currentLevelDefinition);
        int highestCompleted = PlayerPrefs.GetInt("HighestCompletedLevel", 0);
        if (currentLevelNumber > highestCompleted)
        {
            PlayerPrefs.SetInt("HighestCompletedLevel", currentLevelNumber);
            PlayerPrefs.Save();
        }
    }

    public void ReturnToMainMenu()
    {
        // Ana menü sahnesine geçiş yap
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}