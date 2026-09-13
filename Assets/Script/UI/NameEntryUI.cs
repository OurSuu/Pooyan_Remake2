using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ตัวจัดการหน้าจอเข้าชื่อ 3 ตัวอักษร สไตล์ตู้เกม
/// โยกขึ้นลงเพื่อเปลี่ยนตัวอักษร, กดยิงเพื่อตกลง
/// </summary>
public class NameEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] charTexts; // ช่องตัวอักษร 3 ช่อง
    [SerializeField] private TextMeshProUGUI promptText; // ข้อความบอกให้กรอกชื่อ

    private char[] availableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789. ".ToCharArray();
    
    private int currentLetterIndex = 0; // กำลังกรอกตัวอักษรตำแหน่งที่เท่าไหร่ (0-2)
    private int currentSelectionIndex = 0; // ตัวอักษรที่กำลังเลือกอยู่ (Index ใน availableChars)

    private float inputCooldown = 0f;
    private bool isFinished = false;

    private void OnEnable()
    {
        // รีเซ็ตค่าตอนเปิดหน้าจอนี้ขึ้นมา
        currentLetterIndex = 0;
        currentSelectionIndex = 0;
        isFinished = false;
        inputCooldown = 0.5f; // กันไม่ให้กดลั่นทันทีที่หน้าจอเด้งขึ้นมา

        // ล้างช่องตัวอักษร
        for (int i = 0; i < charTexts.Length; i++)
        {
            if (charTexts[i] != null)
                charTexts[i].text = "_";
        }
        
        UpdateDisplay();
    }

    private void Update()
    {
        if (isFinished || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.NameEntry)
            return;

        if (inputCooldown > 0)
        {
            inputCooldown -= Time.deltaTime;
            return;
        }

        // จัดการเลื่อนขึ้นลง (โยกจอย)
        float v = Input.GetAxisRaw("Vertical");
        if (v > 0.5f)
        {
            currentSelectionIndex--;
            if (currentSelectionIndex < 0) currentSelectionIndex = availableChars.Length - 1;
            inputCooldown = 0.15f; // หน่วงนิดนึงไม่ให้ตัวอักษรวิ่งรัวเกิน
            UpdateDisplay();
            AudioManager.Instance?.PlayBalloonPop(); // ใช้เสียงลูกโป่งป๊อปแทนเสียงจิ้มเมนูไปก่อน
        }
        else if (v < -0.5f)
        {
            currentSelectionIndex++;
            if (currentSelectionIndex >= availableChars.Length) currentSelectionIndex = 0;
            inputCooldown = 0.15f;
            UpdateDisplay();
            AudioManager.Instance?.PlayBalloonPop();
        }

        // จัดการกดยืนยัน (กดยิง)
        if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmLetter();
        }
    }

    private void UpdateDisplay()
    {
        if (currentLetterIndex >= charTexts.Length) return;

        // อัปเดตตัวอักษรช่องที่กำลังเลือกอยู่
        charTexts[currentLetterIndex].text = availableChars[currentSelectionIndex].ToString();

        // ทำให้ช่องที่เลือกกระพริบได้นะ (เดี๋ยวไปทำใน Animator หรือโค้ดกระพริบง่ายๆ ก็ได้)
    }

    private void ConfirmLetter()
    {
        AudioManager.Instance?.PlayArrowFire(); // เสียงฟิ้ว! ตอนตกลง
        
        currentLetterIndex++;
        
        if (currentLetterIndex >= charTexts.Length)
        {
            // กรอกครบ 3 ตัวแล้ว จบปิ้ง
            isFinished = true;
            SubmitName();
        }
        else
        {
            // ไปตัวถัดไป เริ่มที่ตัว A (index 0) ใหม่
            currentSelectionIndex = 0;
            UpdateDisplay();
        }
    }

    private void SubmitName()
    {
        string finalName = "";
        for (int i = 0; i < charTexts.Length; i++)
        {
            finalName += charTexts[i].text;
        }

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        int finalStage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : 1;

        // บันทึกลงระบบ
        ScoreManager.Instance?.AddNewHighScore(finalName, finalScore, finalStage);

        // บอกผู้จัดการว่าเสร็จแล้วเว้ย
        GameManager.Instance?.FinishNameEntry();
    }
}
