using UnityEngine;
using System;

public class SwipeDetector : MonoBehaviour
{
    [Header("Settings")]
    public float swipeThreshold = 50f;
    public float doubleTapTimeWindow = 0.3f;
    public float holdTimeThreshold = 0.15f;

    public event Action OnSwipeUp;
    public event Action OnSwipeDown;
    public event Action OnSwipeRight;
    public event Action OnDoubleTap;

    public event Action OnHoldUp;
    public event Action OnHoldDown;
    public event Action OnHoldRight;

    private Vector2 startTouchPosition;
    private bool swipeTriggered = false;
    private float touchStartTime = 0f;
    private bool holdTriggered = false;

    private float lastTapTime = -10f;
    private int currentSwipeDirection = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            swipeTriggered = false;
            holdTriggered = false;
            touchStartTime = Time.time;
            currentSwipeDirection = 0;

            if (Time.time - lastTapTime <= doubleTapTimeWindow)
            {
                OnDoubleTap?.Invoke();
                lastTapTime = -10f;
            }
            else
            {
                lastTapTime = Time.time;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (!swipeTriggered)
            {
                Vector2 swipeDelta = (Vector2)Input.mousePosition - startTouchPosition;

                if (swipeDelta.magnitude > swipeThreshold)
                {
                    if (Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
                    {
                        if (swipeDelta.y > 0) currentSwipeDirection = 1;
                        else currentSwipeDirection = 2;
                    }
                    else
                    {
                        if (swipeDelta.x > 0) currentSwipeDirection = 3;
                    }

                    if (currentSwipeDirection == 1) OnSwipeUp?.Invoke();
                    else if (currentSwipeDirection == 2) OnSwipeDown?.Invoke();
                    else if (currentSwipeDirection == 3) OnSwipeRight?.Invoke();

                    swipeTriggered = true;
                }
            }
            else if (!holdTriggered && Time.time - touchStartTime > holdTimeThreshold)
            {
                if (currentSwipeDirection == 1) OnHoldUp?.Invoke();
                else if (currentSwipeDirection == 2) OnHoldDown?.Invoke();
                else if (currentSwipeDirection == 3) OnHoldRight?.Invoke();

                holdTriggered = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            swipeTriggered = false;
            holdTriggered = false;
            currentSwipeDirection = 0;
        }
    }
}
