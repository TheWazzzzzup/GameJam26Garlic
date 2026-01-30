using System;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyMovement : MonoBehaviour
{
    [Header("Refs (devs only rat bitches)"), SerializeField] private PlayerBehavior _playerBehavior;

    [Header("Movement Speed"), Range(0.001f, 10f), SerializeField] private float moveSpeed;

    private Vector2 _directionToPlayer;

    private void Start()
    {
        if (_playerBehavior is null)
        {
            Debug.LogWarning("No playerBehavior assigned!");
            return;
        }
        
        SetPlayer(_playerBehavior);
    }

    private void Update()
    {
        if (_playerBehavior is null) return;
        
        transform.Translate(_directionToPlayer * (moveSpeed * Time.deltaTime));
    }

    public void SetPlayer(PlayerBehavior playerBehavior)
    {
        _playerBehavior = playerBehavior;
        _directionToPlayer = (_playerBehavior.transform.position - transform.position).normalized;
    }

    public void InitEnemy(SpawnTier tier)
    {
        if (tier is null) return;

        moveSpeed = tier.EnemySpeed;
    }
}