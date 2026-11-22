using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject leaderboardPanel;
    
    [Header("Menu Options")]
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color unselectedColor = Color.white;

    [Header("Balloon Cursor")]
    [SerializeField] private RectTransform balloonCursor; // รูปลูกโป่งหน้าตัวเลือก
    [SerializeField] private float balloonXOffset = -45f; // ระยะห่างทางซ้ายของข้อความ

    private int selectedOption = 0; // 0 = 1 Player, 1 = 2 Players
    private bool isShowingLeaderboard = false;
    private bool hasCutsceneDeliveredBalloon = true;
    private float toggleTimer = 0f;
    private float toggleInterval = 8f; // สลับหน้าทุกๆ 8 วินาที (Attract Mode)

    private float inputCooldown = 0f;
    private Image balloonCursorImage;
    private Coroutine balloonPopRoutine;

    public bool IsTitleVisible => titlePanel != null && titlePanel.activeInHierarchy;
    public bool HasCutsceneDeliveredBalloon => hasCutsceneDeliveredBalloon;
    public RectTransform BalloonCursorRect => balloonCursor;

    public Sprite GetBalloonCursorSprite()
    {
        if (balloonCursor == null) return null;
        if (balloonCursorImage == null) balloonCursorImage = balloonCursor.GetComponent<Image>();
        return balloonCursorImage != null ? balloonCursorImage.sprite : null;
    }

    public void SetCutsceneBalloonWaiting(bool waiting)
    {
        hasCutsceneDeliveredBalloon = !waiting;
        if (balloonCursor == null) return;
        if (balloonCursorImage == null) balloonCursorImage = balloonCursor.GetComponent<Image>();
        if (balloonCursorImage != null)
        {
            balloonCursorImage.enabled = !waiting;
        }
    }

    public void CompleteCutsceneBalloonDelivery()
    {
        hasCutsceneDeliveredBalloon = true;
        if (balloonCursor == null) return;
        if (balloonCursorImage == null) balloonCursorImage = balloonCursor.GetComponent<Image>();
        balloonCursor.gameObject.SetActive(true);
        if (balloonCursorImage != null)
        {
            balloonCursorImage.enabled = true;
        }
        UpdateMenuSelection();
        if (gameObject.activeInHierarchy)
        {
            if (balloonPopRoutine != null) StopCoroutine(balloonPopRoutine);
            balloonPopRoutine = StartCoroutine(PopBalloonCursorRoutine());
        }
    }

    private IEnumerator PopBalloonCursorRoutine()
    {
        if (balloonCursor == null) yield break;
        float t = 0f;
        const float duration = 0.16f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.28f, 1.0f, Mathf.SmoothStep(0f, 1f, t / duration));
            balloonCursor.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        balloonCursor.localScale = Vector3.one;
        balloonPopRoutine = null;
    }

    private void Awake()
    {
        var cutscene = FindFirstObjectByType<StageCutsceneController>(FindObjectsInactive.Include);
        bool waitCutsceneBalloon = cutscene != null && cutscene.enableCutscenes && cutscene.playMainMenuLoop
                                   && PlayerPrefs.GetInt("ShowLeaderboardFirst", 0) != 1;
        SetCutsceneBalloonWaiting(waitCutsceneBalloon);
    }

    private void Start()
    {
        // ถ้าระบบส่งสัญญาณมาว่าเพิ่ง Game Over หรือเพิ่งกรอกชื่อเสร็จ ให้โชว์ Leaderboard ก่อนเลย!
        if (PlayerPrefs.GetInt("ShowLeaderboardFirst", 0) == 1)
        {
            PlayerPrefs.SetInt("ShowLeaderboardFirst", 0); // รีเซ็ตค่าทิ้ง
            ShowLeaderboard();
        }
        else
        {
            // ถ้าเปิดเกมมาปกติ ให้โชว์หน้า Title
            ShowTitle();
        }
        
        UpdateMenuSelection();
    }

    private void Update()
    {
        inputCooldown -= Time.deltaTime;
        toggleTimer -= Time.deltaTime;

        // สลับหน้าระหว่าง Title กับ Leaderboard แบบตู้เกม
        if (toggleTimer <= 0f)
        {
            if (isShowingLeaderboard) ShowTitle();
            else ShowLeaderboard();
        }

        // ถ้ายุ่งอยู่กับหน้า Leaderboard กดปุ่มคีย์บอร์ดใดๆ เพื่อกลับมาหน้า Title (ไม่นับการคลิกเมาส์)
        if (isShowingLeaderboard)
        {
            bool mouseClick = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            if (Input.anyKeyDown && !mouseClick)
            {
                ShowTitle();
                inputCooldown = 0.2f;
            }
            return;
        }

        // ใช้ปุ่ม Shift ในการสลับตัวเลือก 1 Player / 2 Players (รองรับลูกศรขึ้น-ลงเสริมด้วย)
        bool shiftPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        bool arrowPressed = inputCooldown <= 0f && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S));
        if (shiftPressed || arrowPressed)
        {
            selectedOption = selectedOption == 0 ? 1 : 0;
            UpdateMenuSelection();
            inputCooldown = 0.2f;
            AudioManager.Instance?.PlayButtonClick();
            toggleTimer = toggleInterval; // รีเซ็ตเวลาสลับหน้า จะได้ไม่เปลี่ยนหน้าตอนเรากำลังเลือก
        }

        // กดปุ่ม Enter เพื่อยืนยันเริ่มเกม (ไม่มีคลิกซ้ายเพื่อไปต่อ)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartGame();
        }
    }

    private void UpdateMenuSelection()
    {
        if (player1Text != null) 
            player1Text.color = selectedOption == 0 ? selectedColor : unselectedColor;
        if (player2Text != null) 
            player2Text.color = selectedOption == 1 ? selectedColor : unselectedColor;

        if (balloonCursor != null)
        {
            if (balloonCursorImage == null) balloonCursorImage = balloonCursor.GetComponent<Image>();
            balloonCursor.gameObject.SetActive(true);
            if (balloonCursorImage != null) balloonCursorImage.enabled = hasCutsceneDeliveredBalloon;
            TextMeshProUGUI targetText = selectedOption == 0 ? player1Text : player2Text;
            if (targetText != null)
            {
                targetText.ForceMeshUpdate();
                float textHalfWidth = targetText.preferredWidth * 0.5f;
                balloonCursor.anchoredPosition = new Vector2(-textHalfWidth + balloonXOffset, targetText.rectTransform.anchoredPosition.y);
            }
        }
    }

    private void ShowTitle()
    {
        isShowingLeaderboard = false;
        titlePanel.SetActive(true);
        leaderboardPanel.SetActive(false);
        toggleTimer = toggleInterval;
        
        var lbUI = leaderboardPanel.GetComponent<LeaderboardUI>();
        if (lbUI != null) lbUI.RefreshLeaderboard();
    }

    private void ShowLeaderboard()
    {
        isShowingLeaderboard = true;
        if (!hasCutsceneDeliveredBalloon)
        {
            hasCutsceneDeliveredBalloon = true;
            if (balloonCursorImage != null) balloonCursorImage.enabled = true;
        }
        titlePanel.SetActive(false);
        leaderboardPanel.SetActive(true);
        toggleTimer = toggleInterval;
        
        var lbUI = leaderboardPanel.GetComponent<LeaderboardUI>();
        if (lbUI != null) lbUI.RefreshLeaderboard();
    }

    private void StartGame()
    {
        AudioManager.Instance?.PlayArrowFire();
        PlayerPrefs.SetInt("PlayerMode", selectedOption + 1);
        GameSession.ResetSession();
        SceneManager.LoadScene("Autumn"); 
    }
}

