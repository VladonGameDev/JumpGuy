using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public CinemachineVirtualCamera introCamera;
    public CinemachineVirtualCamera gameplayCamera;

    [Header("Follow Targets")]
    public Transform groundFollowTarget;
    public Transform playerFollowTarget;

    [Header("Camera Lock")]
    public bool lockYaw = true;

    [Header("Intro Settings")]
    public float introDuration = 2f;

    [Header("Global FOV")]
    public float defaultFOV = 60f;

    [Header("Roll FOV Settings")]
    public float rollFOV = 65f;
    public float rollFOVSpeed = 5f;

    [Header("Long Roll FOV Settings")]
    public float longRollFOV = 75f;
    public float longRollFOVSpeed = 5f;

    [Header("Slide FOV Settings")]
    public float slideFOV = 55f;
    public float slideFOVSpeed = 5f;

    [Header("Long Slide FOV Settings")]
    public float longSlideFOV = 45f;
    public float longSlideFOVSpeed = 5f;

    private bool gameplayStarted = false;
    private bool cameraLocked = false;
    private float targetFOV;
    private float currentFOV;
    private float currentFOVSpeed;

    void Start()
    {
        targetFOV = defaultFOV;
        currentFOV = defaultFOV;
        currentFOVSpeed = rollFOVSpeed;
    }

    public void StartIntro()
    {
        if (introCamera == null || gameplayCamera == null) return;
        introCamera.Priority = 5;
        gameplayCamera.Priority = 20;
        gameplayStarted = true;
        Invoke(nameof(EnableLock), introDuration);
    }

    void EnableLock()
    {
        cameraLocked = true;
    }

    public void RiseCamera()
    {
        if (gameplayCamera == null || playerFollowTarget == null) return;
        gameplayCamera.Follow = playerFollowTarget;
    }

    public void ReturnCamera()
    {
        if (gameplayCamera == null || groundFollowTarget == null) return;
        gameplayCamera.Follow = groundFollowTarget;
    }

    public void StartRollFOV()
    {
        targetFOV = rollFOV;
        currentFOVSpeed = rollFOVSpeed;
    }

    public void StartLongRollFOV()
    {
        targetFOV = longRollFOV;
        currentFOVSpeed = longRollFOVSpeed;
    }

    public void EndRollFOV()
    {
        targetFOV = defaultFOV;
        currentFOVSpeed = rollFOVSpeed;
    }

    public void StartSlideFOV()
    {
        targetFOV = slideFOV;
        currentFOVSpeed = slideFOVSpeed;
    }

    public void StartLongSlideFOV()
    {
        targetFOV = longSlideFOV;
        currentFOVSpeed = longSlideFOVSpeed;
    }

    public void EndSlideFOV()
    {
        targetFOV = defaultFOV;
        currentFOVSpeed = slideFOVSpeed;
    }

    void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    void OnCameraUpdated(CinemachineBrain brain)
    {
        if (!cameraLocked || brain == null) return;

        if (lockYaw)
        {
            Vector3 euler = brain.transform.eulerAngles;
            euler.y = 0f;
            brain.transform.eulerAngles = euler;
        }
    }

    void LateUpdate()
    {
        if (!cameraLocked || gameplayCamera == null) return;

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * currentFOVSpeed);

        var lens = gameplayCamera.m_Lens;
        lens.FieldOfView = currentFOV;
        gameplayCamera.m_Lens = lens;
    }

    public void PlayShake(float intensity = 1f, float duration = 0.3f)
    {
        if (gameplayCamera == null) return;
        gameplayCamera.transform.DOShakePosition(duration, intensity * 0.3f, 10, 90f, false, true);
    }
}