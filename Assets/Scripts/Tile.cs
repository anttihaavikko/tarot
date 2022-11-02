using UnityEngine;

public class Tile : GridTile
{
    private Card card;
    
    public Vector2Int Position { get; set; }

    public override bool IsEmpty => card == default;

    public void Set(Card c)
    {
        card = c;
    }
}