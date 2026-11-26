using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RunProgressionManager : MonoBehaviour
{
    [Header("Distance Override")]
    public bool useFixedDistance = false;
    public bool useMaxDistance = false;

    [System.Serializable]
    public class PhaseSettings
    {
        [Header("Time & Score")]
        public float startTime = 0f;
        public float scoreMultiplier = 1f;

        [Header("Coins")]
        [Range(0f, 100f)] public float coinSpawnChance = 30f;
        [Range(0f, 100f)] public float rareCoinChance = 10f;

        [Header("Speed")]
        public float maxSpeedIndex = 1.1f;

        [Header("Distances")]
        public float minDistance = 5f;
        public float maxDistance = 10f;
        public float minDistanceAfterRoll = 8f;

        [Header("Spawn Weights")]
        public float lowWeight = 5f;
        public float midWeight = 4f;
        public float highWeight = 5f;
        public float flyingWeight = 3f;
        public float ventWeight = 2f;
        public float doubleWeight = 1f;

        [Header("Double Obstacle Settings")]
        public int minObstaclesBeforeDouble = 5;

        [Header("Repeat Limits")]
        public int maxSameTypeCount = 3;

        [Header("Boosters")]
        [Tooltip("Снимите галочку, чтобы в этой фазе бустеры не появлялись вовсе.")]
        public bool spawnBoosters = true;

        [Tooltip("Нижняя граница паузы между появлениями бустеров, в секундах.")]
        [Min(0f)] public float minBoosterSpawnInterval = 3f;

        [Tooltip("Верхняя граница паузы между появлениями бустеров, в секундах.")]
        [Min(0f)] public float maxBoosterSpawnInterval = 6f;
    }

    // Вес бустера при выборе, какой из пула заспавнить.
    // 0 — тип выпадает из розыгрыша, но остаётся в пуле префабов.
    [System.Serializable]
    public class BoosterSpawnWeight
    {
        public BoosterType type = BoosterType.Magnet;
        [Min(0f)] public float weight = 1f;
    }

    public const float DefaultBoosterSpawnWeight = 1f;
    public const float DefaultBoosterSpawnIntervalMin = 15f;
    public const float DefaultBoosterSpawnIntervalMax = 30f;

    [Header("Phase Settings (5 phases)")]
    public PhaseSettings[] phases = new PhaseSettings[5];

    [Header("Booster Spawn Weights")]
    [Tooltip("Вес каждого типа бустера при случайном выборе. Не указанные типы получают вес 1.")]
    public List<BoosterSpawnWeight> boosterSpawnWeights = new List<BoosterSpawnWeight>();

    [Header("Safe Zone Global Settings")]
    public float safeZoneStartTime = 30f;
    public float safeZoneIntervalMin = 20f;
    public float safeZoneIntervalMax = 40f;

    [Header("Speed Settings")]
    public float baseSpeedGrowthRate = 0.001f;

    [Header("Dash Settings")]
    public float rollDashSpeedMultiplier = 5f;

    [Header("Timer Booster Settings")]
    [Tooltip("Во сколько раз замедляется мир при активном бустере Timer. 1.5 = в полтора раза медленнее обычного.")]
    public float timerSlowdownFactor = 1.5f;
    public float timerTransitionTime = 0.3f;

    public float CurrentSpeedIndex { get; private set; } = 1f;
    public int CurrentPhaseIndex { get; private set; } = 0;
    public float RunTime { get; private set; } = 0f;
    public bool IsRunning { get; private set; } = false;

    // Множитель времени: 1 — обычная скорость, меньше 1 — мир идёт медленнее.
    public float WorldSlowdown => timerMultiplier;

    // Обратная величина от timerSlowdownFactor: 1.5 -> 0.6667 времени.
    public float SlowdownTimeMultiplier => Mathf.Clamp(1f / Mathf.Max(1f, timerSlowdownFactor), 0.05f, 1f);

    private float dashMultiplier = 1f;
    private float timerMultiplier = 1f;
    private float targetTimerMultiplier = 1f;

    public PhaseSettings CurrentPhase => phases[Mathf.Clamp(CurrentPhaseIndex, 0, phases.Length - 1)];
    public float TotalSpeedIndex => CurrentSpeedIndex * dashMultiplier;
    public float WorldSpeedIndex => TotalSpeedIndex * timerMultiplier;

    public void StartRun()
    {
        PlayerPrefs.SetInt("isGameStarted", 1);
        RunTime = 0f;
        CurrentSpeedIndex = 1f;
        CurrentPhaseIndex = 0;
        dashMultiplier = 1f;
        timerMultiplier = 1f;
        IsRunning = true;
    }

    public void StopRun()
    {
        PlayerPrefs.SetInt("isGameStarted", 0);
        IsRunning = false;
    }

    public void SetSlowdown(bool active)
    {
        targetTimerMultiplier = active ? SlowdownTimeMultiplier : 1f;
    }

    // Вес типа в текущей фазе. Тип, которого нет в списке, весит 1,
    // поэтому новый бустер начинает спавниться сразу, без правки настроек.
    public float GetBoosterWeight(BoosterType type)
    {
        for (int i = 0; i < boosterSpawnWeights.Count; i++)
        {
            BoosterSpawnWeight entry = boosterSpawnWeights[i];
            if (entry == null || entry.type != type) continue;

            return Mathf.Max(0f, entry.weight);
        }

        return DefaultBoosterSpawnWeight;
    }

    public bool CanSpawnBoosters => CurrentPhase == null || CurrentPhase.spawnBoosters;

    public float GetBoosterSpawnIntervalMin(PhaseSettings phase)
    {
        if (phase == null) return DefaultBoosterSpawnIntervalMin;

        return Mathf.Max(0f, phase.minBoosterSpawnInterval);
    }

    public float GetBoosterSpawnIntervalMax(PhaseSettings phase)
    {
        if (phase == null) return DefaultBoosterSpawnIntervalMax;

        return Mathf.Max(GetBoosterSpawnIntervalMin(phase), phase.maxBoosterSpawnInterval);
    }

    void Update()
    {
        UpdateTimerMultiplier();

        if (!IsRunning) return;

        RunTime += Time.deltaTime;

        for (int i = phases.Length - 1; i >= 0; i--)
        {
            if (RunTime >= phases[i].startTime)
            {
                CurrentPhaseIndex = i;
                break;
            }
        }

        if (CurrentSpeedIndex < CurrentPhase.maxSpeedIndex)
        {
            float growthRate = (CurrentPhaseIndex == 4) ? (baseSpeedGrowthRate / 2f) : baseSpeedGrowthRate;
            CurrentSpeedIndex += growthRate * Time.deltaTime;

            if (CurrentSpeedIndex > CurrentPhase.maxSpeedIndex)
                CurrentSpeedIndex = CurrentPhase.maxSpeedIndex;
        }
    }

    void UpdateTimerMultiplier()
    {
        if (Mathf.Approximately(timerMultiplier, targetTimerMultiplier)) return;

        float duration = Mathf.Max(timerTransitionTime, 0.01f);
        float step = Time.deltaTime / duration;

        timerMultiplier = Mathf.MoveTowards(timerMultiplier, targetTimerMultiplier, step);
    }

    public void DashForward(float dashForce, float dashDuration)
    {
        StartCoroutine(DashCoroutine(dashForce, dashDuration));
    }

    private IEnumerator DashCoroutine(float dashForce, float dashDuration)
    {
        dashMultiplier = dashForce;
        yield return new WaitForSeconds(dashDuration);
        dashMultiplier = 1f;
    }
}
