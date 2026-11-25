using UnityEngine;
using DG.Tweening;

public class Vent : MonoBehaviour
{
    [Header("Debug")]
    public bool allowMultipleUse = false;

    [Header("Rise Settings")]
    public float riseHeight = 5f;
    public float riseDuration = 0.5f;
    public Ease riseEase = Ease.OutCubic;

    [Header("Fall Settings")]
    public float fallHeight = 0f;
    public float fallDuration = 0f;
    public Ease fallEase = Ease.InCubic;

    private bool hasLaunched = false;
    private RunProgressionManager progressionManager;
    private CameraController cameraController;

    void Start()
    {
        progressionManager = FindObjectOfType<RunProgressionManager>();
        cameraController = FindObjectOfType<CameraController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasLaunched && !allowMultipleUse) return;

        PlayerInputHandler inputHandler = other.GetComponent<PlayerInputHandler>();
        PlayerPhysics playerPhysics = other.GetComponent<PlayerPhysics>();
        PlayerAnimator playerAnimator = other.GetComponent<PlayerAnimator>();
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (inputHandler == null || playerPhysics == null || playerAnimator == null || rb == null) return;

        hasLaunched = true;

        inputHandler.SetInputBlocked(true);
        playerPhysics.ResetPhysics();
        playerAnimator.PlayVent();

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        float speedMultiplier = 1f;
        if (progressionManager != null)
        {
            speedMultiplier = 1f + (progressionManager.TotalSpeedIndex - 1f) / 2f;
        }

        float actualRiseDuration = riseDuration / speedMultiplier;
        float baseFallTime = fallDuration > 0f ? fallDuration : riseDuration;
        float actualFallTime = baseFallTime / speedMultiplier;

        if (cameraController != null)
        {
            cameraController.RiseCamera();
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(rb.DOMoveY(riseHeight, actualRiseDuration).SetEase(riseEase));
        seq.Append(rb.DOMoveY(fallHeight, actualFallTime).SetEase(fallEase));

        seq.OnComplete(() =>
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;

            playerAnimator.StopVent();
            playerAnimator.PlayRun();

            inputHandler.SetInputBlocked(false);

            if (cameraController != null)
            {
                cameraController.ReturnCamera();
            }
        });
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerAnimator playerAnimator = collision.gameObject.GetComponent<PlayerAnimator>();
            if (playerAnimator != null) playerAnimator.PlayDeath();

#if !UNITY_EDITOR
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
#endif
        }
    }
}