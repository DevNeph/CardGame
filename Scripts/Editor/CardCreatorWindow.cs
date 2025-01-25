using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CardEditorWindow : EditorWindow
{
    private CardDataList cardDataList;

    private int toolbarIndex = 0;
    private readonly string[] toolbarLabels = { "Create", "Existing" };

    // 4 ana tür: Border, Fill, Trick, Special
    private string[] cardTypeOptions = { "Border", "Fill", "Trick", "Special" };
    private int selectedCardTypeIndex = 0;

    // Trick Border/Fill
    private string[] trickSetOptions = { "Border", "Fill" };
    private int selectedTrickSetIndex = 0;

    // Normal suits
    private string[] suitTypeOptions = { "Triangle Up", "Triangle Down", "Square", "Dot", "Tridot" };
    private int selectedSuitTypeIndex = 0;

    // Normal colors
    private string[] suitColorOptions = { "Red", "Black" };
    private int selectedSuitColorIndex = 0;

    // Trick/Normal rank
    private int rank = 3;

    // Special SubTypes (şimdilik 1: Joker, sonra Moves, Wild vs. eklenebilir)
    private string[] specialSubTypeOptions = { "Joker", "Empty" };
    private int selectedSpecialSubTypeIndex = 0;

    private Sprite selectedSprite;

    // EXISTING TAB foldouts
    private Dictionary<string, bool> setFoldoutStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> suitFoldoutStates = new Dictionary<string, bool>();
    private bool showTrickFoldout = true;
    private Dictionary<int, bool> trickRankFoldouts = new Dictionary<int, bool>();

    // Special foldouts
    private bool showSpecialFoldout = false;
    // Her specialTypeName'a ait foldout durumunu tutabiliriz
    private Dictionary<string, bool> specialTypeFoldouts = new Dictionary<string, bool>();

    [MenuItem("Window/Card Editor (Special)")]
    public static void ShowWindow()
    {
        GetWindow<CardEditorWindow>("Card Editor (Special)");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        cardDataList = (CardDataList)EditorGUILayout.ObjectField("CardDataList", cardDataList, typeof(CardDataList), false);

        if (cardDataList == null)
        {
            EditorGUILayout.HelpBox("Lütfen bir CardDataList seçiniz.", MessageType.Warning);
            return;
        }

        toolbarIndex = GUILayout.Toolbar(toolbarIndex, toolbarLabels);
        switch (toolbarIndex)
        {
            case 0:
                DrawCreateTab();
                break;
            case 1:
                DrawExistingTab();
                break;
        }
    }

    private void DrawCreateTab()
    {
        EditorGUILayout.LabelField("Kart Oluşturma", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 4 ana tür
        selectedCardTypeIndex = EditorGUILayout.Popup("Card Type", selectedCardTypeIndex, cardTypeOptions);
        string cardType = cardTypeOptions[selectedCardTypeIndex];

        if (cardType == "Border" || cardType == "Fill")
        {
            // Normal
            selectedSuitTypeIndex = EditorGUILayout.Popup("Suit Type", selectedSuitTypeIndex, suitTypeOptions);
            selectedSuitColorIndex = EditorGUILayout.Popup("Suit Color", selectedSuitColorIndex, suitColorOptions);
            rank = EditorGUILayout.IntField("Rank (1..6)", rank);
        }
        else if (cardType == "Trick")
        {
            // Trick
            selectedTrickSetIndex = EditorGUILayout.Popup("Trick Set", selectedTrickSetIndex, trickSetOptions);
            selectedSuitTypeIndex = EditorGUILayout.Popup("Card Suit", selectedSuitTypeIndex, suitTypeOptions);
            selectedSuitColorIndex = EditorGUILayout.Popup("Suit Color", selectedSuitColorIndex, suitColorOptions);
            rank = EditorGUILayout.IntField("Trick Rank (3,5,6)", rank);
        }
        else if (cardType == "Special")
        {
            // Special alt tür: Joker, Moves, Wild, vs.
            selectedSpecialSubTypeIndex = EditorGUILayout.Popup("Special SubType", selectedSpecialSubTypeIndex, specialSubTypeOptions);
            // Joker için rank veya color yok diyebilirsiniz, vs.
        }

        selectedSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", selectedSprite, typeof(Sprite), false);

        if (GUILayout.Button("Kart Oluştur"))
        {
            CreateCard();
        }
    }

    private void CreateCard()
    {
        if (selectedSprite == null)
        {
            EditorUtility.DisplayDialog("Uyarı", "Bir sprite seçiniz!", "Tamam");
            return;
        }

        string cardType = cardTypeOptions[selectedCardTypeIndex];
        CardData newCard = new CardData();
        newCard.cardSprite = selectedSprite;

        if (cardType == "Border" || cardType == "Fill")
        {
            // Normal
            if (rank < 1 || rank > 6)
            {
                EditorUtility.DisplayDialog("Hata", "Rank 1..6 olmalı!", "Tamam");
                return;
            }
            newCard.rank = rank;

            string sType = suitTypeOptions[selectedSuitTypeIndex];
            string sColor = suitColorOptions[selectedSuitColorIndex];
            newCard.cardName = $"{cardType}_{sType}_{sColor}_R{rank}";

            int suitIndex = (selectedSuitTypeIndex * 2) + selectedSuitColorIndex;
            newCard.cardID = GenerateNormalID(cardType, suitIndex, rank);

            AddNormalCard(newCard, cardType, sType, sColor);
        }
        else if (cardType == "Trick")
        {
            // Trick
            if (rank != 3 && rank != 5 && rank != 6)
            {
                EditorUtility.DisplayDialog("Hata", "Trick rank 3,5,6 olmalı!", "Tamam");
                return;
            }
            newCard.rank = rank;
            newCard.isTrick = true;

            string trickSet = trickSetOptions[selectedTrickSetIndex];
            string sType = suitTypeOptions[selectedSuitTypeIndex];
            string sColor = suitColorOptions[selectedSuitColorIndex];

            newCard.cardName = $"{trickSet}Trick{rank}{sType.Replace(" ", "")}{sColor}";
            newCard.cardID = GenerateTrickID(trickSet, rank, sType, sColor);

            AddTrickCard(newCard);
        }
        else if (cardType == "Special")
        {
            // Special SubType
            string subType = specialSubTypeOptions[selectedSpecialSubTypeIndex]; // "Joker"
            
            // Joker => rank=0 vs.
            newCard.rank = 0;
            newCard.cardName = $"Special_{subType}";
            newCard.cardID = 5000; // Belki subType'a göre farklı taban ID vb.

            AddSpecialCard(newCard, subType);
        }

        EditorUtility.SetDirty(cardDataList);
        Debug.Log($"Yeni Kart Oluşturuldu: {newCard.cardName} (ID={newCard.cardID})");
    }

    private int GenerateNormalID(string ctype, int suitIndex, int rank)
    {
        int baseID = (ctype == "Border") ? 1000 : 2000;
        return baseID + suitIndex * 10 + rank;
    }
    private int GenerateTrickID(string trickSet, int rank, string sType, string sColor)
    {
        int baseID = (trickSet == "Border") ? 3000 : 4000;
        int suitIndex = (selectedSuitTypeIndex * 2) + selectedSuitColorIndex;
        return baseID + suitIndex * 10 + rank;
    }

    private void AddNormalCard(CardData newCard, string setName, string suitType, string suitColor)
    {
        if (cardDataList.sets == null)
            cardDataList.sets = new List<SetGroup>();

        var setGroup = cardDataList.sets.FirstOrDefault(sg => sg.setName == setName);
        if (setGroup == null)
        {
            setGroup = new SetGroup { setName = setName, suits = new List<SuitGroup>() };
            cardDataList.sets.Add(setGroup);
        }

        string fullSuitName = suitType + " " + suitColor;
        var suitGroup = setGroup.suits.FirstOrDefault(sg => sg.suitName == fullSuitName);
        if (suitGroup == null)
        {
            suitGroup = new SuitGroup
            {
                suitName = fullSuitName,
                ranks = new List<CardData>()
            };
            setGroup.suits.Add(suitGroup);
        }
        suitGroup.ranks.Add(newCard);
    }

    private void AddTrickCard(CardData newCard)
    {
        if (cardDataList.trickGroup == null)
        {
            cardDataList.trickGroup = new TrickGroup
            {
                groupName = "Trick",
                trickRanks = new List<TrickRankGroup>()
            };
        }
        else if (cardDataList.trickGroup.trickRanks == null)
        {
            cardDataList.trickGroup.trickRanks = new List<TrickRankGroup>();
        }

        int r = newCard.rank;
        var trickRankGroup = cardDataList.trickGroup.trickRanks.FirstOrDefault(tg => tg.rank == r);
        if (trickRankGroup == null)
        {
            trickRankGroup = new TrickRankGroup
            {
                rank = r,
                trickVariations = new List<CardData>()
            };
            cardDataList.trickGroup.trickRanks.Add(trickRankGroup);
        }
        trickRankGroup.trickVariations.Add(newCard);
    }

    // ================== NEW: Special Cards ==================
    private void AddSpecialCard(CardData newCard, string subType)
    {
        // ID'yi sırayla artır
        newCard.cardID = NextSpecialID();

        if (cardDataList.specialGroup == null)
        {
            cardDataList.specialGroup = new SpecialGroup
            {
                groupName = "Special",
                specialTypes = new List<SpecialTypeGroup>()
            };
        }

        var stGroup = cardDataList.specialGroup.specialTypes
            .FirstOrDefault(st => st.typeName == subType);
        if (stGroup == null)
        {
            stGroup = new SpecialTypeGroup
            {
                typeName = subType,
                cards = new List<CardData>()
            };
            cardDataList.specialGroup.specialTypes.Add(stGroup);
        }

        stGroup.cards.Add(newCard);
    }


    private int NextSpecialID()
    {
    if (cardDataList.specialGroup == null || 
        cardDataList.specialGroup.specialTypes == null)
    {
        // SpecialGroup hiç yoksa ilk ID 5001 olsun
        return 5001;
    }

    // Tüm special kartları toplayalım
    var allSpecialCards = cardDataList.specialGroup.specialTypes
        .Where(st => st != null && st.cards != null)
        .SelectMany(st => st.cards);

    if (!allSpecialCards.Any())
    {
        // Henüz special kart yoksa ilk ID 5001
        return 5001;
    }

    // Mevcut special kartlar arasından en yüksek ID'yi bul
    // 5000'den küçük bir değer normal/Trick ID olabilir, yine de en fazla ID'yi buluyoruz.
    int maxID = allSpecialCards.Max(c => c.cardID);
    // Bir sonrakini döndür
    return maxID + 1;
    }


    private void DrawExistingTab()
    {
        EditorGUILayout.LabelField("Mevcut Kartlar", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUI.indentLevel++;

        // 1) Normal (Border/Fill)
        if (cardDataList.sets != null)
        {
            foreach (var setGroup in cardDataList.sets)
            {
                if (setGroup == null) continue;
                if (!setFoldoutStates.ContainsKey(setGroup.setName))
                    setFoldoutStates[setGroup.setName] = false;

                setFoldoutStates[setGroup.setName] = EditorGUILayout.Foldout(setFoldoutStates[setGroup.setName], setGroup.setName);
                if (setFoldoutStates[setGroup.setName])
                {
                    EditorGUI.indentLevel++;
                    if (setGroup.suits != null)
                    {
                        foreach (var suitGroup in setGroup.suits)
                        {
                            if (suitGroup == null) continue;
                            string suitKey = setGroup.setName + "_" + suitGroup.suitName;
                            if (!suitFoldoutStates.ContainsKey(suitKey))
                                suitFoldoutStates[suitKey] = false;

                            string suitLabel = $"{suitGroup.suitName} ({suitGroup.ranks?.Count ?? 0} kart)";
                            suitFoldoutStates[suitKey] = EditorGUILayout.Foldout(suitFoldoutStates[suitKey], suitLabel);

                            if (suitFoldoutStates[suitKey])
                            {
                                EditorGUI.indentLevel++;
                                if (suitGroup.ranks != null)
                                {
                                    foreach (var card in suitGroup.ranks)
                                    {
                                        DrawCardPreview(card);
                                    }
                                }
                                EditorGUI.indentLevel--;
                                EditorGUILayout.Space();
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }
        }

        // 2) Trick
        if (cardDataList.trickGroup != null && cardDataList.trickGroup.trickRanks != null)
        {
            showTrickFoldout = EditorGUILayout.Foldout(showTrickFoldout, "Trick");
            if (showTrickFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var tg in cardDataList.trickGroup.trickRanks)
                {
                    if (tg == null) continue;
                    if (!trickRankFoldouts.ContainsKey(tg.rank))
                        trickRankFoldouts[tg.rank] = false;

                    string trickLabel = $"Rank {tg.rank} ({tg.trickVariations?.Count ?? 0} kart)";
                    trickRankFoldouts[tg.rank] = EditorGUILayout.Foldout(trickRankFoldouts[tg.rank], trickLabel);

                    if (trickRankFoldouts[tg.rank])
                    {
                        EditorGUI.indentLevel++;
                        if (tg.trickVariations != null)
                        {
                            foreach (var card in tg.trickVariations)
                            {
                                DrawCardPreview(card);
                            }
                        }
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        // 3) Special
        if (cardDataList.specialGroup != null && cardDataList.specialGroup.specialTypes != null)
        {
            showSpecialFoldout = EditorGUILayout.Foldout(showSpecialFoldout, "Special");
            if (showSpecialFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var stGroup in cardDataList.specialGroup.specialTypes)
                {
                    if (stGroup == null) continue;
                    // Foldout durumu
                    if (!specialTypeFoldouts.ContainsKey(stGroup.typeName))
                        specialTypeFoldouts[stGroup.typeName] = false;

                    string specialLabel = $"{stGroup.typeName} ({stGroup.cards?.Count ?? 0} kart)";
                    specialTypeFoldouts[stGroup.typeName] = EditorGUILayout.Foldout(specialTypeFoldouts[stGroup.typeName], specialLabel);

                    if (specialTypeFoldouts[stGroup.typeName])
                    {
                        EditorGUI.indentLevel++;
                        if (stGroup.cards != null)
                        {
                            foreach (var card in stGroup.cards)
                            {
                                DrawCardPreview(card);
                            }
                        }
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawCardPreview(CardData card)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        if (card.cardSprite != null)
        {
            Texture2D preview = AssetPreview.GetAssetPreview(card.cardSprite);
            GUILayout.Box(preview, GUILayout.Width(50), GUILayout.Height(50));
        }
        else
        {
            GUILayout.Box("No Sprite", GUILayout.Width(50), GUILayout.Height(50));
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"ID: {card.cardID} | Rank: {card.rank}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Name: {card.cardName}");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
}
