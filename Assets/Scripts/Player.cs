using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [HideInInspector] public Text healthText;

    public enum AttackPattern
    {
        Left,
        Right,
        Forward
    }

    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 25;

    [Header("Attack Pattern State")]
    public AttackPattern currentAttackPattern = AttackPattern.Left;

    [Header("Damage Flash Settings")]
    public Color healthTextDamageFlashColor = Color.red;
    public Color originalColor;
    public float healthTextFlashDuration = 0.3f;
    private Coroutine healthFlashCoroutine;

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
        originalColor = healthText.color;
        UpdateHealthTextUI();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (healthFlashCoroutine != null)
        {
            StopCoroutine(healthFlashCoroutine);
        }
        if (healthText != null)
        {
           healthFlashCoroutine = StartCoroutine(FlashHealthText());
        }
        else
        {
            UpdateHealthTextUI();
        }


        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateHealthTextUI();
            GameManager.gameMagener?.GameOver("You are Dead.");
        }
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
        healthText.color = originalColor;
    }

    private IEnumerator FlashHealthText()
    {
        if (healthText == null) yield break;

        healthText.color = healthTextDamageFlashColor;
        healthText.text = $"{currentHealth}";
        yield return new WaitForSeconds(healthTextFlashDuration);

        healthText.color = originalColor;
        healthFlashCoroutine = null;
    }

    public void MoveToVisualPosition(Vector3 targetWorldPos)
    {
        transform.position = targetWorldPos;
    }

    public List<Vector2Int> GetCurrentAttackPatternRelative(Vector2Int facing)
    {
        List<Vector2Int> pattern = new List<Vector2Int>();

        Vector2Int F = facing;
        Vector2Int L = new Vector2Int(-facing.y, facing.x);
        Vector2Int R = new Vector2Int(facing.y, -facing.x);
        Vector2Int B = -facing;


        switch (currentAttackPattern)
        {
            case AttackPattern.Left:
                pattern.Add(F);
                pattern.Add(L);
                pattern.Add(L + F);
                break;
            case AttackPattern.Right:
                pattern.Add(F);
                pattern.Add(R);
                pattern.Add(R + F);
                break;
            case AttackPattern.Forward:
                pattern.Add(F);
                pattern.Add(F + F);
                break;
        }
        return pattern;
    }

    public void CycleAttackPattern()
    {
        switch (currentAttackPattern)
        {
            case AttackPattern.Left:
                currentAttackPattern = AttackPattern.Right;
                break;
            case AttackPattern.Right:
                currentAttackPattern = AttackPattern.Forward;
                break;
            case AttackPattern.Forward:
                currentAttackPattern = AttackPattern.Left;
                break;
        }
    }
    
    public AttackPattern PeekNextAttackMode()
    {
        switch (currentAttackPattern)
        {
            case AttackPattern.Left: return AttackPattern.Right;
            case AttackPattern.Right: return AttackPattern.Forward;
            case AttackPattern.Forward: default: return AttackPattern.Left;
        }
    }
}