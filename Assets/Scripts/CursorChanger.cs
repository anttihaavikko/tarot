using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

public class CursorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Shaker shaker;
    
    private Camera cam;

    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorManager.Instance.Use(1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CursorManager.Instance.Use(0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!cam)
        {
            cam = Camera.main;
        }

        if (shaker)
        {
            shaker.Shake();            
        }
        
        EffectManager.AddEffect(5, cam.ScreenToWorldPoint(Input.mousePosition).WhereZ(0));
    }
}