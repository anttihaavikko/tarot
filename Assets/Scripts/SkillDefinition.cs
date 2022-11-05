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
}

[Serializable]
public class Skill
{
    public string title;
    [TextArea] public string description;
    public Passive passive;
    public List<CardType> firstCards, secondCards;
    public bool repeatable;
    public int amount;
    
    private CardType firstCard, secondCard;

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
    }

    public void Randomize(IEnumerable<Skill> skills)
    {
        var existing = skills.Where(s => s.title == title).Select(s => s.firstCard).ToList();
        firstCard = firstCards.Where(s => repeatable || !existing.Contains(s)).ToList().Random();
        secondCard = secondCards.Random();
    }

    public string GetDescription()
    {
        var sb = new StringBuilder(description);
        sb.Replace("[1]", Card.GetShortName(firstCard));
        sb.Replace("[2]", Card.GetShortName(secondCard));
        return sb.ToString();
    }
}

public enum SkillTrigger
{
    None,
    Place
}

public enum SkillEffect
{
    None,
    AddMultiplierForEmptyNeighbours
}

public enum Passive
{
    None,
    FurtherExtend
}