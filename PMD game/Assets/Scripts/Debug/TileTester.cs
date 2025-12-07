using UnityEngine;

public class EffectTileTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestEffectTiles();
        }
    }
    
    void TestEffectTiles()
    {
        Debug.Log("=== TESTING EFFECT TILES ===");
        
        var gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null || gridEntity.Grid == null)
        {
            Debug.LogError("No GridEntity or Grid found!");
            return;
        }
        
        // Check current tile
        var currentTile = gridEntity.Grid.Tiles[gridEntity.GridX, gridEntity.GridY];
        Debug.Log($"Current tile: {currentTile.Type}");
        Debug.Log($"Has effect: {currentTile.TileEffect != null}");
        
        if (currentTile.Type == TileType.Effect)
        {
            Debug.Log("You're standing on an effect tile!");
            //gridEntity.OnSteppedOnEffect(currentTile);
        }
        
        // Check surrounding tiles
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int nx = gridEntity.GridX + dx;
                int ny = gridEntity.GridY + dy;
                
                if (gridEntity.Grid.InBounds(nx, ny))
                {
                    var tile = gridEntity.Grid.Tiles[nx, ny];
                    if (tile.Type == TileType.Effect)
                    {
                        Debug.Log($"Effect tile at {nx},{ny}: {tile.TileEffect?.EffectName ?? "No effect"}");
                    }
                }
            }
        }
    }
}