using UnityEngine;

public class GoalTile : Tile
{
    public Sprite activeSprite;
    public Sprite inactiveSprite;

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Goal);
        this.gameObject.name = $"Goal_Tile_{pos.x}_{pos.y}";
        if (sr != null && inactiveSprite != null)
        {
            sr.sprite = inactiveSprite;
            originalColor = sr.color;
        }
    }

    public void UpdateVisualState()
    {
        if (GameManager.Instance == null || sr == null) return;

        bool currentlyHasKey = GameManager.Instance.HasKey();
        sr.sprite = currentlyHasKey ? activeSprite : inactiveSprite;
        originalColor = sr.color;
    }

    public override void OnPlayerEnter(Player player)
    {
        if (GameManager.Instance == null || player == null) return;

        if (GameManager.Instance.HasKey())
        {
            GameManager.Instance.LevelCleared();
        }
    }
}