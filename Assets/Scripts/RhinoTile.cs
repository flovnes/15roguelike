// RhinomanTile.cs
using UnityEngine;
using System.Collections.Generic;

public class RhinomanTile : EnemyTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        maxHealth = 40;
        baseAttackDamage = 12;
        performActionDamage = 10;
        proximityDetectionRange = 2;
        base.Initialize(pos, TileType.Enemy);
        this.gameObject.name = $"Rhinoman_{pos.x}_{pos.y}";
    }

    public override List<Vector2Int> GetRelativeAttackPattern()
    {
        List<Vector2Int> pattern = new List<Vector2Int>();
        Vector2Int front = facingDirection;
        Vector2Int leftDiag = front + new Vector2Int(-facingDirection.y, facingDirection.x);
        Vector2Int rightDiag = front + new Vector2Int(facingDirection.y, -facingDirection.x);

        pattern.Add(front);
        pattern.Add(leftDiag);
        pattern.Add(rightDiag);
        return pattern;
    }

    public override void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid)
    {
        if (GameManager.Instance.IsAnimating()) return;
        actionTurnCounter++;

        bool playerInProximity = CheckProximityAndTurn(playerActualGridPos, proximityDetectionRange);

        if (playerHasBeenSpotted && actionTurnCounter % 2 == 0)
        {
            List<Vector2Int> attackTiles = GetCurrentAttackPatternWorldPositions();
            bool playerHit = false;
            foreach (var tilePos in attackTiles)
            {
                if (tilePos == playerActualGridPos)
                {
                    Debug.Log($"{gameObject.name} at {gridPosition} attacks player in cleave for {performActionDamage} damage.");
                    player.TakeDamage(performActionDamage);
                    playerHit = true;
                    break;
                }
            }
            if (!playerHit)
            {
                AttemptMoveTowards(playerActualGridPos, gameGrid);
            }
        }
    }
}