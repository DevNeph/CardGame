using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class LayoutEditorWindow : EditorWindow
{
    #region Fields
    private LayoutData selectedLayout;
    private int currentLayerIndex = 0;
    private const int maxPositionsPerLayer = 10; // Örnek pozisyon limiti
    #endregion

    #region Menu & Initialization
    [MenuItem("Window/Layout Editor")]
    public static void ShowWindow()
    {
        GetWindow<LayoutEditorWindow>("Layout Editor");
    }
    #endregion

    #region GUI Drawing
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Layout Düzenleyici", EditorStyles.boldLabel);
        
        // LayoutData seçimi
        selectedLayout = (LayoutData)EditorGUILayout.ObjectField("Layout Data", selectedLayout, typeof(LayoutData), false);

        if (selectedLayout != null)
        {
            if (selectedLayout.layers == null)
                selectedLayout.layers = new List<LayerConfiguration>();

            DrawLayerNavigation();
            DrawLayoutEditor();
        }
        else
        {
            EditorGUILayout.HelpBox("Düzenlemek için bir LayoutData seçin veya oluşturun.", MessageType.Info);
        }
    }

    private void DrawLayerNavigation()
    {
        if (selectedLayout.layers.Count == 0) return;

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

    private void DrawLayoutEditor()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Katmanlar ve Pozisyonlar", EditorStyles.boldLabel);

        // Seçili katman düzenleme
        if (selectedLayout.layers.Count > 0)
        {
            var layer = selectedLayout.layers[currentLayerIndex];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Katman {currentLayerIndex + 1}: {layer.layerName}", EditorStyles.boldLabel);
            layer.layerName = EditorGUILayout.TextField("Katman Adı", layer.layerName);
            layer.zIndex = EditorGUILayout.FloatField("Z Index", layer.zIndex);

            // Alt/üst katmanların iz düşümlerini çizme
            DrawLayerSilhouettes();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Pozisyonlar ({layer.positions?.Count ?? 0})", EditorStyles.boldLabel);

            if (layer.positions == null) layer.positions = new List<CardPosition>();

            // Her pozisyonu düzenleme
            for (int j = 0; j < layer.positions.Count; j++)
            {
                var pos = layer.positions[j];
                EditorGUILayout.BeginHorizontal();
                pos.x = EditorGUILayout.FloatField("X", pos.x);
                pos.y = EditorGUILayout.FloatField("Y", pos.y);
                if (GUILayout.Button("Sil", GUILayout.Width(50)))
                {
                    layer.positions.RemoveAt(j);
                    EditorUtility.SetDirty(selectedLayout);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            // Pozisyon limiti kontrolü
            if (layer.positions.Count >= maxPositionsPerLayer)
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
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
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
        }
    }

    private void DrawLayerSilhouettes()
    {
        if (selectedLayout == null || selectedLayout.layers == null) return;
        // Mevcut katmanın üstündeki ve altındaki katmanları yarı şeffaf gösterme
        int[] adjacentLayers = { currentLayerIndex - 1, currentLayerIndex + 1 };

        foreach (int layerIndex in adjacentLayers)
        {
            if (layerIndex >= 0 && layerIndex < selectedLayout.layers.Count)
            {
                var layer = selectedLayout.layers[layerIndex];
                Color silhouetteColor = new Color(0f, 0f, 1f, 0.3f); // Mavi yarı saydam

                // İz düşümü olarak her pozisyonu basitçe dikdörtgen ile çiz
                foreach (var pos in layer.positions)
                {
                    Rect posRect = new Rect(pos.x, pos.y, 20, 20);
                    EditorGUI.DrawRect(posRect, silhouetteColor);
                }
            }
        }
    }
    #endregion
}
