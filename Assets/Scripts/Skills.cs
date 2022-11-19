using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
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
    [SerializeField] private Appearer title, rerollButton, toggleButton;

    private List<Skill> skillPool;
    private bool picking;
    private readonly List<Skill> skills = new();
    private int rerolls;
    private bool shown;

    public bool IsViewingBoard => picking && !shown;

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

    public void Reroll()
    {
        StartCoroutine(DoReroll());
    }

    public void Toggle()
    {
        shown = !shown;
        toggleButton.ShowWithText(shown ? "SHOW BOARD" : "RETURN", 0);
        
        if (!shown)
        {
            title.Hide();
            rerollButton.Hide();
            skillPicks.ForEach(s => s.Hide());
            return;
        }
        
        if (rerolls > 0)
        {
            rerollButton.Show();
        }
        
        board.HideCardPreview();
        title.Show();
        skillPicks.ForEach(s => s.Show());
    }

    private IEnumerator DoReroll()
    {
        rerolls--;
        AudioManager.Instance.PlayEffectAt(12, Vector3.zero);
        title.Hide();
        toggleButton.Hide();
        rerollButton.Hide();
        skillPicks.ForEach(s => s.Hide());
        yield return new WaitForSeconds(1f);
        AudioManager.Instance.PlayEffectAt(9, Vector3.zero);
        yield return Present(true);
    }

    public IEnumerator Present(bool isReroll = false)
    {
        if (!isReroll)
        {
            rerolls = skills.Count(s => s.Matches(Passive.Reroll));   
        }

        title.Show();
        toggleButton.ShowAfter(0.2f);
        shown = true;

        if (rerolls > 0)
        {
            rerollButton.ShowWithText($"REROLL ({rerolls})", 0.3f);
        }
        
        var options = Take(skillPicks.Count);
        skillPicks.ForEach(s => s.Setup(options[skillPicks.IndexOf(s)]));
        picking = true;

        if (isReroll) yield break;
        while (picking) yield return null;
    }

    public void Pick()
    {
        shown = false;
        title.Hide();
        toggleButton.Hide();
        rerollButton.Hide();
        skillPicks.ForEach(s => s.Hide());
    }

    public void DonePicking()
    {
        picking = false;
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
        
        if(skill.Matches(Passive.HandSize))
        {
            board.IncreaseHandSize();
        }
        
        if(skill.Matches(Passive.IncreaseSize))
        {
            board.IncreaseAreaSize();
        }

        var doneRepeating = skills.Count(s => s.title == skill.title) >= skill.firstCards.Count;
        if (!skill.repeatable && (skill.notRepeatableForOthers || doneRepeating))
        {
            Remove(source);
        }
        
        skill.Announce(Vector3.zero, true);
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
    
    public bool Trigger(Passive passive, Card card)
    {
        return Trigger(passive, card.GetCardType(), card.transform.position);
    }
    
    public bool Trigger(Passive passive, Vector3 pos)
    {
        var triggered = Get(passive).ToList();

        triggered.ForEach(skill =>
        {
            skill.Announce(pos);
            skill.Trigger();
        });
        
        return triggered.Any();
    }
    
    public List<Skill> GetTriggered(Passive passive, CardType type, Vector3 pos)
    {
        var types = GetTypesFor(type).ToList();
        var triggered = Get(passive, types).ToList();

        triggered.ForEach(skill =>
        {
            skill.Announce(pos);
            skill.Trigger();
        });
        
        return triggered;
    }

    public bool Trigger(Passive passive, CardType type, Vector3 pos)
    {
        return GetTriggered(passive, type, pos).Any();
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
        return skill.effect switch
        {
            SkillEffect.None => false,
            SkillEffect.DestroyTouching => !board.JustTouched,
            SkillEffect.AddMultiForSlideLength => board.SlideLength < 1,
            SkillEffect.AddMultiplier => false,
            SkillEffect.AddScore => false,
            SkillEffect.SpawnAround => !board.HasEmptyNeighboursWithDiagonals(card),
            SkillEffect.LevelUp => false,
            SkillEffect.DestroySurrounding => !board.HasNeighboursWithDiagonals(card, skill),
            SkillEffect.DestroyNeighbours => !board.HasNeighbours(card, skill),
            SkillEffect.SpawnNeighbours => !board.HasEmptyNeighbours(card),
            SkillEffect.SpawnBehind => !board.BehindSpot,
            SkillEffect.DestroyClosest => !board.GetClosest(card, skill.TargetType, 1).Any(),
            SkillEffect.TransformTouching => !board.JustTouched,
            SkillEffect.TransformSurrounding => !board.HasNeighboursWithDiagonals(card, skill),
            SkillEffect.TransformNeighbours => !board.HasNeighbours(card, skill),
            SkillEffect.AddToDeck => false,
            SkillEffect.MoveTarget => false,
            SkillEffect.FillHoles => !board.HasHoles(skill),
            SkillEffect.SlideTowardsTarget => !board.CanSlideTowardsTarget(card),
            SkillEffect.DestroyRow => !board.GetRow(card).Any(),
            SkillEffect.DestroyColumn => !board.GetColumn(card).Any(),
            SkillEffect.TransformRow => !board.GetRow(card).Any(),
            SkillEffect.TransformColumn => !board.GetColumn(card).Any(),
            SkillEffect.DestroyAll => !board.GetAll(skill.TargetType).Any(),
            SkillEffect.ScoreForNeighbours => !board.GetNeighbours(card, skill, true).Any(),
            SkillEffect.ScoreForNeighboursNoDiagonals => !board.GetNeighbours(card, skill, false).Any(),
            SkillEffect.SpawnOnSides => board.PreviousDirection.magnitude < 1 || !board.HasEmptySides(card),
            _ => throw new ArgumentOutOfRangeException()
        };
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
                board.AddMulti(card.transform.position, board.SlideLength);
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillEffect.AddMultiplier:
                board.AddMulti(card.transform.position, skill.amount);
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
                yield return board.SpawnBehind(skill.TargetType, card.transform.position);
                yield return new WaitForSeconds(0.25f);
                break;
            case SkillEffect.AddToDeck:
                board.AddToDeck(skill.TargetType, skill.amount);
                break;
            case SkillEffect.DestroyClosest:
                yield return board.DestroyCards(board.GetClosest(card, skill.TargetType, 1 + ExtenderCount(skill)), card);
                break;
            case SkillEffect.MoveTarget:
                board.MoveTarget();
                yield return new WaitForSeconds(0.3f);
                break;
            case SkillEffect.TransformSurrounding:
                yield return board.TransformCards(board.GetNeighbours(card, skill, true).ToList(), skill, card.transform.position);
                break;
            case SkillEffect.TransformNeighbours:
                yield return board.TransformCards(board.GetNeighbours(card, skill, false).ToList(), skill, card.transform.position);
                break;
            case SkillEffect.TransformTouching:
                yield return board.TransformCards(new List<Card> { board.JustTouched }, skill, card.transform.position);
                break;
            case SkillEffect.FillHoles:
                var holes = board.GetHoles(skill);
                yield return board.SpawnCards(skill.TargetType, holes, card.transform.position);
                if (holes.Any())
                {
                    yield return new WaitForSeconds(0.3f);
                }
                foreach (var hole in holes)
                {
                    yield return Trigger(SkillTrigger.FillGap, hole.Card);
                    yield return new WaitForSeconds(0.1f);
                }
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
                yield return board.TransformCards(board.GetRow(card).ToList(), skill, card.transform.position);
                break;
            case SkillEffect.TransformColumn:
                yield return board.TransformCards(board.GetColumn(card).ToList(), skill, card.transform.position);
                break;
            case SkillEffect.DestroyAll:
                yield return board.DestroyCards(board.GetAll(skill.TargetType), card);
                break;
            case SkillEffect.ScoreForNeighbours:
                yield return board.ScoreFor(card, skill, true);
                break;
            case SkillEffect.ScoreForNeighboursNoDiagonals:
                yield return board.ScoreFor(card, skill, false);
                break;
            case SkillEffect.SpawnOnSides:
                yield return new WaitForSeconds(0.4f);
                yield return board.SpawnOnSides(card, skill.TargetType);
                yield return new WaitForSeconds(0.25f);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool HasExtender(Skill skill)
    {
        return skills.Any(s => s.requirement && s.requirement.Is(skill));
    }

    private int ExtenderCount(Skill skill)
    {
        return skills.Count(s => s.requirement && s.requirement.Is(skill));
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

            if (skill.cancelSound)
            {
                AudioManager.Instance.PlayEffectAt(skill.cancelSound, p);
            }

            if (skill.cancelDelay > 0)
            {
                yield return new WaitForSeconds(skill.cancelDelay);
            }

            if (card && !skill.canNotFizzle)
            {
                Debug.Log($"{skill.title} fizzled");
                yield return Trigger(SkillTrigger.Fizzle, card);   
            }

            yield break;
        }
        
        skill.Announce(p);

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

    public List<CardType> GetActualType(Skill skill)
    {
        return skill.usesRequirementForMarking ? 
            skills.Where(s => skill.requirement.Is(s)).Select(s => s.MainType).ToList() : 
            new List<CardType>{ skill.MainType };
    }

    public void MarkSkills(CardType type)
    {
        var types = GetTypesFor(type).ToList();
        skills.Where(s => s.MatchesForSecondaryMarking(type)).ToList().ForEach(s => s.Icon.Mark(false));
        skills.Where(s => s.MatchesForMarking(types, this)).ToList().ForEach(s => s.Icon.Mark(true));
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