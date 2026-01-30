using System;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyMovement : MonoBehaviour
{
    [Header("Refs (devs only rat bitches)"), SerializeField] private DateSiteBehavior dateSite;

    [Header("Movement Speed"), Range(0.001f, 10f), SerializeField] private float moveSpeed;

    private Vector2 _directionToPlayer;

    private void Start()
    {
        if (dateSite is null)
        {
            Debug.LogWarning("No playerBehavior assigned!");
            return;
        }
        
        SetDateSite(dateSite);
    }

    private void Update()
    {
        if (dateSite is null) return;
        
        transform.Translate(_directionToPlayer * (moveSpeed * Time.deltaTime));
    }

    public void SetDateSite(DateSiteBehavior dateSite)
    {
        this.dateSite = dateSite;
        _directionToPlayer = (dateSite.transform.position - transform.position).normalized;
    }

    public void InitEnemy(SpawnTier tier)
    {
        if (tier is null) return;

        moveSpeed = tier.EnemySpeed;
    }
}