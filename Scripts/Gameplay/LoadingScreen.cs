using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text progressText;

    // Yükleme durumunu takip etmek için
    private float currentProgress = 0f;
    public bool IsLoading => isLoading;
    private bool isLoading = false;

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

    public async Task LoadGameContent()
    {
        isLoading = true;
        loadingPanel.SetActive(true);

        // UI Elementlerini Yükle
        UpdateProgress(0.1f, "UI elementleri yükleniyor...");
        await LoadUIElements();

        // Kartları Yükle
        UpdateProgress(0.3f, "Kartlar yükleniyor...");
        await LoadCards();

        // Ses Efektlerini Yükle
        UpdateProgress(0.5f, "Ses efektleri yükleniyor...");
        await LoadAudioAssets();

        // Diğer Game Assets'leri Yükle
        UpdateProgress(0.7f, "Oyun içeriği yükleniyor...");
        await LoadGameAssets();

        // Yükleme Tamamlandı
        UpdateProgress(1f, "Yükleme tamamlandı!");
        await Task.Delay(500); // Kısa bir gösterme süresi

        isLoading = false;
        loadingPanel.SetActive(false);
    }

    private void UpdateProgress(float progress, string status)
    {
        currentProgress = progress;
        progressBar.value = currentProgress;
        progressText.text = $"{(currentProgress * 100):F0}%";
        loadingText.text = status;
    }

    private async Task LoadUIElements()
    {
        // UI Prefabları yükle
        var operation = Resources.LoadAsync("Prefabs/UI");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task LoadCards()
    {
        // Kart prefabları ve sprite'ları yükle
        var operation = Resources.LoadAsync("Prefabs/Cards");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task LoadAudioAssets()
    {
        // Ses dosyalarını yükle
        var operation = Resources.LoadAsync("Audio");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task LoadGameAssets()
    {
        // Diğer oyun içeriklerini yükle
        var operation = Resources.LoadAsync("GameAssets");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }
}