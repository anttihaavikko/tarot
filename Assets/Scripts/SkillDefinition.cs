using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnttiStarterKit.Extensions;
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
    public List<CardType> firstCards, secondCards;
    public bool repeatable;
    public bool notRepeatableForOthers;
    public int amount;
    public bool useSecondImage;
    public string cancelShout;
    public float cancelDelay;
    public bool canTargetSame;
    public Sprite iconSprite;
    public float triggerDelay;
    public SkillDefinition requirement;

    private CardType firstCard, secondCard;

    public SkillIcon Icon { get; private set; }

    public CardType MainType => firstCard;
    public CardType ImageType => useSecondImage ? secondCard : firstCard;
    public CardType TargetType => secondCard;
    public bool HasTargetType => secondCards.Any();

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
        if (requirement)
        {
            sb.Replace("[R]", requirement.GetSkill().title);   
        }
        sb.Replace("(", useColors ? "<color=#E0CA3C>" : "");
        sb.Replace(")", useColors ? "</color>" : "");
        return sb.ToString();
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
        return trigger == skillTrigger && firstCard == type;
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
    
    public CardType GetTargetOrRandomType()
    {
        return HasTargetType ? TargetType : Card.GetRandomType();
    }
}

public enum SkillTrigger
{
    None,
    Place,
    Death
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
    TransformColumn
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
    Revenge
}