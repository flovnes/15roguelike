using UnityEngine;
public class BoulderTile : EnvironmentTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Environment);
        gameObject.name = $"Boulder_Tile_{pos.x}_{pos.y}";
        blocksLineOfSight = true;
    }
    public override void OnPlayerSwap(Player player)
    {
        base.OnPlayerSwap(player);
    }
}