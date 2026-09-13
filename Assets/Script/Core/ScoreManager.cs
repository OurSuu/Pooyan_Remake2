using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HighScoreEntry
{
    public string playerName;
    public int score;
    public int stage;
}

[System.Serializable]
public class LeaderboardData
{
    public List<HighScoreEntry> entries = new List<HighScoreEntry>();
}

/// <summary>
/// จัดการเรื่องคะแนน, ไฮสกอร์, และคะแนนจากการทำคอมโบด้วยเนื้อ (อัปเกรดระบบ Top 5 Leaderboard)
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private const string LeaderboardKey = "Pooyan_Leaderboard";
    private const int MaxLeaderboardEntries = 5;

    [SerializeField] private int score;
    private int meatComboCounter;
    private int nextExtraLifeIndex = 0;

    public int Score => score;
    public int MeatComboCounter => meatComboCounter;
    
    // ดึงคะแนนสูงสุดอันดับ 1 มาโชว์
    public int HighScore => (leaderboard.entries.Count > 0) ? leaderboard.entries[0].score : 0;

    private LeaderboardData leaderboard = new LeaderboardData();

    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;
    public event Action<int, Vector3> OnScoreFloating;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadLeaderboard();
    }

    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LeaderboardKey))
        {
            string json = PlayerPrefs.GetString(LeaderboardKey);
            leaderboard = JsonUtility.FromJson<LeaderboardData>(json);
        }
        
        // ถ้ายังไม่มีข้อมูลเลย ให้สร้างข้อมูลสมมติแบบตู้เกม
        if (leaderboard == null || leaderboard.entries == null || leaderboard.entries.Count == 0)
        {
            leaderboard = new LeaderboardData();
            leaderboard.entries.Add(new HighScoreEntry { playerName = "KON", score = 20000, stage = 5 });
            leaderboard.entries.Add(new HighScoreEntry { playerName = "AMI", score = 15000, stage = 4 });
            leaderboard.entries.Add(new HighScoreEntry { playerName = "POO", score = 10000, stage = 3 });
            leaderboard.entries.Add(new HighScoreEntry { playerName = "YAN", score = 5000, stage = 2 });
            leaderboard.entries.Add(new HighScoreEntry { playerName = "PIG", score = 2000, stage = 1 });
            SaveLeaderboard();
        }
    }

    public void SaveLeaderboard()
    {
        // จัดเรียงคะแนนจากมากไปน้อย
        leaderboard.entries.Sort((a, b) => b.score.CompareTo(a.score));
        
        // เก็บแค่ 5 อันดับแรก
        if (leaderboard.entries.Count > MaxLeaderboardEntries)
        {
            leaderboard.entries.RemoveRange(MaxLeaderboardEntries, leaderboard.entries.Count - MaxLeaderboardEntries);
        }

        string json = JsonUtility.ToJson(leaderboard);
        PlayerPrefs.SetString(LeaderboardKey, json);
        PlayerPrefs.Save();
    }

    public List<HighScoreEntry> GetLeaderboard()
    {
        return leaderboard.entries;
    }

    public bool IsTopScore(int currentScore)
    {
        if (currentScore == 0) return false;
        if (leaderboard.entries.Count < MaxLeaderboardEntries) return true;
        
        // เช็คว่าชนะอันดับที่ 5 หรือไม่
        return currentScore > leaderboard.entries[leaderboard.entries.Count - 1].score;
    }

    public void AddNewHighScore(string playerName, int finalScore, int stage)
    {
        leaderboard.entries.Add(new HighScoreEntry { playerName = playerName, score = finalScore, stage = stage });
        SaveLeaderboard();
        OnHighScoreChanged?.Invoke(HighScore);
    }

    public void ResetScore()
    {
        score = 0;
        meatComboCounter = 0;
        nextExtraLifeIndex = 0;
        OnScoreChanged?.Invoke(score);
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;
        int oldScore = score;
        score += points;
        OnScoreChanged?.Invoke(score);

        while (nextExtraLifeIndex < GameConstants.ExtraLifeThresholds.Length
            && oldScore < GameConstants.ExtraLifeThresholds[nextExtraLifeIndex]
            && score >= GameConstants.ExtraLifeThresholds[nextExtraLifeIndex])
        {
            GameManager.Instance?.AddLife();
            nextExtraLifeIndex++;
        }

        // ดักอัปเดต HighScore บน UI แบบ Real-time ถ้าเล่นอยู่แล้วแซงที่ 1 ได้
        if (score > HighScore)
        {
            OnHighScoreChanged?.Invoke(score);
        }
    }

    public void AddMeatPickupScore(Vector3 pos)
    {
        AddScore(GameConstants.ScoreMeatPickup);
        OnScoreFloating?.Invoke(GameConstants.ScoreMeatPickup, pos);
    }
    
    public void AddBalloonPopScore(Vector3 pos) 
    {
        AddScore(GameConstants.ScoreBalloonPop);
        OnScoreFloating?.Invoke(GameConstants.ScoreBalloonPop, pos);
    }
    
    public void AddRockDestroyScore(Vector3 pos)
    {
        AddScore(GameConstants.ScoreRockDestroy);
        OnScoreFloating?.Invoke(GameConstants.ScoreRockDestroy, pos);
    }
    
    public void AddFruitDestroyScore(Vector3 pos)
    {
        AddScore(GameConstants.ScoreFruitDestroy);
        OnScoreFloating?.Invoke(GameConstants.ScoreFruitDestroy, pos);
    }

    public void AddMeatComboScore(Vector3 worldPosition)
    {
        meatComboCounter++;
        int points = GameConstants.ScoreMeatComboBase * (1 << (meatComboCounter - 1));
        AddScore(points);
        OnScoreFloating?.Invoke(points, worldPosition);
    }

    public void ResetMeatCombo() => meatComboCounter = 0;

    public void AddBonusFruitScore(BonusFruitType fruitType, Vector3 pos)
    {
        int points = fruitType switch
        {
            BonusFruitType.Strawberry => GameConstants.ScoreBonusStrawberry,
            BonusFruitType.Cherry => GameConstants.ScoreBonusCherry,
            BonusFruitType.Peach => GameConstants.ScoreBonusPeach,
            _ => 0
        };
        AddScore(points);
        OnScoreFloating?.Invoke(points, pos);
    }
}

public enum BonusFruitType
{
    Strawberry,
    Cherry,
    Peach
}
