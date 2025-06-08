using UnityEngine;

public class EmptyTile : Tile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Environment);
        gameObject.name = $"Empty_Tile_{pos.x}_{pos.y}";
    }
    
    public override void OnPlayerSwap(Player player)
    {
    }
}