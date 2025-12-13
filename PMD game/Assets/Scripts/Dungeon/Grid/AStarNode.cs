using System.Collections.Generic;
using UnityEngine;

public class AStarNode
{
    public Vector2Int Position;
    public AStarNode Parent;
    
    // Costs
    public int GCost; // Distance from start
    public int HCost; // Heuristic distance to end
    public int FCost => GCost + HCost;

    public AStarNode(Vector2Int position)
    {
        Position = position;
    }

    public override bool Equals(object obj)
    {
        if (obj is AStarNode other)
        {
            return Position.Equals(other.Position);
        }

        return false;
    }

    public override int GetHashCode() => Position.GetHashCode();
}
