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

    private void Update()
    {
        if (board.IsActing) return;
        
        var mp = Input.mousePosition;
        var p = cam.ScreenToWorldPoint(mp).WhereZ(0);

        var hit = Physics2D.OverlapCircle(p, 0.1f, cardMask);

        if (!hit) return;
        
        var card = hit.GetComponent<Card>();
        if (card && card != current)
        {
            current = card;
            board.ShowPreview(card.GetCardType());
        }
    }

    public void Clear()
    {
        current = null;
    }
}