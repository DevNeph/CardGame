using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    #region Inspector Fields
    [Header("Card Prefab")]
    [SerializeField] 
    private GameObject cardPrefab;

    [Header("Spawn Area (World Space)")]
    [SerializeField] 
    private float xMin = -5f;
    [SerializeField] 
    private float xMax = 5f;
    [SerializeField] 
    private float yMin = -3f;
    [SerializeField] 
    private float yMax = 3f;
    #endregion

    #region Public Methods
    public void SpawnCard()
    {
        if (cardPrefab != null)
        {
            float randomX = Random.Range(xMin, xMax);
            float randomY = Random.Range(yMin, yMax);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0f);

            Instantiate(cardPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Card Prefab is not assigned in the inspector!");
        }
    }
    #endregion
}
