using System.Collections;
using AnttiStarterKit.Animations;
using TMPro;
using UnityEngine;

public class SkillPick : MonoBehaviour
{
    [SerializeField] private CardPreview preview;
    [SerializeField] private Skills skillPool;
    [SerializeField] private ButtonStyle buttonStyle;
    [SerializeField] private TMP_Text title, description, descriptionShadow;

    private Skill skill;

    public void Setup(Skill s)
    {
        skill = s;
        description.text = skill.GetDescription();
        descriptionShadow.text = skill.GetDescription(false);
        preview.Show(skill.ImageType);
        title.text = skill.title;
    }

    public void Pick()
    {
        buttonStyle.Reset();
        StartCoroutine(DoPick());
    }

    private IEnumerator DoPick()
    {
        skillPool.Add(skill);
        skillPool.Pick();
        yield return new WaitForSeconds(0.5f);
        yield return skillPool.Trigger(SkillTrigger.AddSkill);
        skillPool.DonePicking();
    }

    public void Hide()
    {
        preview.Hide();
    }

    public void Show()
    {
        preview.Show();
    }
}