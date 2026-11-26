using UnityEngine;
using System.Collections.Generic;

public class BoosterEffects : MonoBehaviour
{
    [Header("Magnet")]
    public float magnetRadius = 7f;
    public float magnetPullSpeed = 14f;

    [Header("References")]
    public BoosterManager boosterManager;
    public FirstLevelObjectsSpawn levelSpawner;
    public Transform playerTransform;

    private Coin[] cachedCoins = new Coin[0];
    private float refreshTimer;
    private float refreshInterval = 0.2f;
    private bool shieldApplied;
    private bool slowdownApplied;

    private RunProgressionManager progressionManager;

    private readonly List<Collider> shieldedColliders = new List<Collider>();
    private Collider[] playerColliders = new Collider[0];
    private float shieldSweepTimer;
    private const float ShieldSweepInterval = 0.5f;

    public static BoosterEffects Instance { get; private set; }

    public static bool BoosterActive(BoosterType type)
    {
        return Instance != null && Instance.IsTypeActive(type);
    }

    public static int CoinMultiplier
    {
        get { return BoosterActive(BoosterType.DoubleCoins) ? 2 : 1; }
    }

    public static int ScoreMultiplier
    {
        get { return BoosterActive(BoosterType.DoubleScore) ? 2 : 1; }
    }

    public static bool MagnetActive
    {
        get { return BoosterActive(BoosterType.Magnet); }
    }

    public static bool ShieldActive
    {
        get { return BoosterActive(BoosterType.Shield); }
    }

    void Awake()
    {
        Instance = this;

        if (boosterManager == null) boosterManager = FindObjectOfType<BoosterManager>();
        if (levelSpawner == null) levelSpawner = FindObjectOfType<FirstLevelObjectsSpawn>();
        progressionManager = FindObjectOfType<RunProgressionManager>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ReleaseShield();
    }

    public bool IsTypeActive(BoosterType type)
    {
        return boosterManager != null && boosterManager.IsActive(type);
    }

    void Update()
    {
        UpdateMagnet();
        UpdateSlowdown();
    }

    void LateUpdate()
    {
        UpdateShield();
    }

    void UpdateMagnet()
    {
        if (!IsTypeActive(BoosterType.Magnet)) return;
        if (playerTransform == null) return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            cachedCoins = FindObjectsOfType<Coin>();
        }

        for (int i = 0; i < cachedCoins.Length; i++)
        {
            Coin coin = cachedCoins[i];
            if (coin == null) continue;
            if (coin.IsCollected) continue;

            Transform coinTransform = coin.transform;
            if (Vector3.Distance(coinTransform.position, playerTransform.position) > magnetRadius) continue;

            coin.SetMagnetized(playerTransform, magnetPullSpeed);
        }
    }

    void UpdateSlowdown()
    {
        bool active = IsTypeActive(BoosterType.Timer);
        if (active == slowdownApplied) return;

        slowdownApplied = active;

        if (progressionManager != null) progressionManager.SetSlowdown(active);
    }

    void OnEnable()
    {
        if (levelSpawner != null) levelSpawner.OnObstacleSpawned += HandleObstacleSpawned;
    }

    void OnDisable()
    {
        if (levelSpawner != null) levelSpawner.OnObstacleSpawned -= HandleObstacleSpawned;
    }

    // Новое препятствие нужно обезопасить сразу, а не ждать следующего обхода:
    // иначе игрок успевает врезаться в него в первые кадры после спавна.
    void HandleObstacleSpawned(GameObject obstacle)
    {
        if (!shieldApplied || obstacle == null) return;

        ShieldObstacle(obstacle);
    }

    void UpdateShield()
    {
        bool active = IsTypeActive(BoosterType.Shield);

        if (active && !shieldApplied) ApplyShield();
        else if (!active && shieldApplied) ReleaseShield();

        if (!shieldApplied) return;

        // Периодический добор: страхует от препятствий, появившихся мимо события
        // (сейф-зоны, двойные препятствия, вентиляции).
        shieldSweepTimer -= Time.deltaTime;
        if (shieldSweepTimer > 0f) return;

        shieldSweepTimer = ShieldSweepInterval;
        SweepShield();
    }

    void CachePlayerColliders()
    {
        if (playerTransform == null)
        {
            playerColliders = new Collider[0];
            return;
        }

        playerColliders = playerTransform.GetComponentsInChildren<Collider>(true);
    }

    void ApplyShield()
    {
        shieldApplied = true;
        shieldSweepTimer = 0f;

        shieldedColliders.Clear();
        CachePlayerColliders();

        if (playerTransform == null || levelSpawner == null) return;

        foreach (GameObject obstacle in levelSpawner.SpawnedObstacles)
        {
            ShieldObstacle(obstacle);
        }
    }

    // Обходит все живые препятствия и добирает те, что ещё не защищены.
    void SweepShield()
    {
        if (levelSpawner == null) return;

        // Ссылки на уничтоженные препятствия чистим, иначе список растёт вечно.
        for (int i = shieldedColliders.Count - 1; i >= 0; i--)
        {
            if (shieldedColliders[i] == null) shieldedColliders.RemoveAt(i);
        }

        foreach (GameObject obstacle in levelSpawner.SpawnedObstacles)
        {
            ShieldObstacle(obstacle);
        }
    }

    void ShieldObstacle(GameObject obstacle)
    {
        if (obstacle == null || playerTransform == null) return;

        if (playerColliders == null || playerColliders.Length == 0) CachePlayerColliders();
        if (playerColliders.Length == 0) return;

        foreach (Collider hazard in obstacle.GetComponentsInChildren<Collider>(true))
        {
            if (hazard == null || !hazard.gameObject.CompareTag("GroundObstacle")) continue;
            if (shieldedColliders.Contains(hazard)) continue;

            for (int p = 0; p < playerColliders.Length; p++)
            {
                Collider playerCollider = playerColliders[p];
                if (playerCollider == null) continue;

                Physics.IgnoreCollision(playerCollider, hazard, true);
            }

            shieldedColliders.Add(hazard);
        }
    }

    void ReleaseShield()
    {
        shieldApplied = false;

        if (shieldedColliders.Count == 0) return;

        if (playerTransform != null)
        {
            CachePlayerColliders();

            for (int i = 0; i < shieldedColliders.Count; i++)
            {
                Collider hazard = shieldedColliders[i];
                if (hazard == null) continue;

                for (int p = 0; p < playerColliders.Length; p++)
                {
                    Collider playerCollider = playerColliders[p];
                    if (playerCollider == null) continue;

                    Physics.IgnoreCollision(playerCollider, hazard, false);
                }
            }
        }

        shieldedColliders.Clear();
    }
}
