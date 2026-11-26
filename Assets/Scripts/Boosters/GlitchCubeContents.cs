using UnityEngine;

public class GlitchCubeContents : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] goodPrefabs;
    public GameObject[] badPrefabs;

    [Header("Settings")]
    public float goodChance = 0.5f;
    public int goodCount = 1;
    public float spawnAheadMin = 2.5f;
    public float spawnAheadMax = 4.5f;
    public float spawnSpreadY = 1.2f;

    private BoosterSpawner boosterSpawner;

    void Awake()
    {
        boosterSpawner = FindObjectOfType<BoosterSpawner>();
    }

    public void Emit()
    {
        if (boosterSpawner == null) boosterSpawner = FindObjectOfType<BoosterSpawner>();

        if (Random.value < goodChance)
        {
            if (goodPrefabs == null || goodPrefabs.Length == 0) return;

            for (int i = 0; i < goodCount; i++)
            {
                Spawn(goodPrefabs[Random.Range(0, goodPrefabs.Length)]);
            }
        }
        else
        {
            if (badPrefabs == null) return;

            for (int i = 0; i < badPrefabs.Length; i++)
            {
                Spawn(badPrefabs[i]);
            }
        }
    }

    void Spawn(GameObject prefab)
    {
        if (prefab == null) return;

        Vector3 offset = new Vector3(
            Random.Range(spawnAheadMin, spawnAheadMax),
            Random.Range(0f, spawnSpreadY),
            0f);

        GameObject booster = Instantiate(prefab, transform.position + offset, Quaternion.identity);

        if (boosterSpawner != null) boosterSpawner.RegisterSpawned(booster);
    }
}
