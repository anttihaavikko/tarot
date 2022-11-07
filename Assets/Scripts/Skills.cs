using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using AnttiStarterKit.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

public class Skills : MonoBehaviour
{
    [SerializeField] private List<SkillDefinition> skillDefinitions;
    [SerializeField] private Board board;
    [SerializeField] private Transform skillContainer;
    [SerializeField] private SkillIcon iconPrefab;
    [SerializeField] private List<SkillPick> skillPicks;

    private List<Skill> skillPool;
    private List<Skill> skills = new();

    private bool picking;

    private void Awake()
    {
        skillPool = skillDefinitions.Select(s => s.GetSkill()).ToList();
    }

    private void Update()
    {
        if (DevKey.Down(KeyCode.S))
        {
            Add(Take(1)[0]);
        }

        if (DevKey.Down(KeyCode.E))
        {
            var options = Take(skillPicks.Count);
            skillPicks.ForEach(s => s.Setup(options[skillPicks.IndexOf(s)]));
        }
    }

    public IEnumerator Present()
    {
        var options = Take(skillPicks.Count);
        skillPicks.ForEach(s => s.Setup(options[skillPicks.IndexOf(s)]));
        picking = true;

        while (picking) yield return null;
    }

    public void Pick()
    {
        picking = false;
        skillPicks.ForEach(s => s.Hide());
    }

    public void Add(Skill source)
    {
        var skill = new Skill(source);
        skills.Add(skill);

        var icon = Instantiate(iconPrefab, skillContainer);
        icon.Setup(skill);
        
        skill.SetIcon(icon);

        if (!skill.repeatable && skills.Count(s => s.title == skill.title) >= skill.firstCards.Count)
        {
            Remove(source);
        }
    }

    private void Remove(Skill skill)
    {
        skillPool.Remove(skill);
    }

    public List<Skill> Take(int amount)
    {
        skillPool.ForEach(s => s.Randomize(skills));
        return skillPool.OrderBy(s => Random.value).Take(amount).ToList();
    }

    public IEnumerable<CardType> GetTypesFor(Card card)
    {
        var type = card.GetCardType();
        return skills.Where(s => s.Matches(Passive.Mimic, type)).Select(s => s.TargetType).Concat(new [] { type });
    }

    public IEnumerator Trigger(SkillTrigger trigger, Card card)
    {
        foreach (var s in skills.Where(s => GetTypesFor(card).Any(t => s.Matches(trigger, t))).ToList())
        {
            yield return Act(s, card.transform.position, card);
        }
    }

    public IEnumerator Trigger(SkillTrigger trigger)
    {
        foreach (var s in skills.Where(s => s.Matches(trigger)).ToList())
        {
            yield return Act(s, Vector3.zero);
        }
    }

    private bool ShouldCancel(Skill skill, Card card)
    {
        return skill.effect switch
        {
            SkillEffect.None => false,
            SkillEffect.AddMultiplierIfAlone => !board.IsPlacedAlone(),
            SkillEffect.AddScoreIfAlone => !board.IsPlacedAlone(),
            SkillEffect.DestroyTouching => !board.JustTouched,
            SkillEffect.AddMultiForSlideLength => board.SlideLength < 1,
            SkillEffect.AddMultiplier => false,
            SkillEffect.AddScore => false,
            SkillEffect.SpawnAround => !board.HasEmptyNeighboursWithDiagonals(card),
            SkillEffect.LevelUp => false,
            SkillEffect.DestroySurrounding => !board.HasNeighboursWithDiagonals(card),
            SkillEffect.DestroyNeighbours => !board.HasNeighbours(card),
            SkillEffect.SpawnNeighbours => !board.HasEmptyNeighbours(card),
            _ => false
        };
    }

    public IEnumerator DoEffect(Skill skill, Card card)
    {
        switch (skill.effect)
        {
            case SkillEffect.None:
                break;
            case SkillEffect.AddScore:
            case SkillEffect.AddScoreIfAlone:
                board.AddScore(skill.amount, card.transform.position);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.DestroyTouching:
                yield return board.DestroyCards(new List<Card> { board.JustTouched });
                break;
            case SkillEffect.AddMultiForSlideLength:
                board.AddMulti(board.SlideLength);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.AddMultiplier:
            case SkillEffect.AddMultiplierIfAlone:
                board.AddMulti(skill.amount);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.SpawnAround:
                yield return new WaitForSeconds(0.4f);
                yield return board.SpawnAround(card, skill.TargetType);
                yield return new WaitForSeconds(0.25f);
                break;
            case SkillEffect.LevelUp:
                yield return new WaitForSeconds(0.5f);
                yield return Present();
                break;
            case SkillEffect.DestroySurrounding:
                yield return board.DestroyCards(board.GetNeighbours(card, true).ToList());
                break;
            case SkillEffect.DestroyNeighbours:
                yield return board.DestroyCards(board.GetNeighbours(card, false).ToList());
                break;
            case SkillEffect.SpawnNeighbours:
                yield return new WaitForSeconds(0.4f);
                yield return board.SpawnOnNeighbours(card, skill.TargetType);
                yield return new WaitForSeconds(0.25f);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IEnumerator Act(Skill skill, Vector3 pos, Card card = null)
    {
        var p = pos.RandomOffset(1f);

        if (ShouldCancel(skill, card))
        {
            if (!string.IsNullOrEmpty(skill.cancelShout))
            {
                EffectManager.AddTextPopup(skill.cancelShout, p, 0.8f);
            }
            
            if (skill.cancelDelay > 0)
            {
                yield return new WaitForSeconds(skill.cancelDelay);
            }
            
            yield break;
        }

        EffectManager.AddTextPopup(skill.title, p, 0.8f);
        skill.Trigger();

        yield return DoEffect(skill, card);
    }

    public IEnumerable<Skill> Get(Passive passive, CardType type)
    {
        return skills.Where(s => s.Matches(passive, type));
    }

    public IEnumerable<Skill> Get(Passive passive)
    {
        return skills.Where(s => s.Matches(passive));
    }
}