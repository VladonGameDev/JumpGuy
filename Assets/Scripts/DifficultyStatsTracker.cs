using UnityEngine;
using System.Collections.Generic;

public class DifficultyStatsTracker : MonoBehaviour
{
    public RunProgressionManager progressionManager;

    private float lastActionTime = -1f;
    private bool skipNextAction = false;

    // Данные для ТЕКУЩЕЙ фазы
    private int objectsPassedThisPhase = 0;
    private int currentPhase = -1;
    private float timeEnteredPhase5 = -1f;

    // Для подсчета среднего времени за текущую фазу
    private List<float> actionTimesThisPhase = new List<float>();

    void Start()
    {
        lastActionTime = -1f;
        currentPhase = -1;
        timeEnteredPhase5 = -1f;
        objectsPassedThisPhase = 0;
        actionTimesThisPhase.Clear();
    }

    public void StartRun()
    {
        lastActionTime = -1f;
        currentPhase = -1;
        objectsPassedThisPhase = 0;
        timeEnteredPhase5 = -1f;
        actionTimesThisPhase.Clear();
    }

    void Update()
    {
        if (progressionManager != null && progressionManager.IsRunning)
        {
            if (progressionManager.CurrentPhaseIndex != currentPhase)
            {
                // Если фаза завершилась (и это не 5-я бесконечная)
                if (currentPhase != -1 && currentPhase != 4)
                {
                    SavePhaseStats(currentPhase, objectsPassedThisPhase, actionTimesThisPhase);
                }

                currentPhase = progressionManager.CurrentPhaseIndex;
                objectsPassedThisPhase = 0;
                actionTimesThisPhase.Clear(); // Очищаем тайминги для новой фазы

                if (currentPhase == 4)
                {
                    timeEnteredPhase5 = Time.time;
                }
            }
        }
    }

    public void TrackAction()
    {
        if (progressionManager == null || !progressionManager.IsRunning) return;

        float currentTime = Time.time;

        if (lastActionTime < 0)
        {
            lastActionTime = currentTime;
            return;
        }

        float timeDiff = currentTime - lastActionTime;
        lastActionTime = currentTime;

        if (skipNextAction)
        {
            skipNextAction = false;
            return;
        }

        // Просто запоминаем тайминг в список текущей фазы
        actionTimesThisPhase.Add(timeDiff);
    }

    public void RegisterSafeZoneSpawn()
    {
        skipNextAction = true;
    }

    public void RegisterObjectPassed()
    {
        if (progressionManager == null || !progressionManager.IsRunning) return;

        if (progressionManager.CurrentPhaseIndex == currentPhase)
        {
            objectsPassedThisPhase++;
        }
    }

    public void EndRun()
    {
        if (progressionManager == null) return;

        // Для 5-й фазы проверяем, пробыли ли мы достаточно долго
        if (currentPhase == 4)
        {
            float phase4Duration = progressionManager.phases[4].startTime - progressionManager.phases[3].startTime;
            float timeInPhase5 = Time.time - timeEnteredPhase5;

            if (timeInPhase5 >= phase4Duration)
            {
                SavePhaseStats(currentPhase, objectsPassedThisPhase, actionTimesThisPhase);
            }
            else
            {
                Debug.Log($"Died too early in Phase 5 ({timeInPhase5:F1}s / {phase4Duration:F1}s). Stats not saved.");
            }
        }
    }

    // Сохраняем и объекты, и среднее время за фазу
    private void SavePhaseStats(int phaseIndex, int objectsCount, List<float> times)
    {
        if (objectsCount > 0)
        {
            string countKey = $"DiffStat_RunCount_Phase_{phaseIndex}";
            string sumKey = $"DiffStat_RunSum_Phase_{phaseIndex}";

            int count = PlayerPrefs.GetInt(countKey, 0);
            int sum = PlayerPrefs.GetInt(sumKey, 0);

            PlayerPrefs.SetInt(countKey, count + 1);
            PlayerPrefs.SetInt(sumKey, sum + objectsCount);
        }

        if (times.Count > 0)
        {
            float phaseAverageTime = 0f;
            foreach (float t in times) phaseAverageTime += t;
            phaseAverageTime /= times.Count;

            string timeCountKey = $"DiffStat_TimeCount_Phase_{phaseIndex}";
            string timeSumKey = $"DiffStat_TimeSum_Phase_{phaseIndex}";

            int timeCount = PlayerPrefs.GetInt(timeCountKey, 0);
            float timeSum = PlayerPrefs.GetFloat(timeSumKey, 0f);

            PlayerPrefs.SetInt(timeCountKey, timeCount + 1);
            PlayerPrefs.SetFloat(timeSumKey, timeSum + phaseAverageTime);
        }

        PlayerPrefs.Save();
    }

    // Теперь это среднее время САМОЙ ФАЗЫ (усредненное по всем забегам)
    public float GetAverageTime(int phaseIndex)
    {
        int count = PlayerPrefs.GetInt($"DiffStat_TimeCount_Phase_{phaseIndex}", 0);
        if (count == 0) return 0f;
        return PlayerPrefs.GetFloat($"DiffStat_TimeSum_Phase_{phaseIndex}", 0f) / count;
    }

    public float GetAverageObjectsPerRun(int phaseIndex)
    {
        int count = PlayerPrefs.GetInt($"DiffStat_RunCount_Phase_{phaseIndex}", 0);
        if (count == 0) return 0f;
        return (float)PlayerPrefs.GetInt($"DiffStat_RunSum_Phase_{phaseIndex}", 0) / count;
    }

    public void ResetStats()
    {
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.DeleteKey($"DiffStat_RunCount_Phase_{i}");
            PlayerPrefs.DeleteKey($"DiffStat_RunSum_Phase_{i}");
            PlayerPrefs.DeleteKey($"DiffStat_TimeCount_Phase_{i}");
            PlayerPrefs.DeleteKey($"DiffStat_TimeSum_Phase_{i}");
        }
        PlayerPrefs.Save();
        Debug.Log("Difficulty Stats Reset!");
    }
}