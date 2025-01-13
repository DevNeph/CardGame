using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    #region Fields
    public int cardID;
    private bool isHidden;
    private SpriteRenderer spriteRenderer;
    private static readonly Color COVERED_CARD_COLOR = new Color(0.8f, 0.8f, 0.8f, 1f);
    private bool isInSlot = false;
    private int layerIndex;
    public bool isInteractable = true;

    private GameManager gm;
    private BoxCollider2D boxCollider;
    private Vector3 originalScale;

    // Tüm kart örneklerini tutmak için statik liste
    private static List<Card> allCards = new List<Card>();
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();  
        gm = Object.FindFirstObjectByType<GameManager>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Bu kartı statik listeye ekle
        if(!allCards.Contains(this))
            allCards.Add(this);
        
        // Kart aktif olduğunda görünümünü güncelle
        UpdateCardAppearance();
    }

    private void OnDisable()
    {
        // Kart devre dışı kaldığında statik listeden çıkar
        if(allCards.Contains(this))
            allCards.Remove(this);
    }
    #endregion

    #region Setup and Configuration Methods
    public void SetLayerIndex(int index)
    {
        layerIndex = index;
        Vector3 pos = transform.position;
        // Z değerini layer indexine göre güncelle
        pos.z = -index * 0.1f; 
        transform.position = pos;

        // SpriteRenderer'ın sorting order'ını z pozisyonuna göre ayarla
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -index;
        }
        
        Debug.Log($"Card {cardID} layer index set to {index}, z position: {pos.z}, sorting order: {-index}");
    }

    public void SetupCard(int id, Sprite sprite, bool hidden)
    {
        cardID = id;
        isHidden = hidden;

        if (spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer is null on Card with ID={id}!");
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = isHidden ? new Color(1, 1, 1, 0f) : Color.white;
    }

    public void RevealCard()
    {
        isHidden = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void PlaceInSlot()
    {
        isInSlot = true;
        
        if (boxCollider != null)
            boxCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        transform.localScale = originalScale;
        
        // Tüm kartların görünümünü güncelle
        foreach (Card card in allCards) {
            card.UpdateCardAppearance();
        }
    }
    #endregion

    #region Appearance and Logic Methods
    private void SetAsBehind()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = COVERED_CARD_COLOR;
    }

    private void SetAsInFront()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    public bool IsCardBehind()
    {
        if (isInSlot) return false;

        Vector2 boxSize = boxCollider.size;
        Vector2 boxPosition = (Vector2)transform.position + boxCollider.offset;
        
        // İlk olarak, fiziksel çakışmaları kontrol et
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxPosition, boxSize, 0f);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.gameObject == gameObject) continue;
            
            Card otherCard = overlap.GetComponent<Card>();
            if (otherCard != null && !otherCard.isInSlot && 
                otherCard.spriteRenderer.sortingOrder > spriteRenderer.sortingOrder)
            {
                return true;
            }
        }

        // Statik listedeki diğer kartları incele
        foreach (Card otherCard in allCards)
        {
            if (otherCard == this || otherCard.isInSlot) continue;
            if (otherCard.spriteRenderer.sortingOrder > spriteRenderer.sortingOrder)
            {
                Vector2 otherPos = (Vector2)otherCard.transform.position;
                float xDiff = Mathf.Abs(boxPosition.x - otherPos.x);
                float yDiff = Mathf.Abs(boxPosition.y - otherPos.y);

                if (xDiff < boxSize.x && yDiff < boxSize.y)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void UpdateCardAppearance()
    {
        if (isInSlot)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            if (boxCollider != null) boxCollider.enabled = false;
            transform.localScale = originalScale;
            return;
        }

        bool canBeRevealed = CanBeRevealed();
        if (!canBeRevealed || IsCardBehind())
        {
            SetAsBehind();
            if (boxCollider != null) boxCollider.enabled = false;
        }
        else
        {
            SetAsInFront();
            if (boxCollider != null) boxCollider.enabled = true;
        }

        transform.localScale = originalScale;
    }

    private bool CanBeRevealed()
    {
        Vector2 thisPos = (Vector2)transform.position;
        int currentSortingOrder = spriteRenderer.sortingOrder;
        int topLayer = 0; // En üst layer (sorting order 0)
        
        // En üstten şu anki katmana kadar kontrol
        for (int checkLayer = topLayer; checkLayer > currentSortingOrder; checkLayer--)
        {
            bool hasOverlappingCard = false;
            foreach (Card otherCard in allCards)
            {
                if (otherCard == this || otherCard.isInSlot) continue;
                if (otherCard.spriteRenderer.sortingOrder == checkLayer)
                {
                    Vector2 otherPos = (Vector2)otherCard.transform.position;
                    float xDiff = Mathf.Abs(thisPos.x - otherPos.x);
                    float yDiff = Mathf.Abs(thisPos.y - otherPos.y);
                    
                    if (xDiff < boxCollider.size.x && yDiff < boxCollider.size.y)
                    {
                        hasOverlappingCard = true;
                        break;
                    }
                }
            }

            if (hasOverlappingCard)
                return false;
        }

        return true;
    }

    private int FindNextUpperLayer(int currentSortingOrder)
    {
        int nextLayer = -1;
        foreach (Card otherCard in allCards)
        {
            if (otherCard == this || otherCard.isInSlot) continue;
            int otherOrder = otherCard.spriteRenderer.sortingOrder;
            if (otherOrder > currentSortingOrder)
            {
                if (nextLayer == -1 || otherOrder < nextLayer)
                {
                    nextLayer = otherOrder;
                }
            }
        }
        return nextLayer;
    }
    #endregion

    #region Input Handling
    private void OnMouseDown()
    {
        if (isInSlot || GameManager.IsPopupActive)
        {
            Debug.Log("Kart slot'ta veya popup aktif.");
            return;
        }

        if (!CanBeRevealed())
        {
            Debug.Log("Bu karta tıklanamaz çünkü üst layer'da kart var.");
            return;
        }

        gm?.OnCardClicked(this);
    }
    #endregion
}
