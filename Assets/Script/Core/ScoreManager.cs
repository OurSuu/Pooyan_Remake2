using System;
using UnityEngine;

/// <summary>
/// จัดการเรื่องคะแนน, ไฮสกอร์, และคะแนนจากการทำคอมโบด้วยเนื้อ
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Key สำหรับเซฟไฮสกอร์ลงเครื่อง
    private const string HighScoreKey = "Pooyan_HighScore";

    [SerializeField] private int score;
    [SerializeField] private int highScore;

    private int meatComboCounter; // ตัวนับคอมโบเวลาปาเนื้อ

    public int Score => score;
    public int HighScore => highScore;
    public int MeatComboCounter => meatComboCounter;

    // Events สำหรับอัปเดต UI 
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;
    public event Action<int, Vector3> OnScoreFloating; // ทำเลขคะแนนเด้งๆ ตรงที่เกิดอีเวนต์

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // โหลดไฮสกอร์ที่เคยทำไว้ขึ้นมา
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void ResetScore()
    {
        score = 0;
        meatComboCounter = 0;
        nextExtraLifeIndex = 0;
        OnScoreChanged?.Invoke(score);
    }

    private int nextExtraLifeIndex = 0; // เอาไว้เช็คว่าจะได้ 1-Up ครั้งต่อไปเมื่อไหร่

    // บวกคะแนนหลัก
    public void AddScore(int points)
    {
        if (points <= 0) return;
        int oldScore = score;
        score += points;
        OnScoreChanged?.Invoke(score);

        // เช็คว่าถึงเป้า Extra Life หรือยัง (อิงตามตู้ อาเขต: 30,000 / 70,000)
        while (nextExtraLifeIndex < GameConstants.ExtraLifeThresholds.Length
            && oldScore < GameConstants.ExtraLifeThresholds[nextExtraLifeIndex]
            && score >= GameConstants.ExtraLifeThresholds[nextExtraLifeIndex])
        {
            GameManager.Instance?.AddLife(); // แจกชีวิต!
            nextExtraLifeIndex++;
        }

        // ทุบสถิติใหม่!
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(highScore);
        }
    }

    // พวกฟังก์ชันเพิ่มคะแนนยิบย่อย พร้อมลอยเลขกลางจอ
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

    // คิดคะแนนคอมโบเนื้อ ยิ่งโดนหลายตัวยิ่งคูณเยอะ
    public void AddMeatComboScore(Vector3 worldPosition)
    {
        meatComboCounter++;
        // กฎตู้เกม: 400 * 2^(n-1) -> 400, 800, 1600, 3200, 6400... ทวีคูณไปเรื่อยๆ!
        int points = GameConstants.ScoreMeatComboBase * (1 << (meatComboCounter - 1));
        AddScore(points);
        OnScoreFloating?.Invoke(points, worldPosition);
    }

    // พลาดแล้วก็รีเซ็ตคอมโบเริ่มใหม่นะ
    public void ResetMeatCombo() => meatComboCounter = 0;

    // ได้ผลไม้โบนัส
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

// ชนิดผลไม้จ้า
public enum BonusFruitType
{
    Strawberry,
    Cherry,
    Peach
}
