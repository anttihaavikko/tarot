using UnityEngine;

public class Card : MonoBehaviour
{
    private Board board;
    private Draggable draggable;

    private void Start()
    {
        draggable = GetComponent<Draggable>();
        draggable.preview += Preview;
        draggable.dropped += Place;
    }

    private void Place(Draggable d)
    {
        if (board)
        {
            board.Place(this);
        }
    }

    private void Preview(Draggable d)
    {
        if (board)
        {
            board.Preview(this);
        }
    }

    public void SetBoard(Board b)
    {
        board = b;
    }

    public void Lock()
    {
        draggable.enabled = false;
    }
}