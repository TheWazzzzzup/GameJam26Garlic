using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] private EnemyCollision enemyCollision;

    private void Awake()
    {
        if (enemyCollision is null)
        {
            Debug.LogError("NoCollision");
            return;
        }
        enemyCollision.EnemyHitPlayer += OnPlayerDeath;
    }

    public void OnPlayerDeath()
    {
        Debug.Log("Player death");
        Destroy(gameObject);
    }
}