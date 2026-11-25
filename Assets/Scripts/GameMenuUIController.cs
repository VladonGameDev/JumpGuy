using UnityEngine;
using UnityEngine.UI;

public class GameMenuUIController : MonoBehaviour
{
    [Header("References")]
    public StatsManager statsManager;

    [Header("HUD UI")]
    public GameObject hudPanel;
    public Text hudScoreText;
    public Text hudCoinsText;
    public Text hudRareCoinsText;

    private RunProgressionManager progressionManager;

    void Awake()
    {
        progressionManager = FindObjectOfType<RunProgressionManager>();
    }

    void Start()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    void Update()
    {
        if (progressionManager != null && progressionManager.IsRunning)
        {
            if (hudPanel != null && !hudPanel.activeSelf) hudPanel.SetActive(true);
            UpdateHUD();
        }
        else
        {
            if (hudPanel != null && hudPanel.activeSelf) hudPanel.SetActive(false);
        }
    }

    void UpdateHUD()
    {
        if (hudScoreText != null && statsManager != null)
            hudScoreText.text = $"Score: {statsManager.GetCurrentScore()}";

        if (hudCoinsText != null)
            hudCoinsText.text = $"Coins: {PlayerPrefs.GetInt("Coins", 0)}";

        if (hudRareCoinsText != null)
            hudRareCoinsText.text = $"Rare Coins: {PlayerPrefs.GetInt("RareCoins", 0)}";
    }

    public void OnPlayerDeath()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
    }
}