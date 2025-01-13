using System.Collections;
using UnityEngine;

public class Card : MonoBehaviour
{
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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();  
        gm = Object.FindFirstObjectByType<GameManager>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Kart aktif olduğunda görünümünü güncelle
        UpdateCardAppearance();
    }

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
            // z değeri 0'dan -0.1, -0.2, -0.3 şeklinde gittiği için
            // sorting order'ı da aynı şekilde ayarlıyoruz
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
            spriteRenderer.color = Color.white; // Kartı tamamen görünür yap
        }
    }

    public void PlaceInSlot()
    {
        isInSlot = true;
        
        // BoxCollider'ı devre dışı bırak
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        // Kart slota yerleştiğinde rengi değişmesin
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // Orijinal scale değerini koru
        transform.localScale = originalScale;
    }

    private void SetAsBehind()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = COVERED_CARD_COLOR; // Gri renk kullan
        }
    }

    private void SetAsInFront()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; // Normal renk
        }
    }

    private void OnMouseDown()
    {
        if (isInSlot)
        {
            Debug.Log("Bu kart slota yerleştiği için tıklanamaz.");
            return;
        }

        if (GameManager.IsPopupActive || IsCardBehind())
        {
            Debug.Log("Bu karta tıklanamaz çünkü arka planda veya popup aktif.");
            return;
        }

        gm?.OnCardClicked(this);
    }

    public bool IsCardBehind()
    {
        if (isInSlot) return false;

        Vector2 boxSize = boxCollider.size;
        Vector2 boxPosition = (Vector2)transform.position + boxCollider.offset;
        
        // Tüm çakışan collider'ları bul
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxPosition, boxSize, 0f);
        
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.gameObject == gameObject) continue;
            
            Card otherCard = overlap.GetComponent<Card>();
            if (otherCard != null && !otherCard.isInSlot)
            {
                // Eğer üstte bir kart varsa (sorting order daha büyükse)
                // veya aynı sorting order'da ama daha önde bir kart varsa
                if (otherCard.spriteRenderer.sortingOrder > spriteRenderer.sortingOrder)
                {
                    return true;
                }
            }
        }

        // Ayrıca bir üst layer'da herhangi bir kart var mı kontrol et
        Collider2D[] allCards = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var col in allCards)
        {
            Card otherCard = col.GetComponent<Card>();
            if (otherCard != null && !otherCard.isInSlot && otherCard != this)
            {
                // Eğer kart üst layer'daysa ve pozisyonlar çakışıyorsa
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
        }

        return false;
    }

    public void UpdateCardAppearance()
    {
        // Eğer slot'taysa rengi değiştirme
        if (isInSlot)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            transform.localScale = originalScale;
            return;
        }

        bool isBehind = IsCardBehind();
        
        if (isBehind)
        {
            SetAsBehind();
            if (boxCollider != null) boxCollider.enabled = false;
        }
        else
        {
            // Üstteki tüm kartların alınıp alınmadığını kontrol et
            bool canBeRevealed = CanBeRevealed();
            if (canBeRevealed)
            {
                SetAsInFront();
                if (boxCollider != null) boxCollider.enabled = true;
            }
            else
            {
                SetAsBehind();
                if (boxCollider != null) boxCollider.enabled = false;
            }
        }
        
        transform.localScale = originalScale;
    }

private bool CanBeRevealed()
    {
        Vector2 thisPos = (Vector2)transform.position;
        Collider2D[] allCards = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        int currentSortingOrder = spriteRenderer.sortingOrder;

        // Kartın üzerinde başka bir kart var mı kontrol et
        Card nearestUpperCard = null;
        float minSortingOrder = float.MaxValue;

        foreach (var col in allCards)
        {
            Card otherCard = col.GetComponent<Card>();
            if (otherCard != null && !otherCard.isInSlot && otherCard != this)
            {
                Vector2 otherPos = (Vector2)otherCard.transform.position;
                float xDiff = Mathf.Abs(thisPos.x - otherPos.x);
                float yDiff = Mathf.Abs(thisPos.y - otherPos.y);

                // Eğer pozisyonlar çakışıyorsa ve diğer kart daha üst layer'da ise
                if (xDiff < boxCollider.size.x && yDiff < boxCollider.size.y && 
                    otherCard.spriteRenderer.sortingOrder > currentSortingOrder)
                {
                    // Bu kartın üzerindeki en yakın kartı bul
                    if (otherCard.spriteRenderer.sortingOrder < minSortingOrder)
                    {
                        minSortingOrder = otherCard.spriteRenderer.sortingOrder;
                        nearestUpperCard = otherCard;
                    }
                }
            }
        }

        // Eğer üstte kart varsa
        if (nearestUpperCard != null)
        {
            // Üstteki kart tıklanabilir değilse, bu kart da kapalı kalmalı
            if (!nearestUpperCard.boxCollider.enabled)
            {
                return false;
            }
        }

        return true;
    }

    private int FindNextUpperLayer(int currentSortingOrder, Collider2D[] allCards)
    {
        int nextLayer = -1;
        
        foreach (var col in allCards)
        {
            Card otherCard = col.GetComponent<Card>();
            if (otherCard != null && !otherCard.isInSlot && otherCard != this)
            {
                int otherOrder = otherCard.spriteRenderer.sortingOrder;
                if (otherOrder > currentSortingOrder)
                {
                    if (nextLayer == -1 || otherOrder < nextLayer)
                    {
                        nextLayer = otherOrder;
                    }
                }
            }
        }
        
        return nextLayer;
    }
}