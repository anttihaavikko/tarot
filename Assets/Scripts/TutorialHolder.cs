using System;
using System.Text;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEngine;

public class TutorialHolder : MonoBehaviour
{
    [SerializeField] private Appearer appearer;
    [SerializeField] private TMP_Text text, shadow;

    private Tutorial<TutorialMessage> tutorials;

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
        appearer.Hide();
    }
    
    public void Mark(TutorialMessage message)
    {
        tutorials.Mark(message);
    }

    private void DoShow(TutorialMessage message)
    {
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