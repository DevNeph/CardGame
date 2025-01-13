using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "MyGame/Layout Data", fileName = "NewLayoutData")]
public class LayoutData : ScriptableObject
{
    [Header("Layout Settings")]
    public string layoutName;

    [Header("Layer Configuration")]
    public List<LayerConfiguration> layers = new List<LayerConfiguration>();
}

[System.Serializable]
public class LayerConfiguration
{
    [Header("Layer Settings")]
    public string layerName;
    public float zIndex;
    
    [Header("Card Positions")]
    [Tooltip("Bu layer'daki kartların x,y pozisyonları")]
    public List<CardPosition> positions = new List<CardPosition>();
}

[System.Serializable]
public class CardPosition
{
    [SerializeField]
    [Tooltip("Kartın x pozisyonu")]
    private float posX;
    
    [SerializeField]
    [Tooltip("Kartın y pozisyonu")]
    private float posY;
    
    [SerializeField]
    [Tooltip("Kart başlangıçta gizli mi?")]
    private bool isCardHidden;

    public float x
    {
        get => posX;
        set => posX = value;
    }

    public float y
    {
        get => posY;
        set => posY = value;
    }

    public bool isHidden
    {
        get => isCardHidden;
        set => isCardHidden = value;
    }

    public override string ToString()
    {
        return $"X: {x}, Y: {y}" + (isHidden ? " (Hidden)" : "");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LayoutData))]
public class LayoutDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LayoutData layoutData = (LayoutData)target;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Layout Settings", EditorStyles.boldLabel);
        layoutData.layoutName = EditorGUILayout.TextField("Layout Name", layoutData.layoutName);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

        if (layoutData.layers == null)
            layoutData.layers = new List<LayerConfiguration>();

        for (int i = 0; i < layoutData.layers.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var layer = layoutData.layers[i];
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Layer {i + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove Layer", GUILayout.Width(100)))
            {
                layoutData.layers.RemoveAt(i);
                EditorUtility.SetDirty(layoutData);
                break;
            }
            EditorGUILayout.EndHorizontal();

            layer.layerName = EditorGUILayout.TextField("Layer Name", layer.layerName);
            layer.zIndex = EditorGUILayout.FloatField("Z Index", layer.zIndex);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Positions ({layer.positions?.Count ?? 0})", EditorStyles.boldLabel);

            if (layer.positions == null)
                layer.positions = new List<CardPosition>();

            for (int j = 0; j < layer.positions.Count; j++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField($"Pos {j + 1}", GUILayout.Width(40));
                
                var pos = layer.positions[j];
                
                // X, Y değerlerini yan yana göster
                EditorGUILayout.LabelField("X:", GUILayout.Width(15));
                float newX = EditorGUILayout.FloatField(pos.x, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Y:", GUILayout.Width(15));
                float newY = EditorGUILayout.FloatField(pos.y, GUILayout.Width(50));
                
                // Hidden özelliği geçici olarak devre dışı
                //EditorGUILayout.LabelField("Hidden:", GUILayout.Width(45));
                //bool newHidden = EditorGUILayout.Toggle(pos.isHidden, GUILayout.Width(20));

                //if (newX != pos.x || newY != pos.y || newHidden != pos.isHidden)
                if (newX != pos.x || newY != pos.y)
                {
                    pos.x = newX;
                    pos.y = newY;
                    //pos.isHidden = newHidden;
                    EditorUtility.SetDirty(layoutData);
                }

                // Silme butonu
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    layer.positions.RemoveAt(j);
                    EditorUtility.SetDirty(layoutData);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Position"))
            {
                layer.positions.Add(new CardPosition());
                EditorUtility.SetDirty(layoutData);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

            if (GUILayout.Button("Add New Layer"))
            {
                layoutData.layers.Add(new LayerConfiguration
                {
                    layerName = $"Layer {layoutData.layers.Count + 1}",
                    // Z değerini tersine çeviriyoruz: Her yeni layer daha arkaya gidecek
                    zIndex = layoutData.layers.Count * -0.1f,
                    positions = new List<CardPosition>()
                });
                EditorUtility.SetDirty(layoutData);
            }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(layoutData);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif