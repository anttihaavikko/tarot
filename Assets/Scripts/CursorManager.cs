using System;
using System.Collections.Generic;
using AnttiStarterKit.Managers;
using UnityEngine;

public class CursorManager : Manager<CursorManager>
{
    [SerializeField] private List<Sprite> cursors;

    public void Use(int cursorIndex)
    {
        var cursor = cursors[cursorIndex];
        if (!cursor) return;
        var hotspot = new Vector2(cursor.pivot.x, cursor.texture.height - cursor.pivot.y);
        Cursor.SetCursor(cursor.texture, hotspot, CursorMode.Auto);
    }
}