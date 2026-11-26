using UnityEngine;
using DG.Tweening;

public enum CoinType
{
    Normal,
    Rare
}

public enum HudCoinTarget
{
    Left,
    Right
}

public class Coin : MonoBehaviour
{
    [Header("Settings")]
    public CoinType coinType = CoinType.Normal;
    public int coinValue = 1;
    public float rotationSpeed = 150f;

    [Header("Bobbing")]
    public float bobAmplitude = 0.2f;
    public float bobSpeed = 2f;

    [Header("Animation")]
    public float flyToHudDuration = 0.3f;
    public float magnetCollectDistance = 0.8f;
    public HudCoinTarget hudTarget = HudCoinTarget.Left;

    public bool IsCollected => isCollected;

    private bool isCollected = false;
    private bool magnetized = false;
    private Collider coinCollider;
    private Vector3 startLocalPosition;
    private Transform magnetTarget;
    private float magnetPullSpeed = 14f;

    void Awake()
    {
        coinCollider = GetComponent<Collider>();
        startLocalPosition = transform.localPosition;
    }

    public void Initialize()
    {
        startLocalPosition = transform.localPosition;
    }

    public void SetMagnetized(Transform target, float pullSpeed)
    {
        if (isCollected) return;

        magnetized = true;
        magnetTarget = target;
        magnetPullSpeed = Mathf.Max(pullSpeed, 0.1f);
    }

    void Update()
    {
        if (isCollected) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (magnetized)
        {
            if (magnetTarget == null)
            {
                Collect();
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, magnetTarget.position, magnetPullSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, magnetTarget.position) <= magnetCollectDistance) Collect();

            return;
        }

        float newY = startLocalPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = new Vector3(startLocalPosition.x, newY, startLocalPosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        PlayerCollector collector = other.GetComponent<PlayerCollector>();
        if (collector == null) return;

        isCollected = true;
        if (coinCollider != null) coinCollider.enabled = false;

        collector.CollectCoin(coinValue, coinType);

        PlayCollectAnimation();
    }

    void Collect()
    {
        if (isCollected) return;

        isCollected = true;
        if (coinCollider != null) coinCollider.enabled = false;

        PlayerCollector collector = null;
        if (magnetTarget != null) collector = magnetTarget.GetComponent<PlayerCollector>();
        if (collector == null) collector = FindObjectOfType<PlayerCollector>();

        if (collector != null) collector.CollectCoin(coinValue, coinType);

        PlayCollectAnimation();
    }

    void PlayCollectAnimation()
    {
        transform.DOKill();

        Vector3 targetPos = transform.position + Vector3.up * 1.5f;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            float viewportX = hudTarget == HudCoinTarget.Left ? 0.1f : 0.9f;
            targetPos = mainCam.ViewportToWorldPoint(new Vector3(viewportX, 0.9f, 10f));
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(targetPos, flyToHudDuration).SetEase(Ease.InBack));
        seq.Join(transform.DOScale(Vector3.zero, flyToHudDuration).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}
