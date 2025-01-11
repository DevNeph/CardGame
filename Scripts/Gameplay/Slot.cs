using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [Header("Slot Info")]
    public bool isOccupied = false; 
    public Card occupantCard; // Hangi kart bu slota yerleşti
    public GameManager gameManager;

    /// <summary>
    /// Tıklama senaryosunda, kart bu fonksiyonla slota "taşınır."
    /// </summary>
    public void PlaceCard(Card card)
    {
        if (isOccupied)
        {
            Debug.LogWarning($"Slot {gameObject.name} is already occupied!");
            return;
        }

        isOccupied = true;
        occupantCard = card;

        // Kartın parent'ını bu slot yap, global pozisyonu korumaya gerek yok
        card.transform.SetParent(this.transform, false);

        // Kartı slotun merkezine yerleştir
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.identity;
        // Kartı görünür kıl (gerekirse)
        card.RevealCard();

        // Render sırasını kartın ön plana çıkması için ayarla
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "Default"; // uygun layer adı
            spriteRenderer.sortingOrder = 9;           // öne çıkması için yüksek bir değer
        }

        Debug.Log($"Placed card (ID={card.cardID}) into slot {gameObject.name}");
    }



    /// <summary>
    /// Slotu boşaltma (kart silinince veya başka yolla).
    /// </summary>
    public void ClearSlot()
    {
        occupantCard = null;
        isOccupied = false;
    }
}
