using UnityEngine;
using System.Collections;
using DG.Tweening;
using System;

public class PlayerPhysics : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpPeakHeight = 2.25f;
    public float jumpRiseTime = 0.4f;
    public float jumpHangTime = 0.25f;
    public Ease jumpRiseEase = Ease.OutQuad;

    [Header("Long Jump Settings")]
    public float longJumpHeightMultiplier = 1.25f;
    public float longJumpHangMultiplier = 1.5f;

    [Header("Slide Settings")]
    public float slideDuration = 1.1f;
    public float slideColliderHeight = 1.4f;
    public float colliderTransitionTime = 0.1f;
    public float longSlideDurationMultiplier = 1.5f;

    [Header("Roll Settings")]
    public float rollDuration = 1f;
    public float rollColliderHeight = 1f;
    public float rollColliderCenterYOffset = 1.5f;
    public float rollColliderRestoreOffset = 0.25f;
    public float longRollDurationMultiplier = 1.5f;

    public event Action OnSlideEnded;
    public event Action OnRollEnded;

    public bool IsGrounded { get; set; }
    public bool IsSliding { get; private set; }
    public bool IsRolling { get; private set; }
    public bool IsJumping { get; private set; }

    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private float originalColliderHeight;
    private float originalColliderCenterY;
    private float lowerPointOffset;
    private Coroutine colliderRoutine;

    private float currentSlideSpeedMultiplier = 1f;
    private float currentRollSpeedMultiplier = 1f;
    private float currentJumpSpeedMultiplier = 1f;

    private float jumpStartY;
    private int jumpGeneration = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        originalColliderHeight = playerCollider.height;
        originalColliderCenterY = playerCollider.center.y;
        lowerPointOffset = originalColliderCenterY - originalColliderHeight / 2f;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Platform"))
        {
            IsGrounded = true;
            IsJumping = false;
        }
    }

    public void PerformJump(float speedMultiplier)
    {
        if (IsJumping || IsSliding || IsRolling) return;

        IsJumping = true;
        IsGrounded = false;
        jumpGeneration++;
        currentJumpSpeedMultiplier = speedMultiplier;
        jumpStartY = transform.position.y;

        CancelCurrentAction();
        rb.useGravity = false;
        rb.isKinematic = true;

        StartCoroutine(JumpRoutine(jumpPeakHeight, jumpRiseTime, jumpHangTime, jumpGeneration));
    }

    public void ExtendCurrentJump()
    {
        if (!IsJumping) return;

        StopAllCoroutines();
        transform.DOKill();

        jumpGeneration++;
        int currentGen = jumpGeneration;
        
        IsJumping = true;

        float extendedHeight = jumpPeakHeight * longJumpHeightMultiplier;
        float extendedHang = jumpHangTime * longJumpHangMultiplier;

        StartCoroutine(JumpRoutine(extendedHeight, jumpRiseTime, extendedHang, currentGen));
    }

    IEnumerator JumpRoutine(float targetHeight, float baseRiseTime, float hangTime, int generation)
    {
        float targetY = jumpStartY + targetHeight;
        float riseTime = baseRiseTime / currentJumpSpeedMultiplier;
        float actualHangTime = hangTime / currentJumpSpeedMultiplier;

        transform.DOMoveY(targetY, riseTime).SetEase(jumpRiseEase);

        yield return new WaitForSeconds(riseTime);

        if (generation != jumpGeneration) yield break;

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

        yield return new WaitForSeconds(actualHangTime);

        if (generation != jumpGeneration) yield break;

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void PerformSlide(float speedMultiplier)
    {
        if (IsJumping || IsSliding || IsRolling) return;

        IsSliding = true;
        currentSlideSpeedMultiplier = speedMultiplier;
        CancelCurrentAction();

        rb.isKinematic = false;
        rb.useGravity = true;

        if (colliderRoutine != null) StopCoroutine(colliderRoutine);
        colliderRoutine = StartCoroutine(ChangeColliderSmooth(slideColliderHeight, lowerPointOffset + slideColliderHeight / 2f, colliderTransitionTime));

        float actualSlideDuration = slideDuration / speedMultiplier;
        Invoke(nameof(EndSlide), actualSlideDuration);
    }

    public void ExtendCurrentSlide()
    {
        if (!IsSliding) return;

        CancelInvoke(nameof(EndSlide));
        float actualSlideDuration = (slideDuration * longSlideDurationMultiplier) / currentSlideSpeedMultiplier;
        Invoke(nameof(EndSlide), actualSlideDuration);
    }

    void EndSlide()
    {
        IsSliding = false;
        if (colliderRoutine != null) StopCoroutine(colliderRoutine);
        colliderRoutine = StartCoroutine(ChangeColliderSmooth(originalColliderHeight, lowerPointOffset + originalColliderHeight / 2f, colliderTransitionTime));
        
        OnSlideEnded?.Invoke();
    }

    public void PerformRoll(float speedMultiplier)
    {
        if (IsJumping || IsSliding || IsRolling) return;

        IsRolling = true;
        currentRollSpeedMultiplier = speedMultiplier;
        CancelCurrentAction();

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;

        if (colliderRoutine != null) StopCoroutine(colliderRoutine);
        colliderRoutine = StartCoroutine(ChangeColliderSmooth(rollColliderHeight, lowerPointOffset + rollColliderHeight / 2f + rollColliderCenterYOffset, colliderTransitionTime));

        float actualRollDuration = rollDuration / speedMultiplier;
        float actualRestoreOffset = rollColliderRestoreOffset / speedMultiplier;
        float restoreStartTime = Mathf.Max(0f, actualRollDuration - actualRestoreOffset);

        Invoke(nameof(ReturnGravity), actualRollDuration);
        Invoke(nameof(StartRestoreCollider), restoreStartTime);
        Invoke(nameof(EndRoll), actualRollDuration);
    }

    public void ExtendCurrentRoll()
    {
        if (!IsRolling) return;

        CancelInvoke(nameof(EndRoll));
        CancelInvoke(nameof(ReturnGravity));
        CancelInvoke(nameof(StartRestoreCollider));

        float actualRollDuration = (rollDuration * longRollDurationMultiplier) / currentRollSpeedMultiplier;
        float actualRestoreOffset = rollColliderRestoreOffset / currentRollSpeedMultiplier;
        float restoreStartTime = Mathf.Max(0f, actualRollDuration - actualRestoreOffset);

        Invoke(nameof(ReturnGravity), actualRollDuration);
        Invoke(nameof(StartRestoreCollider), restoreStartTime);
        Invoke(nameof(EndRoll), actualRollDuration);
    }

    void ReturnGravity() => rb.useGravity = true;

    void StartRestoreCollider()
    {
        if (colliderRoutine != null) StopCoroutine(colliderRoutine);
        colliderRoutine = StartCoroutine(ChangeColliderSmooth(originalColliderHeight, lowerPointOffset + originalColliderHeight / 2f, rollColliderRestoreOffset));
    }

    void EndRoll()
    {
        IsRolling = false;
        rb.useGravity = true;
        
        OnRollEnded?.Invoke();
    }

    IEnumerator ChangeColliderSmooth(float targetHeight, float targetCenterY, float duration)
    {
        float elapsedTime = 0f;
        float startHeight = playerCollider.height;
        float startCenterY = playerCollider.center.y;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            playerCollider.height = Mathf.Lerp(startHeight, targetHeight, t);
            playerCollider.center = new Vector3(playerCollider.center.x, Mathf.Lerp(startCenterY, targetCenterY, t), playerCollider.center.z);

            yield return null;
        }

        playerCollider.height = targetHeight;
        playerCollider.center = new Vector3(playerCollider.center.x, targetCenterY, playerCollider.center.z);
    }

    void CancelCurrentAction()
    {
        CancelInvoke(nameof(EndSlide));
        CancelInvoke(nameof(EndRoll));
        CancelInvoke(nameof(ReturnGravity));
        CancelInvoke(nameof(StartRestoreCollider));
        StopAllCoroutines();
    }

    public void ResetPhysics()
    {
        transform.DOKill();
        CancelCurrentAction();

        playerCollider.height = originalColliderHeight;
        playerCollider.center = new Vector3(playerCollider.center.x, originalColliderCenterY, playerCollider.center.z);

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;

        IsSliding = false;
        IsRolling = false;
        IsJumping = false;
    }
}