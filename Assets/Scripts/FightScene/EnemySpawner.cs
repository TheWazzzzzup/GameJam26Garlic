using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] DateSiteBehavior dateSite;
    
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
    TierList _tierList;
    private bool tierIsActive;

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
        
        // Tiers
        if (tiers is not null &&
            tiers.Count > 0 &&
            totalTiersTimeSpawn > 0)
        {
            _tierList = new TierList(totalTiersTimeSpawn, tiers);
            _tierList.StartTiers();
            tierIsActive = true;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled && enemyPrefab != null && _boxCollider != null)
        {
            _tierList?.CheckForTierChange();

            float delay;
            if (tierIsActive)
            {
                SpawnTier tier = _tierList.currentTier;
                delay = Random.Range(tier.MinTimeTick, tier.MaxTimeTick);
            }
            else
            {
                delay = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
            yield return new WaitForSeconds(delay);
            Vector3 position;

            if (tierIsActive)
            {
                int count = Random.Range(_tierList.currentTier.MinEnemyVolume, _tierList.currentTier.MaxEnemyVolume);
                for (int i = 0; i < count; i++)
                {
                    position = RandomPositionInBounds();
                    SpawnAt(position);
                }
            }
            else
            {
                position = RandomPositionInBounds();
                SpawnAt(position);
            }
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
            movement.SetDateSite(dateSite);
            movement.InitEnemy(_tierList?.currentTier);
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
    public SpawnTier currentTier;
    
    private float currentTime;
    private float lastDocumentedTimeStamp;
    private float tierChangeTimeTick;

    private bool isTierChangesValid;
    private List<SpawnTier> tierList;
    private int currentTierIndex;
    private bool tierHitLimit;
    
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

        this.tierList = tiers;
        tierChangeTimeTick = totalTiersTimeSpawn / tierList.Count;
        isTierChangesValid = true;  
        currentTier = tiers[currentTierIndex];
    }

    public void StartTiers()
    {
        if (!isTierChangesValid)
        {
            Debug.LogError("TierList: No tiers available.");
            return;
        }
        currentTime = Time.time;
        lastDocumentedTimeStamp = Time.time;
    }
    
    public void CheckForTierChange()
    {
        if (tierHitLimit) return;
        // check the start time - current timer
        // save the delta
        float deltaTime = Time.time - lastDocumentedTimeStamp;
        // how many times this delta fits inside the tierchangetimetick = current tier
        int index = Mathf.Clamp(Mathf.FloorToInt(deltaTime / tierChangeTimeTick), 0, Mathf.Max(0, tierList.Count - 1));
        // safe guard from index < 0 or >= to list length
        if (index != currentTierIndex)
        {
            currentTierIndex = index;
            currentTier = tierList[index];
        }

        if (currentTierIndex == tierList.Count - 1) tierHitLimit = true;
    }
}

[System.Serializable]
public class SpawnTier
{
    public float MinTimeTick;
    public float MaxTimeTick;
    [Range(1,10)]public int MinEnemyVolume;
    [Range(1,10)]public int MaxEnemyVolume;
    [Range(0, 10)] public float EnemySpeed;
}
