using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    #region Fields
    [Header("Slot Info")]
    public bool isOccupied = false; 
    public Card occupantCard; // Hangi kart bu slota yerleşti
    public GameManager gameManager;
    #endregion

    #region Public Methods

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

        // Kartın transform işlemlerini optimize et
        card.transform.SetParent(transform, true);
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.identity;

        // Kartı görünür kıl
        card.RevealCard();

        // Render sırasını ayarla
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 9;
        }
    }

    /// <summary>
    /// Slotu boşaltma (kart silinince veya başka yolla).
    /// </summary>
    public void ClearSlot()
    {
        occupantCard = null;
        isOccupied = false;
    }

    #endregion
}
