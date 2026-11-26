using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    private BoosterManager boosterManager;
    private float pendingCoins;

    void Awake()
    {
        boosterManager = FindObjectOfType<BoosterManager>();
    }

    public void CollectCoin(int amount, CoinType type)
    {
        float value = amount * BoosterEffects.CoinMultiplier;

        bool halve = BoosterEffects.BoosterActive(BoosterType.HalfCoins) || pendingCoins > 0f;

        pendingCoins += halve ? value * 0.5f : value;

        int total = Mathf.FloorToInt(pendingCoins);
        if (total <= 0) return;

        pendingCoins -= total;

        if (type == CoinType.Rare)
        {
            PlayerPrefs.SetInt("RareCoins", PlayerPrefs.GetInt("RareCoins", 0) + total);
        }
        else
        {
            PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins", 0) + total);
        }
    }

    public void CollectBooster(BoosterType type)
    {
        if (boosterManager == null) return;

        boosterManager.ActivateBooster(type);
    }
}
