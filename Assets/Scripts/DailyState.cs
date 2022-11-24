using System;
using System.Globalization;
using AnttiStarterKit.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

public class DailyState : Manager<DailyState>
{
    private DateTime current;
    private string DateString => FormatDate(current);
    private int BaseSeed => int.Parse(current.ToString("ddMMyyyy"));
    public static string FormatDate(DateTime date) => date.ToString("MMM dd yyyy", new CultureInfo("en-US"));

    public string BoardSuffix => IsDaily ? $"-{BaseSeed.ToString()}" : "";

    public const int MinSkills = 1;
    public const int MaxSkills = 4;
    public const float ModChance = 0.7f;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public bool IsDaily { get; private set; }

    public void Setup(DateTime day)
    {
        IsDaily = true;
        current = day;
    }

    public void Seed(int offset = 0)
    {
        if (!IsDaily) return;
        Random.InitState(BaseSeed + offset);
    }

    public void Clear()
    {
        IsDaily = false;
        Random.InitState(Environment.TickCount);
    }
}