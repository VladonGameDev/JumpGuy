using UnityEngine;

public class StatsManager : MonoBehaviour
{
    [Header("References")]
    public RunProgressionManager progressionManager;

    [Header("Settings")]
    public float baseScoreRate = 10f;
    public string recordKey = "HighScore";
    public string coinsKey = "Coins";

    private float currentScore = 0f;
    private float pendingScore = 0f;
    private int displayedScore = 0;
    private int highScore = 0;

    private float recordCheckTimer = 0f;

    void Start()
    {
        highScore = PlayerPrefs.GetInt(recordKey, 0);

        currentScore = 0f;
        pendingScore = 0f;
        displayedScore = 0;
    }

    void Update()
    {
        if (progressionManager == null || !progressionManager.IsRunning) return;

        AddScore(baseScoreRate * progressionManager.CurrentPhase.scoreMultiplier * Time.deltaTime);

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

    public void AddScore(float amount)
    {
        float value = amount * BoosterEffects.ScoreMultiplier;
        if (BoosterEffects.BoosterActive(BoosterType.HalfScore)) value *= 0.5f;

        pendingScore += value;

        int whole = Mathf.FloorToInt(pendingScore);
        if (whole <= 0) return;

        pendingScore -= whole;
        currentScore += whole;
        displayedScore = Mathf.FloorToInt(currentScore);
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
        PlayerPrefs.SetInt(coinsKey, PlayerPrefs.GetInt(coinsKey, 0) + amount);
    }
}
