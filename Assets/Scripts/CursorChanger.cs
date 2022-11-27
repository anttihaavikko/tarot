using UnityEngine;
using UnityEngine.EventSystems;

public class CursorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorManager.Instance.Use(1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CursorManager.Instance.Use(0);
    }
}