using System;
using System.Linq;
using AnttiStarterKit.Animations;
using UnityEngine;
using AnttiStarterKit.Extensions;

public class Board : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform spotPreview, targetPreview;

    private readonly InfiniteGrid<Tile> grid = new();

    private void Start()
    {
        AddTile(0, 0);

        AddTile(1, 0);
        AddTile(-1, 0);
        AddTile(0, 1);
        
        Grow();
        Grow();

        AddCard();
    }

    private void AddCard()
    {
        var card = Instantiate(cardPrefab, transform);
        card.SetBoard(this);
        card.transform.position = new Vector3(0, -4, 0);
    }

    private void Grow()
    {
        grid.GetEdges().ToList().ForEach(p => AddTile(p.Position.x, p.Position.y));
    }

    public void Preview(Card card)
    {
        var p = card.transform.position;

        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = (start.Position - spot.Position);
        var target = grid.GetSlideTarget(start.Position.x, start.Position.y, dir);
        
        spotPreview.position = start.AsVector3;
        targetPreview.position = target.AsVector3;
    }

    private void AddTile(int x, int y)
    {
        var tile = Instantiate(tilePrefab, transform);
        tile.Position = new Vector2Int(x, y);
        tile.transform.position = new Vector3(x, y, 0);
        grid.Set(x, y, tile);
    }

    public void Place(Card card)
    {
        var t = card.transform;
        var p = t.position;
        
        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = (start.Position - spot.Position);
        var target = grid.GetSlideTarget(start.Position.x, start.Position.y, dir);
        
        target.Value.Set(card);

        card.Lock();
        
        Tweener.MoveToBounceOut(t, start.AsVector3, 0.1f);
        this.StartCoroutine(() => Tweener.MoveToBounceOut(t, target.AsVector3, 0.3f),0.1f);
        
        AddCard();
    }
}