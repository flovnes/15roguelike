using UnityEngine;
using System.Collections.Generic;

public class TrollTile : EnemyTile
{
    public override void Initialize(Vector2Int pos, TileType tileType)
    {
        maxHealth = 100;
        baseAttackDamage = 20;
        performActionDamage = 15;
        proximityDetectionRange = 1;
        base.Initialize(pos, TileType.Enemy);
        this.gameObject.name = $"Troll_{pos.x}_{pos.y}";
        actionTurnCounter = Random.Range(1,4);
    }

    public override List<Vector2Int> GetRelativeAttackPattern()
    {
        return new List<Vector2Int>
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
    }

    public override void PerformAction(Player player, Vector2Int playerActualGridPos, Tile[,] gameGrid)
    {
        if (GameManager.Instance.IsAnimating()) return;
        actionTurnCounter++;

        CheckProximityAndTurn(playerActualGridPos, proximityDetectionRange);

        if (actionTurnCounter % 3 == 0)
        {
            List<Vector2Int> attackTiles = GetCurrentAttackPatternWorldPositions();
            foreach (var tilePos in attackTiles)
            {
                if (GameManager.Instance.InBounds(tilePos))
                {
                    if (tilePos == playerActualGridPos)
                    {
                        player.TakeDamage(performActionDamage);
                    }
                }
            }
        }
    }

    protected override void HandleLootDrop()
    {
        GameManager.Instance?.ReplaceTileInGridDataAndVisuals(this.gridPosition, TileType.Key, GameManager.Instance.keyTilePrefab);
    }
}