using UnityEngine;

public class PlayerTile : Tile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, tileType);
    }

    public override void OnPlayerEnter(Player player)
    {
        Debug.LogWarning("??");
    }

    public void OnSwappedByOther(Tile otherTile)
    {
        // after enemy ai
    }
}