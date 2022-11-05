using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ScrollingImage : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    
    private RawImage image;

    private void Start()
    {
        image = GetComponent<RawImage>();
    }

    private void Update()
    {
        var offset = Time.deltaTime * speed;
        image.uvRect = new Rect(image.uvRect.x + offset, image.uvRect.y + offset, image.uvRect.width, image.uvRect.height);
    }
}