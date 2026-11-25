using UnityEngine;
using UnityEngine.UI;

public class DebugModeController : MonoBehaviour
{
    [Header("References")]
    public PlayerTrailDebug playerTrailDebug;
    public ColliderVisualizer colliderVisualizer;
    public RunProgressionManager progressionManager;
    public DifficultyStatsTracker statsTracker;
    public StatsManager statsManager;

    [Header("UI Elements")]
    public GameObject fpsText;
    public GameObject speedIndexText;
    public GameObject coinsText;
    public GameObject phaseText;
    public GameObject scoreText;

    [Header("Difficulty Stats Panel")]
    public GameObject difficultyStatsPanel;
    public Text difficultyStatsText;
    public Button resetStatsButton;

    [Header("UI Button")]
    public Button debugToggleButton;

    [Header("State")]
    public bool debugModeEnabled = false;

    private Text speedTextComponent;
    private Text phaseTextComponent;
    private Text coinsTextComponent;
    private Text scoreTextComponent;
    private bool gameHasStartedOnce = false;

    void Start()
    {
        if (debugToggleButton != null) debugToggleButton.onClick.AddListener(ToggleDebugMode);
        if (resetStatsButton != null && statsTracker != null) resetStatsButton.onClick.AddListener(OnResetStatsClicked);

        if (speedIndexText != null) speedTextComponent = speedIndexText.GetComponent<Text>();
        if (phaseText != null) phaseTextComponent = phaseText.GetComponent<Text>();
        if (coinsText != null) coinsTextComponent = coinsText.GetComponent<Text>();
        if (scoreText != null) scoreTextComponent = scoreText.GetComponent<Text>();

        ApplyState();
        UpdateDifficultyStatsText();
    }

    void Update()
    {
        if (!debugModeEnabled) return;

        if (progressionManager != null && progressionManager.IsRunning) gameHasStartedOnce = true;

        if (progressionManager != null)
        {
            if (speedTextComponent != null) speedTextComponent.text = "Speed: " + progressionManager.CurrentSpeedIndex.ToString("0.000");
            if (phaseTextComponent != null) phaseTextComponent.text = "Phase: " + (progressionManager.CurrentPhaseIndex + 1);
        }

        if (statsManager != null)
        {
            if (scoreTextComponent != null) scoreTextComponent.text = "Score: " + statsManager.GetCurrentScore().ToString();
            if (coinsTextComponent != null) coinsTextComponent.text = "Coins: " + PlayerPrefs.GetInt("Coins", 0).ToString();
        }

        if (difficultyStatsPanel != null)
        {
            if (!gameHasStartedOnce && PlayerPrefs.GetInt("isGameStarted") == 0)
            {
                if (!difficultyStatsPanel.activeSelf) { difficultyStatsPanel.SetActive(true); UpdateDifficultyStatsText(); }
            }
            else
            {
                if (difficultyStatsPanel.activeSelf) difficultyStatsPanel.SetActive(false);
            }
        }
    }

    void UpdateDifficultyStatsText()
    {
        if (difficultyStatsText != null && statsTracker != null)
        {
            string text = "";
            for (int i = 0; i < 5; i++)
            {
                float avgTime = statsTracker.GetAverageTime(i);
                float avgObjects = statsTracker.GetAverageObjectsPerRun(i);
                text += $"Phase {i + 1}:\nAvg Time: {avgTime:F2}s | Avg Obj: {avgObjects:F1}\n\n";
            }
            difficultyStatsText.text = text;
        }
    }

    void OnResetStatsClicked()
    {
        if (statsTracker != null)
        {
            statsTracker.ResetStats();
            UpdateDifficultyStatsText();
        }
    }

    public void ToggleDebugMode()
    {
        debugModeEnabled = !debugModeEnabled;
        ApplyState();
    }

    void ApplyState()
    {
        if (playerTrailDebug != null) playerTrailDebug.enabled = debugModeEnabled;
        if (colliderVisualizer != null) colliderVisualizer.showColliders = debugModeEnabled;

        if (fpsText != null) fpsText.SetActive(debugModeEnabled);
        if (speedIndexText != null) speedIndexText.SetActive(debugModeEnabled);
        if (coinsText != null) coinsText.SetActive(debugModeEnabled);
        if (phaseText != null) phaseText.SetActive(debugModeEnabled);
        if (scoreText != null) scoreText.SetActive(debugModeEnabled);

        if (difficultyStatsPanel != null && !debugModeEnabled) difficultyStatsPanel.SetActive(false);
    }
}