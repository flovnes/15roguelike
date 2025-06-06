using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public enum PlayerAttackMode
    {
        SwingLeft,
        SwingRight,
        ThrustForward
    }

    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 25;

    [HideInInspector] public Text healthText;

    [Header("Attack Pattern State")]
    public PlayerAttackMode currentAttackMode = PlayerAttackMode.SwingLeft;

    void Awake()
    {
    }

    void Start()
    {
    }

    public void ForceInitialHealthUIDisplayUpdate()
    {
        if (currentHealth == 0 && maxHealth > 0)
            currentHealth = maxHealth;
        UpdateHealthTextUI();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (GameManager.Instance != null) GameManager.Instance.GameOver("You are Dead.");
        }
        
        UpdateHealthTextUI();
    }

    public void RestoreHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        UpdateHealthTextUI();
    }

    void UpdateHealthTextUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}";
        }
    }

    public void MoveToVisualPosition(Vector3 targetWorldPos)
    {
        transform.position = targetWorldPos;
    }

    public List<Vector2Int> GetCurrentAttackPatternRelative(Vector2Int playerFacingDirection)
    {
        List<Vector2Int> pattern = new List<Vector2Int>();

        Vector2Int F = playerFacingDirection;
        Vector2Int L = new Vector2Int(-playerFacingDirection.y, playerFacingDirection.x);
        Vector2Int R = new Vector2Int(playerFacingDirection.y, -playerFacingDirection.x);
        Vector2Int B = -playerFacingDirection;


        switch (currentAttackMode)
        {
            case PlayerAttackMode.SwingLeft:
                pattern.Add(F);
                pattern.Add(L);
                pattern.Add(L + F);
                break;
            case PlayerAttackMode.SwingRight:
                pattern.Add(F);
                pattern.Add(R);
                pattern.Add(R + F);
                break;
            case PlayerAttackMode.ThrustForward:
                pattern.Add(F);
                pattern.Add(F + F);
                break;
        }
        return pattern;
    }

    public void CycleAttackMode()
    {
        switch (currentAttackMode)
        {
            case PlayerAttackMode.SwingLeft:
                currentAttackMode = PlayerAttackMode.SwingRight;
                break;
            case PlayerAttackMode.SwingRight:
                currentAttackMode = PlayerAttackMode.ThrustForward;
                break;
            case PlayerAttackMode.ThrustForward:
                currentAttackMode = PlayerAttackMode.SwingLeft;
                break;
        }
    }
    
    public PlayerAttackMode PeekNextAttackMode()
    {
        switch (currentAttackMode)
        {
            case PlayerAttackMode.SwingLeft: return PlayerAttackMode.SwingRight;
            case PlayerAttackMode.SwingRight: return PlayerAttackMode.ThrustForward;
            case PlayerAttackMode.ThrustForward: default: return PlayerAttackMode.SwingLeft;
        }
    }
}