using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [Header("Card Prefab")]
    public GameObject cardPrefab;

    [Header("Spawn Area (World Space)")]
    public float xMin = -5f;
    public float xMax = 5f;
    public float yMin = -3f;
    public float yMax = 3f;

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
}
