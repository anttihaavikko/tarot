using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevMenu : MonoBehaviour
{
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Board board;
    [SerializeField] private Transform cardPanel, skillPanel;
    [SerializeField] private Skills skills;

    private void Start()
    {
        foreach (var type in EnumUtils.ToList<CardType>())
        {
            var button = Instantiate(buttonPrefab, cardPanel);
            button.GetComponentInChildren<TMP_Text>().text = type.ToString();
            button.onClick.AddListener(() => board.ChangeDrawnTo(type));
        }

        var allSkills = skills.All();
        allSkills.Reverse();
        
        EnumUtils.ToList<CardType>().ToList().ForEach(type =>
        {
            var source = allSkills.Count(c => c.firstCards.Contains(type));
            var target = allSkills.Count(c => c.secondCards.Contains(type));
            Debug.Log($"<color=white>{type}</color>: <color=yellow>{source}</color> source, <color=red>{target}</color> target");
        });
        
        allSkills.GroupBy(x => x.iconSprite)
            .Where(g => g.Count() > 1)
            .ToList()
            .ForEach(g => Debug.Log($"Same icon <color=red>{g.Key.name}</color> for skills: {ListNames(g)}"));
        
        foreach (var skill in allSkills)
        {
            var button = Instantiate(buttonPrefab, skillPanel);
            button.GetComponentInChildren<TMP_Text>().text = skill.title;
            button.onClick.AddListener(() =>
            {
                skills.Randomize(skill);
                skills.Add(skill);
            });
        }
    }

    private string ListNames(IGrouping<Sprite, Skill> grouping)
    {
        return string.Join(", ", grouping.ToList().Select(s => $"<color=yellow>{s.title}</color>"));
    }
}