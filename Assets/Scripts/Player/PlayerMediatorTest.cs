using UnityEngine;

public class PlayerMediatorTest : MonoBehaviour
{
    private PlayerInputHandler inputHandler;
    private PlayerPhysics playerPhysics;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        playerPhysics = GetComponent<PlayerPhysics>();
    }

    void OnEnable()
    {
        inputHandler.OnJumpRequested += Jump;
        inputHandler.OnSlideRequested += Slide;
        inputHandler.OnRollRequested += Roll;

        inputHandler.OnHoldUp += HoldUp;
        inputHandler.OnHoldDown += HoldDown;
        inputHandler.OnHoldRight += HoldRight;
    }

    void OnDisable()
    {
        inputHandler.OnJumpRequested -= Jump;
        inputHandler.OnSlideRequested -= Slide;
        inputHandler.OnRollRequested -= Roll;

        inputHandler.OnHoldUp -= HoldUp;
        inputHandler.OnHoldDown -= HoldDown;
        inputHandler.OnHoldRight -= HoldRight;
    }

    void Jump() => playerPhysics.PerformJump(1f);
    void Slide() => playerPhysics.PerformSlide(1f);
    void Roll() => playerPhysics.PerformRoll(1f);

    void HoldUp() => playerPhysics.ExtendCurrentJump();
    void HoldDown() => playerPhysics.ExtendCurrentSlide();
    void HoldRight() => playerPhysics.ExtendCurrentRoll();
}