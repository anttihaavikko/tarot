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
    private bool picking;
    private readonly List<Skill> skills = new();

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

        if (skill.Matches(Passive.DoubleScore))
        {
            board.DoubleScore();
        }

        var doneRepeating = skills.Count(s => s.title == skill.title) >= skill.firstCards.Count;
        if (!skill.repeatable && (skill.notRepeatableForOthers || doneRepeating))
        {
            Remove(source);
        }
    }

    private void Remove(Skill skill)
    {
        skillPool.Remove(skill);
    }

    public List<Skill> All()
    {
        return skillPool.ToList();
    }

    private List<Skill> Take(int amount)
    {
        skillPool.ForEach(s => s.Randomize(skills));
        return skillPool.OrderBy(s => Random.value).Where(CanObtain).Take(amount).ToList();
    }

    private bool CanObtain(Skill skill)
    {
        return !skill.requirement || skills.Any(s => s.Matches(skill.requirement));
    }

    private IEnumerable<CardType> GetTypesFor(Card card)
    {
        var type = card.GetCardType();
        return GetTypesFor(type);
    }

    private IEnumerable<CardType> GetTypesFor(CardType type)
    {
        return skills.Where(s => s.Matches(Passive.Mimic, type)).Select(s => s.TargetType).Concat(new[] { type });
    }
    
    public bool Trigger(Passive passive, Vector3 pos)
    {
        var triggered = Get(passive).ToList();

        triggered.ForEach(skill =>
        {
            EffectManager.AddTextPopup(skill.title, pos.RandomOffset(1f), 0.8f);
            skill.Trigger();
        });
        
        return triggered.Any();
    }

    public bool Trigger(Passive passive, CardType type, Vector3 pos)
    {
        var types = GetTypesFor(type).ToList();
        var triggered = Get(passive, types).ToList();

        triggered.ForEach(skill =>
        {
            EffectManager.AddTextPopup(skill.title, pos.RandomOffset(1f), 0.8f);
            skill.Trigger();
        });
        
        return triggered.Any();
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

    private bool FailsCondition(Skill skill, Card card)
    {
        return skill.condition switch
        {
            SkillCondition.None => false,
            SkillCondition.IsAlone => !board.IsPlacedAlone(),
            SkillCondition.IsNotAlone => board.IsPlacedAlone(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool ShouldCancel(Skill skill, Card card)
    {
        switch (skill.effect)
        {
            case SkillEffect.None:
                return false;
            case SkillEffect.DestroyTouching:
                return !board.JustTouched;
            case SkillEffect.AddMultiForSlideLength:
                return board.SlideLength < 1;
            case SkillEffect.AddMultiplier:
            case SkillEffect.AddScore:
                return false;
            case SkillEffect.SpawnAround:
                return !board.HasEmptyNeighboursWithDiagonals(card);
            case SkillEffect.LevelUp:
                return false;
            case SkillEffect.DestroySurrounding:
                return !board.HasNeighboursWithDiagonals(card, skill);
            case SkillEffect.DestroyNeighbours:
                return !board.HasNeighbours(card, skill);
            case SkillEffect.SpawnNeighbours:
                return !board.HasEmptyNeighbours(card);
            case SkillEffect.SpawnBehind:
                return !board.BehindSpot;
            case SkillEffect.DestroyClosest:
                return !board.GetClosest(card, skill.TargetType);
            case SkillEffect.TransformTouching:
                return !board.JustTouched;
            case SkillEffect.TransformSurrounding:
                return !board.HasNeighboursWithDiagonals(card, skill);
            case SkillEffect.TransformNeighbours:
                return !board.HasNeighbours(card, skill);
            case SkillEffect.AddToDeck:
            case SkillEffect.MoveTarget:
                return false;
            case SkillEffect.FillHoles:
                return !board.HasHoles();
            case SkillEffect.SlideTowardsTarget:
                return !board.CanSlideTowardsTarget(card);
            case SkillEffect.DestroyRow:
                return !board.GetRow(card).Any();
            case SkillEffect.DestroyColumn:
                return !board.GetColumn(card).Any();
            case SkillEffect.TransformRow:
                return !board.GetRow(card).Any();
            case SkillEffect.TransformColumn:
                return !board.GetColumn(card).Any();
            case SkillEffect.DestroyAll:
                return !board.GetAll(skill.TargetType).Any();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator DoEffect(Skill skill, Card card)
    {
        switch (skill.effect)
        {
            case SkillEffect.None:
                break;
            case SkillEffect.AddScore:
                board.AddScore(skill.amount, card.transform.position);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.DestroyTouching:
                yield return board.DestroyCards(new List<Card> { board.JustTouched }, card);
                break;
            case SkillEffect.AddMultiForSlideLength:
                board.AddMulti(board.SlideLength);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.AddMultiplier:
                board.AddMulti(skill.amount);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.SpawnAround:
                yield return new WaitForSeconds(0.4f);
                var reach = HasExtender(skill) ? 2 : 1;
                yield return board.SpawnAround(card, skill.TargetType, reach);
                yield return new WaitForSeconds(0.25f);
                break;
            case SkillEffect.LevelUp:
                yield return new WaitForSeconds(0.5f);
                yield return Present();
                break;
            case SkillEffect.DestroySurrounding:
                yield return board.DestroyCards(board.GetNeighbours(card, skill, true).ToList(), card);
                break;
            case SkillEffect.DestroyNeighbours:
                yield return board.DestroyCards(board.GetNeighbours(card, skill, false).ToList(), card);
                break;
            case SkillEffect.SpawnNeighbours:
                yield return new WaitForSeconds(0.4f);
                yield return board.SpawnOnNeighbours(card, skill.TargetType);
                yield return new WaitForSeconds(0.25f);
                break;
            case SkillEffect.SpawnBehind:
                yield return new WaitForSeconds(0.4f);
                yield return board.SpawnBehind(skill.TargetType);
                yield return new WaitForSeconds(0.25f);
                break;
            case SkillEffect.AddToDeck:
                board.AddToDeck(skill.TargetType);
                break;
            case SkillEffect.DestroyClosest:
                yield return board.DestroyCards(new List<Card> { board.GetClosest(card, skill.TargetType) }, card);
                break;
            case SkillEffect.MoveTarget:
                board.MoveTarget();
                yield return new WaitForSeconds(0.3f);
                break;
            case SkillEffect.TransformSurrounding:
                yield return board.TransformCards(board.GetNeighbours(card, skill, true).ToList(), skill);
                break;
            case SkillEffect.TransformNeighbours:
                yield return board.TransformCards(board.GetNeighbours(card, skill, false).ToList(), skill);
                break;
            case SkillEffect.TransformTouching:
                yield return board.TransformCards(new List<Card> { board.JustTouched }, skill);
                break;
            case SkillEffect.FillHoles:
                yield return board.SpawnCards(skill.TargetType, board.GetHoles());
                break;
            case SkillEffect.SlideTowardsTarget:
                card.MarkVisit();
                yield return board.SlideTowardsTarget(card);
                break;
            case SkillEffect.DestroyRow:
                yield return board.DestroyCards(board.GetRow(card).ToList(), card);
                break;
            case SkillEffect.DestroyColumn:
                yield return board.DestroyCards(board.GetColumn(card).ToList(), card);
                break;
            case SkillEffect.TransformRow:
                yield return board.TransformCards(board.GetRow(card).ToList(), skill);
                break;
            case SkillEffect.TransformColumn:
                yield return board.TransformCards(board.GetColumn(card).ToList(), skill);
                break;
            case SkillEffect.DestroyAll:
                yield return board.DestroyCards(board.GetAll(skill.TargetType), card);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool HasExtender(Skill skill)
    {
        return skills.Any(s => s.requirement && s.requirement.Is(skill));
    }

    private IEnumerator Act(Skill skill, Vector3 pos, Card card = null)
    {
        var p = pos.RandomOffset(1f);

        if (FailsCondition(skill, card) || ShouldCancel(skill, card))
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

        yield return new WaitForSeconds(skill.triggerDelay);
        
        skill.Trigger();

        yield return DoEffect(skill, card);
    }
    
    public IEnumerable<Skill> Get(Passive passive, List<CardType> types)
    {
        return skills.Where(s => s.Matches(passive, types));
    }

    public IEnumerable<Skill> Get(Passive passive, CardType type)
    {
        return skills.Where(s => s.Matches(passive, type));
    }

    public IEnumerable<Skill> Get(Passive passive)
    {
        return skills.Where(s => s.Matches(passive));
    }

    public void MarkSkills(CardType type)
    {
        var types = GetTypesFor(type).ToList();
        skills.Where(s => types.Contains(s.MainType)).ToList().ForEach(s => s.Icon.Mark(true));
        skills.Where(s => s.HasTargetType && s.TargetType == type).ToList().ForEach(s => s.Icon.Mark(false));
    }

    public void UnMarkSkills()
    {
        skills.ForEach(s => s.Icon.UnMark());
    }

    public int Count(Passive passive)
    {
        return skills.Count(s => s.Matches(passive));
    }
    
    public bool Has(Passive passive)
    {
        return skills.Any(s => s.Matches(passive));
    }

    public bool Has(Passive passive, CardType type)
    {
        var types = GetTypesFor(type).ToList();
        return skills.Any(s => s.Matches(passive, types));
    }

    public void Randomize(Skill skill)
    {
        skill.Randomize(skills);
    }
}