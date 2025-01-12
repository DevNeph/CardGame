using System.Collections;
using UnityEngine;

public class Card : MonoBehaviour
{
    public int cardID;
    private bool isHidden;
    private SpriteRenderer spriteRenderer;
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
        // Z pozisyonunu layer indexine göre güncelle
        Vector3 pos = transform.position;
        // Mevcut z pozisyonunu koru, sadece ince ayar yap
        pos.z += (index * 0.01f); // Çok küçük bir offset ekle
        transform.position = pos;
        
        Debug.Log($"Card {cardID} layer index set to {index}, final z position: {pos.z}");
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
            Color color = spriteRenderer.color;
            color.a = 0.5f; // Yarı saydam
            spriteRenderer.color = color;
        }
    }

    private void SetAsInFront()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f; // Tam opak
            spriteRenderer.color = color;
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
                // Z pozisyonlarını karşılaştır
                float thisZ = transform.position.z;
                float otherZ = otherCard.transform.position.z;

                // Debug için z pozisyonlarını yazdır
                Debug.Log($"This card Z: {thisZ}, Other card Z: {otherZ}");

                // Eğer diğer kart daha üstteyse (z değeri daha küçükse)
                if (otherZ < thisZ)
                {
                    Debug.Log($"Card {cardID} is behind card {otherCard.cardID}");
                    SetAsBehind(); // Kartı kararttık
                    return true;
                }
                else
                {
                    SetAsInFront(); // Kart öndeyse normal hale getirdik
                }
            }
        }

        // Hiçbir kartın arkasında değilse normal hale getir
        SetAsInFront();
        return false;
    }

    public void UpdateCardAppearance()
    {
        // Eğer slot'taysa rengi değiştirme ve orijinal scale'i koru
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
            SetAsInFront();
            if (boxCollider != null) boxCollider.enabled = true;
        }
        
        // Her durumda orijinal scale'i koru
        transform.localScale = originalScale;
    }

}