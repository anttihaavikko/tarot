using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Extensions;
using UnityEngine;

public class CardTooltipper : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cardMask;
    [SerializeField] private Board board;

    private Card current;
    private bool hovering;

    private void Update()
    {
        if (board.IsActing || board.IsPaused) return;
        
        var mp = Input.mousePosition;
        var p = cam.ScreenToWorldPoint(mp).WhereZ(0);

        var hit = Physics2D.OverlapCircle(p, 0.1f, cardMask);

        if (!hit)
        {
            if (hovering)
            {
                hovering = false;
                CursorManager.Instance.Use(0);   
            }
            return;
        }

        var card = hit.GetComponent<Card>();
        
        if (!card) return;
        ShowCursor(card);

        if (card == current || board.IsDragging) return;
        current = card;
        board.ShowPreview(card.GetCardType());
        hovering = true;
    }

    private void ShowCursor(Card card)
    {
        if (card.IsDragging)
        {
            CursorManager.Instance.Use(3);
            return;
        }
        
        if (card.IsLocked)
        {
            CursorManager.Instance.Use(1);
            return;
        }
        
        CursorManager.Instance.Use(2);
    }

    public void Clear()
    {
        current = null;
    }
}