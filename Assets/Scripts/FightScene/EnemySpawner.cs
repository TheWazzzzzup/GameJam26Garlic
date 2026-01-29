using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] PlayerBehavior playerBehavior;
    
    [Header("Spawn Area")]
    [Tooltip("Enemy prefab to spawn.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Minimum seconds between spawns.")]
    [SerializeField] private float minSpawnInterval = 2f;

    [Tooltip("Maximum seconds between spawns.")]
    [SerializeField] private float maxSpawnInterval = 5f;
    
    [Header("Difficulty Tier List")]
    [SerializeField] List<SpawnTier> tiers;
    [SerializeField] float totalTiersTimeSpawn;

    private BoxCollider2D _boxCollider;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        if (enemyPrefab is null)
        {
            Debug.LogError($"{name}: Enemy prefab is not assigned. Spawning disabled.");
            return;
        }

        if (minSpawnInterval > maxSpawnInterval)
        {
            Debug.LogWarning($"{name}: minSpawnInterval > maxSpawnInterval; clamping.");
            maxSpawnInterval = minSpawnInterval;
        }

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled && enemyPrefab != null && _boxCollider != null)
        {
            float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(delay);

            Vector3 position = RandomPositionInBounds();
            SpawnAt(position);
        }
    }

    private Vector3 RandomPositionInBounds()
    {
        Bounds bounds = _boxCollider.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        float z = bounds.center.z;
        return new Vector3(x, y, z);
    }

    private void SpawnAt(Vector3 worldPosition)
    {
        if (enemyPrefab is null) return;

        GameObject go = Instantiate(enemyPrefab, worldPosition, Quaternion.identity, transform);
        if (go.TryGetComponent(out EnemyMovement movement))
        {
            movement.SetPlayer(playerBehavior);
        }
    }

    [ContextMenu("Spawn Enemies At 4 Corners")]
    private void SpawnEnemiesAtFourCorners()
    {
        if (_boxCollider is null)
            _boxCollider = GetComponent<BoxCollider2D>();

        if (_boxCollider is null)
        {
            Debug.LogError($"{name}: BoxCollider2D is missing. Cannot spawn at corners.");
            return;
        }

        if (enemyPrefab is null)
        {
            Debug.LogError($"{name}: Enemy prefab is not assigned.");
            return;
        }

        Bounds bounds = _boxCollider.bounds;
        float z = bounds.center.z;

        SpawnAt(new Vector3(bounds.min.x, bounds.min.y, z)); // bottom-left
        SpawnAt(new Vector3(bounds.max.x, bounds.min.y, z)); // bottom-right
        SpawnAt(new Vector3(bounds.max.x, bounds.max.y, z)); // top-right
        SpawnAt(new Vector3(bounds.min.x, bounds.max.y, z)); // top-left
    }
}

public class TierList
{
    private float currentTime;
    private float lastDocumentedTimeStamp;
    private float tierChangeTimeTick;

    private bool isTierChangesValid;
    public SpawnTier currentTier;
    
    public TierList(float totalTiersTimeSpawn, List<SpawnTier> tiers)
    {
        if (tiers is null || tiers.Count <= 0)
        {
            Debug.LogError("TierList: No tiers available.");
            return;
        }
        if (totalTiersTimeSpawn <= 0)
        {
            totalTiersTimeSpawn = 100;
        }
        
        tierChangeTimeTick = totalTiersTimeSpawn / totalTiersTimeSpawn;
        isTierChangesValid = true;  
        currentTier = tiers[0];
    }

    public void StartTier(float startTime)
    {
        if (!isTierChangesValid)
        {
            Debug.LogError("TierList: No tiers available.");
            return;
        }
        currentTime = Time.time;
    }
    
    public void CheckForTierChange()
    {
        // check the start time - current timer
        // save the delta
        // how many times this delta fits inside the tierchangetimetick = current tier
        // safe guard from index < 0 or >= to list length
    }
}

[System.Serializable]
public class SpawnTier
{
    public float MinTimeTick;
    public float MaxTimeTick;
    public float MinEnemyVoulme;
    public float MaxEnemyVolume;
}
