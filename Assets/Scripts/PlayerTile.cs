using UnityEngine;

public class PlayerTile : Tile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, tileType);
    }

    public override void OnPlayerSwap(Player player)
    {
    }

    public void OnSwappedByOther(Tile otherTile)
    {
    }
}