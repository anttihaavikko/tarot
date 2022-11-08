using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using UnityEngine;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Game;
using AnttiStarterKit.Managers;
using AnttiStarterKit.Utils;
using TMPro;
using Random = System.Random;

public class Board : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform spotPreview, targetPreview, preview;
    [SerializeField] private SpriteRenderer previewLane;
    [SerializeField] private Transform target;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform hand;
    [SerializeField] private CardPreview cardPreview;
    [SerializeField] private Deck deck;
    [SerializeField] private List<TMP_Text> moveCounters;
    [SerializeField] private Transform expBar;
    [SerializeField] private ScoreDisplay scoreDisplay;
    [SerializeField] private Skills skills;
    [SerializeField] private GameObject devMenu;

    private readonly InfiniteGrid<Tile> grid = new();

    private Vector2Int prevPos, prevDir;
    private Tile targetTile;
    private Card justPlaced;

    private int movesLeft;
    private int MoveCount => 5 + skills.Count(Passive.AddMove) - skills.Count(Passive.MultiIncreaseAndDecreaseMoves);

    private int level = 1;
    private int exp;
    
    private const float MaxDropDistance = 0.7f;
    private const float PanTime = 0.3f;

    private bool targetReached;

    private Card drawnCard;
    
    public Card JustTouched { get; private set; }
    public int SlideLength { get; private set; }
    public Tile BehindSpot { get; private set; }

    private void Start()
    {
        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                AddTile(x, y);
            }
        }
        
        movesLeft = MoveCount;
        UpdateMoveDisplay();
        
        RepositionCamera();
        Invoke(nameof(AddCard), PanTime + 0.1f);
        
        MoveTarget();
    }

    public void MoveTarget()
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
        
        if (DevKey.Down(KeyCode.Tab))
        {
            devMenu.SetActive(!devMenu.activeSelf);
        }
        
        if (DevKey.Down(KeyCode.A))
        {
            deck.AddToTop(CardType.Death);
        }
    }

    public void ChangeDrawnTo(CardType type)
    {
        drawnCard.TransformTo(type);
        ShowPreview(type);
    }

    private void AddCard()
    {
        var type = deck.Pull();
        drawnCard = CreateCard(type, deck.GetSpawn());
        var t = drawnCard.transform;
        Tweener.MoveToQuad(t, t.position + new Vector3(0.8f, 0.4f, 0), 0.2f);
        this.StartCoroutine(() => Tweener.MoveToBounceOut(t, hand.position, 0.3f), 0.2f);
        
        var transforms = skills.Get(Passive.TransformOnDraw, type).ToList();
        if (transforms.Any())
        {
            this.StartCoroutine(() =>
            {
                var first = transforms.First();
                var targetType = first.TargetType;
                drawnCard.TransformTo(targetType);
                ShowPreview(targetType);
                first.Trigger();
                EffectManager.AddTextPopup(first.title, drawnCard.transform.position, 0.8f);
            }, 0.5f);
        }

        ShowPreview(type);
    }

    private Card CreateCard(CardType type, Vector3 pos)
    {
        var card = Instantiate(cardPrefab, transform);
        card.Init(this, type);
        card.transform.position = pos;
        return card;
    }

    private void Grow()
    {
        grid.GetEdges()
            .Where(a => grid.GetNeighboursWithDiagonals(a.Position.x, a.Position.y).Any(b => b.IsOccupied))
            .ToList()
            .ForEach(p => AddTile(p.Position.x, p.Position.y));
        
        skills.Get(Passive.FurtherExtend).ToList().ForEach(s =>
        {
            grid.GetAll()
                .Where(t => t.IsOccupied && s.Matches(t.Value.Card.GetCardType()))
                .SelectMany(t => grid.GetNeighboursWithDiagonals(t.Position.x, t.Position.y, 2))
                .Where(g => g.IsWall)
                .ToList()
                .ForEach(g => AddTile(g.Position.x, g.Position.y));
        });
        
        MoveTarget();

        RepositionCamera();
    }

    private void RepositionCamera()
    {
        const float perStep = 0.9f;
        var size = grid.GetSize();
        var max = Mathf.Max(size.x * 0.7f, size.y);

        cam.orthographicSize = 1f + max * perStep;

        var center = grid.GetCenter();
        Tweener.MoveToBounceOut(cam.transform, center.WhereZ(-10), PanTime);
        var handPos = center - new Vector3(1f + perStep * max * 0.9f, perStep * max * 0.9f, 0);
        Tweener.MoveToBounceOut(hand, handPos, PanTime);
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
        if (!grid.Get(x, y).IsWall) return;
        var tile = Instantiate(tilePrefab, transform);
        tile.Position = new Vector2Int(x, y);
        tile.transform.position = Scale(new Vector3(x, y, 0));
        grid.Set(x, y, tile);
    }

    public void Slide(Card card)
    {
        StartCoroutine(DoSlide(card));
    }

    private IEnumerator DoSlide(Card card)
    {
        grid.ResetSlide();
        
        justPlaced = card;
        
        HidePreview();
        
        var t = card.transform;
        var p = InvertScale(t.position);
        
        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = start.Position - spot.Position;
        var end = grid.GetSlideTarget(start.Position.x, start.Position.y, dir);
        
        if (start.Position.x != spot.Position.x && start.Position.y != spot.Position.y ||
            Vector3.Distance(p, spot.AsVector3) > MaxDropDistance)
        {
            card.ReturnToHand();
            ShowPreview(card.GetCardType());
            yield break;
        }
        
        skills.UnMarkSkills();
        card.Placed();

        JustTouched = grid.CollisionTarget ? grid.CollisionTarget.Card : null;
        BehindSpot = grid.BehindSpot;
        SlideLength = Mathf.RoundToInt(Vector2Int.Distance(start.Position, end.Position));
        
        HideCardPreview();
        
        movesLeft--;

        Tweener.MoveToBounceOut(t, Scale(start.AsVector3), 0.1f);
        var cardPos = Scale(end.AsVector3);
        var targetPos = cardPos;
        var duration = 0.05f * Vector3.Distance(t.position, targetPos);
        this.StartCoroutine(() => Tweener.MoveToBounceOut(t, targetPos, duration),0.1f);
        
        end.Value.Set(card);
        card.Lock();
        targetReached = false;

        yield return new WaitForSeconds(duration);

        yield return skills.Trigger(SkillTrigger.Place, card);

        if (targetReached || end.Value == targetTile)
        {
            yield return ReachedTarget(cardPos);
        }
        
        UpdateMoveDisplay();

        if (movesLeft > 0)
        {
            yield return new WaitForSeconds(0.5f);
            AddCard();
        }
    }

    public IEnumerator SpawnCards(CardType type, List<Tile> tiles)
    {
        JustTouched = null;
        BehindSpot = null;

        foreach (var tile in tiles)
        {
            if (!tile.IsEmpty) continue;
            var card = CreateCard(type, tile.transform.position);
            card.Lock();
            tile.Set(card);
        }

        foreach (var tile in tiles)
        {
            yield return skills.Trigger(SkillTrigger.Place, tile.Card);
            
            if (tile == targetTile)
            {
                targetReached = true;
            }
        }

        UpdateMoveDisplay();
    }

    private IEnumerator ReachedTarget(Vector3 cardPos)
    {
        if (movesLeft == MoveCount - 1)
        {
            var doubles = skills.Trigger(Passive.MultiIncreaseAndDecreaseMoves, cardPos);
            scoreDisplay.AddMulti(doubles ? 2 : 1);
            EffectManager.AddTextPopup("SPLENDID!", cardPos.RandomOffset(1f) + Vector3.up, 0.7f);
        }

        Grow();

        movesLeft = MoveCount;
        exp++;

        UpdateExpBar();

        if (exp == level)
        {
            var amount = grid.GetEmptyCount() * 10;

            yield return new WaitForSeconds(0.5f);

            AddScore(amount, cardPos);
            scoreDisplay.ResetMulti();

            exp = 0;
            level++;

            yield return new WaitForSeconds(0.5f);

            yield return skills.Present();

            UpdateExpBar();
        }
    }

    private void UpdateExpBar()
    {
        var ratio = Mathf.Clamp01(1f * exp / level);
        Tweener.ScaleToBounceOut(expBar, new Vector3(ratio, 1f, 1f), 0.2f);
    }

    private Vector3 Scale(Vector3 v)
    {
        return new Vector3(v.x, v.y * 1.5f, v.z);
    }
    
    private Vector3 InvertScale(Vector3 v)
    {
        return new Vector3(v.x, v.y / 1.5f, v.z);
    }

    private void UpdateMoveDisplay()
    {
        moveCounters.ForEach(t => t.text = $"{movesLeft} MOVES LEFT");
    }

    public void AddMulti(int amount = 1)
    {
        scoreDisplay.AddMulti(amount);
    }

    public void AddScore(int amount, Vector3 pos)
    {
        var doubles = justPlaced && skills.Trigger(Passive.ScoreDoubler, justPlaced.GetCardType(), justPlaced.transform.position + Vector3.up);
        var extraMulti = doubles ? 2 : 1;

        var amt = extraMulti * amount;
        scoreDisplay.Add(amt);
        var shown = amt * scoreDisplay.Multi;
        EffectManager.AddTextPopup(shown.AsScore(), pos.RandomOffset(1f), 1.3f);
    }

    public bool IsPlacedAlone()
    {
        var pos = justPlaced.Tile.Position;
        return grid.GetNeighbours(pos.x, pos.y).All(g => g.IsWall || g.IsEmpty);
    }

    public IEnumerator DestroyCards(List<Card> cards)
    {
        var targets = cards.Where(c => !c.IsDying).ToList();
        
        targets.ForEach(c => c.ShakeForever());
        yield return new WaitForSeconds(0.3f);
        foreach (var c in targets)
        {
            yield return skills.Trigger(SkillTrigger.Death, c);
            c.Tile.Clear();
            c.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator SpawnAround(Card card, CardType type)
    {
        var targets = grid.GetNeighboursWithDiagonals(card.Tile.Position.x, card.Tile.Position.y).Where(t => t.IsEmpty).ToList();
        yield return SpawnCards(type, targets.Select(t => t.Value).ToList());
    }
    
    public IEnumerator SpawnOnNeighbours(Card card, CardType type)
    {
        var targets = grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y).Where(t => t.IsEmpty).ToList();
        yield return SpawnCards(type, targets.Select(t => t.Value).ToList());
    }

    public bool HasEmptyNeighbours(Card card)
    {
        return grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y).Any(t => t.IsEmpty);
    }
    
    public bool HasEmptyNeighboursWithDiagonals(Card card)
    {
        return grid.GetNeighboursWithDiagonals(card.Tile.Position.x, card.Tile.Position.y).Any(t => t.IsEmpty);
    }

    public bool HasNeighboursWithDiagonals(Card card, Skill skill)
    {
        return grid.GetNeighboursWithDiagonals(card.Tile.Position.x, card.Tile.Position.y)
            .Any(t => TileMatchesSkill(t, skill));
    }
    
    public bool HasNeighbours(Card card, Skill skill)
    {
        return grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y)
            .Any(t => TileMatchesSkill(t, skill));
    }
    
    public IEnumerable<Card> GetNeighbours(Card card, Skill skill, bool diagonals)
    {
        var spots = diagonals ?
            grid.GetNeighboursWithDiagonals(card.Tile.Position.x, card.Tile.Position.y).Where(t => TileMatchesSkill(t, skill)) :
            grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y).Where(t => TileMatchesSkill(t, skill));
        
        return spots.Where(s => s.Value.Card != card).Select(s => s.Value.Card);
    }

    private bool TileMatchesSkill(InfiniteGrid<Tile>.GridSpot spot, Skill skill)
    {
        return spot.IsOccupied && (!skill.HasTargetType || spot.Value.Contains(skill.TargetType));
    }

    public IEnumerator SpawnBehind(CardType type)
    {
        yield return SpawnCards(type, new List<Tile>{ BehindSpot });
    }

    public void AddToDeck(CardType type)
    {
        deck.AddToTop(type);
    }

    public void HideCardPreview()
    {
        cardPreview.Hide();
    }

    public void ShowPreview(CardType type)
    {
        skills.UnMarkSkills();
        cardPreview.Show(type);
        skills.MarkSkills(type);
    }

    public Card GetClosest(Card card, CardType type)
    {
        var options = grid.GetAll().Where(s => s.IsOccupied && s.Value.Contains(type)).ToList();
        return options.Any() ? 
            options.OrderBy(o => Vector2Int.Distance(card.Tile.Position, o.Position)).First().Value.Card : 
            default;
    }
}