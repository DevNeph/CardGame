using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class UnifiedLayoutEditorWindow : EditorWindow
{
    #region Fields
    private LayoutData selectedLayout;
    private LayoutData previousLayout;
    private int currentLayerIndex = 0;
    private const int markerSize = 10;
    private Dictionary<Vector2, GameObject> activePredefinedSpawns = new Dictionary<Vector2, GameObject>();

    private bool showOnlyCurrentLayer = false;

    [SerializeField] private GameObject cardPrefab;

    private List<GameObject> spawnedCards = new List<GameObject>();
    #endregion

    #region Menu & Initialization
    [MenuItem("Window/Unified Layout Editor")]
    public static void ShowWindow()
    {
        GetWindow<UnifiedLayoutEditorWindow>("Layout Editor");
    }
    #endregion

    #region GUI Drawing

    private Texture2D CreateColorTexture(Color color)
    {
        // 1x1 boyutunda bir Texture2D oluştur
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color); // Texture'in ilk pikseline renk ayarla
        texture.Apply(); // Texture değişikliğini uygula
        return texture;
    }


    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unified Layout Editor", EditorStyles.boldLabel);

        selectedLayout = (LayoutData)EditorGUILayout.ObjectField("Layout Data", selectedLayout, typeof(LayoutData), false);
        cardPrefab = (GameObject)EditorGUILayout.ObjectField("Card Prefab", cardPrefab, typeof(GameObject), false);

        if (selectedLayout == null)
        {
            EditorGUILayout.HelpBox("Bir LayoutData seçin.", MessageType.Info);
            return;
        }

        // Layout değiştiğinde veya ilk seçimde spawn işlemi yap
        if (selectedLayout != previousLayout)
        {
            ClearSpawnedCards();
            SpawnCardsForThreeLayers();
            previousLayout = selectedLayout;
        }

        if (selectedLayout.layers == null || selectedLayout.layers.Count == 0)
        {
            EditorGUILayout.HelpBox("Seçilen LayoutData'da katman yok.", MessageType.Warning);
            return;
        }

        DrawLayerNavigation();
        DrawPredefinedPositionButtons();
        DrawLayerEditor();

        if (GUILayout.Button("Layout'u Kaydet"))
        {
            EditorUtility.SetDirty(selectedLayout);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawLayerNavigation()
    {
        if (selectedLayout.layers.Count == 0) return;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("<", GUILayout.Width(30)))
        {
            currentLayerIndex = Mathf.Max(0, currentLayerIndex - 1);
            ClearSpawnedCards();
            SpawnCardsForThreeLayers();
        }

        EditorGUILayout.LabelField($"Katman {currentLayerIndex + 1}/{selectedLayout.layers.Count}");

        if (GUILayout.Button(">", GUILayout.Width(30)))
        {
            currentLayerIndex = Mathf.Min(selectedLayout.layers.Count - 1, currentLayerIndex + 1);
            ClearSpawnedCards();
            SpawnCardsForThreeLayers();
        }

        EditorGUILayout.EndHorizontal();

        // "Sadece bu katmanı göster" checkbox'ını ekleyelim
        showOnlyCurrentLayer = EditorGUILayout.Toggle("Sadece bu katmanı göster", showOnlyCurrentLayer);
        ClearSpawnedCards();
        SpawnCardsForThreeLayers();
    }


    private void DrawPredefinedPositionButtons()
    {
        EditorGUILayout.LabelField("Önceden Belirlenmiş Pozisyonlar", EditorStyles.boldLabel);
        var layer = selectedLayout.layers[currentLayerIndex];

        List<Vector2> gridPositions = new List<Vector2>
        {
            // İlk satır
            new Vector2(-2.3f, 3.5f), new Vector2(-1.38f, 3.5f), new Vector2(-0.46f, 3.5f),
            new Vector2(0.46f, 3.5f), new Vector2(1.38f, 3.5f), new Vector2(2.3f, 3.5f),

            // İkinci satır
            new Vector2(-2.3f, 2.375f), new Vector2(-1.38f, 2.375f), new Vector2(-0.46f, 2.375f),
            new Vector2(0.46f, 2.375f), new Vector2(1.38f, 2.375f), new Vector2(2.3f, 2.375f),

            // Üçüncü satır
            new Vector2(-2.3f, 1.25f), new Vector2(-1.38f, 1.25f), new Vector2(-0.46f, 1.25f),
            new Vector2(0.46f, 1.25f), new Vector2(1.38f, 1.25f), new Vector2(2.3f, 1.25f),

            // Dördüncü satır
            new Vector2(-2.3f, 0.125f), new Vector2(-1.38f, 0.125f), new Vector2(-0.46f, 0.125f),
            new Vector2(0.46f, 0.125f), new Vector2(1.38f, 0.125f), new Vector2(2.3f, 0.125f),

            // Beşinci satır
            new Vector2(-2.3f, -1.0f), new Vector2(-1.38f, -1.0f), new Vector2(-0.46f, -1.0f),
            new Vector2(0.46f, -1.0f), new Vector2(1.38f, -1.0f), new Vector2(2.3f, -1.0f),

            // Beşinci satır
            new Vector2(-2.3f, -2.125f), new Vector2(-1.38f, -2.125f), new Vector2(-0.46f, -2.125f),
            new Vector2(0.46f, -2.125f), new Vector2(1.38f, -2.125f), new Vector2(2.3f, -2.125f),
        };

        int columns = 6; // Sütun sayısı
        Texture2D blueTexture = CreateColorTexture(Color.blue);
        Texture2D whiteTexture = CreateColorTexture(Color.grey);

        // Koordinatları grid şeklinde çiz
        for (int row = 0; row < gridPositions.Count; row += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= gridPositions.Count) break;

                Vector2 pos = gridPositions[index];
                bool isActive = activePredefinedSpawns.ContainsKey(pos);

                // Buton stilini ayarla
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    normal = { background = isActive ? blueTexture : whiteTexture }
                };

                // Butonu oluştur
                if (GUILayout.Button("", buttonStyle, GUILayout.Width(50), GUILayout.Height(50)))
                {
                    if (isActive)
                    {
                        // Aktif pozisyondaki kartı kaldır
                        GameObject cardToRemove = activePredefinedSpawns[pos];
                        if (cardToRemove != null) DestroyImmediate(cardToRemove);
                        activePredefinedSpawns.Remove(pos);
                        layer.positions.RemoveAll(p => Mathf.Approximately(p.x, pos.x) && Mathf.Approximately(p.y, pos.y));
                    }
                    else
                    {
                        // Yeni pozisyon ekle ve kart spawn et
                        AddPositionToCurrentLayer(pos);
                        if (cardPrefab != null)
                        {
                            Vector3 spawnPosition = new Vector3(pos.x, pos.y, -layer.zIndex * 0.1f);
                            GameObject spawnedCard = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
                            if (spawnedCard != null)
                            {
                                spawnedCard.transform.position = spawnPosition;
                                SpriteRenderer sr = spawnedCard.GetComponent<SpriteRenderer>();
                                if (sr != null)
                                {
                                    Color transparentColor = Color.blue; // Mevcut katman için mavi
                                    transparentColor.a = 0.5f; // %50 opaklık
                                    sr.color = transparentColor;
                                }
                                spawnedCards.Add(spawnedCard);
                                activePredefinedSpawns[pos] = spawnedCard;
                            }
                        }
                    }
                    EditorUtility.SetDirty(selectedLayout);
                    ClearSpawnedCards();
                    SpawnCardsForThreeLayers();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }



    private void DrawLayerEditor()
    {
        var layers = selectedLayout.layers;
        if (layers == null || layers.Count == 0) return;

        var layer = layers[currentLayerIndex];
        if (layer.positions == null) layer.positions = new List<CardPosition>();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Katman {currentLayerIndex + 1}: {layer.layerName}", EditorStyles.boldLabel);
        layer.layerName = EditorGUILayout.TextField("Katman Adı", layer.layerName);
        layer.zIndex = EditorGUILayout.FloatField("Z Index", layer.zIndex);

        DrawLayerSilhouettes();

        EditorGUILayout.LabelField($"Pozisyonlar ({layer.positions.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < layer.positions.Count; i++)
        {
            var pos = layer.positions[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"X: {pos.x} Y: {pos.y}");
            if (GUILayout.Button("Sil", GUILayout.Width(50)))
            {
                layer.positions.RemoveAt(i);
                EditorUtility.SetDirty(selectedLayout);
                ClearSpawnedCards();
                SpawnCardsForThreeLayers();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (layer.positions.Count >= 10)
        {
            EditorGUILayout.HelpBox("Bu katmanda daha fazla pozisyon eklenemez.", MessageType.Warning);
        }
        else
        {
            if (GUILayout.Button("Yeni Pozisyon Ekle"))
            {
                layer.positions.Add(new CardPosition { x = 0, y = 0, isHidden = false });
                EditorUtility.SetDirty(selectedLayout);
            }
        }

        if (GUILayout.Button("Katmanı Sil"))
        {
            selectedLayout.layers.RemoveAt(currentLayerIndex);
            currentLayerIndex = Mathf.Clamp(currentLayerIndex, 0, selectedLayout.layers.Count - 1);
            EditorUtility.SetDirty(selectedLayout);
            ClearSpawnedCards();
            SpawnCardsForThreeLayers();
        }

        if (GUILayout.Button("Yeni Katman Ekle"))
        {
            selectedLayout.layers.Add(new LayerConfiguration
            {
                layerName = "Yeni Katman",
                zIndex = 0f,
                positions = new List<CardPosition>()
            });
            currentLayerIndex = selectedLayout.layers.Count - 1;
            EditorUtility.SetDirty(selectedLayout);
            ClearSpawnedCards();
            SpawnCardsForThreeLayers();
        }
    }
    #endregion

    #region Silhouette Drawing
    private void DrawLayerSilhouettes()
    {
        if (selectedLayout == null || selectedLayout.layers == null) return;
        int[] adjacentLayers = { currentLayerIndex - 1, currentLayerIndex + 1 };

        foreach (int layerIndex in adjacentLayers)
        {
            if (layerIndex >= 0 && layerIndex < selectedLayout.layers.Count)
            {
                var layer = selectedLayout.layers[layerIndex];
                Color silhouetteColor = Color.clear;
                if (layerIndex > currentLayerIndex) silhouetteColor = new Color(1f, 0f, 0f, 0.1f); // kırmızı üst
                else if (layerIndex < currentLayerIndex) silhouetteColor = new Color(1f, 1f, 0f, 0.3f); // sarı alt

                foreach (var pos in layer.positions)
                {
                    Rect posRect = new Rect(pos.x, pos.y, markerSize, markerSize);
                    EditorGUI.DrawRect(posRect, silhouetteColor);
                }
            }
        }
    }
    #endregion

    #region Functionality
    private void AddPositionToCurrentLayer(Vector2 position)
    {
        var layers = selectedLayout.layers;
        if (layers == null || layers.Count == 0) return;
        var currentLayer = layers[currentLayerIndex];
        if (currentLayer.positions == null) currentLayer.positions = new List<CardPosition>();

        if (currentLayer.positions.Count >= 36) // Maksimum kart sayısı 30
        {
            Debug.LogWarning("Bu katmanda daha fazla pozisyon eklenemez.");
            return;
        }

        currentLayer.positions.Add(new CardPosition { x = position.x, y = position.y, isHidden = false });
        EditorUtility.SetDirty(selectedLayout);

        ClearSpawnedCards();
        SpawnCardsForThreeLayers();
    }


    private void ClearSpawnedCards()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null) DestroyImmediate(card);
        }
        spawnedCards.Clear();
    }

    private void SpawnCardsForThreeLayers()
    {
        if (selectedLayout == null || selectedLayout.layers == null || cardPrefab == null)
            return;

        ClearSpawnedCards();

        if (showOnlyCurrentLayer)
        {
            // Sadece mevcut katmanı spawn et
            SpawnLayerIfExists(currentLayerIndex, Color.blue);
        }
        else
        {
            // Mevcut, bir üst ve bir alt katmanı spawn et
            SpawnLayerIfExists(currentLayerIndex, Color.blue); // Mevcut katman
            SpawnLayerIfExists(currentLayerIndex + 1, Color.red); // Üst katman
            SpawnLayerIfExists(currentLayerIndex - 1, Color.yellow); // Alt katman
        }
    }


    private void SpawnLayerIfExists(int layerIndex, Color color)
    {
        if(layerIndex < 0 || layerIndex >= selectedLayout.layers.Count) return;

        var layer = selectedLayout.layers[layerIndex];
        if(layer.positions == null) return;

        foreach(var pos in layer.positions)
        {
            Vector3 spawnPosition = new Vector3(pos.x, pos.y, -layer.zIndex * 1f);
            GameObject spawnedCard = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
            if(spawnedCard != null)
            {
                spawnedCard.transform.position = spawnPosition;
                SpriteRenderer sr = spawnedCard.GetComponent<SpriteRenderer>();
                if(sr != null)
                {
                    Color transparentColor = color;
                    transparentColor.a = 0.7f; // %50 opaklık
                    sr.color = transparentColor;
                }
                spawnedCards.Add(spawnedCard);
            }
        }
    }

    #endregion
}
