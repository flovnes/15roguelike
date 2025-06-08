using UnityEngine;
public class RockTile : EnvironmentTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Environment);
        this.gameObject.name = $"Rock_Tile_{pos.x}_{pos.y}";
        blocksLineOfSight = false;
    }
    public override void OnPlayerSwap(Player player)
    {
        base.OnPlayerSwap(player);
    }
}