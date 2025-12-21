using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding
{
    private DungeonGrid _grid;
    
    private static readonly Vector2Int[] Directions = {
        new Vector2Int(1, 0),    // Right
        new Vector2Int(-1, 0),   // Left
        new Vector2Int(0, 1),    // Up
        new Vector2Int(0, -1),   // Down
        new Vector2Int(1, 1),    // Up-Right
        new Vector2Int(1, -1),   // Down-Right
        new Vector2Int(-1, 1),   // Up-Left
        new Vector2Int(-1, -1)   // Down-Left
    };
    
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;
    
    public AStarPathfinding(DungeonGrid grid)
    {
        _grid = grid;
    }
    
        public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int endPos)
    {
        // OPTIMIZATION: Early exit if start == end
        if (startPos == endPos)
        {
            Debug.Log("A*: Start equals end, returning empty path");
            return new List<Vector2Int>();
        }
        
        // OPTIMIZATION: Check if end is even reachable (simple check)
        if (!IsWalkable(endPos, endPos))
        {
            Debug.Log($"A*: End position {endPos} is not walkable");
            return new List<Vector2Int>();
        }
        
        // OPTIMIZATION: Limit path length based on Manhattan distance
        int manhattanDistance = Mathf.Abs(startPos.x - endPos.x) + Mathf.Abs(startPos.y - endPos.y);
        if (manhattanDistance > 30) // Adjust this based on your dungeon size
        {
            Debug.Log($"A*: Path too long ({manhattanDistance} tiles), using simple movement");
            return new List<Vector2Int>();
        }
        
        Debug.Log($"=== A* START: From {startPos} to {endPos} (distance: {manhattanDistance}) ===");
        
        // First, let's do a simple connectivity check
        if (!IsConnected(startPos, endPos))
        {
            Debug.Log($"A*: No connection between {startPos} and {endPos}!");
            return new List<Vector2Int>();
        }
        
        // Create start and end nodes
        Node startNode = new Node(startPos);
        Node endNode = new Node(endPos);
        
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        
        openSet.Add(startNode);
        
        // OPTIMIZATION: Reduce max iterations based on distance
        int maxIterations = Mathf.Min(1000, manhattanDistance * 50);
        int iterations = 0;
        
        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // Get node with lowest F cost
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || 
                   (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            // Found the path
            if (currentNode.Position.Equals(endNode.Position))
            {
                Debug.Log($"=== A* SUCCESS: Path found in {iterations} iterations ===");
                var path = CalculatePath(currentNode);
                return path;
            }
            
            // Check all neighbors (OPTIMIZATION: Limit to 4 directions for speed)
            Vector2Int[] searchDirections;
            if (manhattanDistance > 10)
            {
                // For longer distances, use 4-direction for speed
                searchDirections = new Vector2Int[] {
                    new Vector2Int(1, 0),    // Right
                    new Vector2Int(-1, 0),   // Left
                    new Vector2Int(0, 1),    // Up
                    new Vector2Int(0, -1)    // Down
                };
            }
            else
            {
                // For short distances, use 8-direction
                searchDirections = Directions;
            }
            
            foreach (Vector2Int direction in searchDirections)
            {
                Vector2Int neighborPos = currentNode.Position + direction;
                
                // Skip if not walkable
                if (!IsWalkable(neighborPos, endPos))
                {
                    continue;
                }
                
                // Check diagonal clearance for diagonal moves
                if (IsDiagonal(direction))
                {
                    if (!IsValidDiagonalMove(currentNode.Position, neighborPos, endPos))
                    {
                        continue;
                    }
                }
                
                Node neighborNode = new Node(neighborPos);
                
                if (closedSet.Contains(neighborNode))
                    continue;
                
                int moveCost = IsDiagonal(direction) ? DiagonalCost : StraightCost;
                int newGCost = currentNode.GCost + moveCost;
                
                Node existingNode = openSet.Find(n => n.Position.Equals(neighborPos));
                
                if (existingNode == null || newGCost < existingNode.GCost)
                {
                    if (existingNode == null)
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.HCost = CalculateHCost(neighborPos, endPos);
                        neighborNode.Parent = currentNode;
                        openSet.Add(neighborNode);
                    }
                    else
                    {
                        existingNode.GCost = newGCost;
                        existingNode.Parent = currentNode;
                    }
                }
            }
        }
        
        Debug.Log($"=== A* FAILED: No path found after {iterations} iterations ===");
        return new List<Vector2Int>();
    }
    
        // OPTIMIZATION: Faster connectivity check
        private bool IsConnected(Vector2Int start, Vector2Int end)
        {
            // Simple Manhattan distance check first
            int distance = Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
            if (distance == 1) return true; // Adjacent tiles are always connected if walkable
        
            // Only do flood fill for longer distances
            if (distance > 20) return true; // Assume connected for very long distances
        
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
            queue.Enqueue(start);
            visited.Add(start);
        
            int maxVisited = 100; // Limit flood fill
            int visitedCount = 0;
        
            while (queue.Count > 0 && visitedCount < maxVisited)
            {
                var current = queue.Dequeue();
                visitedCount++;
            
                if (current == end)
                {
                    return true;
                }
            
                // Check 4 directions only (faster)
                Vector2Int[] cardinals = {
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(0, 1), new Vector2Int(0, -1)
                };
            
                foreach (var dir in cardinals)
                {
                    var neighbor = current + dir;
                
                    if (!_grid.InBounds(neighbor.x, neighbor.y))
                        continue;
                    
                    if (visited.Contains(neighbor))
                        continue;
                    
                    if (_grid.Tiles[neighbor.x, neighbor.y].Walkable || neighbor == end)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        
            return false;
        }
    
    private bool IsWalkable(Vector2Int pos, Vector2Int targetPos)
    {
        // Allow moving onto target position (player)
        if (pos.Equals(targetPos))
        {
            return true;
        }
            
        // Check bounds
        if (!_grid.InBounds(pos.x, pos.y))
        {
            // Debug.Log($"A*: Position {pos} out of bounds");
            return false;
        }
            
        // Check if tile is walkable
        if (!_grid.Tiles[pos.x, pos.y].Walkable)
        {
            // Debug.Log($"A*: Position {pos} not walkable (type: {_grid.Tiles[pos.x, pos.y].Type})");
            return false;
        }
            
        // Check if tile is occupied (except by target)
        var occupant = _grid.Tiles[pos.x, pos.y].Occupant;
        if (occupant != null && !pos.Equals(targetPos))
        {
            // Allow moving through non-blocking entities
            if (occupant.BlocksMovement)
            {
                // Debug.Log($"A*: Position {pos} occupied by blocking entity: {occupant.name}");
                return false;
            }
        }
            
        return true;
    }
    
    private bool IsValidDiagonalMove(Vector2Int from, Vector2Int to, Vector2Int targetPos)
    {
        // Check if this is a diagonal move
        int dx = to.x - from.x;
        int dy = to.y - from.y;
    
        if (dx == 0 || dy == 0)
        {
            // Not diagonal, always valid
            return true;
        }
    
        // For diagonal move, check both adjacent cardinal tiles
        // They must be walkable or be the target position
    
        // Check horizontal adjacent
        Vector2Int horizontal = new Vector2Int(from.x + dx, from.y);
        if (!horizontal.Equals(targetPos) && !IsWalkable(horizontal, targetPos))
        {
            return false;
        }
    
        // Check vertical adjacent
        Vector2Int vertical = new Vector2Int(from.x, from.y + dy);
        if (!vertical.Equals(targetPos) && !IsWalkable(vertical, targetPos))
        {
            return false;
        }
    
        return true;
    }
    
    private bool IsDiagonal(Vector2Int direction)
    {
        return direction.x != 0 && direction.y != 0;
    }
    
    private int CalculateHCost(Vector2Int a, Vector2Int b)
    {
        // Chebyshev distance for 8-direction movement
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) * StraightCost;
    }
    
    private List<Vector2Int> CalculatePath(Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;
        
        while (currentNode != null)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        
        path.Reverse();
        return path;
    }
    
    private void LogPath(List<Vector2Int> path)
    {
        if (path.Count == 0)
        {
            Debug.Log("Path: Empty");
            return;
        }
        
        string pathStr = "Path: ";
        for (int i = 0; i < path.Count; i++)
        {
            pathStr += $"{path[i]}";
            if (i < path.Count - 1) pathStr += " → ";
        }
        Debug.Log(pathStr);
    }
    
    private void DebugPathfindingFailure(Vector2Int start, Vector2Int end)
    {
        Debug.Log($"=== PATHFINDING DEBUG ===");
        Debug.Log($"Start: {start}, End: {end}");
        Debug.Log($"Start walkable: {IsWalkable(start, end)}");
        Debug.Log($"End walkable: {IsWalkable(end, end)}");
        
        // Check a few tiles around start
        Debug.Log($"Checking tiles around start {start}:");
        foreach (var dir in Directions)
        {
            var checkPos = start + dir;
            bool walkable = IsWalkable(checkPos, end);
            Debug.Log($"  {checkPos}: walkable={walkable}");
        }
        
        // Check if there's a clear line of sight
        Debug.Log($"Checking direct line from {start} to {end}:");
        Vector2Int cur = start;
        while (cur != end)
        {
            int dx = end.x - cur.x;
            int dy = end.y - cur.y;
            
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
                cur.x += dx > 0 ? 1 : -1;
            else
                cur.y += dy > 0 ? 1 : -1;
                
            Debug.Log($"  {cur}: walkable={IsWalkable(cur, end)}, type={_grid.Tiles[cur.x, cur.y].Type}");
        }
    }
    
    public Vector2Int? GetNextStep(Vector2Int startPos, Vector2Int endPos)
    {
        Debug.Log($"A*: Getting next step from {startPos} to {endPos}");
        
        // Quick check: if already adjacent, just return player position
        int distance = Mathf.Max(
            Mathf.Abs(startPos.x - endPos.x),
            Mathf.Abs(startPos.y - endPos.y)
        );
        
        if (distance == 1)
        {
            Debug.Log($"A*: Already adjacent to target, returning target position");
            return endPos;
        }
        
        List<Vector2Int> path = FindPath(startPos, endPos);
        
        if (path.Count > 1)
        {
            Debug.Log($"A*: Path found! Steps: {path.Count}, Next: {path[1]}");
            return path[1];
        }
        else if (path.Count == 1)
        {
            Debug.Log($"A*: Already at target position");
            return null;
        }
        else
        {
            Debug.LogError($"A*: No path found!");
            return null;
        }
    }
    
    // Simple Node class
    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;
        
        public Node(Vector2Int position) => Position = position;
        
        public override bool Equals(object obj) => 
            obj is Node other && Position.Equals(other.Position);
        
        public override int GetHashCode() => Position.GetHashCode();
    }
}