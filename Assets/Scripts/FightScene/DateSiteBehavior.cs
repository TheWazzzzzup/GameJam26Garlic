using System;
using DefaultNamespace;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DateSiteBehavior : MonoBehaviour
{
    private bool gameLost;
    
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            AcceptDamage(Data.EnemyDamage);
        }
    }

    void AcceptDamage(float damageAmount)
    {
        if (gameLost) return;

        Data.CurrentHealth -= damageAmount;
        Debug.Log(Data.CurrentHealth);

        if (Data.CurrentHealth <= 0)
        {
            // death event
            gameLost = true;
            return;
        }
        
        // Hit event
        Debug.Log($"Date Site Attacked! {Data.EnemyDamage} hp hit!");
    }

}