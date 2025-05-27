using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyTile : Tile
{
    public int maxHealth = 50;
    public int currentHealth;
    public int attackDamage = 15;
    public int proximityAttackDamage = 10;
    private bool isDefeated = false;

    public Vector2Int facingDirection { get; protected set; }

    protected enum AttackPatternType { ForwardLine, Cone, Adjacent }
    [SerializeField] protected AttackPatternType attackPattern = AttackPatternType.Cone;
    [SerializeField] protected int attackRange = 2;

    protected override void Awake()
    {
        base.Awake();
        SetRandomFacingDirection();
        currentHealth = maxHealth; 
    }

    public void SetRandomFacingDirection()
    {
        int rand = Random.Range(0, 4);
        switch (rand)
        {
            case 0: facingDirection = Vector2Int.up; break;
            case 1: facingDirection = Vector2Int.down; break;
            case 2: facingDirection = Vector2Int.left; break;
            case 3: facingDirection = Vector2Int.right; break;
        }
    }

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, tileType);
        currentHealth = maxHealth;
        SetRandomFacingDirection();
        UpdateOriginalColor();
    }


    public override void OnPlayerEnter(Player player)
    {
        if (isDefeated) return;
        player.TakeDamage(attackDamage);
    }
    
    public void TakeDamage(int damageAmount)
    {
        if (isDefeated) return;

        currentHealth -= damageAmount;

        if (sr != null) StartCoroutine(FlashColor(Color.red, 0.15f));


        if (currentHealth <= 0)
        {
            currentHealth = 0;
            DefeatEnemy();
        }
    }

    // enemy turn
    public virtual void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid)
    {
        if (isDefeated || !gameObject.activeInHierarchy) return;

        List<Vector2Int> currentAttackTiles = GetCurrentAttackPatternWorldPositions();
        bool playerInAttackZone = false;
        foreach (Vector2Int targetPos in currentAttackTiles)
        {
            if (targetPos == playerActualGridPos)
            {
                playerInAttackZone = true;
                break;
            }
        }

        if (playerInAttackZone)
        {
            player.TakeDamage(proximityAttackDamage);
            Vector2Int dirToPlayer = playerActualGridPos - gridPosition;
            if (Mathf.Abs(dirToPlayer.x) > Mathf.Abs(dirToPlayer.y)) facingDirection = new Vector2Int(System.Math.Sign(dirToPlayer.x), 0);
            else if (dirToPlayer.y != 0) facingDirection = new Vector2Int(0, System.Math.Sign(dirToPlayer.y));

        }
    }

    public void DefeatEnemy()
    {
        if (isDefeated) return;
        isDefeated = true;
        if (sr != null) sr.color = Color.grey; 
        GameManager.Instance?.EnemyDefeated();
        UpdateOriginalColor();
    }

    public bool IsDefeated() { return isDefeated; }

    // Returns a list of GRID coordinates relative to the enemy that are part of its pattern
    public virtual List<Vector2Int> GetRelativeAttackPattern()
    {
        List<Vector2Int> pattern = new List<Vector2Int>();
        switch (attackPattern)
        {
            case AttackPatternType.ForwardLine:
                for (int i = 1; i <= attackRange; i++)
                {
                    pattern.Add(new Vector2Int(facingDirection.x * i, facingDirection.y * i));
                }
                break;

            case AttackPatternType.Cone:
                pattern.Add(facingDirection);
                if (attackRange >= 1) // Only add while range allows
                {
                    // Get perpendicular directions
                    Vector2Int perp1 = new(facingDirection.y, -facingDirection.x);
                    Vector2Int perp2 = new(-facingDirection.y, facingDirection.x);
                    pattern.Add(facingDirection + perp1); // Diagonal front-left
                    pattern.Add(facingDirection + perp2); // Diagonal front-right
                }
                break;

            case AttackPatternType.Adjacent:
                pattern.Add(Vector2Int.up);
                pattern.Add(Vector2Int.down);
                pattern.Add(Vector2Int.left);
                pattern.Add(Vector2Int.right);
                break;
        }
        return pattern;
    }

    public List<Vector2Int> GetCurrentAttackPatternWorldPositions()
    {
        List<Vector2Int> worldPositions = new List<Vector2Int>();
        List<Vector2Int> relativePattern = GetRelativeAttackPattern();

        foreach (Vector2Int relativePos in relativePattern)
            worldPositions.Add(gridPosition + relativePos);
            
        return worldPositions;
    }
    
    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        if (sr == null) yield break;
        Color actualOriginalColor = originalColor;
        
        sr.color = flashColor;
        yield return new WaitForSeconds(duration / 2);
        
        sr.color = actualOriginalColor;
        yield return new WaitForSeconds(duration / 2);
        
        if (!isDefeated) sr.color = actualOriginalColor; else sr.color = Color.grey;
    }
}