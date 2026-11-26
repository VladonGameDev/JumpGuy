using UnityEngine;
using System;
using System.Collections.Generic;

public class BoosterManager : MonoBehaviour
{
    [Serializable]
    public class BoosterDuration
    {
        public BoosterType type;
        public float duration = 10f;
    }

    [Header("Settings")]
    public List<BoosterDuration> boosterDurations = new List<BoosterDuration>();
    public float defaultDuration = 10f;

    public event Action<BoosterType> OnBoosterActivated;
    public event Action<BoosterType> OnBoosterExpired;

    private readonly Dictionary<BoosterType, float> activeBoosters = new Dictionary<BoosterType, float>();
    private readonly List<BoosterType> typeBuffer = new List<BoosterType>();
    private readonly List<BoosterType> expiredBuffer = new List<BoosterType>();

    public IReadOnlyDictionary<BoosterType, float> ActiveBoosters => activeBoosters;

    public bool IsActive(BoosterType type)
    {
        return activeBoosters.ContainsKey(type);
    }

    public float GetRemainingTime(BoosterType type)
    {
        float remaining;
        return activeBoosters.TryGetValue(type, out remaining) ? remaining : 0f;
    }

    public float GetDuration(BoosterType type)
    {
        foreach (BoosterDuration data in boosterDurations)
        {
            if (data.type == type) return data.duration;
        }

        return defaultDuration;
    }

    public void ActivateBooster(BoosterType type)
    {
        bool wasActive = activeBoosters.ContainsKey(type);
        activeBoosters[type] = GetDuration(type);

        if (!wasActive)
        {
            OnBoosterActivated?.Invoke(type);
        }
    }

    public void DeactivateBooster(BoosterType type)
    {
        if (!activeBoosters.Remove(type)) return;

        OnBoosterExpired?.Invoke(type);
    }

    public void ClearBoosters()
    {
        expiredBuffer.Clear();
        expiredBuffer.AddRange(activeBoosters.Keys);

        activeBoosters.Clear();

        foreach (BoosterType type in expiredBuffer)
        {
            OnBoosterExpired?.Invoke(type);
        }

        expiredBuffer.Clear();
    }

    void Update()
    {
        if (activeBoosters.Count == 0) return;

        expiredBuffer.Clear();
        typeBuffer.Clear();
        typeBuffer.AddRange(activeBoosters.Keys);

        foreach (BoosterType type in typeBuffer)
        {
            float remaining = activeBoosters[type] - Time.deltaTime;

            if (remaining <= 0f)
            {
                expiredBuffer.Add(type);
            }
            else
            {
                activeBoosters[type] = remaining;
            }
        }

        foreach (BoosterType type in expiredBuffer)
        {
            activeBoosters.Remove(type);
            OnBoosterExpired?.Invoke(type);
        }

        expiredBuffer.Clear();
    }
}
