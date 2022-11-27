using System;
using System.Collections.Generic;
using System.Text;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEngine;

public class TutorialHolder : MonoBehaviour
{
    [SerializeField] private Appearer appearer;
    [SerializeField] private TMP_Text text, shadow;
    [SerializeField] private List<Appearer> edgeSpots;
    [SerializeField] private Appearer arrow;

    private Tutorial<TutorialMessage> tutorials;
    private bool spotsShown, arrowShown;

    private void Start()
    {
        tutorials = new Tutorial<TutorialMessage>("TarotTutorials");
        tutorials.onShow += DoShow;
    }

    private void Update()
    {
        if (DevKey.Down(KeyCode.D))
        {
            tutorials.Clear();
        }
    }

    public void Show(TutorialMessage message)
    {
        tutorials.Show(message);
    }

    public void Hide()
    {
        HideArrow();
        appearer.Hide();
    }

    private void HideArrow()
    {
        if (!arrowShown) return;
        arrowShown = false;
        arrow.Hide();
    }

    public void HideSpots()
    {
        if (!spotsShown) return;
        spotsShown = false;
        edgeSpots.ForEach(s => s.Hide());
    }
    
    public void Mark(TutorialMessage message)
    {
        tutorials.Mark(message);
    }

    private void DoShow(TutorialMessage message)
    {
        if (message == TutorialMessage.Intro)
        {
            arrowShown = true;
            arrow.Show();
        }
        
        if (message == TutorialMessage.PlaceOnEdge)
        {
            spotsShown = true;
            edgeSpots.ForEach(s => s.Show());
        }
        
        var msg = GetMessage(message);
        text.text = Colorize(msg, "#E0CA3C");
        shadow.text = Colorize(msg, "#1B1B1E");
        appearer.Show();
    }

    private string Colorize(string text, string color)
    {
        var sb = new StringBuilder(text);
        sb.Replace("(", $"<color={color}>");
        sb.Replace(")", "</color>");
        return sb.ToString().ToUpper();
    }

    private string GetMessage(TutorialMessage message)
    {
        return message switch
        {
            TutorialMessage.PlaceOnEdge => "Place the (card) on the (edge)...",
            TutorialMessage.Intro => "(DROP) THE (CARD) TO THE (EDGE) OF THE BOARD TO (SLIDE) IT TO PLAY AND (REACH THE TARGET)!",
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null)
        };
    }
}

public enum TutorialMessage
{
    Intro,
    PlaceOnEdge
}