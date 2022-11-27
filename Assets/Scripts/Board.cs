using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using UnityEngine;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Game;
using AnttiStarterKit.Managers;
using AnttiStarterKit.ScriptableObjects;
using AnttiStarterKit.Utils;
using AnttiStarterKit.Visuals;
using Leaderboards;
using TMPro;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform spotPreview, targetPreview, preview;
    [SerializeField] private SpriteRenderer previewLane;
    [SerializeField] private Transform target;
    [SerializeField] private Camera cam, displaceCam;
    [SerializeField] private Transform hand;
    [SerializeField] private CardPreview cardPreview;
    [SerializeField] private Deck deck;
    [SerializeField] private List<TMP_Text> moveCounters;
    [SerializeField] private Transform expBar;
    [SerializeField] private ScoreDisplay scoreDisplay;
    [SerializeField] private Skills skills;
    [SerializeField] private GameObject devMenu;
    [SerializeField] private EffectCamera effectCamera;
    [SerializeField] private LineDrawer lineDrawer;
    [SerializeField] private Transform handSpotPrefab;
    [SerializeField] private CardTooltipper tooltipper;
    [SerializeField] private SpriteRenderer playArea;
    [SerializeField] private Shaker moveShaker;
    [SerializeField] private SoundCollection harpSounds;
    [SerializeField] private GameObject gameOverContainer;
    [SerializeField] private TutorialHolder tutorial;
    [SerializeField] private SoundComposition explosionSound, transformSound, placeSound;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Appearer undoButton;
    [SerializeField] private Plants plants;
    [SerializeField] private Appearer pauseLabel, dailyResumeButton, menuButton, giveUpButton, resumeButton, againButton;

    private readonly InfiniteGrid<Tile> grid = new();
    private readonly List<Card> drawnCards = new();

    private Vector2Int prevPos, prevDir;
    private Tile targetTile;
    private Card justPlaced;
    private int targetMoves;
    private int handSize = 1;

    private int prevScore, prevMulti, prevMoves;
    private Card prevCard;

    private int movesLeft;
    private int MoveCount => 5 + skills.Count(Passive.AddMove) - skills.Count(Passive.MultiIncreaseAndDecreaseMoves);

    private int level = 2;
    private int exp;
    private int fieldSize = 7;
    
    private const float MaxDropDistance = 0.7f;
    private const float PanTime = 0.3f;

    private bool targetReached;

    private Card drawnCard;
    private bool canPlace;
    private int soundIndex;
    private bool alreadyOver;
    private bool paused;
    
    public Card JustTouched { get; private set; }
    public int SlideLength { get; private set; }
    public Tile BehindSpot { get; private set; }
    public Vector2Int PreviousDirection { get; private set; }
    public bool CanUndo { get; set; }
    
    public bool IsActing => !skills.IsViewingBoard && !canPlace;
    public bool IsDragging => drawnCards.Any(c => c.IsDragging);
    public Vector3 MidPoint => cam.transform.position.WhereZ(0);
    public Vector3 SkyPoint => MidPoint + Vector3.Distance(MidPoint, hand.transform.position) * Vector3.up;
    public bool HasEnded => alreadyOver;
    public bool IsPaused => paused;

    private void Start()
    {
        AudioManager.Instance.TargetPitch = 1;
        
        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                AddTile(x, y, false);
            }
        }
        
        StartCoroutine(MoveTarget());
        
        SetupDaily();
        
        UpdateAreaSize();
        
        movesLeft = MoveCount;
        UpdateMoveDisplay();
        
        RepositionCamera();
        
        deck.Init();
        StartCoroutine(AddStartHand());

        canPlace = true;
        
        Invoke(nameof(TenSecondTimer), 10f);
        Invoke(nameof(IntroTutorial), 1f);
    }

    private IEnumerator AddStartHand()
    {
        yield return new WaitForSeconds(PanTime);
        
        for (var i = 0; i < handSize; i++)
        {
            yield return new WaitForSeconds(0.5f);
            AddCard();
        }
    }

    private void IntroTutorial()
    {
        tutorial.Show(TutorialMessage.Intro);
    }

    private void TenSecondTimer()
    {
        if (!paused)
        {
            StartCoroutine(skills.Trigger(SkillTrigger.Timer));   
        }
        
        Invoke(nameof(TenSecondTimer), 10f);
    }

    public void Undo()
    {
        undoButton.Hide();
        
        scoreDisplay.Set(prevScore, prevMulti);
        movesLeft = prevMoves;
        
        UpdateMoveDisplay();
        
        deck.AddToTop(drawnCard.GetCardType(), 1);
        deck.AddToTop(prevCard.GetCardType(), 1);
        
        prevCard.Tile.Clear();
        Destroy(prevCard.gameObject);
        Destroy(drawnCards.Last().gameObject);
        drawnCards.Clear();
        
        AddCardWithoutTriggers();
    }

    private void SetupDaily()
    {
        if (!DailyState.Instance.IsDaily) return;
        
        DailyState.Instance.Seed();

        fieldSize = Random.Range(4, 10);
        var skillCount = Random.Range(DailyState.MinSkills, DailyState.MaxSkills + 1);
        
        if (Random.value < DailyState.ModChance)
        {
            skills.AddRandomDailyMod();
        }
            
        for (var i = 0; i < skillCount; i++)
        {
            skills.AddRandom();
        }
    }

    private void UpdateAreaSize()
    {
        playArea.size = Scale(new Vector3(fieldSize * 2 - 1.5f, fieldSize * 2 - 2f));
    }

    public IEnumerator MoveTarget()
    {
        DailyState.Instance.Seed(targetMoves + 1234);
        target.gameObject.SetActive(true);
        var spot = skills.Has(Passive.TargetDriller) ? grid.GetRandom() : grid.RandomFree();
        if (spot == default)
        {
            GameOver(true);
            yield break;
        }

        if (spot.IsOccupied)
        {
            skills.Trigger(Passive.TargetDriller, spot.Value.Card);
            skills.GetTriggered(Passive.TargetDriller, spot.Value.Card.transform.position);
            yield return DestroyCards(new List<Card> { spot.Value.Card }, null);
        }
        
        targetTile = spot.Value;
        target.position = targetTile.transform.position;
        PulseAt(target.position);
        targetMoves++;
    }

    public void Resume()
    {
        paused = false;
        
        pauseLabel.Hide();
        resumeButton.Hide();
        giveUpButton.Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !alreadyOver)
        {
            paused = !paused;

            if (paused)
            {
                pauseLabel.Show();
                resumeButton.Show();
                giveUpButton.Show();
                return;
            }
            
            Resume();
        }
        
        if (DevKey.Down(KeyCode.Tab))
        {
            devMenu.SetActive(!devMenu.activeSelf);
        }

        if (DevKey.Down(KeyCode.T))
        {
            DrawLines(SkyPoint, drawnCards);
        }
        
        if (DevKey.Down(KeyCode.Y))
        {
            DrawLines(Vector3.zero, drawnCards);
        }
    }

    public void IncreaseAreaSize()
    {
        fieldSize++;
        UpdateAreaSize();
    }

    public void ChangeDrawnTo(CardType type)
    {
        drawnCard.TransformTo(type);
        ShowPreview(type);
    }

    private void AddCardWithoutTriggers()
    {
        RepositionHand(true);
        
        var p = hand.position;
        var type = deck.Pull();
        drawnCard = CreateCard(type, deck.GetSpawn(), false);
        var t = drawnCard.transform;
        t.parent = hand;
        Tweener.MoveToQuad(t, t.position + new Vector3(0.8f, 0.4f, 0), 0.2f);
        this.StartCoroutine(() => Tweener.MoveToBounceOut(t, hand.position, 0.3f), 0.2f);
        this.StartCoroutine(() => drawnCard.Bounce(Vector3.down), 0.35f);
        drawnCard.RandomizeRotation();
        
        placeSound.Play(p);
    }

    public void AddCard()
    {
        DailyState.Instance.Seed(1234 + level + drawnCards.Count);
        
        RepositionHand(true);
        
        var p = hand.position;
        var type = deck.Pull();
        drawnCard = CreateCard(type, deck.GetSpawn(), false);
        var t = drawnCard.transform;
        t.parent = hand;
        Tweener.MoveToQuad(t, t.position + new Vector3(0.8f, 0.4f, 0), 0.2f);
        this.StartCoroutine(() => Tweener.MoveToBounceOut(t, hand.position, 0.3f), 0.2f);
        this.StartCoroutine(() => drawnCard.Bounce(Vector3.down), 0.35f);
        drawnCard.RandomizeRotation();
        
        placeSound.Play(p);

        prevMulti = scoreDisplay.Multi;

        this.StartCoroutine(() =>
        {
            var transforms = skills.GetTriggered(Passive.TransformOnDraw, type, p, true).ToList();
            if (!transforms.Any() && Random.value < 0.5f)
            {
                transforms = skills.GetTriggered(Passive.GambleTransform, p, true).ToList();
            }

            if (!transforms.Any()) return;
            var first = transforms.First();
            var targetType = GetTransformTypeFor(drawnCard, first.TargetType);
            var originalType = drawnCard.GetCardType();
            drawnCard.TransformTo(targetType);
            ShowPreview(targetType);
            first.Trigger();

            StartCoroutine(skills.Trigger(SkillTrigger.Transform, originalType, drawnCard, 0.6f));

        }, 0.5f);

        ShowPreview(type);

        drawnCards.Insert(0, drawnCard);

        if (skills.Trigger(Passive.MultiDrawShuffleReset, t.position))
        {
            AddMulti(t.position);
        }
    }

    public void Shuffled()
    {
        var p = deck.transform.position;
        
        if (skills.Trigger(Passive.MultiDrawShuffleReset, p))
        {
            scoreDisplay.ResetMulti();
        }
    }

    private void RepositionHand(bool useOffset)
    {
        if (drawnCards.Any())
        {
            for (var i = 0; i < drawnCards.Count; i++)
            {
                var offset = i + (useOffset ? 1 : 0);
                var pos = hand.position + Vector3.right * offset + Vector3.down * (0.2f * offset);
                Tweener.MoveToBounceOut(drawnCards.ElementAt(i).transform, pos, 0.3f);
            }
        }
    }

    private Card CreateCard(CardType type, Vector3 pos, bool pulse = true)
    {
        var card = Instantiate(cardPrefab, transform);
        card.Init(this, type);
        card.transform.position = pos;
        card.Announce();
        if (pulse)
        {
            PulseAt(pos);   
        }
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

        RepositionCamera();
    }

    private void RepositionCamera()
    {
        const float perStep = 0.9f;
        var size = grid.GetSize();
        var max = Mathf.Max(size.x * 0.7f, size.y);

        cam.orthographicSize = displaceCam.orthographicSize = 1f + max * perStep;

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

    private bool NoFreeSpots()
    {
        return grid.GetClosest(Vector3.zero) == default;
    }

    public void Preview(Card card)
    {
        var p = InvertScale(card.transform.position);

        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = start.Position - spot.Position;
        var path = grid.GetSlidePath(start.Position.x, start.Position.y, dir);
        var end = path.LastOrDefault();

        if (!path.Any() || 
            Vector3.Distance(p, spot.AsVector3) > MaxDropDistance || 
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

    private void AddTile(int x, int y, bool pulse = true)
    {
        var diff = fieldSize - Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
        if (diff < 3)
        {
            playArea.gameObject.SetActive(true);
            if (diff < 2) return;
        }

        if (!grid.Get(x, y).IsWall) return;
        var tile = Instantiate(tilePrefab, transform);
        tile.Position = new Vector2Int(x, y);
        var p = Scale(new Vector3(x, y, 0));
        tile.transform.position = p;
        if (pulse)
        {
            // PulseAt(p, false);
        }
        grid.Set(x, y, tile);
        
        // plants.Clear(Scale(p));
        // var spots = grid.GetNeighboursWithDiagonals(x, y, 4).Where(t => t.IsWall).ToList();
        // spots.ForEach(spot => plants.Add(Scale(spot.AsVector3)));
    }

    public void Slide(Card card)
    {
        StartCoroutine(DoSlide(card));
    }

    private InfiniteGrid<Tile>.GridSpot GetSlideTarget(Card card, int x, int y, Vector2Int dir)
    {
        PreviousDirection = dir;
        var slidePath = grid.GetSlidePath(x, y, dir);
        return skills.Trigger(Passive.StopsOnTarget, card)
            ? slidePath.FirstOrDefault(s => s.Value == targetTile) ?? slidePath.Last()
            : slidePath.Last();
    }

    private IEnumerator DoSlide(Card card)
    {
        CursorManager.Instance.Use(0);
        
        CanUndo = true;

        prevScore = scoreDisplay.Total;
        prevMoves = movesLeft;
        prevCard = card;
        
        if (!canPlace)
        {
            tutorial.Show(TutorialMessage.PlaceOnEdge);
            card.ReturnToHand();
            ShowPreview(card.GetCardType());
            HidePreview();
            placeSound.Play(card.transform.position);
            yield break;
        }

        canPlace = false;
        grid.ResetSlide();
        justPlaced = card;
        HidePreview();
        
        var t = card.transform;
        var p = InvertScale(t.position);
        
        var spot = grid.GetClosestEdge(p);
        var start = grid.GetClosest(p);
        var dir = start.Position - spot.Position;

        var end = GetSlideTarget(card, start.Position.x, start.Position.y, dir);

        if (start.Position.x != spot.Position.x && start.Position.y != spot.Position.y ||
            Vector3.Distance(p, spot.AsVector3) > MaxDropDistance)
        {
            tutorial.Show(TutorialMessage.PlaceOnEdge);
            card.ReturnToHand();
            placeSound.Play(card.transform.position);
            ShowPreview(card.GetCardType());
            canPlace = true;
            yield break;
        }

        tutorial.Mark(TutorialMessage.PlaceOnEdge);
        tutorial.HideSpots();

        skills.UnMarkSkills();
        card.Placed();
        drawnCards.Remove(card);
        card.transform.parent = transform;

        JustTouched = grid.CollisionTarget ? grid.CollisionTarget.Card : null;
        BehindSpot = grid.BehindSpot;
        SlideLength = Mathf.RoundToInt(Vector2Int.Distance(start.Position, end.Position));
        
        HideCardPreview();
        
        var cardPos = Scale(end.AsVector3);
        placeSound.Play(cardPos);

        if (!skills.Trigger(Passive.FreeMove, card.GetCardType(), cardPos))
        {
            movesLeft--;   
        }

        RepositionHand(false);

        Tweener.MoveToBounceOut(t, Scale(start.AsVector3), 0.1f);
        var duration = 0.05f * Vector3.Distance(t.position, cardPos);
        yield return new WaitForSeconds(0.1f);
        Tweener.MoveToBounceOut(t, cardPos, duration);
        card.Bounce(dir);

        end.Value.Set(card);
        card.Lock();
        targetReached = false;

        PulseAt(cardPos);
        AudioManager.Instance.PlayEffectFromCollection(2, cardPos);

        yield return new WaitForSeconds(duration);
        
        HideTarget(card.Tile);

        yield return skills.PlaceTrigger(card, true);

        if (targetReached || end.Value == targetTile)
        {
            yield return ReachedTarget(cardPos);
        }
        
        UpdateMoveDisplay();

        if (movesLeft == 1)
        {
            yield return new WaitForSeconds(0.75f);
            AudioManager.Instance.PlayEffectAt(8, Vector3.zero);
            EffectManager.AddTextPopup("Last Move!", MidPoint, 1.2f);
            moveShaker.Shake();
            effectCamera.BaseEffect(0.3f);
            yield return new WaitForSeconds(0.5f);
        }

        if (NoFreeSpots())
        {
            yield return new WaitForSeconds(1f);
            GameOver(false);
            yield break;
        }

        if (movesLeft > 0)
        {
            yield return new WaitForSeconds(0.5f);
            canPlace = true;
            yield return deck.TryShuffle();
            AddCard();

            if (CanUndo && drawnCards.Count <= 1)
            {
                undoButton.Show();
            }
            
            yield break;
        }
        
        yield return new WaitForSeconds(1.5f);
        GameOver(false);
    }

    public void GameOver(bool filled)
    {
        if (alreadyOver) return;

        Resume();
        alreadyOver = true;
        StartCoroutine(ShowGameOver(filled));
    }

    private IEnumerator ShowGameOver(bool filled)
    {
        if (filled)
        {
            yield return new WaitForSeconds(1f);
            effectCamera.BaseEffect(0.3f);
            EffectManager.AddTextPopup("Full Board Bonus!", MidPoint, 1.5f);
            AudioManager.Instance.PlayEffectAt(13, MidPoint);
            yield return new WaitForSeconds(0.2f);
            AddScore(scoreDisplay.Total, MidPoint, false);
            yield return new WaitForSeconds(2f);         
        }

        if (scoreDisplay.Total > 0)
        {
            scoreManager.SubmitScore(scoreDisplay.Total, level, DailyState.Instance.BoardSuffix);
        }
        
        effectCamera.BaseEffect(0.5f);
        AudioManager.Instance.PlayEffectAt(14, MidPoint, 1f);
        AudioManager.Instance.PlayEffectAt(10, MidPoint, 3f);
        AudioManager.Instance.PlayEffectFromCollection(4, MidPoint);
        gameOverContainer.SetActive(true);
        AudioManager.Instance.TargetPitch = 0;

        yield return new WaitForSeconds(0.2f);

        if (DailyState.Instance.IsDaily)
        {
            dailyResumeButton.Show();
            yield break;
        }
        
        menuButton.Show();
        againButton.Show();
    }

    public void PulseAt(Vector3 pos, bool lines = true)
    {
        var set = lines ? new[] { 0, 2 } : new[] { 0 };
        EffectManager.AddEffects(set, pos);
        effectCamera.BaseEffect(0.2f);
    }
    
    private void ExplodeAt(Vector3 pos)
    {
        EffectManager.AddEffects(new []{ 1, 2, 3 }, pos);
        effectCamera.BaseEffect(0.5f);
        PulseAt(pos, false);
    }

    public IEnumerator SpawnCards(CardType type, List<Tile> tiles, Vector3 lineStart)
    {
        JustTouched = null;
        BehindSpot = null;
        PreviousDirection = Vector2Int.zero;

        foreach (var tile in tiles)
        {
            if (!tile.IsEmpty) continue;
            var card = CreateCard(type, tile.transform.position);
            card.Lock();
            card.RandomizeRotation();
            tile.Set(card);
            HideTarget(card.Tile);
            DrawLines(lineStart, new List<Card>{ card });
            card.Bounce(lineStart - card.transform.position);
        }

        foreach (var tile in tiles)
        {
            yield return skills.PlaceTrigger(tile.Card);
            
            if (tile == targetTile)
            {
                targetReached = true;
            }
        }

        UpdateMoveDisplay();
    }

    private void HideTarget(Tile tile)
    {
        if (tile == targetTile)
        {
            target.gameObject.SetActive(false);   
        }
    }

    private IEnumerator ReachedTarget(Vector3 cardPos)
    {
        AudioManager.Instance.PlayEffectAt(harpSounds.At(soundIndex), cardPos, 0.5f, false);

        var reachedWithFirst = movesLeft == MoveCount - 1;
        
        if (reachedWithFirst)
        {
            EffectManager.AddTextPopup("SPLENDID!", cardPos.RandomOffset(1f) + Vector3.up, 0.7f);
            var doubles = skills.Trigger(Passive.MultiIncreaseAndDecreaseMoves, cardPos);
            AddMulti(cardPos, doubles ? 2 : 1);
        }
        
        soundIndex = reachedWithFirst ? (soundIndex + 1) % 7 : 0;

        yield return new WaitForSeconds(0.2f);
        
        CanUndo = false;

        Grow();
        yield return MoveTarget();

        movesLeft = MoveCount;
        exp++;
        var levelMod =  GetExpSpeed();

        UpdateExpBar();

        if (exp == Mathf.CeilToInt(level * levelMod))
        {
            yield return new WaitForSeconds(0.5f);

            if (alreadyOver) yield break;
            
            var amount = grid.GetEmptyCount() * 10;
            AudioManager.Instance.PlayEffectAt(11, cardPos, 0.7f, false);

            yield return new WaitForSeconds(0.5f);

            AddScore(amount, cardPos);
            scoreDisplay.ResetMulti();

            exp = 0;
            level++;

            yield return new WaitForSeconds(0.5f);
            
            AudioManager.Instance.PlayEffectAt(9, Vector3.zero);

            yield return skills.Present();

            UpdateExpBar();
        }
    }

    private float GetExpSpeed()
    {
        var mods = skills.Get(Passive.ExpSpeedMod).ToList();
        var speed = 1f;
        mods.ForEach(s => speed *= s.amount * 0.01f);
        return speed;
    }

    private void UpdateExpBar()
    {
        var levelMod =  GetExpSpeed();
        var ratio = Mathf.Clamp01(1f * exp / Mathf.CeilToInt(level * levelMod));
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
        var color = movesLeft < 2 ? "#E0CA3C" : "#FBFFFE";
        var move = movesLeft == 1 ? "MOVE" : "MOVES";
        moveCounters[0].text = $"{movesLeft} {move} LEFT";
        moveCounters[1].text = $"<color={color}>{movesLeft}</color> {move} LEFT";
    }

    public void AddMulti(Vector3 pos, int amount = 1)
    {
        scoreDisplay.AddMulti(amount);
        EffectManager.AddTextPopup($"+x{amount}", pos.RandomOffset(0.5f) + Vector3.down, 0.9f);
    }

    public void AddScore(int amount, Vector3 pos, bool useMulti = true)
    {
        AudioManager.Instance.PitchFor(1.1f, 0.7f);
        
        var doubles = justPlaced && skills.Trigger(Passive.ScoreDoubler, justPlaced.GetCardType(), justPlaced.transform.position + Vector3.up);
        var extraMulti = doubles ? 2 : 1;

        var amt = extraMulti * amount;
        var willUseMulti = useMulti && (amount > 0 || !skills.Trigger(Passive.NoNegativeMulti, MidPoint));
        scoreDisplay.Add(amt, willUseMulti);
        var shown = amt * (willUseMulti ? scoreDisplay.Multi : 1);

        if (shown < 0)
        {
            StartCoroutine(skills.Trigger(SkillTrigger.LosePoints));
        }
        
        EffectManager.AddTextPopup(shown.AsScore(), pos.RandomOffset(1f), 1.3f);
        effectCamera.BaseEffect(0.2f);

        if (amt >= 0)
        {
            AudioManager.Instance.PlayEffectFromCollection(5, pos);
            return;
        }
        
        AudioManager.Instance.PlayEffectAt(16, pos, 0.5f);
    }

    public bool IsPlacedAlone()
    {
        if (!justPlaced) return false;
        var pos = justPlaced.Tile.Position;
        return grid.GetNeighbours(pos.x, pos.y).All(g => g.IsWall || g.IsEmpty);
    }

    public IEnumerator DestroyCards(List<Card> cards, Card source)
    {
        CanUndo = false;
        
        var from = source ? source.transform.position : SkyPoint;
        var targets = cards.Where(c => !c.IsDying).OrderBy(c => Vector3.Distance(from, c.transform.position)).ToList();
        DrawLines(from, targets, false, true);
        AudioManager.Instance.PitchFor(0.7f, 0.5f);
        var immortals = targets.Where(c => skills.Has(Passive.Immortal, c.GetCardType())).ToList();
        targets = targets.Except(immortals).ToList();

        targets.ForEach(c => c.ShakeForever());
        yield return new WaitForSeconds(0.3f);
        foreach (var c in targets)
        {
            yield return skills.Trigger(SkillTrigger.Death, c);
            var p = c.transform.position;
            var type = c.GetCardType();
            var tile = c.Tile;
            tile.Clear();
            ExplodeAt(p);
            c.gameObject.SetActive(false);
            explosionSound.Play(p, 0.6f);

            var replaces = skills.GetTriggered(Passive.Replace, type, p, true);
            if (replaces.Any())
            {
                yield return new WaitForSeconds(0.2f);
                yield return SpawnCards(replaces.First().TargetType, new List<Tile> { tile }, hand.position);
            }

            if (source)
            {
                yield return skills.Trigger(SkillTrigger.Kill, source);
            }
            
            yield return new WaitForSeconds(0.2f);
        }

        foreach (var c in immortals)
        {
            yield return new WaitForSeconds(0.2f);
            var pos = c.transform.position;
            skills.Trigger(Passive.Immortal, c.GetCardType(), pos);
            PulseAt(pos);
            c.Pulsate();
            c.Flash();
            yield return new WaitForSeconds(0.2f);
            yield return skills.Trigger(SkillTrigger.DefyDeath, c);
            yield return new WaitForSeconds(0.1f);
        }

        if (source && skills.Trigger(Passive.Revenge, source.transform.position))
        {
            yield return new WaitForSeconds(0.3f);
            yield return DestroyCards(new List<Card> { source }, null);
        }

        yield return new WaitForSeconds(0.3f);
    }

    private void DrawLines(Vector3 from, List<Card> targets, bool lightFlash = false, bool darkFlash = false)
    {
        AudioManager.Instance.PlayEffectFromCollection(4, from);
        var color = new Color(1f, 1f, 1f, 0.75f);
        effectCamera.BaseEffect(0.2f);
        
        targets.ForEach(c =>
        {
            if (lightFlash)
            {
                c.Flash();
            }

            if (darkFlash)
            {
                c.DarkFlash();
            }
            
            lineDrawer.AddThunderLine(from.RandomOffset(0.25f), c.transform.position.RandomOffset(0.25f), color, Random.Range(0.4f, 0.8f), 1f);
        });
    }

    private CardType GetTransformTypeFor(Card card, CardType defaultType)
    {
        var triggered = skills.GetTriggered(Passive.TransformForcer, card.GetCardType(), card.transform.position, true);
        return triggered.Any() ? triggered.First().TargetType : defaultType;
    }

    public IEnumerator TransformCards(List<Card> cards, Skill skill, Vector3 lineStart)
    {
        CanUndo = false;
        
        var targets = cards.Where(c => !c.IsDying).OrderBy(c => Vector3.Distance(lineStart, c.transform.position)).ToList();
        
        DrawLines(lineStart, targets, true);
        AudioManager.Instance.PitchFor(1.3f, 0.5f);

        targets.ForEach(c =>
        {
            c.Shake();
            c.Bounce(lineStart - c.transform.position);
        });
        yield return new WaitForSeconds(0.2f);
        
        foreach (var c in targets)
        {
            var originalType = c.GetCardType();
            c.TransformTo(GetTransformTypeFor(c, skill.ExtraType), skill.title);
            transformSound.Play(c.transform.position);
            DrawLines(lineStart, new List<Card> { c }, true);
            yield return new WaitForSeconds(0.1f);
            yield return skills.Trigger(SkillTrigger.Transform, originalType, c);
            yield return new WaitForSeconds(0.1f);
            yield return skills.PlaceTrigger(c);
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.3f);
    }

    public IEnumerator SpawnAround(Card card, CardType type, int reach)
    {
        var targets = grid.GetNeighboursWithDiagonals(card.Tile.Position.x, card.Tile.Position.y, reach).Where(t => t.IsEmpty).ToList();
        yield return SpawnCards(type, targets.Select(t => t.Value).ToList(), card.transform.position);
    }

    public IEnumerator SpawnOnSides(Card card, CardType type)
    {
        yield return SpawnCards(type, GetSides(card).ToList(), card.transform.position);
    }
    
    public IEnumerator SpawnOnNeighbours(Card card, CardType type)
    {
        var targets = grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y).Where(t => t.IsEmpty).ToList();
        yield return SpawnCards(type, targets.Select(t => t.Value).ToList(), card.transform.position);
    }

    public bool HasEmptySides(Card card)
    {
        return GetSides(card).Any(t => t.IsEmpty);
    }

    private IEnumerable<Tile> GetSides(Card card)
    {
        var p = card.Tile.Position;
        var left = grid.Get(p.x + PreviousDirection.y, p.y + PreviousDirection.x);
        var right = grid.Get(p.x - PreviousDirection.y, p.y - PreviousDirection.x);
        return new[] { left, right }.Where(t => t.IsEmpty).Select(t => t.Value);
    }

    private bool IsOnSide(Tile tile, Tile behind)
    {
        if (SlideLength == 0) return true;
        if (!behind) return false;
        return tile.Position.x != behind.Position.x && tile.Position.y != behind.Position.y;
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
            .Where(s => s.IsOccupied && s.Value.Card != card)
            .Any(t => TileMatchesSkill(t, skill));
    }
    
    public bool HasNeighbours(Card card, Skill skill)
    {
        return grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y)
            .Where(s => s.IsOccupied && s.Value.Card != card)
            .Any(t => TileMatchesSkill(t, skill));
    }
    
    public IEnumerable<Card> GetNeighbours(Tile tile, bool diagonals)
    {
        var spots = diagonals ?
            grid.GetNeighboursWithDiagonals(tile.Position.x, tile.Position.y).Where(t => t.IsOccupied) :
            grid.GetNeighbours(tile.Position.x, tile.Position.y).Where(t => t.IsOccupied);
        
        return spots.Where(s => s.Value != tile).Select(s => s.Value.Card);
    }
    
    public IEnumerable<Card> GetNeighbours(Card card, Skill skill, bool diagonals)
    {
        var spots = diagonals ?
            grid.GetNeighboursWithDiagonals(card.Tile.Position.x, card.Tile.Position.y).Where(t => TileMatchesSkill(t, skill)) :
            grid.GetNeighbours(card.Tile.Position.x, card.Tile.Position.y).Where(t => TileMatchesSkill(t, skill));
        
        return spots.Where(s => s.Value.Card != card).Select(s => s.Value.Card);
    }

    public IEnumerator ScoreFor(Card card, Skill skill, bool doDiagonals)
    {
        yield return new WaitForSeconds(0.3f);
        
        var neighbours = GetNeighbours(card, skill, doDiagonals).ToList();
        var p = card.transform.position;

        foreach (var c in neighbours)
        {
            ScoreFor(c, skill.amount, p);
            yield return new WaitForSeconds(0.15f);
        }
    }

    public void ScoreFor(Card c, int amount, Vector3 p, bool pulse = true)
    {
        AddScore(amount, c.transform.position);
        DrawLines(p, new List<Card> { c });
        if (pulse)
        {
            PulseAt(p);   
        }
        c.Pulsate();
        c.Flash();
    }

    public IEnumerator MultiFor(Card card, Skill skill, bool doDiagonals)
    {
        yield return new WaitForSeconds(0.3f);
        
        var neighbours = GetNeighbours(card, skill, doDiagonals).ToList();
        var p = card.transform.position;

        foreach (var c in neighbours)
        {
            AddMulti(c.transform.position, skill.amount);
            DrawLines(p, new List<Card>{ c });
            PulseAt(p);
            c.Pulsate();
            c.Flash();
            yield return new WaitForSeconds(0.15f);
        }
    }

    private bool TileMatchesSkill(InfiniteGrid<Tile>.GridSpot spot, Skill skill)
    {
        return spot.IsOccupied && (!skill.HasTargetType || spot.Value.Contains(skill.TargetType));
    }

    public IEnumerator SpawnBehind(CardType type, Vector3 lineStart)
    {
        yield return SpawnCards(type, new List<Tile>{ BehindSpot }, lineStart);
    }

    public void AddToDeck(CardType type, int amount)
    {
        deck.AddToTop(type, amount);
    }

    public void HideCardPreview()
    {
        undoButton.Hide();
        tutorial.Hide();
        cardPreview.Hide();
        tooltipper.Clear();
    }

    public void ShowPreview(CardType type)
    {
        skills.UnMarkSkills();
        cardPreview.Show(type);
        skills.MarkSkills(type);
    }

    public List<Card> GetAll(CardType type)
    {
        var options = grid.GetAll().Where(s => s.IsOccupied && s.Value.Contains(type)).ToList();
        return options.Select(s => s.Value.Card).ToList();
    }

    public List<Card> GetClosest(Card card, CardType type, int amount)
    {
        var options = grid.GetAll().Where(s => s.IsOccupied && s.Value.Contains(type)).ToList();
        return options.OrderBy(o => Vector2Int.Distance(card.Tile.Position, o.Position))
            .Take(amount)
            .Select(s => s.Value.Card)
            .ToList();
    }

    public void DoubleScore()
    {
        justPlaced = null;
        AddScore(scoreDisplay.Total, Vector3.zero, false);
    }

    private bool IsSurrounded(Tile tile)
    {
        return GetNeighbours(tile, false).Count() == 4;
    }
    
    private bool IsAlmostSurrounded(Tile tile, Skill skill)
    {
        var neighbours = GetNeighbours(tile, false).ToList();
        return skills.HasExtender(skill) && 
               neighbours.Count == 3 && 
               grid.GetNeighbours(tile.Position.x, tile.Position.y).All(n => n.IsOccupied || !n.IsWall && GetNeighbours(n.Value, false).Count() >= 3);
    }

    public List<Tile> GetHoles(Skill skill)
    {
        return grid.GetAll()
            .Where(s => s.IsEmpty && (IsSurrounded(s.Value) || IsAlmostSurrounded(s.Value, skill)))
            .Select(s => s.Value)
            .ToList();
    }

    public bool HasHoles(Skill skill)
    {
        return GetHoles(skill).Any();
    }

    public bool CanSlideTowardsTarget(Card card)
    {
        var tp = targetTile.Position;
        var cp = card.Tile.Position;
        var dir = tp - cp;
        var isAligned = dir.x == 0 || dir.y == 0;
        return isAligned && dir.magnitude >= 1;
    }

    public IEnumerator SlideTowardsTarget(Card card)
    {
        var tp = targetTile.Position;
        var cp = card.Tile.Position;
        var dir = tp - cp;
        var isAligned = dir.x == 0 || dir.y == 0;

        if (!isAligned || dir.magnitude < 1)
        {
            card.ClearVisits();
            yield break;
        }
        
        var t = card.transform;
        var slideTarget = GetSlideTarget(card, cp.x, cp.y, dir / Mathf.RoundToInt(dir.magnitude));
        
        var sp = slideTarget.Value.Position;
        
        if ((sp - cp).magnitude < 1 || card.HasVisited(sp))
        {
            card.ClearVisits();
            yield break;
        }

        targetReached = false;
        
        var pos = Scale(slideTarget.AsVector3);
        var duration = 0.05f * Vector3.Distance(t.position, pos);
        yield return new WaitForSeconds(0.1f);
        Tweener.MoveToBounceOut(t, pos, duration);
        
        SlideLength = Mathf.RoundToInt(Vector2Int.Distance(cp, slideTarget.Position));
        
        card.Tile.Clear();
        slideTarget.Value.Set(card);
        card.MarkVisit();
        
        card.Bounce(dir);

        PulseAt(pos);
        placeSound.Play(pos);

        yield return new WaitForSeconds(duration);
        
        HideTarget(card.Tile);
        AudioManager.Instance.PlayEffectFromCollection(2, pos);

        yield return skills.PlaceTrigger(card, true);
        
        if (targetReached || slideTarget.Value == targetTile)
        {
            card.ClearVisits();
            yield return new WaitForSeconds(0.3f);
            yield return ReachedTarget(pos);
        }
    }

    public List<Card> GetColumn(Card card)
    {
        return grid.GetColumn(card.Tile.Position.x)
            .Select(s => s.Value.Card)
            .Where(c => c != card)
            .ToList();
    }
    
    public List<Card> GetRow(Card card)
    {
        return grid.GetRow(card.Tile.Position.y)
            .Select(s => s.Value.Card)
            .Where(c => c != card)
            .ToList();
    }

    public void IncreaseHandSize()
    {
        if (deck.IsInitialized)
        {
            AddCard();   
        }

        var pos = hand.position + Vector3.right * handSize + Vector3.down * (0.2f * handSize);
        var spot = Instantiate(handSpotPrefab, hand);
        spot.position = pos;
        
        handSize++;
    }

    public void GambleMulti(Vector3 pos)
    {
        if (Random.value < 0.5f)
        {
            AddMulti(pos, scoreDisplay.Multi);
            return;
        }
        
        scoreDisplay.ResetMulti();
        EffectManager.AddTextPopup("BAD LUCK!", pos.RandomOffset(0.5f) + Vector3.down, 0.9f);
    }

    public void PlayPickSound(Vector3 p)
    {
        placeSound.Play(p);
    }
}