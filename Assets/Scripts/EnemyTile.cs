using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class EnemyTile : Tile
{
    public Sprite deadSprite;
    public int maxHealth = 30;
    public int currentHealth;
    public int baseAttackDamage = 10;
    public int performActionDamage = 5;

    protected bool isDefeated = false;
    public Vector2Int facingDirection { get; protected set; }

    protected int actionTurnCounter = 1;
    protected bool playerHasBeenSpotted = false;
    protected int turnsSincePlayerSpotted = 0;
    public int proximityDetectionRange = 2;


    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHealth;
        if (facingDirection == Vector2Int.zero) SetDefaultFacingDirection();
    }

    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        base.Initialize(pos, tileType);
        currentHealth = maxHealth;
        if (facingDirection == Vector2Int.zero) SetDefaultFacingDirection();
        isDefeated = false;
        actionTurnCounter = Random.Range(1, 3);
        playerHasBeenSpotted = false;
        turnsSincePlayerSpotted = 0;
    }

    public virtual void SetDefaultFacingDirection()
    {
        int rand = Random.Range(0, 4);
        if (rand == 0) facingDirection = Vector2Int.up;
        else if (rand == 1) facingDirection = Vector2Int.down;
        else if (rand == 2) facingDirection = Vector2Int.left;
        else facingDirection = Vector2Int.right;
    }

    public virtual void SetFacingDirection(Vector2Int newDirection)
    {
        if (newDirection != Vector2Int.zero)
        {
            facingDirection = newDirection;
        }
    }

    public virtual void TakeDamage(int damageAmount)
    {
        if (sr != null) StartCoroutine(FlashColorFeedback(GameManager.gameMagener.enemyHitFlashColor, GameManager.gameMagener.enemyHitFlashDuration));

        if (isDefeated) return;
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            DefeatEnemy();
        }
    }

    protected virtual IEnumerator FlashColorFeedback(Color flashColor, float duration)
    {
        Color actualOriginalColor = originalColor;
        sr.color = flashColor;
        yield return new WaitForSeconds(duration);
        sr.color = actualOriginalColor;
    }

    public virtual void DefeatEnemy()
    {
        if (isDefeated) return;
        isDefeated = true;
        if (sr != null)
        {
            if (deadSprite != null)
            {
                sr.sprite = deadSprite;
            }
        }

        HandleLootDrop();

        GameManager.gameMagener?.EnemyDefeated();
    }

    protected virtual void HandleLootDrop()
    {
    }

    public bool IsDefeated() { return isDefeated; }

    public override void OnPlayerSwap(Player player)
    {
        if (isDefeated || player == null) return;
        player.TakeDamage(baseAttackDamage);
    }

    public abstract void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid);
    public abstract List<Vector2Int> GetAttackPattern();
    public List<Vector2Int> GetCurrentAttackPatternWorldPositions()
    {
        List<Vector2Int> worldPositions = new List<Vector2Int>();
        List<Vector2Int> relativePattern = GetAttackPattern();
        foreach (Vector2Int relativePos in relativePattern)
        {
            worldPositions.Add(gridPosition + relativePos);
        }
        return worldPositions;
    }

    protected bool CheckProximityAndTurn(Vector2Int playerPos, int detectionRange)
    {
        int distanceToPlayer = Mathf.Abs(playerPos.x - gridPosition.x) +
                               Mathf.Abs(playerPos.y - gridPosition.y);

        if (distanceToPlayer <= detectionRange)
        {
            if (!playerHasBeenSpotted) Debug.Log($"{gameObject.name} spotted player!");

            playerHasBeenSpotted = true;
            turnsSincePlayerSpotted = 0;

            Vector2Int directionToPlayer = playerPos - gridPosition;
            if (directionToPlayer != Vector2Int.zero)
            {
                if (Mathf.Abs(directionToPlayer.x) >= Mathf.Abs(directionToPlayer.y))
                    SetFacingDirection(new Vector2Int(System.Math.Sign(directionToPlayer.x), 0));
                else
                    SetFacingDirection(new Vector2Int(0, System.Math.Sign(directionToPlayer.y)));
            }
            return true;
        }
        else if (playerHasBeenSpotted)
        {
            turnsSincePlayerSpotted++;
        }
        return false;
    }

    protected virtual void AttemptMoveTowards(Vector2Int playerPos, Tile[,] gameGrid)
    {
        if (GameManager.gameMagener == null) return;

        Vector2Int bestMoveDir;
        int minDistance = int.MaxValue;
        List<Vector2Int> potentialMoveDirs = new List<Vector2Int>();

        Vector2Int[] cardinalDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int dir in cardinalDirections)
        {
            Vector2Int nextPos = gridPosition + dir;
            if (GameManager.gameMagener.InBounds(nextPos))
            {
                Tile tileAtNextPos = gameGrid[nextPos.x, nextPos.y];
                int dist = Mathf.Abs(nextPos.x - playerPos.x) + Mathf.Abs(nextPos.y - playerPos.y);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    potentialMoveDirs.Clear();
                    potentialMoveDirs.Add(dir);
                }
                else if (dist == minDistance)
                {
                    potentialMoveDirs.Add(dir);
                }
            }
        }

        if (potentialMoveDirs.Count > 0)
        {
            bestMoveDir = potentialMoveDirs[Random.Range(0, potentialMoveDirs.Count)];
            GameManager.gameMagener.AttemptEnemyTileSwap(this, gameObject, bestMoveDir);
        }
    }
}