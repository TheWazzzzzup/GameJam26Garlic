using System;
using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    public event Action EnemyHitPlayer;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("DateSite"))
        {
            Debug.Log("Player collided with " + other.name);
            EnemyHitPlayer?.Invoke();
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Destroy(gameObject);
        }
    }
}