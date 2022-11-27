using System;
using System.Collections.Generic;
using AnttiStarterKit.Managers;
using UnityEngine;

public class CursorManager : Manager<CursorManager>
{
    [SerializeField] private List<CursorDefinition> cursors;

    public void Use(int cursorIndex)
    {
        cursors[cursorIndex].Use();
    }
}

[Serializable]
public class CursorDefinition
{
    [SerializeField] private Texture2D cursor;
    [SerializeField] private Vector2 hotspot;

    public void Use()
    {
        Cursor.SetCursor(cursor, hotspot, CursorMode.Auto);
    }
}