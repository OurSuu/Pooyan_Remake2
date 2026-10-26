using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ไฟล์นี้โคตรพ่อโคตรแม่จัดการ UI บนหน้าจอเลยเพื่อน ทั้งคะแนน เลือด ฉากจบ หน้าเกมโอเวอร์ อยู่ในนี้หมด
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText; // ช่องแสดงแต้ม
    [SerializeField] private TextMeshProUGUI highScoreText; // สถิติแต้มสูงสุด
    [SerializeField] private TextMeshProUGUI livesText; // จำนวนชีวิตหมู (แบบตัวเลขเผื่อใช้งาน)
    [SerializeField] private Transform livesContainer; // กล่องใส่ไอคอนชีวิต
    [SerializeField] private GameObject lifeIconPrefab; // Prefab หรือไอคอนต้นแบบ (เผื่อได้ชีวิตเพิ่มเกิน 3 รูป)
    [SerializeField] private List<GameObject> lifeIcons = new List<GameObject>(); // รายการไอคอนชีวิต (3 รูป)
    [SerializeField] private TextMeshProUGUI stageText; // ตอนนี้เล่นอยู่ฉากไหน
    [SerializeField] private GameObject wolfIcon; // รูปไอคอนหมาป่า (แสดงคู่กับจำนวนหมาป่าที่เหลือ)
    [SerializeField] private TextMeshProUGUI remainingWolvesText; // ตัวเลขหมาป่าที่ต้องตบให้หมด
    [SerializeField] private GameObject cliffCounterPanel; // แผงนับจำนวนหมาป่าที่ขึ้นหน้าผาไปได้
    [SerializeField] private TextMeshProUGUI cliffCounterText; // Text ข้างในแผงบนอีกที

    [Header("Screens")]
    [SerializeField] private GameObject titleScreen; // หน้าจอเริ่มเกม
    [SerializeField] private GameObject stageClearScreen; // หน้าจอแสดงผลตอนผ่านด่าน
    [SerializeField] private GameObject gameOverScreen; // เกมโอเวอร์โว้ย
    [SerializeField] private GameObject bonusResultPanel; // สรุปผลคะแนนด่านโบนัส
    [SerializeField] private GameObject nameEntryScreen; // หน้าจอกรอกชื่อคนเก่ง
    [SerializeField] private GameObject leaderboardScreen; // หน้าจออวดคะแนน Top 5

    private void Start()
    {
        // หากมี livesContainer และยังไม่ได้ใส่ไอคอนใน list ให้ดึง child มาใส่เป็นค่าเริ่มต้น
        if (livesContainer != null && (lifeIcons == null || lifeIcons.Count == 0))
        {
            if (lifeIcons == null) lifeIcons = new List<GameObject>();
            for (int i = 0; i < livesContainer.childCount; i++)
            {
                lifeIcons.Add(livesContainer.GetChild(i).gameObject);
            }
        }

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
            HandleStateChanged(GameManager.Instance.CurrentState); // สั่งอัปเดตหน้าจอตั้งแต่เริ่ม
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

    public void UpdateRemainingWolves(int remaining)
    {
        if (wolfIcon != null) wolfIcon.SetActive(true);
        if (remainingWolvesText != null)
        {
            if (wolfIcon != null)
            {
                remainingWolvesText.text = $": {remaining}";
            }
            else
            {
                remainingWolvesText.text = $"WOLVES: {remaining}";
            }
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

    public void UpdateLives(int lives)
    {
        bool hasIcons = (lifeIcons != null && lifeIcons.Count > 0) || livesContainer != null;
        if (livesText != null)
        {
            if (hasIcons)
            {
                livesText.gameObject.SetActive(false); // ซ่อน text เพราะแสดงเป็นรูปไอคอนแทนแล้ว
            }
            else
            {
                livesText.text = $"LIVES: {lives}";
            }
        }

        if (hasIcons)
        {
            // ถ้าชีวิตเพิ่มขึ้นเกินจำนวนไอคอนที่มี (เช่น ได้ 1-UP) ให้ก็อปปี้ไอคอนเพิ่มอัตโนมัติ
            while (lifeIcons.Count < lives && (lifeIconPrefab != null || (lifeIcons.Count > 0 && lifeIcons[0] != null)))
            {
                GameObject template = lifeIconPrefab != null ? lifeIconPrefab : lifeIcons[0];
                Transform parent = livesContainer != null ? livesContainer : template.transform.parent;
                GameObject newIcon = Instantiate(template, parent);
                newIcon.name = $"LifeIcon_{lifeIcons.Count + 1}";
                lifeIcons.Add(newIcon);
            }

            for (int i = 0; i < lifeIcons.Count; i++)
            {
                if (lifeIcons[i] != null)
                {
                    lifeIcons[i].SetActive(i < lives);
                }
            }
        }
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
            case GameState.MainMenu:
                if (titleScreen != null) titleScreen.SetActive(true);
                break;
            case GameState.Cutscene:
            case GameState.Playing:
                if (cliffCounterPanel != null)
                    cliffCounterPanel.SetActive(GameManager.Instance != null && !GameManager.Instance.IsOddStage && !GameManager.Instance.IsBonusSceneActive);
                if (stageText != null && GameManager.Instance != null)
                    stageText.text = GameManager.Instance.IsBonusSceneActive ? "BONUS STAGE" : $"STAGE: {GameManager.Instance.CurrentStage}";
                break;
            case GameState.BonusStage:
                if (stageText != null) stageText.text = "BONUS STAGE";
                if (cliffCounterPanel != null) cliffCounterPanel.SetActive(false);
                if (wolfIcon != null) wolfIcon.SetActive(false);
                if (remainingWolvesText != null) remainingWolvesText.text = "BONUS TIME!";
                break;
            case GameState.NameEntry:
                if (nameEntryScreen != null) nameEntryScreen.SetActive(true);
                break;
            case GameState.Leaderboard:
                if (leaderboardScreen != null) leaderboardScreen.SetActive(true);
                break;
        }
    }

    private void ShowStageClear()
    {
        int stage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : 1;
        // ถ้าเป็นด่าน 2 และมีคัทซีนก่อนเข้าโบนัส (กระเช้าเลื่อนลงมาถึงพื้น แม่หมูเดินไปหาลูกหมูกระโดดดีใจ) ให้เล่นคัทซีนก่อน!
        if (StageCutsceneController.Instance != null && StageCutsceneController.Instance.HasClearCutsceneForStage(stage))
        {
            StageCutsceneController.Instance.PlayStageClearCutscene(stage, AdvanceAfterClear);
            return;
        }

        // กรณีด่านอื่นๆ ให้ใช้หน้าจอ Stage Clear ปกติ
        if (stageClearScreen != null) stageClearScreen.SetActive(true);
        Invoke(nameof(AdvanceAfterClear), 2f); // หน่วงไว้แป๊บนึงค่อยไปด่านถัดไป
    }

    private void AdvanceAfterClear()
    {
        CancelInvoke(nameof(AdvanceAfterClear));
        HideAllScreens();
        if (GameManager.Instance == null) return;
        
        GameManager.Instance.AdvanceStage(); // สั่งย้ายด่าน
    }

    private void ShowGameOver()
    {
        // จบเห่ ขึ้นหน้าจอเลยเพื่อน
        HideAllScreens();
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
    }

    private void HideAllScreens()
    {
        // สับสวิตช์ปิดหน้าจอใหญ่ๆ ทั้งหมด
        if (titleScreen != null) titleScreen.SetActive(false);
        if (stageClearScreen != null) stageClearScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (bonusResultPanel != null) bonusResultPanel.SetActive(false);
        if (nameEntryScreen != null) nameEntryScreen.SetActive(false);
        if (leaderboardScreen != null) leaderboardScreen.SetActive(false);
    }
}
