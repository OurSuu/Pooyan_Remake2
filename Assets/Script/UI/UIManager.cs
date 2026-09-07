using TMPro;
using UnityEngine;

/// <summary>
/// ไฟล์นี้โคตรพ่อโคตรแม่จัดการ UI บนหน้าจอเลยเพื่อน ทั้งคะแนน เลือด ฉากจบ หน้าเกมโอเวอร์ อยู่ในนี้หมด
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText; // ช่องแสดงแต้ม
    [SerializeField] private TextMeshProUGUI highScoreText; // สถิติแต้มสูงสุด
    [SerializeField] private TextMeshProUGUI livesText; // จำนวนชีวิตหมู
    [SerializeField] private TextMeshProUGUI stageText; // ตอนนี้เล่นอยู่ฉากไหน
    [SerializeField] private TextMeshProUGUI remainingWolvesText; // ตัวเลขหมาป่าที่ต้องตบให้หมด
    [SerializeField] private GameObject cliffCounterPanel; // แผงนับจำนวนหมาป่าที่ขึ้นหน้าผาไปได้
    [SerializeField] private TextMeshProUGUI cliffCounterText; // Text ข้างในแผงบนอีกที

    [Header("Screens")]
    [SerializeField] private GameObject stageClearScreen; // หน้าจอแสดงผลตอนผ่านด่าน
    [SerializeField] private GameObject gameOverScreen; // เกมโอเวอร์โว้ย
    [SerializeField] private GameObject bonusResultPanel; // สรุปผลคะแนนด่านโบนัส

    private void Start()
    {
        // สมัครรับข่าวสารตอนคะแนนเปลี่ยน
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            ScoreManager.Instance.OnHighScoreChanged += UpdateHighScore;
            UpdateScore(ScoreManager.Instance.Score);
            UpdateHighScore(ScoreManager.Instance.HighScore);
        }

        // สมัครรับข่าวสารตอนเลือดลดหรือเปลี่ยนฉาก
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

        // นับจำนวนหมาป่าบนหน้าผาด้วย
        if (BoulderSystem.Instance != null)
            BoulderSystem.Instance.OnCliffCountChanged += UpdateCliffCounter;

        // อัปเดตตัวเลขหมาป่าที่ต้องล่าให้หมด
        if (WolfTracker.Instance != null)
        {
            WolfTracker.Instance.OnKillCountChanged += UpdateRemainingWolves;
            UpdateRemainingWolves(WolfTracker.Instance.RemainingKills);
        }

        // เริ่มเกมมาต้องปิดพวกหน้าจอบังๆ ให้หมดก่อน
        HideAllScreens();
    }

    private void OnDestroy()
    {
        // ย้ายซีนแล้วก็ลืมๆ ข่าวสารไปซะ ไม่งั้นเกมแครช
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
        // ด่านเลขคู่ถึงจะโชว์ตัวเลขหน้าผานะเฟ้ย
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
        // ด่านเคลียร์!! เปิด UI โชว์รัวๆ
        if (stageClearScreen != null) stageClearScreen.SetActive(true);
        Invoke(nameof(AdvanceAfterClear), 2f); // หน่วงไว้แป๊บนึงค่อยไปด่านถัดไป
    }

    private void AdvanceAfterClear()
    {
        HideAllScreens();
        if (GameManager.Instance == null) return;
        
        GameManager.Instance.AdvanceStage(); // สั่งย้ายด่าน
    }

    private void ShowGameOver()
    {
        // จบเห่ ขึ้นหน้าจอเลยเพื่อน
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
    }

    private void HideAllScreens()
    {
        // สับสวิตช์ปิดหน้าจอใหญ่ๆ ทั้งหมด
        if (stageClearScreen != null) stageClearScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (bonusResultPanel != null) bonusResultPanel.SetActive(false);
    }
}
