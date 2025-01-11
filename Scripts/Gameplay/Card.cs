using System.Collections;
using UnityEngine;

public class Card : MonoBehaviour
{
    public int cardID;
    private bool isHidden;
    private SpriteRenderer spriteRenderer;
    private bool isInSlot = false;

    private GameManager gm;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            StartCoroutine(FadeInCard());
        }
    }

    private IEnumerator FadeInCard()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            while (color.a < 1f)
            {
                color.a += Time.deltaTime;
                spriteRenderer.color = color;
                yield return null;
            }
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

        float thisCardZ = transform.position.z;

        Card[] allCards = FindObjectsOfType<Card>();
        foreach (Card card in allCards)
        {
            if (card != this && !card.isInSlot && card.transform.position.z < thisCardZ)
            {
                return true;
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

        if (IsCardBehind())
        {
            SetAsBehind();
        }
        else
        {
            SetAsInFront();
        }
    }
}
