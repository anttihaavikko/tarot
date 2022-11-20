using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private Image radial;
    [SerializeField] private List<GameObject> images;
    [SerializeField] private ColorCollection colors, patternColors, radialColors;

    private GameObject current;
    private Appearer appearer;

    private void Awake()
    {
        appearer = GetComponent<Appearer>();
    }

    public void MakeUnique()
    {
        pattern.material = Instantiate(pattern.material);

        var pics = images.Select(i => i.GetComponent<Image>()).ToList();
        var mat = Instantiate(pics[0].material);
        mat.SetFloat("_Offset", Random.value * 100f);
        mat.SetFloat("_Speed", Random.Range(0.4f, 0.6f));
        mat.SetFloat("_Amount", Random.Range(0.04f, 0.06f));
        pics.ForEach(p => p.material = mat);
    }

    public void Show()
    {
        appearer.Show();
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
        radial.color = radialColors.Get((int)type);
        pattern.gameObject.SetActive(false);
        pattern.gameObject.SetActive(true);
        appearer.Show();
    }

    public void Hide()
    {
        appearer.Hide();
    }
}