using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MyGame/Layout Data", fileName = "NewLayoutData")]
public class LayoutData : ScriptableObject
{
    [Header("Layout Name")]
    public string layoutName;
    
    [Header("Positions")]
    public List<LayoutPosition> positions;

    
}

[System.Serializable]
public class LayoutPosition
{
    // Bu pozisyonda bir kart olacak, layer (katman) bilgisi vb. var.
    public Vector3 position;   // X,Y,Z konum
    public float rotation;     // Eğer döndürmek istersen
    public int layer;          // Mahjong stilinde üst üste dizilim
    public bool isHidden;      // Başlangıçta gizli/sürpriz mi?

    // İstersek "width, height" gibi ek değerler de tutabiliriz 
    // ya da "uniqueID" koyabiliriz.
}
