using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using AnttiStarterKit.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "Skill", order = 0)]
public class SkillDefinition : ScriptableObject
{
    [SerializeField] private Skill skill;

    public Skill GetSkill()
    {
        return new Skill(skill);
    }

    public bool Is(Skill other)
    {
        return skill.title == other.title;
    }
}

[Serializable]
public class Skill
{
    public string title;
    [TextArea] public string description;
    public Passive passive;
    public SkillTrigger trigger;
    public SkillCondition condition;
    public SkillEffect effect;
    public List<CardType> firstCards, secondCards, extraTypes;
    public Sprite iconSprite;
    public float triggerDelay, announceDelay;
    public SkillDefinition requirement;
    public AudioClip sound;
    
    [Space]
    public int amount;
    
    [Space]
    public string cancelShout;
    public float cancelDelay;
    public AudioClip cancelSound;
    
    [Space]
    public bool repeatable;
    public bool notRepeatableForOthers;
    public bool useSecondImage;
    public bool canTargetSame;
    public bool anyTriggers;
    public bool isNotMarked;
    public bool usesRequirementForMarking;
    public bool canNotFizzle;
    public bool skipAnnounce;
    public bool manualPlaceOnly;
    public bool skipMulti;

    private CardType firstCard, secondCard;

    public SkillIcon Icon { get; private set; }

    public CardType MainType => firstCard;
    public CardType ImageType => useSecondImage ? secondCard : firstCard;
    public CardType TargetType => secondCard;
    public bool HasTargetType => secondCards.Any();

    public CardType ExtraType => extraTypes.Any() ? extraTypes.Random() : EnumUtils.Random<CardType>();

    public Skill(Skill source)
    {
        title = source.title;
        description = source.description;
        passive = source.passive;
        firstCards = source.firstCards.ToList();
        secondCards = source.secondCards.ToList();
        amount = source.amount;
        repeatable = source.repeatable;
        firstCard = source.firstCard;
        secondCard = source.secondCard;
        trigger = source.trigger;
        effect = source.effect;
        useSecondImage = source.useSecondImage;
        cancelShout = source.cancelShout;
        cancelDelay = source.cancelDelay;
        canTargetSame = source.canTargetSame;
        notRepeatableForOthers = source.notRepeatableForOthers;
        iconSprite = source.iconSprite;
        condition = source.condition;
        triggerDelay = source.triggerDelay;
        requirement = source.requirement;
        anyTriggers = source.anyTriggers;
        isNotMarked = source.isNotMarked;
        usesRequirementForMarking = source.usesRequirementForMarking;
        canNotFizzle = source.canNotFizzle;
        sound = source.sound;
        cancelSound = source.cancelSound;
        skipAnnounce = source.skipAnnounce;
        extraTypes = source.extraTypes.ToList();
        manualPlaceOnly = source.manualPlaceOnly;
        announceDelay = source.announceDelay;
        skipMulti = source.skipMulti;
    }

    public void Randomize(IEnumerable<Skill> skills)
    {
        var existing = skills.Where(s => s.title == title).Select(s => s.firstCard).ToList();
        firstCard = firstCards.Where(s => repeatable || !existing.Contains(s)).ToList().Random();
        secondCard = secondCards.Where(s => canTargetSame || s != firstCard).ToList().Random();
    }

    public string GetDescription(bool useColors = true)
    {
        var sb = new StringBuilder(description);
        sb.Replace("[1]", Card.GetShortName(firstCard));
        sb.Replace("[2]", Card.GetShortName(secondCard));
        sb.Replace("[11]", Card.GetName(firstCard));
        sb.Replace("[22]", Card.GetName(secondCard));
        sb.Replace("[X]", Mathf.Abs(amount).ToString());
        if (requirement)
        {
            sb.Replace("[R]", requirement.GetSkill().title);   
        }
        sb.Replace("(", useColors ? "<color=#E0CA3C>" : "");
        sb.Replace(")", useColors ? "</color>" : "");
        return sb.ToString();
    }

    public bool MatchesForSecondaryMarking(CardType type)
    {
        return !isNotMarked && HasTargetType && TargetType == type;
    }

    public bool MatchesForMarking(List<CardType> types, Skills skills)
    {
        return !isNotMarked && skills.GetActualType(this).Any(types.Contains);
    }
    
    public bool Matches(CardType type)
    {
        return firstCard == type;
    }
    
    public bool Matches(Passive pas, CardType type)
    {
        return passive == pas && firstCard == type;
    }
    
    public bool Matches(Passive pas, List<CardType> types)
    {
        return passive == pas && types.Contains(firstCard);
    }
    
    public bool Matches(Passive pas)
    {
        return passive == pas;
    }

    public bool Matches(SkillTrigger skillTrigger, CardType type)
    {
        return trigger == skillTrigger && (firstCard == type || anyTriggers);
    }
    
    public bool Matches(SkillTrigger skillTrigger)
    {
        return trigger == skillTrigger;
    }

    public bool Matches(SkillDefinition skill)
    {
        return skill.Is(this);
    }

    public void Trigger()
    {
        if (Icon)
        {
            Icon.Pulsate();
        }
    }

    public void SetIcon(SkillIcon i)
    {
        Icon = i;
    }

    public void Announce(Vector3 position, bool force = false)
    {
        if (skipAnnounce && !force) return;
        EffectManager.AddTextPopup(title, position.RandomOffset(1f), 0.8f);
        if (!sound) return;
        AudioManager.Instance.PlayEffectAt(sound, position);
    }
}

public enum SkillTrigger
{
    None,
    Place,
    Death,
    DefyDeath,
    FillGap,
    Transform,
    AddSkill,
    Fizzle,
    Kill,
    TargetMove,
    Timer,
    LosePoints
}

public enum SkillCondition
{
    None,
    IsAlone,
    IsNotAlone
}

public enum SkillEffect
{
    None,
    DestroyTouching,
    AddMultiForSlideLength,
    AddMultiplier,
    SpawnAround,
    LevelUp,
    DestroySurrounding,
    DestroyNeighbours,
    SpawnNeighbours,
    AddScore,
    SpawnBehind,
    AddToDeck,
    DestroyClosest,
    MoveTarget,
    TransformSurrounding,
    TransformNeighbours,
    TransformTouching,
    FillHoles,
    SlideTowardsTarget,
    DestroyRow,
    DestroyColumn,
    TransformRow,
    TransformColumn,
    DestroyAll,
    ScoreForNeighbours,
    ScoreForNeighboursNoDiagonals,
    SpawnOnSides,
    GambleMulti,
    MultiForNeighbours,
    AddScoreForEach
}

public enum Passive
{
    None,
    FurtherExtend,
    Mimic,
    TransformOnDraw,
    AddMove,
    ScoreDoubler,
    MultiIncreaseAndDecreaseMoves,
    DoubleScore,
    Extender,
    Revenge,
    Immortal,
    HandSize,
    MultiDrawShuffleReset,
    Replace,
    FreeMove,
    IncreaseSize,
    NoNegativeMulti,
    Reroll,
    StopsOnTarget,
    TransformForcer,
    GambleTransform,
    ExpSpeedMod
}