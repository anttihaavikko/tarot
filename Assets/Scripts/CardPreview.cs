using System.Collections.Generic;
using AnttiStarterKit.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPreview : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private Image bg;
    [SerializeField] private List<GameObject> images;
    [SerializeField] private ColorCollection colors;

    private GameObject current;

    public void Show(CardType type)
    {
        title.text = Card.GetName(type);

        if (current)
        {
            current.SetActive(false);
        }

        current = images[(int)type];
        current.SetActive(true);
        bg.color = colors.Get((int)type);
    }

    public void Hide()
    {
    }
}