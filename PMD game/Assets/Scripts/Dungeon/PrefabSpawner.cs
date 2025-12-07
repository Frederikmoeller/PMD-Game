using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct PrefabMapping
{
    public TileType Type;
    public GameObject Prefab;
}

public class PrefabSpawner : MonoBehaviour
{
    public DungeonGenerator Generator;
    public List<PrefabMapping> Mappings;
    public Transform Parent;

    public void SpawnAll()
    {
        if (Generator == null || Generator.Grid == null) { Debug.LogError("No grid"); return; }
        if (Parent == null) Parent = transform;
        
        // destroy previous children
        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(Parent.GetChild(i).gameObject);
        }

        var grid = Generator.Grid;
        for (int x = 0; x < grid.Width; x++)
        for (int y = 0; y < grid.Height; y++)
        {
            var t = grid.Tiles[x, y];
            if (t.Type == TileType.Floor || t.Type == TileType.Wall) continue;

            foreach (var m in Mappings)
            {
                if (m.Type == t.Type && m.Prefab != null)
                {
                    var go = Instantiate(m.Prefab, new Vector3(x, y, 0), Quaternion.identity, Parent);
                    var ge = go.GetComponent<GridEntity>();
                    if (ge != null)
                    {
                        ge.GridX = x;
                        ge.GridY = y;
                        ge.SetGrid(Generator.Grid);
                        Generator.Grid.Tiles[x, y].Occupant = ge;
                    }
                    break;
                }
            }
        }
    }

    public void SpawnFromGrid(DungeonGrid grid)
    {
        // Clear Previous
        //Clear();

        if (grid == null){ Debug.LogError("No grid provided"); return; }
        if (Parent == null) Parent = transform;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var t = grid.Tiles[x, y];
                if (t.Type == TileType.Effect || t.Type == TileType.Stairs || t.Type == TileType.Floor ||
                    t.Type == TileType.Wall) continue;

                foreach (var m in Mappings)
                {
                    if (m.Type != t.Type || m.Prefab == null) continue;
                    var go = Instantiate(m.Prefab, new Vector3(x + 0.5f, y + 0.5f, 0), quaternion.identity, Parent);
                    var ge = go.GetComponent<GridEntity>();
                    if (ge != null)
                    {
                        ge.GridX = x;
                        ge.GridY = y;
                        ge.SetGrid(grid);
                        if (ge.BlocksMovement)
                        {
                            grid.Tiles[x, y].Occupant = ge;
                        }
                    }
                    break;
                }
            }
        }
    }

    public void Clear()
    {
        if (Parent == null) Parent = transform;

        for (int i = Parent.childCount; i >= 0; i--)
        {
            if (Application.isPlaying && Parent.childCount > 0)
            {
                Destroy(Parent.GetChild(i).gameObject);
            }
            else
            {
                //DestroyImmediate(Parent.GetChild(i).gameObject);
            }
        }
    }
}
