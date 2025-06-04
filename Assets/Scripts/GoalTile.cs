using UnityEngine;

public class GoalTile : Tile
{
    [Header("State Sprites")]
    public Sprite activeSprite;
    public Sprite inactiveSprite;
    private bool isOpen = false;

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, TileType.Goal);
        this.gameObject.name = $"Goal_Tile_{pos.x}_{pos.y}";
        isOpen = false;
        if (sr != null && inactiveSprite != null)
        {
            sr.sprite = inactiveSprite;
            originalColor = sr.color;
        }
    }

    public void UpdateVisualState()
    {
        if (GameManager.Instance == null) return;

        isOpen = GameManager.Instance.HasKey();
        if (sr != null)
        {
            sr.sprite = isOpen ? activeSprite : inactiveSprite;
            originalColor = sr.color;
        }
    }

    public override void OnPlayerEnter(Player player)
    {
        if (GameManager.Instance == null) return;

        if (isOpen)
        {
            GameManager.Instance.LevelCleared();
        }
    }
}