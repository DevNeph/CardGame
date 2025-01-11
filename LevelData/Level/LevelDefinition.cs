using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MyGame/Level Definition", fileName = "NewLevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    public string levelName;
    public int levelNumber;

    [Header("Which layout?")]
    public LayoutData layoutData;

    [Header("Which card IDs are used?")]
    public List<int> allowedCardIDs;

    [Header("Total Card Count for this level")]
    public int totalCards;

    [Header("Cards Required to Complete the Level")]
    public int cardsToCollect;

}
