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
        return skills.Where(s => s.Matches(Passive.Mimic, type)).Select(s => s.SecondType).Concat(new [] { type });
    }

    public IEnumerator Trigger(SkillTrigger trigger, Card card)
    {
        foreach (var s in skills.Where(s => GetTypesFor(card).Any(t => s.Matches(trigger, t))).ToList())
        {
            yield return Act(s, card.transform.position);
        }
    }

    public IEnumerator Trigger(SkillTrigger trigger)
    {
        foreach (var s in skills.Where(s => s.Matches(trigger)).ToList())
        {
            yield return Act(s, Vector3.zero);
        }
    }

    public IEnumerator Act(Skill skill, Vector3 pos)
    {
        var delay = 0f;
        var p = pos.RandomOffset(1f);
        var touchEffect = new []
        {
            SkillEffect.DestroyTouching
        }.Contains(skill.effect);

        if (skill.effect == SkillEffect.AddMultiForSlideLength && board.SlideLength < 1) yield break;

        if (skill.effect is SkillEffect.AddMultiplierIfAlone or SkillEffect.AddScoreIfAlone)
        {
            if (!board.IsPlacedAlone())
            {
                EffectManager.AddTextPopup("ANXIETY!", p, 0.8f);
                yield return new WaitForSeconds(0.3f);
                yield break;
            }
        }

        if (touchEffect && !board.JustTouched) yield break;
        
        EffectManager.AddTextPopup(skill.title, p, 0.8f);
        skill.Trigger();

        if (skill.effect == SkillEffect.AddMultiForSlideLength)
        {
            board.AddMulti(board.SlideLength);
            delay = 0.4f;
        }
        
        if (skill.effect == SkillEffect.AddMultiplierIfAlone)
        {
            board.AddMulti(skill.amount);
            delay = 0.4f;
        }
        
        if (skill.effect == SkillEffect.AddScoreIfAlone)
        {
            board.AddScore(skill.amount, pos);
            delay = 0.4f;
        }
        
        if (skill.effect == SkillEffect.AddMultiplier)
        {
            board.AddMulti(skill.amount);
            delay = 0.25f;
        }

        if (skill.effect == SkillEffect.DestroyTouching)
        {
            var target = board.JustTouched;
            if (target)
            {
                yield return board.DestroyCards(new List<Card> { target });
            }
        }

        yield return new WaitForSeconds(delay);
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