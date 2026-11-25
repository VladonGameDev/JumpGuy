using UnityEngine;

public class FlyingObstacle : MonoBehaviour
{
    [Header("Vertical Movement")]
    [Tooltip("Upper Y position limit")]
    public float maxYPosition = 3f;
    [Tooltip("Lower Y position limit")]
    public float minYPosition = 1f;
    [Tooltip("Vertical movement speed")]
    public float verticalSpeed = 2f;

    [Header("Horizontal Movement")]
    public float horizontalAmplitude = 1f;
    public float horizontalSpeed = 1.5f;

    private Vector3 startPosition;
    private float timeOffset;
    private float verticalPhase;

    void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 100f);
        verticalPhase = Random.Range(0f, Mathf.PI * 2f); // Случайная начальная фаза
    }

    void Update()
    {
        // Плавное синусоидальное движение между границами
        float amplitude = (maxYPosition - minYPosition) / 2f;
        float centerY = (maxYPosition + minYPosition) / 2f;

        verticalPhase += verticalSpeed * Time.deltaTime;

        // Плавный синус
        float newY = centerY + Mathf.Sin(verticalPhase) * amplitude;

        // Небольшое движение вперёд-назад
        float newX = transform.position.x + Mathf.Sin((Time.time + timeOffset) * horizontalSpeed) * horizontalAmplitude * Time.deltaTime;

        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}