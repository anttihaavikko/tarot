using System;
using System.Linq;
using AnttiStarterKit.Animations;
using UnityEngine;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Utils;

public class Board : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform spotPreview, targetPreview, preview;
    [SerializeField] private SpriteRenderer previewLane;
    [SerializeField] private Transform target;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform hand;

    private readonly InfiniteGrid<Tile> grid = new();

    private Vector2Int prevPos, prevDir;
    private Tile targetTile;
    
    private const double MaxDropDistance = 0.7f;

    private void Start()
    {
        // AddTile(0, 0);

        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                AddTile(x, y);
            }
        }

        AddCard();
        MoveTarget();
    }

    private void MoveTarget()
    {
        targetTile = grid.RandomFree().Value;
        target.position = targetTile.transform.position;
    }

    private void Update()
    {
        if (DevKey.Down(KeyCode.Q))
        {
            Grow();
        }
    }

    private void AddCard()
    {
        var card = Instantiate(cardPrefab, transform);
        card.SetBoard(this);
        card.transform.position = hand.position;
    }

    private void Grow()
    {
        grid.GetEdges()
            .Where(a => grid.GetNeighboursWithDiagonals(a.Position.x, a.Position.y).Any(b => b.IsOccupied))
            .ToList()
            .ForEach(p => AddTile(p.Position.x, p.Position.y));
        
        MoveTarget();

        RepositionCamera();
    }

    private void RepositionCamera()
    {
        var size = grid.GetSize();
        var max = Mathf.Max(size.x, size.y);
        cam.orthographicSize = 5f + max * 0.5f;

        var center = grid.GetCenter();
        cam.transform.position = center.WhereZ(-10);
        hand.position = (center - Vector3.one * (2f + 0.5f * max)).WhereZ(0);
    }

    private void HidePreview()
    {
        preview.gameObject.SetActive(false);
        previewLane.gameObject.SetActive(false);
        prevDir = Vector2Int.zero;
        prevPos = Vector2Int.zero;
    }

    public void Preview(Card card)
    {
        var p = InvertScale(card.transform.position);

        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = start.Position - spot.Position;
        var end = grid.GetSlideTarget(start.Position.x, start.Position.y, dir);

        if (Vector3.Distance(p, spot.AsVector3) > MaxDropDistance || 
            start.Position.x != spot.Position.x && start.Position.y != spot.Position.y)
        {
            HidePreview();
            return;
        }
        
        spotPreview.position = Scale(start.AsVector3);
        targetPreview.position = Scale(end.AsVector3);
        
        if (prevDir == dir && prevPos == start.Position) return;

        prevDir = dir;
        prevPos = start.Position;

        preview.gameObject.SetActive(true);
        var targetPos = Scale(spot.AsVector3);
        var duration = 0.3f * Vector3.Distance(p, targetPos);
        preview.transform.position = Scale(spot.AsVector3);
        Tweener.MoveToQuad(preview, Scale(end.AsVector3), duration);
        
        previewLane.transform.position = Scale((start.AsVector3 + end.AsVector3) * 0.5f);
        previewLane.gameObject.SetActive(true);
        var width = Mathf.Abs(start.Position.x - end.Position.x) + 1f - 0.1f;
        var height = (Mathf.Abs(start.Position.y - end.Position.y) + 1f) * 1.5f - 0.1f;
        previewLane.size = new Vector3(width, height);
    }

    private void AddTile(int x, int y)
    {
        var tile = Instantiate(tilePrefab, transform);
        tile.Position = new Vector2Int(x, y);
        tile.transform.position = Scale(new Vector3(x, y, 0));
        grid.Set(x, y, tile);
    }

    public void Place(Card card)
    {
        HidePreview();
        
        var t = card.transform;
        var p = InvertScale(t.position);
        
        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = (start.Position - spot.Position);
        var end = grid.GetSlideTarget(start.Position.x, start.Position.y, dir);

        if (start.Position.x != spot.Position.x && start.Position.y != spot.Position.y) return;
        if (Vector3.Distance(p, spot.AsVector3) > MaxDropDistance) return;

        end.Value.Set(card);

        card.Lock();
        
        Tweener.MoveToBounceOut(t, Scale(start.AsVector3), 0.1f);
        var targetPos = Scale(end.AsVector3);
        var duration = 0.05f * Vector3.Distance(t.position, targetPos);
        this.StartCoroutine(() => Tweener.MoveToBounceOut(t, targetPos, duration),0.1f);

        if (end.Value == targetTile)
        {
            Grow();
        }
        
        AddCard();
    }

    private Vector3 Scale(Vector3 v)
    {
        return new Vector3(v.x, v.y * 1.5f, v.z);
    }
    
    private Vector3 InvertScale(Vector3 v)
    {
        return new Vector3(v.x, v.y / 1.5f, v.z);
    }
}