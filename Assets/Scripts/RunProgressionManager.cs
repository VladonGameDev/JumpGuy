using UnityEngine;
using System.Collections;

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
    }

    [Header("Phase Settings (5 phases)")]
    public PhaseSettings[] phases = new PhaseSettings[5];

    [Header("Safe Zone Global Settings")]
    public float safeZoneStartTime = 30f;
    public float safeZoneIntervalMin = 20f;
    public float safeZoneIntervalMax = 40f;

    [Header("Speed Settings")]
    public float baseSpeedGrowthRate = 0.001f;

    [Header("Dash Settings")]
    public float rollDashSpeedMultiplier = 5f;

    public float CurrentSpeedIndex { get; private set; } = 1f;
    public int CurrentPhaseIndex { get; private set; } = 0;
    public float RunTime { get; private set; } = 0f;
    public bool IsRunning { get; private set; } = false;

    private float dashMultiplier = 1f;

    public PhaseSettings CurrentPhase => phases[Mathf.Clamp(CurrentPhaseIndex, 0, phases.Length - 1)];
    public float TotalSpeedIndex => CurrentSpeedIndex * dashMultiplier;

    public void StartRun()
    {
        PlayerPrefs.SetInt("isGameStarted", 1);
        RunTime = 0f;
        CurrentSpeedIndex = 1f;
        CurrentPhaseIndex = 0;
        dashMultiplier = 1f;
        IsRunning = true;
    }

    public void StopRun()
    {
        PlayerPrefs.SetInt("isGameStarted", 0);
        IsRunning = false;
    }

    void Update()
    {
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