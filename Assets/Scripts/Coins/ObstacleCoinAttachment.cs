using UnityEngine;

public class ObstacleCoinAttachment : MonoBehaviour
{
    public enum ObstacleType
    {
        Low,
        Mid,
        High,
        Flying,
        Vent,
        SafeZone,
        DoubleLow,
        DoubleMid,
        DoubleHigh
    }

    [Header("Settings")]
    public ObstacleType obstacleType;

    [Header("Patterns")]
    public CoinPattern[] lowPatterns;
    public CoinPattern[] midPatterns;
    public CoinPattern[] highPatterns;
    public CoinPattern[] flyingPatterns;
    public CoinPattern[] ventPatterns;
    public CoinPattern[] safeZonePatterns;
    public CoinPattern[] doubleLowPatterns;
    public CoinPattern[] doubleMidPatterns;
    public CoinPattern[] doubleHighPatterns;

    public void SpawnCoins(GameObject normalCoinPrefab, GameObject rareCoinPrefab, Transform parentTransform, float spawnChance, float rareChance)
    {
        if (normalCoinPrefab == null) return;

        CoinPattern pattern = GetRandomPattern();
        if (pattern == null || pattern.positions.Length == 0) return;

        bool isSafeZone = obstacleType == ObstacleType.SafeZone;

        if (!isSafeZone && Random.Range(0f, 100f) > spawnChance) return;

        GameObject group = new GameObject("CoinGroup");
        group.transform.SetParent(parentTransform);
        group.transform.localPosition = Vector3.zero;

        bool spawnRareGroup = rareCoinPrefab != null && Random.Range(0f, 100f) <= rareChance;
        int lastIndex = pattern.positions.Length - 1;
        bool hasSpawnedAny = false;

        for (int i = 0; i < pattern.positions.Length; i++)
        {
            if (isSafeZone && Random.Range(0f, 100f) > spawnChance) continue;

            bool isRare;

            if (isSafeZone)
            {
                isRare = rareCoinPrefab != null && Random.Range(0f, 100f) <= rareChance;
            }
            else
            {
                isRare = spawnRareGroup && (i == lastIndex);
            }

            GameObject prefabToSpawn = isRare ? rareCoinPrefab : normalCoinPrefab;

            GameObject coinObj = Instantiate(prefabToSpawn, group.transform);
            coinObj.transform.localPosition = pattern.positions[i];
            hasSpawnedAny = true;

            Coin coinScript = coinObj.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.Initialize();
            }
        }

        if (!hasSpawnedAny) Destroy(group);
    }

    CoinPattern GetRandomPattern()
    {
        CoinPattern[] pool = null;

        switch (obstacleType)
        {
            case ObstacleType.Low: pool = lowPatterns; break;
            case ObstacleType.Mid: pool = midPatterns; break;
            case ObstacleType.High: pool = highPatterns; break;
            case ObstacleType.Flying: pool = flyingPatterns; break;
            case ObstacleType.Vent: pool = ventPatterns; break;
            case ObstacleType.SafeZone: pool = safeZonePatterns; break;
            case ObstacleType.DoubleLow: pool = doubleLowPatterns; break;
            case ObstacleType.DoubleMid: pool = doubleMidPatterns; break;
            case ObstacleType.DoubleHigh: pool = doubleHighPatterns; break;
        }

        if (pool == null || pool.Length == 0) return null;

        return pool[Random.Range(0, pool.Length)];
    }
}