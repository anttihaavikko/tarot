using System;
using System.Collections.Generic;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Managers;
using AnttiStarterKit.ScriptableObjects;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEngine;
using AnttiStarterKit.Extensions;
using Random = UnityEngine.Random;

public class Card : MonoBehaviour
{
    [SerializeField] private TMP_Text title, number;
    [SerializeField] private SpriteCollection cardSprites;
    [SerializeField] private ColorCollection cardColors, patternColors, radialColors;
    [SerializeField] private SpriteRenderer sprite, bg, pattern, radial;
    [SerializeField] private Draggable draggable;
    [SerializeField] private Pulsater pulsater;
    [SerializeField] private Material flashSMaterial, normalMaterial, darkFlashMaterial;
    [SerializeField] private SoundCollection cardSounds;
    [SerializeField] private Transform shadow;
    [SerializeField] private Transform wrap;

    private Board board;
    private Shaker shaker;

    private CardType type;
    private List<Vector2Int> visited = new ();

    private Vector3 originalSize;
    
    public Tile Tile { get; set; }
    public bool IsDying { get; private set; }

    public bool IsDragging => draggable.IsDragging;

    private void Awake()
    {
        shaker = GetComponent<Shaker>();
        draggable.preview += Preview;
        draggable.dropped += Place;
        draggable.pick += Picked;
        radial.transform.Rotate(new Vector3(0, 0, Random.value * 360));
        pattern.flipX = Random.value < 0.5f;
        pattern.flipY = Random.value < 0.5f;
    }

    public void DarkFlash()
    {
        DoFlash(darkFlashMaterial);
    }
    
    public void Flash()
    {
        DoFlash(flashSMaterial);
    }

    private void DoFlash(Material mat)
    {
        sprite.material = pattern.material = mat;
        this.StartCoroutine(() => sprite.material = pattern.material = normalMaterial, 0.2f);
    }

    private void ResetPicked()
    {
        shadow.gameObject.SetActive(false);
        transform.localScale = originalSize;
        RandomizeRotation();
    }

    public void RandomizeRotation()
    {
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
    }

    private void Picked()
    {
        shadow.gameObject.SetActive(true);
        originalSize = transform.localScale;
        transform.localScale *= 1.1f;
        RandomizeRotation();
        board.HideCardPreview();
        board.PlayPickSound(transform.position);
    }

    public void Placed()
    {
        ResetPicked();
        draggable.pick -= Picked;
    }

    private void Place(Draggable d)
    {
        if (board)
        {
            board.Slide(this);
        }
    }

    private void Preview(Draggable d)
    {
        if (board)
        {
            board.Preview(this);
            var p = transform.position;
            var diff = Vector3.MoveTowards(p, Vector3.zero, 0.2f);
            shadow.position = p + diff.normalized * 0.2f;
        }
    }

    public void Init(Board b, CardType t)
    {
        board = b;
        Init(t);
    }

    private void Init(CardType t)
    {
        type = t;
        gameObject.name = title.text = GetName();
        sprite.sprite = cardSprites.Get((int)t);
        bg.color = cardColors.Get((int)t);
        pattern.color = patternColors.Get((int)t);
        radial.color = radialColors.Get((int)t);
        number.text = GetNumber();
    }

    public void Lock()
    {
        draggable.NormalizeSortOrder();
        draggable.enabled = false;
    }

    public string GetName()
    {
        return GetName(type);
    }

    public void Announce()
    {
        var sound = cardSounds.At((int)type);
        AudioManager.Instance.PlayEffectAt(sound, transform.position);
    }

    public static string GetName(CardType type)
    {
        return type switch
        {
            CardType.Fool => "The Fool",
            CardType.Magician => "The Magician",
            CardType.HighPriestess => "The High Priestess",
            CardType.Empress => "The Empress",
            CardType.Emperor => "The Emperor",
            CardType.Hierophant => "The Hierophant",
            CardType.Lovers => "The Lovers",
            CardType.Chariot => "The Chariot",
            CardType.Strength => "The Strength",
            CardType.Hermit => "The Hermit",
            CardType.WheelOfFortune => "Wheel of Fortune",
            CardType.Justice => "Justice",
            CardType.HangedMan => "The Hanged Man",
            CardType.Death => "Death",
            CardType.Temperance => "Temperance",
            CardType.Devil => "The Devil",
            CardType.Tower => "The Tower",
            CardType.Star => "The Star",
            CardType.Moon => "The Moon",
            CardType.Sun => "The Sun",
            CardType.Judgement => "Judgement",
            CardType.World => "The World",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    
    public static string GetShortName(CardType type)
    {
        return type switch
        {
            CardType.Fool => "Fool",
            CardType.Magician => "Magician",
            CardType.HighPriestess => "High Priestess",
            CardType.Empress => "Empress",
            CardType.Emperor => "Emperor",
            CardType.Hierophant => "Hierophant",
            CardType.Lovers => "Lovers",
            CardType.Chariot => "Chariot",
            CardType.Strength => "Strength",
            CardType.Hermit => "Hermit",
            CardType.WheelOfFortune => "Wheel of Fortune",
            CardType.Justice => "Justice",
            CardType.HangedMan => "Hanged Man",
            CardType.Death => "Death",
            CardType.Temperance => "Temperance",
            CardType.Devil => "Devil",
            CardType.Tower => "Tower",
            CardType.Star => "Star",
            CardType.Moon => "Moon",
            CardType.Sun => "Sun",
            CardType.Judgement => "Judgement",
            CardType.World => "World",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    
    public string GetNumber()
    {
        return type switch
        {
            CardType.Fool => "0",
            CardType.Magician => "I",
            CardType.HighPriestess => "II",
            CardType.Empress => "III",
            CardType.Emperor => "IV",
            CardType.Hierophant => "V",
            CardType.Lovers => "VI",
            CardType.Chariot => "VII",
            CardType.Strength => "VII",
            CardType.Hermit => "IX",
            CardType.WheelOfFortune => "X",
            CardType.Justice => "XI",
            CardType.HangedMan => "XII",
            CardType.Death => "XIII",
            CardType.Temperance => "XIV",
            CardType.Devil => "XV",
            CardType.Tower => "XVI",
            CardType.Star => "XVII",
            CardType.Moon => "XVIII",
            CardType.Sun => "XIX",
            CardType.Judgement => "XX",
            CardType.World => "XXI",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public CardType GetCardType()
    {
        return type;
    }

    public void Shake()
    {
        shaker.Shake();
    }

    public void ShakeForever()
    {
        IsDying = true;
        shaker.ShakeForever();
    }

    public void TransformTo(CardType target, string message = null)
    {
        var pos = transform.position;
        
        if (!string.IsNullOrEmpty(message))
        {
            EffectManager.AddTextPopup(message, pos, 0.8f);
        }
        
        Init(target);
        board.PulseAt(transform.position);
        
        Flash();
        RandomizeRotation();
        // Announce();
    }

    public void ReturnToHand()
    {
        Bounce(draggable.ReturnPos - transform.position);
        ResetPicked();
        draggable.Return();
    }

    public static CardType GetRandomType()
    {
        return EnumUtils.Random<CardType>();
    }

    public void MarkVisit()
    {
        visited.Add(Tile.Position);
    }

    public void ClearVisits()
    {
        visited.Clear();
    }

    public bool HasVisited(Vector2Int pos)
    {
        return visited.Contains(pos);
    }

    public void Pulsate()
    {
        pulsater.Pulsate();
    }

    public void Bounce(Vector2Int dir)
    {
        var d = new Vector3(dir.x, dir.y, 0);
        Bounce(d);
    }

    public void Bounce(Vector3 dir)
    {
        var d = dir .normalized * 0.1f;
        var target = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector3(0.8f, 1.2f, 1f) : new Vector3(1.2f, 0.8f, 1f);

        Tweener.MoveLocalToBounceOut(wrap, d, 0.2f);
        Tweener.ScaleToBounceOut(wrap, target, 0.2f);
        this.StartCoroutine(() =>
        {
            Tweener.ScaleToQuad(wrap, Vector3.one, 0.2f);
            Tweener.MoveLocalToQuad(wrap, Vector3.zero, 0.2f);
        }, 0.2f);
    }
}

public enum CardType
{
    Fool,
    Magician,
    HighPriestess,
    Empress,
    Emperor,
    Hierophant,
    Lovers,
    Chariot,
    Strength,
    Hermit,
    WheelOfFortune,
    Justice,
    HangedMan,
    Death,
    Temperance,
    Devil,
    Tower,
    Star,
    Moon,
    Sun,
    Judgement,
    World,
}