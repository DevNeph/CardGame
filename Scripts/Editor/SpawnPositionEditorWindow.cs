using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SpawnPositionEditorWindow : EditorWindow
{
    #region Fields
    private LayoutData selectedLayout;
    private int currentLayerIndex = 0;
    private const int markerSize = 10;
    private Color markerColor = Color.red;
    [SerializeField] private GameObject cardPrefab; // Spawn etmek için prefab
    #endregion

    #region Menu & Initialization
    [MenuItem("Window/Spawn Position Editor")]
    public static void ShowWindow()
    {
        GetWindow<SpawnPositionEditorWindow>("Spawn Position Editor");
    }
    #endregion

    #region GUI Drawing
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Spawn Pozisyon Editörü", EditorStyles.boldLabel);
        
        // LayoutData seçimi
        selectedLayout = (LayoutData)EditorGUILayout.ObjectField("Layout Data", selectedLayout, typeof(LayoutData), false);
        
        // Prefab seçimi
        cardPrefab = (GameObject)EditorGUILayout.ObjectField("Card Prefab", cardPrefab, typeof(GameObject), false);

        if (selectedLayout == null)
        {
            EditorGUILayout.HelpBox("Lütfen düzenlenecek bir LayoutData seçin.", MessageType.Info);
            return;
        }

        if (selectedLayout.layers == null || selectedLayout.layers.Count == 0)
        {
            EditorGUILayout.HelpBox("Seçilen LayoutData'da katman yok.", MessageType.Warning);
            return;
        }

        // Katman navigasyonu
        DrawLayerNavigation();

        // Seçili katmandaki pozisyonları çiz ve spawn etme butonları ekle
        DrawSpawnPositions();
    }

    private void DrawLayerNavigation()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(30)))
        {
            currentLayerIndex = Mathf.Max(0, currentLayerIndex - 1);
        }
        
        EditorGUILayout.LabelField($"Katman {currentLayerIndex + 1}/{selectedLayout.layers.Count}");
        
        if (GUILayout.Button(">", GUILayout.Width(30)))
        {
            currentLayerIndex = Mathf.Min(selectedLayout.layers.Count - 1, currentLayerIndex + 1);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpawnPositions()
    {
        var layer = selectedLayout.layers[currentLayerIndex];
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Katman {currentLayerIndex + 1}: {layer.layerName}", EditorStyles.boldLabel);

        // Her pozisyon için bir "Spawn Et" butonu ekleyin
        for (int i = 0; i < layer.positions.Count; i++)
        {
            var pos = layer.positions[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Pozisyon {i + 1}: X={pos.x}, Y={pos.y}");

            if (GUILayout.Button("Bu Pozisyonda Spawn Et", GUILayout.Width(150)))
            {
                SpawnCardAtPosition(pos, layer.zIndex);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    #endregion

    #region Spawn Logic
    private void SpawnCardAtPosition(CardPosition pos, float zIndex)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("Card Prefab atanmadı!");
            return;
        }

        // Katmana göre derinlik ayarla
        Vector3 spawnPosition = new Vector3(pos.x, pos.y, -zIndex * 0.1f);

        // Prefabı instantiate et
        GameObject spawnedCard = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
        if (spawnedCard != null)
        {
            spawnedCard.transform.position = spawnPosition;
            // İsteğe bağlı: kartı görünür yap, renk ayarla vb.
        }
    }

    #endregion
}
