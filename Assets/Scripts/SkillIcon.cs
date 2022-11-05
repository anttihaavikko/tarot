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
        Debug.Log(skill.GetDescription());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

    public void Pulsate()
    {
        pulsater.Pulsate();
    }
}