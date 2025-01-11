using System.Collections;
using UnityEngine;

public class Card : MonoBehaviour
{
    public int cardID;
    private bool isHidden;
    private SpriteRenderer spriteRenderer;
    private bool isInSlot = false;

    private GameManager gm;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();  // BoxCollider2D referansını al
        gm = Object.FindFirstObjectByType<GameManager>();
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
        SetAsBehind();
    }

    private void SetAsBehind()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.5f;
            spriteRenderer.color = color;
        }
    }

    private void SetAsInFront()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
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

        // BoxCollider2D boyutlarını kullanarak overlap kontrolü yap
        Vector2 boxSize = boxCollider.size;
        Vector2 boxPosition = (Vector2)transform.position + boxCollider.offset;
        
        // Tüm çakışan collider'ları bul
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxPosition, boxSize, 0f);
        
        foreach (Collider2D overlap in overlaps)
        {
            // Kendisini atla
            if (overlap.gameObject == gameObject) continue;
            
            Card otherCard = overlap.GetComponent<Card>();
            if (otherCard != null && !otherCard.isInSlot)
            {
                // Diğer kart daha düşük z değerine sahipse (yani üstte ise)
                if (otherCard.transform.position.z < transform.position.z)
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
            SetAsBehind();
            return;
        }

        // Kart üstte kart var mı kontrol et
        if (IsCardBehind())
        {
            SetAsBehind();  // Üstte kart varsa yarı saydam yap
        }
        else
        {
            SetAsInFront(); // Üstte kart yoksa tam opak yap
        }
    }
}
