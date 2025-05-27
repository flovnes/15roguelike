using UnityEngine;

public class HealthPickupTile : Tile
{
    public int healthToRestore = 50;
    private bool isConsumed = false;

    public override void OnPlayerEnter(Player player)
    {
        if (isConsumed)
            return;

        player.RestoreHealth(healthToRestore);
        isConsumed = true;
        GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }
}