using UnityEngine;
using System.Collections.Generic;

public class DeerTile : EnemyTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        maxHealth = 25;
        baseAttackDamage = 5;
        performActionDamage = 8;
        proximityDetectionRange = 5;
        base.Initialize(pos, TileType.Enemy);
        this.gameObject.name = $"Deerman_{pos.x}_{pos.y}";
    }

    public override List<Vector2Int> GetAttackPattern()
    {
        List<Vector2Int> pattern = new List<Vector2Int>();
        for (int i = 1; i <= 4; i++)
        {
            pattern.Add(new Vector2Int(facingDirection.x * i, facingDirection.y * i));
        }
        return pattern;
    }

    public override void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid)
    {
        if (GameManager.gameMagener.IsAnimating()) return;
        actionTurnCounter++;

        bool playerInSight = CheckProximityAndTurn(playerActualGridPos, proximityDetectionRange);

        if (playerInSight) 
        {
            List<Vector2Int> attackTiles = GetCurrentAttackPatternWorldPositions();
            if (attackTiles.Contains(playerActualGridPos))
            {
                bool clearShot = true;
                Vector2Int currentCheckPos = this.gridPosition;
                for (int i = 1; i <= 4; i++) 
                {
                    currentCheckPos += facingDirection;
                    if (currentCheckPos == playerActualGridPos) break;
                    if (!GameManager.gameMagener.InBounds(currentCheckPos)) { clearShot = false; break; }

                    Tile tileInPath = gameGrid[currentCheckPos.x, currentCheckPos.y];
                    if (tileInPath is EnemyTile || (tileInPath is EnvironmentTile env && env.blocksLineOfSight))
                    {
                        clearShot = false;
                        break;
                    }
                }

                if (clearShot)
                    player.TakeDamage(performActionDamage);
            }
        }
    }
}