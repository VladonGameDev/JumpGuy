using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMediator : MonoBehaviour
{
    private RunProgressionManager progressionManager;
    private CameraController cameraController;
    private GameMenuUIController menuUIController;
    private DifficultyStatsTracker statsTracker;

    private PlayerInputHandler inputHandler;
    private PlayerPhysics playerPhysics;
    private PlayerAnimator playerAnimator;

    private float currentSpeedMultiplier = 1f;

    void Awake()
    {
        progressionManager = FindObjectOfType<RunProgressionManager>();
        cameraController = FindObjectOfType<CameraController>();
        menuUIController = FindObjectOfType<GameMenuUIController>();
        statsTracker = FindObjectOfType<DifficultyStatsTracker>();

        inputHandler = GetComponent<PlayerInputHandler>();
        playerPhysics = GetComponent<PlayerPhysics>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    void Start()
    {
        playerAnimator.ResetAll();
        PlayerPrefs.SetInt("Coins", 0);
        PlayerPrefs.SetInt("RareCoins", 0);
        if (statsTracker != null) statsTracker.StartRun();
    }

    void OnEnable()
    {
        inputHandler.OnJumpRequested += HandleJump;
        inputHandler.OnSlideRequested += HandleSlide;
        inputHandler.OnRollRequested += HandleRoll;

        inputHandler.OnHoldUp += HandleHoldJump;
        inputHandler.OnHoldDown += HandleHoldSlide;
        inputHandler.OnHoldRight += HandleHoldRoll;

        playerPhysics.OnSlideEnded += HandleSlideEnd;
        playerPhysics.OnRollEnded += HandleRollEnd;
    }

    void OnDisable()
    {
        inputHandler.OnJumpRequested -= HandleJump;
        inputHandler.OnSlideRequested -= HandleSlide;
        inputHandler.OnRollRequested -= HandleRoll;

        inputHandler.OnHoldUp -= HandleHoldJump;
        inputHandler.OnHoldDown -= HandleHoldSlide;
        inputHandler.OnHoldRight -= HandleHoldRoll;

        playerPhysics.OnSlideEnded -= HandleSlideEnd;
        playerPhysics.OnRollEnded -= HandleRollEnd;
    }

    void Update()
    {
        CalculateSpeedMultiplier();

        if (PlayerPrefs.GetInt("isGameStarted") == 1 && !playerAnimator.IsVenting)
        {
            if (playerPhysics.IsGrounded && !playerPhysics.IsSliding && !playerPhysics.IsRolling && !playerPhysics.IsJumping)
            {
                playerAnimator.PlayRun();
            }
        }
    }

    void CalculateSpeedMultiplier()
    {
        if (progressionManager != null)
        {
            float speedFactor = 1f + (progressionManager.TotalSpeedIndex - 1f) / 2f;

            // Замедление времени растягивает действия игрока во столько же раз:
            // ролл, слайд, лонг-ролл, лонг-слайд, прыжок и лонг-прыжок длятся
            // дольше, поэтому в слоу-мо их длина относительно мира сохраняется.
            currentSpeedMultiplier = speedFactor * progressionManager.WorldSlowdown;
        }
        else
        {
            currentSpeedMultiplier = 1f;
        }
    }

    void HandleJump()
    {
        if (statsTracker != null) statsTracker.TrackAction();
        playerAnimator.PlayJump();
        playerPhysics.PerformJump(currentSpeedMultiplier);
    }

    void HandleSlide()
    {
        if (statsTracker != null) statsTracker.TrackAction();
        playerAnimator.StopRun();
        playerAnimator.PlaySlide(currentSpeedMultiplier);
        playerPhysics.PerformSlide(currentSpeedMultiplier);

        if (cameraController != null) cameraController.StartSlideFOV();
    }

    void HandleRoll()
    {
        if (statsTracker != null) statsTracker.TrackAction();
        playerAnimator.StopRun();
        playerAnimator.PlayRoll(currentSpeedMultiplier);
        playerPhysics.PerformRoll(currentSpeedMultiplier);

        if (cameraController != null) cameraController.StartRollFOV();
        if (progressionManager != null) progressionManager.DashForward(1.4f, playerPhysics.rollDuration / currentSpeedMultiplier);
    }

    void HandleHoldJump()
    {
        playerPhysics.ExtendCurrentJump();
    }

    void HandleHoldSlide()
    {
        playerAnimator.PlayLongSlide(currentSpeedMultiplier);
        playerPhysics.ExtendCurrentSlide();

        if (cameraController != null) cameraController.StartLongSlideFOV();
    }

    void HandleHoldRoll()
    {
        playerAnimator.PlayLongRoll(currentSpeedMultiplier);
        playerPhysics.ExtendCurrentRoll();

        if (cameraController != null) cameraController.StartLongRollFOV();

        if (progressionManager != null)
        {
            float longDuration = (playerPhysics.rollDuration * playerPhysics.longRollDurationMultiplier) / currentSpeedMultiplier;
            progressionManager.DashForward(1.4f, longDuration);
        }
    }

    void HandleSlideEnd()
    {
        playerAnimator.StopSlide();
        if (cameraController != null) cameraController.EndSlideFOV();
    }

    void HandleRollEnd()
    {
        playerAnimator.StopRoll();
        if (cameraController != null) cameraController.EndRollFOV();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Platform"))
        {
            playerAnimator.PlayLand();
            if (PlayerPrefs.GetInt("isGameStarted") == 1)
            {
                playerAnimator.PlayRun();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "GroundObstacle")
        {
            playerAnimator.PlayDeath();
            if (cameraController != null) cameraController.PlayShake(2f, 0.3f);
            if (menuUIController != null) menuUIController.OnPlayerDeath();
            if (progressionManager != null) progressionManager.StopRun();
            if (statsTracker != null) statsTracker.EndRun();

            PlayerDeathExplosion explosion = GetComponent<PlayerDeathExplosion>();
            if (explosion != null) explosion.PlayExplosion();

            StartCoroutine(ReloadRoutine());
        }
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
#if !UNITY_EDITOR
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#else
        yield break;
#endif
    }
}
