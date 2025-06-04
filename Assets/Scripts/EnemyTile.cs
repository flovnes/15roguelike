// using UnityEngine;
// using System.Collections.Generic;
// using System.Collections;

// public class BaseEnemyTile : Tile
// {
//     public int maxHealth = 50;
//     public int currentHealth;
//     public int attackDamage = 15;
//     private bool isDefeated = false;

//     public Vector2Int facingDirection { get; protected set; }

//     protected enum AttackPatternType { ForwardLine, Cone, Adjacent }
//     [SerializeField] protected AttackPatternType attackPattern = AttackPatternType.Cone;
//     [SerializeField] protected int attackRange = 2;

//     private int turnsSincePlayerSpotted = 0;
//     private bool playerHasBeenSpotted = false;

//     protected override void Awake()
//     {
//         base.Awake();
//         SetRandomFacingDirection();
//         currentHealth = maxHealth;
//     }

//     public void SetRandomFacingDirection()
//     {
//         int rand = Random.Range(0, 4);
//         switch (rand)
//         {
//             case 0: facingDirection = Vector2Int.up; break;
//             case 1: facingDirection = Vector2Int.down; break;
//             case 2: facingDirection = Vector2Int.left; break;
//             case 3: facingDirection = Vector2Int.right; break;
//         }
//     }

//     public override void Initialize(Vector2Int pos, TileType tileType)
//     {
//         base.Initialize(pos, tileType);
//         currentHealth = maxHealth;
//         SetRandomFacingDirection();
//         UpdateOriginalColor();
//     }


//     public override void OnPlayerEnter(Player player)
//     {
//         if (isDefeated) return;
//         player.TakeDamage(attackDamage);
//     }

//     public void TakeDamage(int damageAmount)
//     {
//         if (isDefeated) return;

//         currentHealth -= damageAmount;

//         if (sr != null) StartCoroutine(FlashColor(Color.red, 0.15f));


//         if (currentHealth <= 0)
//         {
//             currentHealth = 0;
//             DefeatEnemy();
//         }
//     }

//     public virtual void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid)
//     {
//         if (isDefeated || !gameObject.activeInHierarchy || GameManager.Instance.IsAnimating()) return;

//         int distanceToPlayer = Mathf.Abs(playerActualGridPos.x - this.gridPosition.x) +
//                             Mathf.Abs(playerActualGridPos.y - this.gridPosition.y);

//         bool playerInAttackZone = false;


//         if (distanceToPlayer <= 2)
//         {
//             List<Vector2Int> currentAttackTiles = GetCurrentAttackPatternWorldPositions();
//             playerHasBeenSpotted = true;
//             turnsSincePlayerSpotted = 0;

//             foreach (Vector2Int targetPosInPattern in currentAttackTiles)
//             {
//                 if (targetPosInPattern == playerActualGridPos)
//                 {
//                     playerInAttackZone = true;
//                     break;
//                 }
//             }

//             if (playerInAttackZone)
//             {
//                 player.TakeDamage(attackDamage);
//             }
//             else
//             {
//                 Vector2Int directionToPlayer = playerActualGridPos - this.gridPosition;
//                 if (directionToPlayer != Vector2Int.zero)
//                 {
//                     if (Mathf.Abs(directionToPlayer.x) >= Mathf.Abs(directionToPlayer.y))
//                     {
//                         SetFacingDirection(new Vector2Int(System.Math.Sign(directionToPlayer.x), 0));
//                     }
//                     else
//                     {
//                         SetFacingDirection(new Vector2Int(0, System.Math.Sign(directionToPlayer.y)));
//                     }
//                 }
//             }
//         }
//         else if (playerHasBeenSpotted)
//         {
//             turnsSincePlayerSpotted++;
//         }

//         if (!playerInAttackZone && playerHasBeenSpotted && turnsSincePlayerSpotted % 3 == 0)
//         {
//             AttemptMoveTowardsPlayer(playerActualGridPos, gameGrid);
//             if (playerInAttackZone)
//                 player.TakeDamage(attackDamage);
//         }
//     }

//     protected virtual void AttemptMoveTowardsPlayer(Vector2Int playerPos, Tile[,] gameGrid)
//     {
//         if (GameManager.Instance == null) return;

//         Vector2Int moveDirection = Vector2Int.zero;
//         int bestDist = int.MaxValue; 

//         Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
//         List<Vector2Int> possibleMoves = new();

//         foreach (Vector2Int dir in directions)
//         {
//             Vector2Int potentialNextPos = this.gridPosition + dir;
//             if (GameManager.Instance.InBounds(potentialNextPos))
//             {
//                 Tile tileAtTarget = gameGrid[potentialNextPos.x, potentialNextPos.y];
//                 int distToPlayer = Mathf.Abs(potentialNextPos.x - playerPos.x) + Mathf.Abs(potentialNextPos.y - playerPos.y);
//                 if (distToPlayer < bestDist)
//                 {
//                     bestDist = distToPlayer;
//                     possibleMoves.Clear();
//                     possibleMoves.Add(dir);
//                 }
//                 else if (distToPlayer == bestDist)
//                 {
//                     possibleMoves.Add(dir);
//                 }
//             }
//         }

//         if (possibleMoves.Count > 0)
//         {
//             moveDirection = possibleMoves[Random.Range(0, possibleMoves.Count)];
//             GameManager.Instance.AttemptEnemyTileSwap(this, this.gameObject, moveDirection);
//         }
//     }

//     public void DefeatEnemy()
//     {
//         if (isDefeated) return;
//         isDefeated = true;
//         if (sr != null) sr.color = Color.grey;
//         GameManager.Instance?.EnemyDefeated();
//         UpdateOriginalColor();
//     }

//     public bool IsDefeated() { return isDefeated; }

//     public virtual List<Vector2Int> GetRelativeAttackPattern()
//     {
//         List<Vector2Int> pattern = new List<Vector2Int>();
//         switch (attackPattern)
//         {
//             case AttackPatternType.ForwardLine:
//                 for (int i = 1; i <= attackRange; i++)
//                 {
//                     pattern.Add(new Vector2Int(facingDirection.x * i, facingDirection.y * i));
//                 }
//                 break;

//             case AttackPatternType.Cone:
//                 pattern.Add(facingDirection);
//                 if (attackRange >= 1)
//                 {
//                     Vector2Int perp1 = new(facingDirection.y, -facingDirection.x);
//                     Vector2Int perp2 = new(-facingDirection.y, facingDirection.x);
//                     pattern.Add(facingDirection + perp1);
//                     pattern.Add(facingDirection + perp2);
//                 }
//                 break;

//             case AttackPatternType.Adjacent:
//                 pattern.Add(Vector2Int.up);
//                 pattern.Add(Vector2Int.down);
//                 pattern.Add(Vector2Int.left);
//                 pattern.Add(Vector2Int.right);
//                 break;
//         }
//         return pattern;
//     }

//     public List<Vector2Int> GetCurrentAttackPatternWorldPositions()
//     {
//         List<Vector2Int> worldPositions = new List<Vector2Int>();
//         List<Vector2Int> relativePattern = GetRelativeAttackPattern();

//         foreach (Vector2Int relativePos in relativePattern)
//             worldPositions.Add(gridPosition + relativePos);

//         return worldPositions;
//     }

//     private IEnumerator FlashColor(Color flashColor, float duration)
//     {
//         if (sr == null) yield break;
//         Color actualOriginalColor = originalColor;

//         sr.color = flashColor;
//         yield return new WaitForSeconds(duration / 2);

//         sr.color = actualOriginalColor;
//         yield return new WaitForSeconds(duration / 2);

//         if (!isDefeated) sr.color = actualOriginalColor; else sr.color = Color.grey;
//     }
    
//     public void SetFacingDirection(Vector2Int newDirection)
//     {
//         if (newDirection != Vector2Int.zero)
//         {
//             facingDirection = newDirection;
//         }
//     }
// }