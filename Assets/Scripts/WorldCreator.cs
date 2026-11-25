using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class WorldCreator : MonoBehaviour
{
    [System.Serializable]
    public class RoadPrefab
    {
        public GameObject prefab;
        [Header("Spawn Rules")]
        public int maxConsecutiveSpawns = 2;
        public int minPrefabsBeforeRespawn = 3;
    }

    [Header("References")]
    public RunProgressionManager progressionManager;
    public List<RoadPrefab> roadPrefabs;

    [Header("Settings")]
    public float spawnDistance = 10f;
    public float despawnDistance = 20f;
    public float maxSpawnDistanceX = 30f;
    public float initialSpawnPositionX = 0f;
    public float roadSpeed = 5f;

    public Transform playerTransform;

    private List<GameObject> spawnedRoads = new List<GameObject>();
    private List<int> spawnHistoryIndices = new List<int>();

    void Start()
    {
        SpawnInitialRoads();
    }

    void Update()
    {
        if (PlayerPrefs.GetInt("isGameStarted") == 1)
        {
            MoveRoads();
            CheckDespawnRoads();
        }
    }

    void MoveRoads()
    {
        float currentSpeed = progressionManager != null ? progressionManager.TotalSpeedIndex : 1f;
        foreach (GameObject road in spawnedRoads)
        {
            road.transform.Translate(Vector3.left * roadSpeed * currentSpeed * Time.deltaTime);
        }

        if (spawnedRoads.Count > 0 && spawnedRoads[spawnedRoads.Count - 1].transform.position.x - playerTransform.position.x < maxSpawnDistanceX)
        {
            SpawnNewRoad();
        }
    }

    void SpawnInitialRoads()
    {
        float spawnPositionX = initialSpawnPositionX;
        while (spawnPositionX < despawnDistance)
        {
            SpawnRoadAt(spawnPositionX);
            spawnPositionX += spawnDistance;
        }
    }

    void SpawnNewRoad()
    {
        float newX = spawnedRoads[spawnedRoads.Count - 1].transform.position.x + spawnDistance;
        SpawnRoadAt(newX);
    }

    void SpawnRoadAt(float x)
    {
        int randomIndex = GetRandomRoadIndex();
        if (randomIndex == -1) return;

        GameObject newRoad = Instantiate(roadPrefabs[randomIndex].prefab, new Vector3(x, 0f, 0f), Quaternion.identity);
        spawnedRoads.Add(newRoad);
        spawnHistoryIndices.Add(randomIndex);
    }

    void CheckDespawnRoads()
    {
        if (spawnedRoads.Count > 0 && spawnedRoads[0].transform.position.x < -despawnDistance)
        {
            GameObject despawnedRoad = spawnedRoads[0];
            spawnedRoads.RemoveAt(0);
            if (spawnHistoryIndices.Count > 0) spawnHistoryIndices.RemoveAt(0);
            Destroy(despawnedRoad);
        }
    }

    int GetRandomRoadIndex()
    {
        if (roadPrefabs.Count == 0) return -1;

        List<int> availableIndices = new List<int>();

        for (int i = 0; i < roadPrefabs.Count; i++)
        {
            if (CanSpawnRoad(i))
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0) return 0;

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    bool CanSpawnRoad(int index)
    {
        int consecutiveCount = 0;
        int stepsSinceLastSpawn = -1;

        for (int i = spawnHistoryIndices.Count - 1; i >= 0; i--)
        {
            if (stepsSinceLastSpawn != -1)
            {
                stepsSinceLastSpawn++;
            }
            else if (spawnHistoryIndices[i] == index)
            {
                stepsSinceLastSpawn = 0;
            }

            if (i == spawnHistoryIndices.Count - 1 || consecutiveCount > 0)
            {
                if (spawnHistoryIndices[i] == index)
                {
                    consecutiveCount++;
                }
                else
                {
                    if (consecutiveCount > 0) break;
                }
            }
        }

        if (consecutiveCount >= roadPrefabs[index].maxConsecutiveSpawns) return false;
        if (stepsSinceLastSpawn != -1 && stepsSinceLastSpawn < roadPrefabs[index].minPrefabsBeforeRespawn) return false;

        return true;
    }
}