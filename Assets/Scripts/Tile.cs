using UnityEngine;

public class Tile : GridTile
{
    private Card card;
    
    public Vector2Int Position { get; set; }

    public override bool IsEmpty => card == default;
    public Card Card => card;

    public void Set(Card c)
    {
        c.Tile = this;
        card = c;
    }

    public void Clear()
    {
        card = null;
    }
}