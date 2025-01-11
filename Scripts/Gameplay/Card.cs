using UnityEngine;
using System.Collections.Generic;

public class Card : MonoBehaviour
{
    public int cardID;
    public int uniqueID;
    private bool isHidden;
    private SpriteRenderer spriteRenderer;
    private bool isInSlot = false;
    private GameManager gm;
    private BoxCollider2D boxCollider;
    
    // Stores all cards above this card based on sorting order + overlap
    private HashSet<Card> cardsAbove = new HashSet<Card>();

    private void Awake()
    {
        // Get existing components or add them if missing
        spriteRenderer = GetComponent<SpriteRenderer>();
        gm = Object.FindFirstObjectByType<GameManager>();
        boxCollider = GetComponent<BoxCollider2D>() ?? gameObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void Update()
    {
        // Each frame, recalculate which cards are above
        CheckCardsAbove();
    }

    /// <summary>
    /// Checks for overlapping cards with a higher sorting order, then updates appearance.
    /// </summary>
    private void CheckCardsAbove()
    {
        cardsAbove.Clear();

        // Check all overlap in this card's collider area
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(transform.position, boxCollider.size, 0f);

        foreach (Collider2D hit in overlaps)
        {
            if (hit.gameObject == gameObject) 
                continue;

            Card otherCard = hit.GetComponent<Card>();
            if (otherCard != null)
            {
                // If the other card's sorting order is higher, consider it "above"
                if (otherCard.GetSortingOrder() > GetSortingOrder())
                {
                    cardsAbove.Add(otherCard);
                }
            }
        }

        // Update this card’s appearance based on whether it has cards above
        UpdateCardAppearance();
    }

    /// <summary>
    /// Sets up initial card data.
    /// </summary>
    public void SetupCard(int id, Sprite sprite, bool hidden)
    {
        cardID = id;
        isHidden = hidden;

        if (spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer is null on Card with ID = {id}!");
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = isHidden ? new Color(1, 1, 1, 0f) : Color.white;

        // Resize the box collider to match the sprite bounds
        if (boxCollider != null && sprite != null)
        {
            boxCollider.size = sprite.bounds.size;
        }
    }

    private void OnMouseDown()
    {
        // Only allow clicks if there are no cards above and the card is not in a slot
        if (CanBeClicked())
        {
            Debug.Log($"Card {uniqueID} clicked successfully!");
            gm?.OnCardClicked(this);
        }
        else
        {
            Debug.Log($"[Blocked Click] Card {uniqueID} is in slot or has cards above.");
        }
    }

    /// <summary>
    /// Checks if this card can be clicked.
    /// </summary>
    private bool CanBeClicked()
    {
        // If placed in a slot, do not allow clicking
        if (isInSlot) return false;

        // If there's a popup active, do not allow clicking
        if (GameManager.IsPopupActive) return false;

        // If at least one card is above, do not allow clicking
        if (cardsAbove.Count > 0) return false;

        return true;
    }

    /// <summary>
    /// Marks the card as placed in a slot (commonly used in solitaire-like games).
    /// </summary>
    public void PlaceInSlot()
    {
        isInSlot = true;
        UpdateCardAppearance();
    }

    /// <summary>
    /// Reveals the card if it's hidden and has no cards above.
    /// </summary>
    public void RevealCard()
    {
        isHidden = false;
        if (cardsAbove.Count == 0 && !isInSlot)
        {
            var color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
            Debug.Log($"Card {uniqueID} revealed");
        }
    }

    /// <summary>
    /// Updates the card's visual appearance (e.g., partially transparent if blocked).
    /// </summary>
    public void UpdateCardAppearance()
    {
        if (spriteRenderer == null) return;

        Color color = spriteRenderer.color;
        if (isInSlot || cardsAbove.Count > 0)
        {
            color.a = 0.5f; // semi-transparent if blocked or in a slot
        }
        else
        {
            color.a = 1f;   // fully opaque if free
        }
        spriteRenderer.color = color;
    }

    /// <summary>
    /// Retrieves the SpriteRenderer's sorting order for comparisons.
    /// </summary>
    public int GetSortingOrder()
    {
        return spriteRenderer == null ? 0 : spriteRenderer.sortingOrder;
    }

    /// <summary>
    /// Visually debug what’s happening in the Scene view.
    /// Green if free, red if blocked.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;
        Gizmos.color = (cardsAbove.Count == 0) ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, boxCollider.size);
    }
}