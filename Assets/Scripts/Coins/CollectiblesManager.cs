using UnityEngine;

public class CollectiblesManager : MonoBehaviour
{
    [Header("References")]
    public RunProgressionManager progressionManager;
    public FirstLevelObjectsSpawn levelSpawner;

    [Header("Coin Prefabs")]
    public GameObject normalCoinPrefab;
    public GameObject rareCoinPrefab;

    void OnEnable()
    {
        if (levelSpawner != null)
        {
            levelSpawner.OnObstacleSpawned += HandleObstacleSpawned;
        }
    }

    void OnDisable()
    {
        if (levelSpawner != null)
        {
            levelSpawner.OnObstacleSpawned -= HandleObstacleSpawned;
        }
    }

    void HandleObstacleSpawned(GameObject obstacle)
    {
        if (progressionManager == null || normalCoinPrefab == null) return;

        ObstacleCoinAttachment attachment = obstacle.GetComponent<ObstacleCoinAttachment>();

        if (attachment != null)
        {
            float spawnChance = progressionManager.CurrentPhase.coinSpawnChance;
            float rareChance = progressionManager.CurrentPhase.rareCoinChance;

            if (attachment.obstacleType == ObstacleCoinAttachment.ObstacleType.SafeZone)
            {
                spawnChance = 100f;
            }

            attachment.SpawnCoins(normalCoinPrefab, rareCoinPrefab, obstacle.transform, spawnChance, rareChance);
        }
    }
}

