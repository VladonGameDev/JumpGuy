using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    private int isRunningHash;
    private int isGroundedHash;
    private int jumpTriggerHash;
    private int slideHash;
    private int longSlideHash;
    private int rollHash;
    private int longRollHash;
    private int deathTriggerHash;
    private int ventHash;
    private int flyHash;

    public bool IsVenting { get; private set; }

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        isRunningHash = Animator.StringToHash("IsRunning");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        jumpTriggerHash = Animator.StringToHash("Jump");
        slideHash = Animator.StringToHash("IsSliding");
        longSlideHash = Animator.StringToHash("IsLongSliding");
        rollHash = Animator.StringToHash("IsRolling");
        longRollHash = Animator.StringToHash("IsLongRolling");
        deathTriggerHash = Animator.StringToHash("Death");
        ventHash = Animator.StringToHash("IsVenting");
        flyHash = Animator.StringToHash("IsFlying");
    }

    public void ResetAll()
    {
        animator.speed = 1f;
        animator.SetBool(isRunningHash, false);
        animator.SetBool(isGroundedHash, false);
        animator.SetBool(slideHash, false);
        animator.SetBool(longSlideHash, false);
        animator.SetBool(rollHash, false);
        animator.SetBool(longRollHash, false);
        animator.SetBool(ventHash, false);
        animator.SetBool(flyHash, false);
        IsVenting = false;
    }

    public void PlayRun()
    {
        animator.speed = 1f;
        animator.SetBool(isRunningHash, true);
    }

    public void StopRun()
    {
        animator.SetBool(isRunningHash, false);
    }

    public void PlayJump()
    {
        animator.SetTrigger(jumpTriggerHash);
        animator.SetBool(isGroundedHash, false);
    }

    public void PlayLand()
    {
        animator.SetBool(isGroundedHash, true);
    }

    public void PlaySlide(float speedMultiplier = 1f)
    {
        animator.speed = speedMultiplier;
        animator.SetBool(slideHash, true);
    }

    public void PlayLongSlide(float speedMultiplier = 1f)
    {
        animator.speed = speedMultiplier;
        animator.SetBool(longSlideHash, true);
    }

    public void StopSlide()
    {
        animator.speed = 1f;
        animator.SetBool(slideHash, false);
        animator.SetBool(longSlideHash, false);
    }

    public void PlayRoll(float speedMultiplier = 1f)
    {
        animator.speed = speedMultiplier;
        animator.SetBool(rollHash, true);
    }

    public void PlayLongRoll(float speedMultiplier = 1f)
    {
        animator.speed = speedMultiplier;
        animator.SetBool(longRollHash, true);
    }

    public void StopRoll()
    {
        animator.speed = 1f;
        animator.SetBool(rollHash, false);
        animator.SetBool(longRollHash, false);
    }

    public void PlayDeath()
    {
        animator.speed = 1f;
        animator.SetTrigger(deathTriggerHash);
    }

    public void PlayVent()
    {
        IsVenting = true;
        animator.speed = 1f;
        animator.SetBool(ventHash, true);
    }

    public void StopVent()
    {
        IsVenting = false;
        animator.speed = 1f;
        animator.SetBool(ventHash, false);
    }

    public void PlayFlying()
    {
        animator.speed = 1f;
        animator.SetBool(flyHash, true);
    }

    public void StopFlying()
    {
        animator.speed = 1f;
        animator.SetBool(flyHash, false);
    }
}