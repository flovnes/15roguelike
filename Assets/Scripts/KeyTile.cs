using UnityEngine;

public class KeyTile : Tile
{
    [Header("Visuals")]
    public Sprite keySprite;

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Key);
        this.gameObject.name = $"Key_Tile_{pos.x}_{pos.y}";
        if (sr != null && keySprite != null)
        {
            sr.sprite = keySprite;
            originalColor = sr.color;
        }
    }

    public override void OnPlayerSwap(Player player)
    {
        if (GameManager.gameMagener == null || GameManager.gameMagener.HasKey()) return;

        GameManager.gameMagener.CollectKey();

        GameManager.gameMagener.ReplaceTileInGridDataAndVisuals(this.gridPosition, TileType.Empty, GameManager.gameMagener.emptyTilePrefab);
    }
}