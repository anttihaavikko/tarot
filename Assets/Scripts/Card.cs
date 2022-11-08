using System;
using AnttiStarterKit.Animations;
using AnttiStarterKit.ScriptableObjects;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Card : MonoBehaviour
{
    [SerializeField] private TMP_Text title, number;
    [SerializeField] private SpriteCollection cardSprites;
    [SerializeField] private ColorCollection cardColors, patternColors;
    [SerializeField] private SpriteRenderer sprite, bg, pattern, radial;

    private Board board;
    private Draggable draggable;
    private Shaker shaker;

    private CardType type;
    
    public Tile Tile { get; set; }
    public bool IsDying { get; private set; }

    private void Awake()
    {
        shaker = GetComponent<Shaker>();
        draggable = GetComponent<Draggable>();
        draggable.preview += Preview;
        draggable.dropped += Place;
        draggable.pick += HidePreview;
        radial.transform.Rotate(new Vector3(0, 0, Random.value * 360));
        pattern.flipX = Random.value < 0.5f;
        pattern.flipY = Random.value < 0.5f;
    }

    private void HidePreview()
    {
        board.HideCardPreview();
    }

    public void Placed()
    {
        draggable.pick -= HidePreview;
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
        title.text = GetName();
        sprite.sprite = cardSprites.Get((int)t);
        bg.color = cardColors.Get((int)t);
        pattern.color = patternColors.Get((int)t);
        number.text = GetNumber();
    }

    public void Lock()
    {
        draggable.enabled = false;
        draggable.NormalizeSortOrder();
    }

    public string GetName()
    {
        return GetName(type);
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

    public void TransformTo(CardType target)
    {
        Init(target);
    }

    public void ReturnToHand()
    {
        draggable.Return();
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