using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text letter;
    [SerializeField] private Pulsater pulsater;

    private Skill skill;

    public void Setup(Skill s)
    {
        skill = s;
        letter.text = s.title[..1];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillTooltip.Instance.Show(skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillTooltip.Instance.Hide();
    }

    public void Pulsate()
    {
        pulsater.Pulsate();
    }
}