using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    public void CollectCoin(int amount, CoinType type)
    {
        if (type == CoinType.Rare)
        {
            int rareCoins = PlayerPrefs.GetInt("RareCoins", 0);
            PlayerPrefs.SetInt("RareCoins", rareCoins + amount);
        }
        else
        {
            int coins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", coins + amount);
        }
    }
}
