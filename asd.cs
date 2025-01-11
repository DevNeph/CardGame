using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PatternSandbox : MonoBehaviour

{

    [System.Serializable] // Bu önemli, Unity inspector'da görünmesi için
    public class CardPlacement
    {
        public GameObject cardObject;
        public Vector3 position;
        public int layer;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer outlineRenderer;
    }
    
    public GameObject cardPrefab;
    public float gridSize = 1f;
    public int currentLayer = 1;
    public bool isEditing = true; // Düzenleme modu açık/kapalı
    
    [Header("Layer Visuals")]
    public Color[] layerColors = new Color[] 
    {
        new Color(0.3f, 0.3f, 1f, 1f),    // Layer 1
        new Color(0.3f, 1f, 0.3f, 1f),    // Layer 2
        new Color(1f, 0.3f, 0.3f, 1f)     // Layer 3
    };

    public float layerDepthOffset = 0.1f;
    public float outlineWidth = 0.1f;
    public List<CardPlacement> placedCards = new List<CardPlacement>();

    private Camera mainCamera;
    private Vector3 lastMousePosition;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isEditing) return;

        // Mouse pozisyonunu grid'e snap et
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 snappedPos = SnapToGrid(mousePos);
        
        // Sol tık ile kart yerleştir
        if (Input.GetMouseButtonDown(0))
        {
            PlaceCard(snappedPos);
        }
        
        // Sağ tık ile kart sil
        if (Input.GetMouseButtonDown(1))
        {
            RemoveCard(snappedPos);
        }

        // Scroll wheel ile layer değiştir
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentLayer = Mathf.Clamp(currentLayer + (scroll > 0 ? 1 : -1), 1, 3);
        }

        // S tuşu ile pattern'i kaydet
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
        {
            SavePattern();
        }

        // L tuşu ile pattern'i yükle
        if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftControl))
        {
            LoadPattern();
        }
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x * 2) / 2;
        float y = Mathf.Round(position.y * 2) / 2;
        return new Vector3(x, y, -currentLayer * layerDepthOffset);
    }

    public void PlaceCard(Vector3 position)
    {
        // Aynı pozisyonda kart var mı kontrol et
        if (placedCards.Exists(card => Vector3.Distance(card.position, position) < 0.1f))
            return;

        GameObject card = Instantiate(cardPrefab, position, Quaternion.identity);
        card.transform.SetParent(transform);

        var placement = new CardPlacement
        {
            cardObject = card,
            position = position,
            layer = currentLayer
        };

        placedCards.Add(placement);
        UpdateCardVisuals(placement);
    }

    public void RemoveCard(Vector3 position)
    {
        var card = placedCards.Find(c => Vector3.Distance(c.position, position) < 0.25f);
        if (card != null)
        {
            Destroy(card.cardObject);
            placedCards.Remove(card);
        }
    }

    public void SavePattern()
    {
        #if UNITY_EDITOR
        var pattern = ScriptableObject.CreateInstance<LayoutData>();
        pattern.positions = new List<LayoutPosition>();

        foreach (var card in placedCards)
        {
            pattern.positions.Add(new LayoutPosition
            {
                position = card.position,
                rotation = 0,
                layer = card.layer,
                isHidden = false
            });
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Pattern",
            "NewPattern",
            "asset",
            "Save pattern as asset"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(pattern, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Pattern saved to: " + path);
        }
        #endif
    }

    public void LoadPattern()
    {
        #if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel(
            "Load Pattern",
            "Assets",
            "asset"
        );

        if (!string.IsNullOrEmpty(path))
        {
            // Unity proje path'ine çevir
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var pattern = AssetDatabase.LoadAssetAtPath<LayoutData>(path);
            
            if (pattern != null)
            {
                // Mevcut kartları temizle
                foreach (var card in placedCards)
                {
                    if (card.cardObject != null)
                        Destroy(card.cardObject);
                }
                placedCards.Clear();

                // Pattern'i yükle
                foreach (var pos in pattern.positions)
                {
                    currentLayer = pos.layer;
                    PlaceCard(pos.position);
                }
                Debug.Log("Pattern loaded from: " + path);
            }
        }
        #endif
    }
    public void UpdateCardVisuals(CardPlacement card)
    {
        if (card.cardObject != null)
        {
            // Ana sprite için renk ve z-pozisyonu ayarla
            if (card.spriteRenderer == null)
                card.spriteRenderer = card.cardObject.GetComponent<SpriteRenderer>();

            if (card.spriteRenderer != null)
            {
                Color layerColor = layerColors[card.layer - 1];
                card.spriteRenderer.color = layerColor;
                
                // Z pozisyonunu layer'a göre ayarla
                Vector3 pos = card.position;
                pos.z = -card.layer * layerDepthOffset;
                card.cardObject.transform.position = pos;
            }

            // Çerçeve için outline sprite oluştur veya güncelle
            if (card.outlineRenderer == null)
            {
                GameObject outlineObj = new GameObject("Outline");
                outlineObj.transform.SetParent(card.cardObject.transform);
                outlineObj.transform.localPosition = Vector3.zero;
                outlineObj.transform.localScale = Vector3.one * (1 + outlineWidth);
                
                card.outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
                card.outlineRenderer.sprite = card.spriteRenderer.sprite;
                card.outlineRenderer.sortingOrder = card.spriteRenderer.sortingOrder - 1;
            }

            // Outline rengini ayarla
            Color outlineColor = layerColors[card.layer - 1];
            outlineColor.a = 0.5f; // Yarı saydam
            card.outlineRenderer.color = outlineColor;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PatternSandbox))]
public class PatternSandboxEditor : Editor
{
    private PatternSandbox sandbox;
    private Tool lastTool;

    private void OnEnable()
    {
        sandbox = target as PatternSandbox;
        lastTool = Tools.current;
        Tools.current = Tool.None;
    }

    private void OnDisable()
    {
        Tools.current = lastTool;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Clear All Cards"))
        {
            ClearAllCards();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Patterns", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Pyramid Pattern"))
        {
            CreatePyramidPattern();
        }

        if (GUILayout.Button("Create Cross Pattern"))
        {
            CreateCrossPattern();
        }

        if (GUILayout.Button("Save Pattern"))
        {
            SaveCurrentPattern();
        }
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Snap to grid
            Vector3 snapPos = SnapToGrid(hit.point);
            
            // Preview card placement
            Handles.color = sandbox.layerColors[sandbox.currentLayer - 1];
            Handles.DrawWireCube(snapPos, Vector3.one * sandbox.gridSize);

            // Left click to place card
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                PlaceCard(snapPos);
                e.Use();
            }

            // Preview için gizmo rengini current layer'a göre ayarla
            Handles.color = sandbox.layerColors[sandbox.currentLayer - 1];
            
            // Right click to remove card
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                RemoveCard(snapPos);
                e.Use();
            }
        }

        // Layer control with scroll wheel
        if (e.type == EventType.ScrollWheel)
        {
            sandbox.currentLayer = Mathf.Clamp(sandbox.currentLayer + (e.delta.y > 0 ? -1 : 1), 1, 3);
            e.Use();
            Repaint();
        }

        SceneView.RepaintAll();
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x * 2) / 2;
        float y = Mathf.Round(position.y * 2) / 2;
        return new Vector3(x, y, -sandbox.currentLayer * 0.1f);
    }

    private void PlaceCard(Vector3 position)
    {
        GameObject card = PrefabUtility.InstantiatePrefab(sandbox.cardPrefab) as GameObject;
        card.transform.position = position;
        card.transform.SetParent(sandbox.transform);

        var placement = new PatternSandbox.CardPlacement
        {
            cardObject = card,
            position = position,
            layer = sandbox.currentLayer
        };

        sandbox.placedCards.Add(placement);
        sandbox.UpdateCardVisuals(placement);

        EditorUtility.SetDirty(sandbox);
    }


    private void RemoveCard(Vector3 position)
    {
        var card = sandbox.placedCards.Find(c => Vector3.Distance(c.position, position) < 0.25f);
        if (card != null)
        {
            DestroyImmediate(card.cardObject);
            sandbox.placedCards.Remove(card);
            EditorUtility.SetDirty(sandbox);
        }
    }

    private void ClearAllCards()
    {
        foreach (var card in sandbox.placedCards)
        {
            if (card.cardObject != null)
                DestroyImmediate(card.cardObject);
        }
        sandbox.placedCards.Clear();
        EditorUtility.SetDirty(sandbox);
    }

    private void CreatePyramidPattern()
    {
        // Örnek piramit pattern
        Vector2[] layer3 = new Vector2[] { new Vector2(0, 2) };
        Vector2[] layer2 = new Vector2[] { new Vector2(-0.5f, 1), new Vector2(0.5f, 1) };
        Vector2[] layer1 = new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0) };

        ClearAllCards();
        CreatePatternLayer(layer3, 3);
        CreatePatternLayer(layer2, 2);
        CreatePatternLayer(layer1, 1);
    }

    private void CreateCrossPattern()
    {
        // Örnek cross pattern
        Vector2[] layer3 = new Vector2[] { new Vector2(0, 0) };
        Vector2[] layer2 = new Vector2[] { 
            new Vector2(-0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 0.5f), new Vector2(0, -0.5f)
        };

        ClearAllCards();
        CreatePatternLayer(layer3, 3);
        CreatePatternLayer(layer2, 2);
    }

    private void CreatePatternLayer(Vector2[] positions, int layer)
    {
        sandbox.currentLayer = layer;
        foreach (Vector2 pos in positions)
        {
            PlaceCard(new Vector3(pos.x, pos.y, -layer * 0.1f));
        }
    }

    private void SaveCurrentPattern()
    {
        // Pattern'ı ScriptableObject olarak kaydet
        var pattern = CreateInstance<LayoutData>();
        pattern.positions = new List<LayoutPosition>();

        foreach (var card in sandbox.placedCards)
        {
            pattern.positions.Add(new LayoutPosition
            {
                position = card.position,
                rotation = 0,
                layer = card.layer,
                isHidden = false
            });
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Pattern",
            "NewPattern",
            "asset",
            "Save pattern as asset"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(pattern, path);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif