using System;
using AnttiStarterKit.Managers;
using AnttiStarterKit.Utils;
using UnityEngine;

public class StartView : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.TargetPitch = 1;
    }

    private void Update()
    {
        if (DevKey.Down(KeyCode.D))
        {
            PlayerPrefs.DeleteKey("PlayerName");
        }
    }

    public void Play()
    {
        DailyState.Instance.Clear();
        SceneChanger.Instance.ChangeScene(PlayerPrefs.HasKey("PlayerName") ? "Main" : "Name");
    }
}