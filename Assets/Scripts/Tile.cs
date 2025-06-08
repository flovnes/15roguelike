using UnityEngine;

public enum TileType
{
    Empty,
    Trap,
    Enemy,
    HealthPickup,
    Player,
    Environment,
    Goal,
    Key
}

public abstract class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public TileType type;

    protected SpriteRenderer sr;
    protected Color originalColor;
    protected bool isHighlightedAsAttackTarget = false;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    public virtual void Initialize(Vector2Int pos, TileType tileType)
    {
        gridPosition = pos;
        type = tileType;
        gameObject.name = $"{type}_Tile_{pos.x}_{pos.y}";
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    public virtual void OnPlayerSwap(Player player)
    {
    }

    public virtual void UpdateVisuals(Sprite sprite) // use later for ?changing hero classes maybe
    {
        if (sr != null) sr.sprite = sprite;
    }

    public virtual void SetHighlight(bool isHighlighted, Color highlightColor)
    {
        if (sr == null) return;
        isHighlightedAsAttackTarget = isHighlighted;
        sr.color = isHighlighted ? highlightColor : originalColor;
    }
}

