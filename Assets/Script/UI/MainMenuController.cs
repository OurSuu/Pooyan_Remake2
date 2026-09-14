using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private int selectedOption = 0; // 0 = 1 Player, 1 = 2 Players
    private bool isShowingLeaderboard = false;
    private float toggleTimer = 0f;
    private float toggleInterval = 8f; // สลับหน้าทุกๆ 8 วินาที (Attract Mode)

    private float inputCooldown = 0f;

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

        // ถ้ายุ่งอยู่กับหน้า Leaderboard กดปุ่มอะไรก็จะกลับมาหน้า Title ทันที (หรือกดเริ่มเกมเลย)
        if (isShowingLeaderboard)
        {
            if (Input.anyKeyDown)
            {
                ShowTitle();
                    inputCooldown = 0.2f;
            }
            return;
        }

        // จัดการเลื่อนเมนู 1 Player / 2 Player
        float v = Input.GetAxisRaw("Vertical");
        if (inputCooldown <= 0f)
        {
            if (v > 0.5f || v < -0.5f)
            {
                selectedOption = selectedOption == 0 ? 1 : 0;
                UpdateMenuSelection();
                inputCooldown = 0.2f;
                AudioManager.Instance?.PlayBalloonPop();
                toggleTimer = toggleInterval; // รีเซ็ตเวลาสลับหน้า จะได้ไม่เปลี่ยนหน้าตอนเรากำลังเลือก
            }
        }

        // กดยืนยันเริ่มเกม!
        if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
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

