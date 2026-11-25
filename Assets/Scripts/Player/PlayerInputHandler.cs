using UnityEngine;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Debug")]
    public bool showInputLogs = false;

    public event Action OnJumpRequested;
    public event Action OnSlideRequested;
    public event Action OnRollRequested;

    public event Action OnHoldUp;
    public event Action OnHoldDown;
    public event Action OnHoldRight;

    private SwipeDetector swipeDetector;
    private PlayerPhysics playerPhysics;

    public bool IsInputBlocked { get; set; } = false;

    void Awake()
    {
        swipeDetector = GetComponent<SwipeDetector>();
        playerPhysics = GetComponent<PlayerPhysics>();
    }

    void OnEnable()
    {
        swipeDetector.OnSwipeUp += () => HandleInput(ActionType.Jump);
        swipeDetector.OnSwipeDown += () => HandleInput(ActionType.Slide);
        swipeDetector.OnSwipeRight += () => HandleInput(ActionType.Roll);

        swipeDetector.OnHoldUp += () => { Log("HOLD UP"); OnHoldUp?.Invoke(); };
        swipeDetector.OnHoldDown += () => { Log("HOLD DOWN"); OnHoldDown?.Invoke(); };
        swipeDetector.OnHoldRight += () => { Log("HOLD RIGHT"); OnHoldRight?.Invoke(); };

        swipeDetector.OnDoubleTap += () => Log("DOUBLE TAP DETECTED!");
    }

    public void SetInputBlocked(bool blocked)
    {
        IsInputBlocked = blocked;
    }

    void HandleInput(ActionType type)
    {
        if (IsInputBlocked)
        {
            Log($"INPUT: {type} | IGNORED | Reason: Input is blocked");
            return;
        }

        if (PlayerPrefs.GetInt("isGameStarted") != 1)
        {
            Log($"INPUT: {type} | IGNORED | Reason: Game not started");
            return;
        }

        string reason;
        if (CanActNow(type, out reason))
        {
            Log($"INPUT: {type} | EXECUTED");
            InvokeAction(type);
        }
        else
        {
            Log($"INPUT: {type} | IGNORED | Reason: {reason}");
        }
    }

    bool CanActNow(ActionType type, out string reason)
    {
        reason = "";

        if (type == ActionType.Jump)
        {
            if (!playerPhysics.IsGrounded) { reason = "Not grounded"; return false; }
            if (playerPhysics.IsSliding) { reason = "Is sliding"; return false; }
            if (playerPhysics.IsRolling) { reason = "Is rolling"; return false; }
            if (playerPhysics.IsJumping) { reason = "Is jumping"; return false; }
        }
        else
        {
            if (playerPhysics.IsJumping) { reason = "Is jumping"; return false; }
            if (playerPhysics.IsSliding) { reason = "Is sliding"; return false; }
            if (playerPhysics.IsRolling) { reason = "Is rolling"; return false; }
        }
        return true;
    }

    void InvokeAction(ActionType type)
    {
        if (type == ActionType.Jump) OnJumpRequested?.Invoke();
        else if (type == ActionType.Slide) OnSlideRequested?.Invoke();
        else if (type == ActionType.Roll) OnRollRequested?.Invoke();
    }

    void Log(string message)
    {
        if (showInputLogs)
        {
            Debug.Log($"[{Time.time:F2}] {message}");
        }
    }

    enum ActionType { Jump, Slide, Roll }
}