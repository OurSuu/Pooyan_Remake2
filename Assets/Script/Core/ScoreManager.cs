using System;
using UnityEngine;

/// <summary>
/// Manages score, high score, and meat combo scoring.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private const string HighScoreKey = "Pooyan_HighScore";

    [SerializeField] private int score;
    [SerializeField] private int highScore;

    private int meatComboCounter;

    public int Score => score;
    public int HighScore => highScore;
    public int MeatComboCounter => meatComboCounter;

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
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void ResetScore()
    {
        score = 0;
        meatComboCounter = 0;
        nextExtraLifeIndex = 0;
        OnScoreChanged?.Invoke(score);
    }

    private int nextExtraLifeIndex = 0;

    public void AddScore(int points)
    {
        if (points <= 0) return;
        int oldScore = score;
        score += points;
        OnScoreChanged?.Invoke(score);

        // Extra Life check (arcade: 30,000 / 70,000)
        while (nextExtraLifeIndex < GameConstants.ExtraLifeThresholds.Length
            && oldScore < GameConstants.ExtraLifeThresholds[nextExtraLifeIndex]
            && score >= GameConstants.ExtraLifeThresholds[nextExtraLifeIndex])
        {
            GameManager.Instance?.AddLife();
            nextExtraLifeIndex++;
        }

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(highScore);
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
        // Arcade rule: 400 * 2^(n-1) → 400, 800, 1600, 3200, 6400...
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
