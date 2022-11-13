using System;
using System.Collections.Generic;
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
        
        foreach (var skill in skills.All())
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
}