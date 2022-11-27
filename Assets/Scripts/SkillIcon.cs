using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text letter;
    [SerializeField] private Pulsater pulsater;
    [SerializeField] private Image icon, shadow;

    [SerializeField] private Color normalColor, sourceColor, targetColor;

    private Skill skill;

    public void Setup(Skill s)
    {
        skill = s;
        letter.text = s.title[..1];
        icon.sprite = shadow.sprite = s.iconSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillTooltip.Instance.Show(skill);
        CursorManager.Instance.Use(1);
        pulsater.Pulsate();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillTooltip.Instance.Hide();
        CursorManager.Instance.Use(0);
    }

    public void Pulsate()
    {
        pulsater.Pulsate();
    }

    public void Mark(bool isSource)
    {
        Pulsate();
        icon.color = isSource ? sourceColor : targetColor;
    }

    public void UnMark()
    {
        icon.color = normalColor;
    }
}