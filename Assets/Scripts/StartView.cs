using UnityEngine;

public class StartView : MonoBehaviour
{
    public void Play()
    {
        DailyState.Instance.Clear();
        SceneChanger.Instance.ChangeScene("Main");
    }
}