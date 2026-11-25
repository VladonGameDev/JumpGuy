using UnityEngine;

public class JitterEffect : MonoBehaviour
{
    [Header("Settings")]
    public float jitterAmount = 0.02f;
    public float jitterSpeed = 0.05f;

    private Vector3 startPosition;
    private float timer;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= jitterSpeed)
        {
            timer = 0f;

            float offsetX = Random.Range(-jitterAmount, jitterAmount);
            float offsetY = Random.Range(-jitterAmount, jitterAmount);
            float offsetZ = Random.Range(-jitterAmount, jitterAmount);

            transform.localPosition = startPosition + new Vector3(offsetX, offsetY, offsetZ);
        }
    }
}