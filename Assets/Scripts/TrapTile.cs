using UnityEngine;

public class TrapTile : Tile
{
    public int damage = 10;
    private bool isActive = true;

    [Header("State Sprites")]
    public Sprite activeSprite;
    public Sprite inactiveSprite;

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Trap);
        if (sr != null && activeSprite != null)
        {
            sr.sprite = activeSprite;
            originalColor = sr.color;
        }
        this.gameObject.name = $"Trap_Tile_{pos.x}_{pos.y}";
    }

    public override void OnPlayerSwap(Player player)
    {
        if (!isActive || player == null) return;

        player.TakeDamage(damage);
        isActive = false;

        if (sr != null && inactiveSprite != null)
        {
            sr.sprite = inactiveSprite;
        }
    }
}