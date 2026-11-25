using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    private int isRunningHash;
    private int isGroundedHash;
    private int jumpTriggerHash;
    private int slideHash;
    private int deathTriggerHash;
    private int flyHash;
    private int squeezeHash;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        isRunningHash = Animator.StringToHash("IsRunning");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        jumpTriggerHash = Animator.StringToHash("Jump");
        slideHash = Animator.StringToHash("IsSliding");
        deathTriggerHash = Animator.StringToHash("Death");
        flyHash = Animator.StringToHash("IsFlying");
        squeezeHash = Animator.StringToHash("IsSqueezing");
    }

    public void ResetAll()
    {
        animator.speed = 1f;
        animator.SetBool(isRunningHash, false);
        animator.SetBool(slideHash, false);
        animator.SetBool(isGroundedHash, false);
        animator.SetBool(squeezeHash, false);
        animator.SetBool(flyHash, false);
    }

    public void StartRunning()
    {
        animator.speed = 1f;
        animator.SetBool(isRunningHash, true);
    }

    public void StopRunning()
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

    public void StartSlide(float speedMultiplier = 1f)
    {
        // Ускоряем анимацию слайда
        animator.speed = speedMultiplier;
        animator.SetBool(slideHash, true);
    }

    public void StopSlide()
    {
        animator.speed = 1f;
        animator.SetBool(slideHash, false);
    }

    public void PlayDeath()
    {
        animator.speed = 1f;
        animator.SetTrigger(deathTriggerHash);
    }

    public void StartVent()
    {
        animator.speed = 1f;
        animator.SetBool(flyHash, true);
    }

    public void StopVent()
    {
        animator.speed = 1f;
        animator.SetBool(flyHash, false);
    }

    public void StartRoll(float speedMultiplier = 1f)
    {
        // Ускоряем анимацию ролла, чтобы она успевала за кодом
        animator.speed = speedMultiplier;
        animator.SetBool(squeezeHash, true);
    }

    public void StopRoll()
    {
        animator.speed = 1f;
        animator.SetBool(squeezeHash, false);
    }
}
