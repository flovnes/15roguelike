using UnityEngine;

public abstract class EnvironmentTile : Tile
{
    public bool blocksLineOfSight = false;

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, tileType);
        type = TileType.Environment;
    }

    public override void OnPlayerSwap(Player player)
    {
    }
}