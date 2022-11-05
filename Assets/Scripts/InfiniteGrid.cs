using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InfiniteGrid<T> where T : GridTile
{
    private readonly Dictionary<string, GridSpot> items = new();
    public T CollisionTarget { get; private set; }

    public GridSpot Get(int x, int y)
    {
        var key = GetKey(x, y);
        return new GridSpot(x, y, items.ContainsKey(key) ? items[key].Value : default);
    }

    public void Set(int x, int y, T value)
    {
        items.Add(GetKey(x, y), new GridSpot(x, y, value));
    }

    public void Clear(int x, int y)
    {
        items.Remove(GetKey(x, y));
    }

    private static string GetKey(int x, int y)
    {
        return $"{x},{y}";
    }

    public bool IsTile(int x, int y)
    {
        return items.ContainsKey(GetKey(x, y));
    }

    public Vector3 GetCenter()
    {
        var positions = items.Values.Select(a => a.Position).ToList();
        var x = (positions.Max(p => p.x) + positions.Min(p => p.x)) * 0.5f;
        var y = (positions.Max(p => p.y) + positions.Min(p => p.y)) * 0.5f;
        return new Vector3(x, y, 0);
    }

    public Vector2Int GetSize()
    {
        var positions = items.Values.Select(a => a.Position).ToList();
        var x = Mathf.Abs(positions.Max(p => p.x)) + Mathf.Abs(positions.Min(p => p.x));
        var y = Mathf.Abs(positions.Max(p => p.y)) + Mathf.Abs(positions.Min(p => p.y));
        return new Vector2Int(x, y);
    }

    public GridSpot RandomFree()
    {
        return items.Values.Where(a => a.IsEmpty).OrderBy(_ => Random.value).First();
    }

    public GridSpot GetClosest(Vector3 pos)
    {
        return items.Values
            .Where(v => v.IsEmpty && IsOnEdge(v.Position.x, v.Position.y))
            .OrderBy(v => Vector3.Distance(pos, v.AsVector3))
            .First();
    }
    
    public GridSpot GetClosestEdge(Vector3 pos)
    {
        return GetEdges()
            .OrderBy(v => Vector3.Distance(pos, new Vector3(v.Position.x, v.Position.y)))
            .First();
    }

    public IEnumerable<GridSpot> GetNeighbours(int x, int y)
    {
        return new []
        {
            Get(x + 1, y),
            Get(x - 1, y),
            Get(x, y + 1),
            Get(x, y - 1)
        };
    }
    
    public IEnumerable<GridSpot> GetNeighboursWithDiagonals(int x, int y, int reach = 1)
    {
        var spots = new List<GridSpot>();
        
        for (var i = -reach; i <= reach; i++)
        {
            for (var j = -reach; j <= reach; j++)
            {
                spots.Add(Get(x + i, y + j));
            }   
        }

        return spots;
    }

    public IEnumerable<GridSpot> GetEdgeNeighbours(int x, int y)
    {
        return GetNeighbours(x, y).Where(v => v.IsWall);
    }
    
    public IEnumerable<GridSpot> GetEdgeNeighboursWithDiagonals(int x, int y)
    {
        return GetNeighboursWithDiagonals(x, y).Where(v => v.IsWall);
    }

    public IEnumerable<GridSpot> GetEdges()
    {
        var all = items.SelectMany(v =>
        {
            var p = GetPosition(v.Key);
            return GetEdgeNeighboursWithDiagonals(p.x, p.y);
        }).GroupBy(a => GetKey(a.Position.x, a.Position.y)).Select(g => g.First());

        return all;
    }

    public bool IsOnEdge(int x, int y)
    {
        return GetNeighbours(x, y).Any(v => v.IsWall);
    }

    public Vector2Int GetPosition(string key)
    {
        var parts = key.Split(",");
        return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public GridSpot GetSlideTarget(int x, int y, Vector2Int dir)
    {
        var next = Get(x + dir.x, y + dir.y);
        CollisionTarget = next.IsOccupied ? next.Value : default;
        return next.IsWall || !next.IsEmpty ? Get(x, y) : GetSlideTarget(next.Position.x, next.Position.y, dir);
    }
    
    public List<GridSpot> GetAll()
    {
        return items.Values.ToList();
    }

    public int GetEmptyCount()
    {
        return items.Values.Count(a => a.IsEmpty);
    }
    
    public class GridSpot
    {
        public Vector2Int Position;
        public T Value;

        public bool IsWall => Value == default;
        public bool IsEmpty => Value != default && Value.IsEmpty;
        public bool IsOccupied => Value != default && !Value.IsEmpty;

        public Vector3 AsVector3 => new(Position.x, Position.y);

        public GridSpot(int x, int y, T val)
        {
            Position = new Vector2Int(x, y);
            Value = val;
        }
        
        public GridSpot(Vector2Int pos)
        {
            Position = pos;
            Value = default;
        }
    }
}

public abstract class GridTile : MonoBehaviour
{
    public virtual bool IsEmpty => true;
}