using DefaultNamespace;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] private EnemyCollision enemyCollision;

    [SerializeField] private float health = 10;
    
    private void Awake()
    {
        if (enemyCollision is null)
        {
            Debug.LogError("NoCollision");
            return;
        }
        enemyCollision.EnemyDealDamage += AcceptDamage;
    }
    
    void AcceptDamage()
    {
        health -= Data.PlayerDamage;
        
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}