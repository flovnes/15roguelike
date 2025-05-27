using UnityEngine;
public class RockTile : EnvironmentTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Environment);
        this.gameObject.name = $"Rock_Tile_{pos.x}_{pos.y}";
    }
    public override void OnPlayerEnter(Player player)
    {
        base.OnPlayerEnter(player);
    }
}