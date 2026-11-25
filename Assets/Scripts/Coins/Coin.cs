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
    public HudCoinTarget hudTarget = HudCoinTarget.Left;

    private bool isCollected = false;
    private Collider coinCollider;
    private Vector3 startPosition;

    void Awake()
    {
        coinCollider = GetComponent<Collider>();
    }

    public void Initialize()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        if (isCollected) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        isCollected = true;
        if (coinCollider != null) coinCollider.enabled = false;

        PlayerCollector collector = other.GetComponent<PlayerCollector>();
        if (collector != null)
        {
            collector.CollectCoin(coinValue, coinType);
        }

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