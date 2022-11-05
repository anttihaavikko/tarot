using AnttiStarterKit.Animations;
using TMPro;
using UnityEngine;

public class SkillPick : MonoBehaviour
{
    [SerializeField] private TMP_Text title, description;
    [SerializeField] private Skills skillPool;
    [SerializeField] private ButtonStyle buttonStyle;

    private Skill skill;

    public void Setup(Skill s)
    {
        skill = s;
        title.text = s.title;
        description.text = s.GetDescription();
    }

    public void Pick()
    {
        buttonStyle.Reset();
        skillPool.Add(skill);
        skillPool.Pick();
    }
}