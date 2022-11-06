using AnttiStarterKit.Animations;
using AnttiStarterKit.Managers;
using TMPro;
using UnityEngine;

public class SkillTooltip : Manager<SkillTooltip>
{
    [SerializeField] private TMP_Text title, description, descShadow;
    [SerializeField] private CardPreview cardPreview;

    public void Hide()
    {
        cardPreview.Hide();
    }

    public void Show(Skill skill)
    {
        cardPreview.Show(skill.ImageType);
        title.text = skill.title;
        description.text = skill.GetDescription();
        descShadow.text = skill.GetDescription(false);
    }
}