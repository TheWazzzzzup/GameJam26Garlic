using System;
using UnityEngine;

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
        }
    }

    private void Update()
    {
        if (_playerBehavior is null) return;
        
        _directionToPlayer = (_playerBehavior.transform.position - transform.position).normalized;
        transform.Translate(_directionToPlayer * (moveSpeed * Time.deltaTime));
    }
}