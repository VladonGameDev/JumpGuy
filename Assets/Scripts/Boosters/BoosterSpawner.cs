using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoosterSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] boosterPrefabs;
    public GameObject glitchCubePrefab;

    [Header("Movement")]
    public float worldSpeed = 5f;
    public float destroyDistance = -15f;

    [Header("Spawn Position")]
    public float spawnYPosition = 1f;
    public float spawnClearance = 5f;
    public float minObstacleDistance = 2f;
    public float coinClearRadius = 1.5f;

    [Header("Glitch Cube")]
    public float glitchDistanceBeforeSafeZone = 8f;
    public float glitchMinDistanceAfterObstacle = 3f;

    private struct HorizontalRange
    {
        public float min;
        public float max;

        public HorizontalRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    private RunProgressionManager progressionManager;
    private FirstLevelObjectsSpawn levelSpawner;

    private readonly List<GameObject> spawnedBoosters = new List<GameObject>();
    private readonly List<HorizontalRange> obstacleRanges = new List<HorizontalRange>();
    private float spawnTimer;

    void Awake()
    {
        EnsureProgressionManager();
    }

    // Менеджер ищем лениво, а не только в Awake: если он появится в сцене позже,
    // спавнер иначе навсегда остался бы на запасных интервалах и равных весах.
    void EnsureProgressionManager()
    {
        if (progressionManager == null) progressionManager = FindObjectOfType<RunProgressionManager>();
    }

    void OnEnable()
    {
        Subscribe();
    }

    void OnDisable()
    {
        if (levelSpawner != null) levelSpawner.OnSafeZoneSpawned -= HandleSafeZoneSpawned;
    }

    void Start()
    {
        Subscribe();
        ResetSpawnTimer();
    }

    void Subscribe()
    {
        if (levelSpawner == null) levelSpawner = FindObjectOfType<FirstLevelObjectsSpawn>();
        if (levelSpawner == null) return;

        levelSpawner.OnSafeZoneSpawned -= HandleSafeZoneSpawned;
        levelSpawner.OnSafeZoneSpawned += HandleSafeZoneSpawned;
    }

    void Update()
    {
        if (PlayerPrefs.GetInt("isGameStarted") != 1) return;

        MoveBoosters();
        CheckDespawnBoosters();

        // Галочка фазы гасит только появление новых бустеров:
        // уже летящие продолжают движение и доезжают до despawn.
        EnsureProgressionManager();
        if (progressionManager != null && !progressionManager.CanSpawnBoosters) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnBooster();
            ResetSpawnTimer();
        }
    }

    void ResetSpawnTimer()
    {
        // Интервалы живут в настройках текущей фазы прогрессии.
        // Без менеджера берём запасные значения из констант.
        float min = RunProgressionManager.DefaultBoosterSpawnIntervalMin;
        float max = RunProgressionManager.DefaultBoosterSpawnIntervalMax;

        EnsureProgressionManager();

        if (progressionManager != null)
        {
            min = progressionManager.GetBoosterSpawnIntervalMin(progressionManager.CurrentPhase);
            max = progressionManager.GetBoosterSpawnIntervalMax(progressionManager.CurrentPhase);
        }

        spawnTimer = Random.Range(min, max);
    }

    void SpawnBooster()
    {
        if (boosterPrefabs == null || boosterPrefabs.Length == 0) return;

        GameObject prefab = PickBoosterPrefab();
        if (prefab == null) return;

        float spawnX = GetSpawnX();

        SpawnObject(prefab, new Vector3(spawnX, spawnYPosition, 0f));
    }

    // Взвешенный выбор: при равных весах ведёт себя как прежний Random.Range,
    // при нулевом весе тип просто не выпадает.
    GameObject PickBoosterPrefab()
    {
        float totalWeight = 0f;

        for (int i = 0; i < boosterPrefabs.Length; i++)
        {
            totalWeight += GetWeight(boosterPrefabs[i]);
        }

        // Все веса обнулены — честно ничего не спавним, а не падаем на первый префаб.
        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);

        for (int i = 0; i < boosterPrefabs.Length; i++)
        {
            GameObject prefab = boosterPrefabs[i];
            if (prefab == null) continue;

            float weight = GetWeight(prefab);
            if (weight <= 0f) continue;

            if (roll < weight) return prefab;
            roll -= weight;
        }

        // Сюда попадаем только из-за погрешности float — отдаём последний ненулевой.
        for (int i = boosterPrefabs.Length - 1; i >= 0; i--)
        {
            if (GetWeight(boosterPrefabs[i]) > 0f) return boosterPrefabs[i];
        }

        return null;
    }

    float GetWeight(GameObject prefab)
    {
        if (prefab == null) return 0f;

        Booster booster = prefab.GetComponent<Booster>();
        if (booster == null) return RunProgressionManager.DefaultBoosterSpawnWeight;

        EnsureProgressionManager();
        if (progressionManager == null) return RunProgressionManager.DefaultBoosterSpawnWeight;

        return progressionManager.GetBoosterWeight(booster.boosterType);
    }

    void HandleSafeZoneSpawned(float safeZoneX)
    {
        if (glitchCubePrefab == null) return;

        float lastObstacleX = levelSpawner != null ? levelSpawner.LastObstacleX : transform.position.x;
        float spawnX = safeZoneX - glitchDistanceBeforeSafeZone;

        if (spawnX < lastObstacleX + glitchMinDistanceAfterObstacle)
        {
            spawnX = lastObstacleX + glitchMinDistanceAfterObstacle;
        }

        if (spawnX > safeZoneX) spawnX = safeZoneX;

        SpawnObject(glitchCubePrefab, new Vector3(spawnX, spawnYPosition, 0f));
    }

    void SpawnObject(GameObject prefab, Vector3 position)
    {
        StartCoroutine(SpawnRoutine(prefab, position));
    }

    IEnumerator SpawnRoutine(GameObject prefab, Vector3 position)
    {
        ClearCoinsAt(position);

        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
        RegisterSpawned(spawned);

        yield return null;

        ClearCoinsAt(position);
    }

    public void RegisterSpawned(GameObject booster)
    {
        if (booster == null) return;

        spawnedBoosters.Add(booster);
    }

    void ClearCoinsAt(Vector3 position)
    {
        if (!Application.isPlaying) return;

        float radius = Mathf.Max(coinClearRadius, 0f);
        if (radius <= 0f) return;

        Collider[] overlaps = Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null) continue;

            Coin coin = overlap.GetComponentInParent<Coin>();
            if (coin == null) continue;

            Transform coinParent = coin.transform.parent;
            GameObject target = coinParent != null && coinParent != transform ? coinParent.gameObject : coin.gameObject;

            Destroy(target);
        }
    }

    float GetSpawnX()
    {
        float clearance = Mathf.Max(spawnClearance, 0f);
        float minDistance = Mathf.Max(clearance, minObstacleDistance);
        float lastObstacleX = levelSpawner != null ? levelSpawner.LastObstacleX : transform.position.x;
        float spawnX = lastObstacleX + clearance;

        if (levelSpawner == null) return spawnX;

        CollectObstacleRanges();
        if (obstacleRanges.Count == 0) return spawnX;

        float halfWidth = GetBoosterHalfWidth();

        bool moved = true;
        int guard = 0;
        while (moved && guard++ <= obstacleRanges.Count)
        {
            moved = false;

            for (int i = 0; i < obstacleRanges.Count; i++)
            {
                float blockedMin = obstacleRanges[i].min - minDistance - halfWidth;
                float blockedMax = obstacleRanges[i].max + minDistance + halfWidth;

                if (spawnX < blockedMin || spawnX > blockedMax) continue;

                spawnX = obstacleRanges[i].max + minDistance + halfWidth;
                moved = true;
            }
        }

        return spawnX;
    }

    void CollectObstacleRanges()
    {
        obstacleRanges.Clear();
        AddObstacleRanges(levelSpawner.SpawnedObstacles);
        AddObstacleRanges(levelSpawner.SpawnedSafeZones);
    }

    void AddObstacleRanges(IReadOnlyList<GameObject> obstacles)
    {
        if (obstacles == null) return;

        for (int i = 0; i < obstacles.Count; i++)
        {
            GameObject obstacle = obstacles[i];
            if (obstacle == null) continue;

            Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            float minX = 0f;
            float maxX = 0f;

            for (int r = 0; r < renderers.Length; r++)
            {
                Bounds bounds = renderers[r].bounds;

                if (!hasBounds)
                {
                    minX = bounds.min.x;
                    maxX = bounds.max.x;
                    hasBounds = true;
                }
                else
                {
                    if (bounds.min.x < minX) minX = bounds.min.x;
                    if (bounds.max.x > maxX) maxX = bounds.max.x;
                }
            }

            if (hasBounds) obstacleRanges.Add(new HorizontalRange(minX, maxX));
        }
    }

    float GetBoosterHalfWidth()
    {
        if (boosterPrefabs == null) return 0f;

        float halfWidth = 0f;

        for (int i = 0; i < boosterPrefabs.Length; i++)
        {
            GameObject prefab = boosterPrefabs[i];
            if (prefab == null) continue;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                Vector3 scale = renderer.transform.lossyScale;
                Vector3 offset = prefab.transform.InverseTransformPoint(renderer.transform.position);
                float extents = Mathf.Abs(renderer.localBounds.extents.x * scale.x);

                minX = Mathf.Min(minX, offset.x - extents);
                maxX = Mathf.Max(maxX, offset.x + extents);
            }

            if (minX > maxX) continue;

            float prefabHalfWidth = Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX));
            if (prefabHalfWidth > halfWidth) halfWidth = prefabHalfWidth;
        }

        return halfWidth;
    }

    void MoveBoosters()
    {
        float speed = worldSpeed;
        if (progressionManager != null)
        {
            speed *= progressionManager.WorldSpeedIndex;
        }

        float moveAmount = speed * Time.deltaTime;

        for (int i = 0; i < spawnedBoosters.Count; i++)
        {
            GameObject booster = spawnedBoosters[i];
            if (booster == null) continue;

            booster.transform.position += Vector3.left * moveAmount;
        }
    }

    void CheckDespawnBoosters()
    {
        for (int i = spawnedBoosters.Count - 1; i >= 0; i--)
        {
            GameObject booster = spawnedBoosters[i];

            if (booster == null)
            {
                spawnedBoosters.RemoveAt(i);
                continue;
            }

            if (booster.transform.position.x < destroyDistance)
            {
                spawnedBoosters.RemoveAt(i);
                Destroy(booster);
            }
        }
    }
}
