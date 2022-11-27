using AnttiStarterKit.ScriptableObjects;
using UnityEngine;

public class Plant : MonoBehaviour
{
    [SerializeField] private SpriteCollection sprites;
    [SerializeField] private SpriteRenderer sprite;

    public void Start()
    {
        sprite.sprite = sprites.Random();
    }
}