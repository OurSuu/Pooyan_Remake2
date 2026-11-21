using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// หน้าจอแสดงตารางคะแนน 5 อันดับแรก
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] rankTexts; // 1TH, 2ND, 3RD, 4TH, 5TH
    [SerializeField] private TextMeshProUGUI[] scoreTexts; // คะแนน
    [SerializeField] private TextMeshProUGUI[] nameTexts; // ชื่อคนเล่น
    [SerializeField] private TextMeshProUGUI[] stageTexts; // ด่านที่ไปถึง

    private void OnEnable()
    {
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard()
    {
        if (ScoreManager.Instance == null) return;

        List<HighScoreEntry> entries = ScoreManager.Instance.GetLeaderboard();

        for (int i = 0; i < 5; i++)
        {
            // ล้างข้อมูลเก่า
            if (scoreTexts.Length > i && scoreTexts[i] != null) scoreTexts[i].text = "";
            if (nameTexts.Length > i && nameTexts[i] != null) nameTexts[i].text = "";
            if (stageTexts.Length > i && stageTexts[i] != null) stageTexts[i].text = "";
            if (rankTexts.Length > i && rankTexts[i] != null)
            {
                // ตู้เกมมักจะโชว์ 1ST 2ND 3RD 4TH 5TH
                string rankSuffix = i == 0 ? "ST" : i == 1 ? "ND" : i == 2 ? "RD" : "TH";
                rankTexts[i].text = (i + 1) + rankSuffix;
            }

            // ถ้ามีข้อมูลคะแนน ก็เอามาใส่
            if (entries != null && i < entries.Count)
            {
                if (scoreTexts.Length > i && scoreTexts[i] != null) 
                    scoreTexts[i].text = entries[i].score.ToString("D0");
                
                if (nameTexts.Length > i && nameTexts[i] != null) 
                    nameTexts[i].text = entries[i].playerName;

                if (stageTexts.Length > i && stageTexts[i] != null) 
                    stageTexts[i].text = "STAGE " + entries[i].stage.ToString();
            }
        }
    }
}
