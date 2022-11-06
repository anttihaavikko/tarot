using System.Collections.Generic;
using AnttiStarterKit.Animations;
using AnttiStarterKit.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AnttiStarterKit.Extensions;

public class CardPreview : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private Image bg;
    [SerializeField] private RawImage pattern;
    [SerializeField] private List<GameObject> images;
    [SerializeField] private ColorCollection colors, patternColors;

    private GameObject current;
    private Appearer appearer;

    private void Start()
    {
        appearer = GetComponent<Appearer>();
    }

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
        pattern.color = patternColors.Get((int)type);
        pattern.material.SetColor("_Color", pattern.color);
        pattern.gameObject.SetActive(false);
        pattern.gameObject.SetActive(true);
        appearer.Show();
    }

    public void Hide()
    {
        appearer.Hide();
    }
}