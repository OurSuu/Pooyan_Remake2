using TMPro;
using UnityEngine;

/// <summary>
/// HUD â€” score, lives, stage, cliff counter, game over / stage clear screens.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI remainingWolvesText; // Add this!
    [SerializeField] private GameObject cliffCounterPanel;
    [SerializeField] private TextMeshProUGUI cliffCounterText;

    [Header("Screens")]
    [SerializeField] private GameObject stageClearScreen;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject bonusResultPanel;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            ScoreManager.Instance.OnHighScoreChanged += UpdateHighScore;
            UpdateScore(ScoreManager.Instance.Score);
            UpdateHighScore(ScoreManager.Instance.HighScore);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged += UpdateLives;
            GameManager.Instance.OnStageChanged += UpdateStage;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameOver += ShowGameOver;
            GameManager.Instance.OnStageClear += ShowStageClear;

            UpdateLives(GameManager.Instance.Lives);
            UpdateStage(GameManager.Instance.CurrentStage);
        }

        if (BoulderSystem.Instance != null)
            BoulderSystem.Instance.OnCliffCountChanged += UpdateCliffCounter;

        if (WolfTracker.Instance != null)
        {
            WolfTracker.Instance.OnKillCountChanged += UpdateRemainingWolves;
            UpdateRemainingWolves(WolfTracker.Instance.RemainingKills);
        }

        HideAllScreens();
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
            ScoreManager.Instance.OnHighScoreChanged -= UpdateHighScore;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged -= UpdateLives;
            GameManager.Instance.OnStageChanged -= UpdateStage;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameOver -= ShowGameOver;
            GameManager.Instance.OnStageClear -= ShowStageClear;
        }

        if (BoulderSystem.Instance != null)
            BoulderSystem.Instance.OnCliffCountChanged -= UpdateCliffCounter;

        if (WolfTracker.Instance != null)
        {
            WolfTracker.Instance.OnKillCountChanged -= UpdateRemainingWolves;
        }
    }

    private void UpdateRemainingWolves(int remaining)
    {
        if (remainingWolvesText != null)
        {
            remainingWolvesText.text = $"WOLVES: {remaining}";
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
    }

    private void UpdateHighScore(int highScore)
    {
        if (highScoreText != null) highScoreText.text = $"HIGH: {highScore}";
    }

    private void UpdateLives(int lives)
    {
        if (livesText != null) livesText.text = $"LIVES: {lives}";
    }

    private void UpdateStage(int stage)
    {
        if (stageText != null) stageText.text = $"STAGE: {stage}";
    }

    private void UpdateCliffCounter(int count, int threshold)
    {
        if (cliffCounterPanel != null)
            cliffCounterPanel.SetActive(GameManager.Instance != null && !GameManager.Instance.IsOddStage);

        if (cliffCounterText != null)
            cliffCounterText.text = $"{count}/{threshold}";
    }

    private void HandleStateChanged(GameState state)
    {
        HideAllScreens();

        switch (state)
        {
            case GameState.Playing:
                if (cliffCounterPanel != null)
                    cliffCounterPanel.SetActive(GameManager.Instance != null && !GameManager.Instance.IsOddStage);
                break;
            case GameState.BonusStage:
                if (bonusResultPanel != null) bonusResultPanel.SetActive(true);
                break;
        }
    }

    private void ShowStageClear()
    {
        if (stageClearScreen != null) stageClearScreen.SetActive(true);
        Invoke(nameof(AdvanceAfterClear), 2f);
    }

    private void AdvanceAfterClear()
    {
        HideAllScreens();

        if (GameManager.Instance == null) return;

        
        GameManager.Instance.AdvanceStage();
    }

    private void ShowGameOver()
    {
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
    }

    private void HideAllScreens()
    {
        if (stageClearScreen != null) stageClearScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (bonusResultPanel != null) bonusResultPanel.SetActive(false);
    }
}

