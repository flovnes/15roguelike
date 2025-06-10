using UnityEngine;
using System.Collections.Generic;

public class LizardmanTile : EnemyTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        maxHealth = 20;
        baseAttackDamage = 8;
        performActionDamage = 6;
        proximityDetectionRange = 3;
        base.Initialize(pos, TileType.Enemy);
        this.gameObject.name = $"Lizardman_{pos.x}_{pos.y}";
    }

    public override List<Vector2Int> GetAttackPattern()
    {
        return new List<Vector2Int> { facingDirection };
    }

    public override void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid)
    {
        if (GameManager.gameMagener.IsAnimating()) return;
        actionTurnCounter++;    

        bool playerInProximity = CheckProximityAndTurn(playerActualGridPos, proximityDetectionRange);

        if (playerInProximity)
        {
            List<Vector2Int> attackTiles = GetCurrentAttackPatternWorldPositions();
            if (attackTiles.Contains(playerActualGridPos))
            {
                player.TakeDamage(performActionDamage);
            }
            else
            {
                AttemptMoveTowards(playerActualGridPos, gameGrid);
            }
        }
    }
}