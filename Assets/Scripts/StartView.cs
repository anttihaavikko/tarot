using System;
using AnttiStarterKit.Managers;
using UnityEngine;

public class StartView : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.TargetPitch = 1;
    }

    public void Play()
    {
        DailyState.Instance.Clear();
        SceneChanger.Instance.ChangeScene("Main");
    }
}