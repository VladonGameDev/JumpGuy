using UnityEngine;
using System.Collections.Generic;

public class FirstLevelObjectsSpawn : MonoBehaviour
{
    [Header("References")]
    public RunProgressionManager progressionManager;
    public DifficultyStatsTracker statsTracker;

    public event System.Action<GameObject> OnObstacleSpawned;

    [System.Serializable]
    public class ObstaclesPrefab
    {
        public GameObject prefab;
        public float spawnWeight;
        public float yPosition;
    }

    [System.Serializable]
    public class VentObstacle
    {
        public List<VentObstaclePart> parts = new List<VentObstaclePart>();
        public float spawnWeight = 1f;
        public float yPosition = 0f;
        public float extraDistance = 0f;
        public float preDistanceReduction = 0f;
    }

    [System.Serializable]
    public class VentObstaclePart
    {
        public GameObject prefab;
        public float distanceFromPrevious = 1f;
    }

    [System.Serializable]
    public class DoubleObstacle
    {
        public GameObject containerPrefab;
        public GameObject firstPart;
        public float firstYPosition = 0f;
        public GameObject secondPart;
        public float secondYPosition = 0f;
        public float distanceBetween = 2f;
        public float spawnWeight = 1f;
        public float extraDistance = 0f;
        public ObstacleCoinAttachment.ObstacleType doubleType;
    }

    [System.Serializable]
    public class SafeZone
    {
        public GameObject openingPrefab;
        public List<GameObject> middlePrefabs = new List<GameObject>();
        public GameObject closingPrefab;
        public float spawnWeight = 1f;
        public float yPosition = 0.5f;
        public float platformDistance = 1.5f;
        public int middleMin = 2;
        public int middleMax = 5;
    }

    [Header("Obstacle Categories")]
    public List<ObstaclesPrefab> lowObstaclePrefabs;
    public List<ObstaclesPrefab> midObstaclePrefabs;
    public List<ObstaclesPrefab> highObstaclePrefabs;
    public List<ObstaclesPrefab> flyingObstaclePrefabs;
    public List<VentObstacle> ventObstacles;
    public List<DoubleObstacle> doubleObstacles;
    public List<SafeZone> safeZones;

    [Header("Distance Settings")]
    public float destroyDistance = -15f;
    public float maxSpawnDistanceX = 40f;
    public float XSpawnPositionOnStart = 15f;

    [Header("Safe Zone Settings")]
    public float safeZoneBuffer = 10f;

    public Transform playerTransform;

    private const float obstacleSpeed = 5f;

    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private List<GameObject> spawnedSafeZones = new List<GameObject>();

    private HashSet<GameObject> passedObstacles = new HashSet<GameObject>();

    private ObstacleType lastSpawnedType = ObstacleType.Low;
    private int sameTypeCount = 0;
    private float lastSpawnedX;
    private float nextSafeZoneSpawnTime = 0f;

    private List<ObstacleType> spawnHistory = new List<ObstacleType>();
    private int obstaclesSinceLastDouble = 0;

    private enum ObstacleType
    {
        Low, Mid, High, Flying, Vent, Double, SafeZone
    }

    void Start()
    {
        lastSpawnedX = XSpawnPositionOnStart;

        if (progressionManager != null)
        {
            nextSafeZoneSpawnTime = progressionManager.safeZoneStartTime;
        }

        SpawnInitialObstacles();
    }

    void Update()
    {
        if (PlayerPrefs.GetInt("isGameStarted") == 1)
        {
            MoveObjects();
            CheckDespawnObstacles();
            CheckSpawnNewObstacle();
        }
    }

    void SpawnInitialObstacles()
    {
        while (lastSpawnedX < maxSpawnDistanceX)
        {
            SpawnRandomObstacle();
        }
    }

    void MoveObjects()
    {
        float currentSpeed = obstacleSpeed;
        if (progressionManager != null)
        {
            currentSpeed *= progressionManager.TotalSpeedIndex;
        }

        float moveAmount = currentSpeed * Time.deltaTime;
        float playerX = playerTransform.position.x;

        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                obstacle.transform.Translate(Vector3.left * moveAmount);

                if (obstacle.transform.position.x < playerX && !passedObstacles.Contains(obstacle))
                {
                    passedObstacles.Add(obstacle);
                    if (statsTracker != null) statsTracker.RegisterObjectPassed();
                }
            }
        }

        foreach (GameObject sz in spawnedSafeZones)
        {
            if (sz != null)
            {
                sz.transform.Translate(Vector3.left * moveAmount);
            }
        }
    }

    void CheckSpawnNewObstacle()
    {
        float lastObstacleX = GetLastObstacleX();
        if (lastObstacleX - playerTransform.position.x < maxSpawnDistanceX)
        {
            SpawnRandomObstacle();
        }
    }

    void SpawnRandomObstacle()
    {
        ObstacleType type = GetRandomObstacleType();

        if (type == ObstacleType.Vent) SpawnVentObstacle();
        else if (type == ObstacleType.Double) SpawnDoubleObstacle();
        else if (type == ObstacleType.SafeZone) SpawnSafeZone();
        else
        {
            GameObject prefab = GetRandomPrefabByType(type);
            if (prefab == null) return;

            float spawnX = GetLastObstacleX() + GetSpawnDistance(type);
            float yPos = GetYPositionByType(type, prefab);
            GameObject newObstacle = Instantiate(prefab, new Vector3(spawnX, yPos, 0f), Quaternion.identity);
            spawnedObstacles.Add(newObstacle);

            if (type == ObstacleType.Flying)
            {
                if (newObstacle.GetComponentInChildren<FlyingObstacle>() == null)
                    newObstacle.AddComponent<FlyingObstacle>();
            }

            OnObstacleSpawned?.Invoke(newObstacle);

            lastSpawnedType = type;
            lastSpawnedX = spawnX;

            AddToHistory(type);
        }
    }

    void AddToHistory(ObstacleType type)
    {
        spawnHistory.Add(type);
        if (spawnHistory.Count > 20) spawnHistory.RemoveAt(0);

        if (type == ObstacleType.Double) obstaclesSinceLastDouble = 0;
        else obstaclesSinceLastDouble++;
    }

    float GetDistanceMultiplier()
    {
        if (progressionManager == null) return 1f;
        return 1f + (progressionManager.TotalSpeedIndex - 1f) / 2f;
    }

    float GetSpawnDistance(ObstacleType type)
    {
        if (progressionManager == null) return Random.Range(5f, 10f);

        var phase = progressionManager.CurrentPhase;
        float distanceMultiplier = GetDistanceMultiplier();

        if (progressionManager.useFixedDistance)
        {
            float fixedDistance;
            if (lastSpawnedType == ObstacleType.Mid)
            {
                fixedDistance = phase.minDistanceAfterRoll;
            }
            else
            {
                fixedDistance = progressionManager.useMaxDistance ? phase.maxDistance : phase.minDistance;
            }
            return fixedDistance * distanceMultiplier;
        }

        if (lastSpawnedType == ObstacleType.Mid)
            return phase.minDistanceAfterRoll * distanceMultiplier;

        return Random.Range(phase.minDistance, phase.maxDistance) * distanceMultiplier;
    }

    ObstacleType GetRandomObstacleType()
    {
        if (progressionManager == null) return ObstacleType.Low;

        var phase = progressionManager.CurrentPhase;
        List<KeyValuePair<ObstacleType, float>> weightedTypes = new List<KeyValuePair<ObstacleType, float>>();

        if (phase.lowWeight > 0) weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.Low, phase.lowWeight));
        if (phase.midWeight > 0) weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.Mid, phase.midWeight));
        if (phase.highWeight > 0) weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.High, phase.highWeight));

        if (phase.flyingWeight > 0 && lastSpawnedType != ObstacleType.Mid)
            weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.Flying, phase.flyingWeight));

        if (phase.ventWeight > 0 && lastSpawnedType != ObstacleType.Mid)
            weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.Vent, phase.ventWeight));

        bool canSpawnDouble = CanSpawnDouble(phase);
        if (phase.doubleWeight > 0 && canSpawnDouble)
            weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.Double, phase.doubleWeight));

        bool canSpawnSafeZone = progressionManager.IsRunning && progressionManager.RunTime >= nextSafeZoneSpawnTime;
        if (canSpawnSafeZone && safeZones.Count > 0)
            weightedTypes.Add(new KeyValuePair<ObstacleType, float>(ObstacleType.SafeZone, 1f));

        if (weightedTypes.Count == 0) return ObstacleType.Low;

        float totalWeight = 0f;
        foreach (var item in weightedTypes) totalWeight += item.Value;

        float randomValue = Random.Range(0f, totalWeight);
        float weightSum = 0f;
        ObstacleType newType = weightedTypes[0].Key;

        foreach (var item in weightedTypes)
        {
            weightSum += item.Value;
            if (randomValue <= weightSum) { newType = item.Key; break; }
        }

        if (newType == lastSpawnedType) sameTypeCount++;
        else sameTypeCount = 1;

        if (sameTypeCount > phase.maxSameTypeCount)
        {
            List<ObstacleType> otherTypes = new List<ObstacleType>();
            foreach (var item in weightedTypes)
                if (item.Key != lastSpawnedType) otherTypes.Add(item.Key);

            if (otherTypes.Count > 0)
            {
                newType = otherTypes[Random.Range(0, otherTypes.Count)];
                sameTypeCount = 1;
            }
        }

        return newType;
    }

    bool CanSpawnDouble(RunProgressionManager.PhaseSettings phase)
    {
        return obstaclesSinceLastDouble >= phase.minObstaclesBeforeDouble;
    }

    void SpawnVentObstacle()
    {
        if (ventObstacles.Count == 0) return;
        VentObstacle vent = GetRandomVentObstacle();
        if (vent == null || vent.parts.Count == 0) return;

        float distanceMultiplier = GetDistanceMultiplier();
        float baseDistance = Random.Range(progressionManager.CurrentPhase.minDistance, progressionManager.CurrentPhase.maxDistance) * distanceMultiplier;
        float spawnDistance = Mathf.Max(1f, baseDistance - vent.preDistanceReduction);

        float currentX = GetLastObstacleX() + spawnDistance;

        GameObject group = new GameObject("VentObstacleGroup");
        group.transform.position = new Vector3(currentX, vent.yPosition, 0);

        for (int i = 0; i < vent.parts.Count; i++)
        {
            if (vent.parts[i].prefab == null) continue;
            float xOffset = (i == 0) ? 0f : vent.parts[i].distanceFromPrevious;
            currentX += xOffset;
            GameObject newPart = Instantiate(vent.parts[i].prefab, group.transform);
            newPart.transform.position = new Vector3(currentX, vent.yPosition, 0f);

            if (i != 1) passedObstacles.Add(newPart);
        }

        spawnedObstacles.Add(group);

        if (vent.parts.Count > 0 && vent.parts[0].prefab != null)
        {
            Transform firstPart = group.transform.GetChild(0);
            OnObstacleSpawned?.Invoke(firstPart.gameObject);
        }

        lastSpawnedType = ObstacleType.Vent;

        if (vent.extraDistance > 0)
        {
            GameObject bufferMarker = new GameObject("VentBufferMarker");
            bufferMarker.transform.position = new Vector3(currentX + vent.extraDistance, 0, 0);
            spawnedObstacles.Add(bufferMarker);
            passedObstacles.Add(bufferMarker);
        }

        lastSpawnedX = currentX + vent.extraDistance;
        AddToHistory(ObstacleType.Vent);
    }

    void SpawnDoubleObstacle()
    {
        if (doubleObstacles.Count == 0) return;
        DoubleObstacle doubleObstacle = GetRandomDoubleObstacle();
        if (doubleObstacle == null || doubleObstacle.containerPrefab == null) return;

        float distanceMultiplier = GetDistanceMultiplier();
        float spawnDistance = Random.Range(progressionManager.CurrentPhase.minDistance, progressionManager.CurrentPhase.maxDistance) * distanceMultiplier;

        float currentX = GetLastObstacleX() + spawnDistance;

        GameObject group = Instantiate(doubleObstacle.containerPrefab, new Vector3(currentX, 0, 0), Quaternion.identity);
        group.name = "DoubleObstacleGroup";

        ObstacleCoinAttachment coinAttachment = group.GetComponent<ObstacleCoinAttachment>();
        if (coinAttachment != null)
        {
            coinAttachment.obstacleType = doubleObstacle.doubleType;
        }

        if (doubleObstacle.firstPart != null)
        {
            GameObject part1 = Instantiate(doubleObstacle.firstPart, group.transform);
            part1.transform.localPosition = new Vector3(0, doubleObstacle.firstYPosition, 0);
            passedObstacles.Add(part1);
            CleanupChildCoins(part1);
        }

        if (doubleObstacle.secondPart != null)
        {
            GameObject part2 = Instantiate(doubleObstacle.secondPart, group.transform);
            part2.transform.localPosition = new Vector3(doubleObstacle.distanceBetween, doubleObstacle.secondYPosition, 0);
            CleanupChildCoins(part2);
        }

        spawnedObstacles.Add(group);
        OnObstacleSpawned?.Invoke(group);

        lastSpawnedType = ObstacleType.Double;

        if (doubleObstacle.extraDistance > 0)
        {
            GameObject bufferMarker = new GameObject("DoubleBufferMarker");
            bufferMarker.transform.position = new Vector3(currentX + doubleObstacle.extraDistance, 0, 0);
            spawnedObstacles.Add(bufferMarker);
            passedObstacles.Add(bufferMarker);
        }

        lastSpawnedX = currentX + doubleObstacle.extraDistance;
        AddToHistory(ObstacleType.Double);
    }

    void CleanupChildCoins(GameObject part)
    {
        ObstacleCoinAttachment[] attachments = part.GetComponentsInChildren<ObstacleCoinAttachment>();
        foreach (var att in attachments)
        {
            Destroy(att);
        }

        Transform[] allChildren = part.GetComponentsInChildren<Transform>();
        foreach (var t in allChildren)
        {
            if (t.name.Contains("CoinGroup") || t.name.Contains("Coin"))
            {
                Destroy(t.gameObject);
            }
        }
    }

    void SpawnSafeZone()
    {
        if (safeZones.Count == 0) return;
        SafeZone zone = GetRandomSafeZone();
        if (zone == null) return;

        float distanceMultiplier = GetDistanceMultiplier();
        float currentX = GetLastObstacleX() + (Random.Range(progressionManager.CurrentPhase.minDistance, progressionManager.CurrentPhase.maxDistance) * distanceMultiplier);

        if (zone.openingPrefab != null)
            spawnedSafeZones.Add(Instantiate(zone.openingPrefab, new Vector3(currentX, zone.yPosition, 0f), Quaternion.identity));

        int middleCount = Random.Range(zone.middleMin, zone.middleMax + 1);
        for (int i = 0; i < middleCount; i++)
        {
            if (zone.middlePrefabs.Count == 0) break;
            GameObject randomMiddle = zone.middlePrefabs[Random.Range(0, zone.middlePrefabs.Count)];
            if (randomMiddle == null) continue;
            currentX += zone.platformDistance;

            GameObject middlePart = Instantiate(randomMiddle, new Vector3(currentX, zone.yPosition, 0f), Quaternion.identity);
            spawnedSafeZones.Add(middlePart);

            OnObstacleSpawned?.Invoke(middlePart);
        }

        if (zone.closingPrefab != null)
        {
            currentX += zone.platformDistance;
            spawnedSafeZones.Add(Instantiate(zone.closingPrefab, new Vector3(currentX, zone.yPosition, 0f), Quaternion.identity));
        }

        GameObject bufferMarker = new GameObject("SafeZoneBufferMarker");
        bufferMarker.transform.position = new Vector3(currentX + safeZoneBuffer, 0, 0);
        spawnedObstacles.Add(bufferMarker);
        passedObstacles.Add(bufferMarker);

        if (progressionManager != null)
        {
            float randomInterval = Random.Range(progressionManager.safeZoneIntervalMin, progressionManager.safeZoneIntervalMax);
            nextSafeZoneSpawnTime = progressionManager.RunTime + randomInterval;
        }

        if (statsTracker != null) statsTracker.RegisterSafeZoneSpawn();

        lastSpawnedType = ObstacleType.SafeZone;
        lastSpawnedX = currentX + safeZoneBuffer;

        AddToHistory(ObstacleType.SafeZone);
    }

    float GetLastObstacleX()
    {
        for (int i = spawnedObstacles.Count - 1; i >= 0; i--)
            if (spawnedObstacles[i] != null) return spawnedObstacles[i].transform.position.x;

        for (int i = spawnedSafeZones.Count - 1; i >= 0; i--)
            if (spawnedSafeZones[i] != null) return spawnedSafeZones[i].transform.position.x;

        return lastSpawnedX;
    }

    void CheckDespawnObstacles()
    {
        if (spawnedObstacles.Count > 0 && spawnedObstacles[0].transform.position.x < destroyDistance)
        {
            GameObject despawned = spawnedObstacles[0];
            spawnedObstacles.RemoveAt(0);
            passedObstacles.Remove(despawned);
            Destroy(despawned);
        }

        if (spawnedSafeZones.Count > 0 && spawnedSafeZones[0].transform.position.x < destroyDistance)
        {
            GameObject despawned = spawnedSafeZones[0];
            spawnedSafeZones.RemoveAt(0);
            Destroy(despawned);
        }
    }

    GameObject GetRandomPrefabByType(ObstacleType type)
    {
        switch (type)
        {
            case ObstacleType.Low: return GetRandomObstaclePrefab(lowObstaclePrefabs);
            case ObstacleType.Mid: return GetRandomObstaclePrefab(midObstaclePrefabs);
            case ObstacleType.High: return GetRandomObstaclePrefab(highObstaclePrefabs);
            case ObstacleType.Flying: return GetRandomObstaclePrefab(flyingObstaclePrefabs);
            default: return null;
        }
    }

    float GetYPositionByType(ObstacleType type, GameObject prefab)
    {
        List<ObstaclesPrefab> list = type == ObstacleType.Low ? lowObstaclePrefabs
            : type == ObstacleType.Mid ? midObstaclePrefabs
            : type == ObstacleType.High ? highObstaclePrefabs
            : flyingObstaclePrefabs;

        foreach (var data in list) if (data.prefab == prefab) return data.yPosition;
        return 0f;
    }

    VentObstacle GetRandomVentObstacle()
    {
        float totalWeight = 0f;
        foreach (var v in ventObstacles) totalWeight += v.spawnWeight;
        float r = Random.Range(0f, totalWeight);
        float sum = 0f;
        foreach (var v in ventObstacles) { sum += v.spawnWeight; if (r <= sum) return v; }
        return ventObstacles[ventObstacles.Count - 1];
    }

    DoubleObstacle GetRandomDoubleObstacle()
    {
        float totalWeight = 0f;
        foreach (var d in doubleObstacles) totalWeight += d.spawnWeight;
        float r = Random.Range(0f, totalWeight);
        float sum = 0f;
        foreach (var d in doubleObstacles) { sum += d.spawnWeight; if (r <= sum) return d; }
        return doubleObstacles[doubleObstacles.Count - 1];
    }

    SafeZone GetRandomSafeZone()
    {
        float totalWeight = 0f;
        foreach (var s in safeZones) totalWeight += s.spawnWeight;
        float r = Random.Range(0f, totalWeight);
        float sum = 0f;
        foreach (var s in safeZones) { sum += s.spawnWeight; if (r <= sum) return s; }
        return safeZones[safeZones.Count - 1];
    }

    GameObject GetRandomObstaclePrefab(List<ObstaclesPrefab> list)
    {
        if (list.Count == 0) return null;
        float totalWeight = 0f;
        foreach (var p in list) totalWeight += p.spawnWeight;
        float r = Random.Range(0f, totalWeight);
        float sum = 0f;
        foreach (var p in list) { sum += p.spawnWeight; if (r <= sum) return p.prefab; }
        return list[list.Count - 1].prefab;
    }
}