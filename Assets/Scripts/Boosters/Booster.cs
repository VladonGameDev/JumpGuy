using UnityEngine;
using DG.Tweening;

public class Booster : MonoBehaviour
{
    [Header("Settings")]
    public BoosterType boosterType = BoosterType.Magnet;
    public float rotationSpeed = 120f;

    [Header("Animation")]
    public Transform visual;
    public float bobAmplitude = 0.18f;
    public float bobDuration = 0.9f;
    public float pulseAmount = 1.12f;
    public float pulseDuration = 0.7f;
    public float tiltAngle = 12f;
    public float tiltDuration = 1.4f;
    public float introDuration = 0.35f;

    private const float IntroStartScale = 0.35f;

    private bool isCollected = false;
    private Collider boosterCollider;
    private Vector3 visualBasePosition;
    private Vector3 visualBaseScale;
    private Vector3 visualBaseRotation;
    private Tween introTween;
    private Tween pulseTween;
    private Tween bobTween;
    private Tween tiltTween;

    void Awake()
    {
        boosterCollider = GetComponent<Collider>();

        if (visual == null)
        {
            Renderer childRenderer = GetComponentInChildren<Renderer>();
            if (childRenderer != null) visual = childRenderer.transform;
        }

        // Базовые значения снимаем один раз, до любых твинов:
        // иначе повторный запуск анимации поймал бы промежуточный кадр.
        CaptureBaseTransform();
    }

    // Нулевой скейл означает, что визуал ещё не готов (или его подменили после Awake):
    // в этом случае базу берём заново позже, иначе бустер «схлопнулся» бы в ноль.
    void CaptureBaseTransform()
    {
        if (visual == null) return;
        if (visualBaseScale != Vector3.zero) return;

        visualBasePosition = visual.localPosition;
        visualBaseScale = visual.localScale;
        visualBaseRotation = visual.localEulerAngles;
    }

    void Start()
    {
        StartVisualAnimation();
    }

    void StartVisualAnimation()
    {
        if (visual == null) return;

        // Страховка: если Awake ещё не отработал (или визуал подменили),
        // берём базовые значения здесь — иначе схлопнули бы бустер в ноль.
        CaptureBaseTransform();

        KillTweens();

        // Возвращаем визуал в исходное состояние перед новым проигрыванием.
        visual.localPosition = visualBasePosition;
        visual.localScale = visualBaseScale;
        visual.localEulerAngles = visualBaseRotation;

        float delay = Random.Range(0f, Mathf.Max(bobDuration, pulseDuration));

        // 1) Появление: скейл от маленького к базовому.
        if (introDuration > 0f)
        {
            visual.localScale = visualBaseScale * IntroStartScale;
            introTween = visual.DOScale(visualBaseScale, introDuration)
                .SetEase(Ease.OutBack)
                .SetDelay(delay * 0.25f)
                .OnComplete(BeginPulse);
        }
        else
        {
            BeginPulse();
        }

        // 2) Покачивание по высоте — не трогает скейл.
        if (bobAmplitude > 0f && bobDuration > 0f)
        {
            bobTween = visual.DOLocalMoveY(visualBasePosition.y + bobAmplitude, bobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);
        }

        // 3) Наклон — тоже отдельная ось, со скейлом не конфликтует.
        if (tiltAngle > 0f && tiltDuration > 0f)
        {
            tiltTween = visual.DOLocalRotate(visualBaseRotation + new Vector3(tiltAngle, 0f, tiltAngle * 0.5f), tiltDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);
        }
    }

    // Пульсация масштаба запускается только после появления,
    // иначе OutBack-твин и Yoyo-твин боролись бы за localScale.
    void BeginPulse()
    {
        if (visual == null || isCollected) return;
        if (pulseAmount <= 1f || pulseDuration <= 0f) return;

        pulseTween = visual.DOScale(visualBaseScale * pulseAmount, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void KillTweens()
    {
        if (introTween != null && introTween.IsActive()) introTween.Kill();
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();
        if (bobTween != null && bobTween.IsActive()) bobTween.Kill();
        if (tiltTween != null && tiltTween.IsActive()) tiltTween.Kill();

        introTween = null;
        pulseTween = null;
        bobTween = null;
        tiltTween = null;
    }

    void Update()
    {
        if (isCollected) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        PlayerCollector collector = other.GetComponent<PlayerCollector>();
        if (collector == null) return;

        isCollected = true;
        if (boosterCollider != null) boosterCollider.enabled = false;

        GlitchCubeContents contents = GetComponent<GlitchCubeContents>();
        if (contents != null)
        {
            contents.Emit();
            Destroy(gameObject);
            return;
        }

        collector.CollectBooster(boosterType);

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        KillTweens();
    }
}
