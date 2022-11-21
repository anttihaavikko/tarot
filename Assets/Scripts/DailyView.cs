using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Managers;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class DailyView : MonoBehaviour
{
    [SerializeField] private List<TMP_Text> dateLabels, infos;
    [SerializeField] private Skills skills;
    [SerializeField] private Transform skillContainer;
    [SerializeField] private Transform infoContainer;

    private int offset;
    private DateTime current;
    private int fieldSize;

    private void Start()
    {
        AudioManager.Instance.TargetPitch = 1;
        ChangeDate(0);
    }

    public void Play()
    {
        SceneChanger.Instance.ChangeScene("Main");
    }

    public void ChangeDate(int direction)
    {
        offset = Mathf.Min(0, offset + direction);
        current = DateTime.UtcNow.Add(new TimeSpan(offset, 0, 0, 0));
        dateLabels.ForEach(t => t.text = DailyState.FormatDate(current));
        
        DailyState.Instance.Setup(current);

        for (var i = 0; i < skillContainer.childCount; i++)
        {
            var child = skillContainer.GetChild(i);
            if (child != infoContainer)
            {
                Destroy(child.gameObject);   
            }
        }
        
        skills.ResetPool();
        DailyState.Instance.Seed();

        fieldSize = Random.Range(4, 10);
        var skillCount = Random.Range(DailyState.MinSkills, DailyState.MaxSkills + 1);
        
        infos.ForEach(i => i.text = $"Board size of {(fieldSize - 2) * 2 + 1}, start with");
        
        if (Random.value < DailyState.ModChance)
        {
            skills.AddRandomDailyMod();
        }

        for (var i = 0; i < skillCount; i++)
        {
            skills.AddRandom();
        }
    }
}