using UnityEngine;

public class StatsManager : MonoBehaviour
{
    [Header("References")]
    public RunProgressionManager progressionManager;

    [Header("Settings")]
    public float baseScoreRate = 10f; // 10 очков в секунду
    public string recordKey = "HighScore";
    public string coinsKey = "Coins";

    private float currentScore = 0f;
    private int displayedScore = 0;
    private int highScore = 0;

    private float recordCheckTimer = 0f;

    void Start()
    {
        highScore = PlayerPrefs.GetInt(recordKey, 0);

        currentScore = 0f;
        displayedScore = 0;
    }

    void Update()
    {
        if (progressionManager != null && progressionManager.IsRunning)
        {
            float scoreMultiplier = progressionManager.CurrentPhase.scoreMultiplier;
            currentScore += baseScoreRate * scoreMultiplier * Time.deltaTime;

            int newScore = Mathf.FloorToInt(currentScore);
            if (newScore != displayedScore)
            {
                displayedScore = newScore;
            }

            // ѕроверка рекорда (раз в секунду)
            recordCheckTimer += Time.deltaTime;
            if (recordCheckTimer >= 1f)
            {
                recordCheckTimer = 0f;
                if (displayedScore > highScore)
                {
                    highScore = displayedScore;
                    PlayerPrefs.SetInt(recordKey, highScore);
                    PlayerPrefs.Save();
                }
            }
        }
    }

    public int GetCurrentScore()
    {
        return displayedScore;
    }

    public int GetHighScore()
    {
        return highScore;
    }

    public void AddCoins(int amount)
    {
        int currentCoins = PlayerPrefs.GetInt(coinsKey, 0);
        currentCoins += amount;
        PlayerPrefs.SetInt(coinsKey, currentCoins);
    }
}
