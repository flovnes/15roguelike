using UnityEngine;
public class BushTile : EnvironmentTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Environment);
        gameObject.name = $"Bush_Tile_{pos.x}_{pos.y}";
    }
    public override void OnPlayerEnter(Player player)
    {
        base.OnPlayerEnter(player);
    }
}