using UnityEngine;

public class HealthPickupTile : Tile
{
    public int healthToRestore = 20;
    private bool isActive = true;

    [Header("State Sprites")]
    public Sprite activeSprite;
    public Sprite inactiveSprite;

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.HealthPickup);
        if (sr != null && activeSprite != null)
        {
            sr.sprite = activeSprite;
            originalColor = sr.color;
        }
        this.gameObject.name = $"HP_Tile_{pos.x}_{pos.y}";
    }

    public override void OnPlayerSwap(Player player)
    {
        if (!isActive || player == null) return;

        player.RestoreHealth(healthToRestore);
        isActive = false;

        if (sr != null && inactiveSprite != null)
        {
            sr.sprite = inactiveSprite;
        }
    }
}