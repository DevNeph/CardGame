using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class LoadingScreen : MonoBehaviour
{
    #region Singleton & Fields
    public static LoadingScreen Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text progressText;

    private float currentProgress = 0f;
    private bool isLoading = false;
    public bool IsLoading => isLoading;
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

    #region Public Methods
    public async Task LoadGameContent()
    {
        isLoading = true;
        loadingPanel.SetActive(true);

        UpdateProgress(0.1f, "UI elementleri yükleniyor...");
        await LoadUIElements();

        UpdateProgress(0.3f, "Kartlar yükleniyor...");
        await LoadCards();

        UpdateProgress(0.5f, "Ses efektleri yükleniyor...");
        await LoadAudioAssets();

        UpdateProgress(0.7f, "Oyun içeriği yükleniyor...");
        await LoadGameAssets();

        UpdateProgress(1f, "Yükleme tamamlandı!");
        await Task.Delay(500); // Kısa bir gösterme süresi

        isLoading = false;
        loadingPanel.SetActive(false);
    }
    #endregion

    #region Private Methods
    private void UpdateProgress(float progress, string status)
    {
        currentProgress = progress;
        progressBar.value = currentProgress;
        progressText.text = $"{(currentProgress * 100):F0}%";
        loadingText.text = status;
    }

    private async Task LoadUIElements()
    {
        var operation = Resources.LoadAsync("Prefabs/UI");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task LoadCards()
    {
        var operation = Resources.LoadAsync("Prefabs/Cards");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task LoadAudioAssets()
    {
        var operation = Resources.LoadAsync("Audio");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task LoadGameAssets()
    {
        var operation = Resources.LoadAsync("GameAssets");
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }
    #endregion
}
