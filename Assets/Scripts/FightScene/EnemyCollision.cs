using System;
using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    public event Action EnemyDealDamage;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EnemyDealDamage?.Invoke();
        }
    }
}