using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton จ้า — ตัวคุมสถานะเกม (Game State), จำนวนชีวิต, แล้วก็การเปลี่ยนด่าน
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Stage")]
    [SerializeField] private int currentStage = 1;
    [SerializeField] private int lives = GameConstants.StartingLives;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int CurrentStage => currentStage;
    public int Lives => lives;
    
    public bool DeathFreeze { get; private set; }
    
    public bool IsOddStage => currentStage % 2 == 1;
    
    public StageDirection CurrentDirection =>
        IsOddStage ? StageDirection.Descending : StageDirection.Ascending;

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnStageChanged;
    public event Action<int> OnLivesChanged;
    public event Action OnStageClear;
    public event Action OnGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartGame()
    {
        currentStage = 1;
        lives = GameConstants.StartingLives;
        DeathFreeze = false;
        
        ScoreManager.Instance?.ResetScore();
        
        SetState(GameState.Playing);
        OnLivesChanged?.Invoke(lives);

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetDiedFromBoulder(false);
            player.Respawn();
        }

        OnStageChanged?.Invoke(currentStage);
        
        FindAnyObjectByType<StageFlowController>()?.RestartCurrentWave(false);
    }

    public bool ShouldRunGameplay
    {
        get
        {
            if (DeathFreeze) return false;
            return CurrentState == GameState.Playing || CurrentState == GameState.BonusStage;
        }
    }

    public void SetState(GameState state)
    {
        if (CurrentState == state) return;
        CurrentState = state;
        OnStateChanged?.Invoke(state);
    }

    public void LoseLife(bool diedFromBoulder = false)
    {
        if (CurrentState == GameState.BonusStage) return; 
        if (DeathFreeze) return; 

        DeathFreeze = true;
        lives = Mathf.Max(0, lives - 1);
        OnLivesChanged?.Invoke(lives);

        var spawner = FindAnyObjectByType<WolfSpawner>();
        spawner?.StopAllSpawning();

        if (lives <= 0)
            TriggerGameOver();
        else
            StartCoroutine(RestartWaveRoutine(diedFromBoulder)); 
    }

    private IEnumerator RestartWaveRoutine(bool diedFromBoulder)
    {
        yield return new WaitForSeconds(2f); 
        DeathFreeze = false;

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetDiedFromBoulder(false);
            player.Respawn();
        }

        FindAnyObjectByType<StageFlowController>()?.RestartCurrentWave(diedFromBoulder);
    }

    public void ClearDeathFreeze()
    {
        DeathFreeze = false;
    }

    public void AddLife()
    {
        lives++;
        OnLivesChanged?.Invoke(lives);
        AudioManager.Instance?.PlayExtraLife(); 
    }

    public void TriggerStageClear()
    {
        if (CurrentState == GameState.StageClear || CurrentState == GameState.GameOver) return;
        SetState(GameState.StageClear);
        OnStageClear?.Invoke();
    }

    public void AdvanceStage()
    {
        if (currentStage % 2 == 0)
        {
            EnterBonusStage();
        }
        else
        {
            currentStage++;
            OnStageChanged?.Invoke(currentStage);
            SetState(GameState.Playing);
        }
    }

    public void EnterBonusStage()
    {
        SetState(GameState.BonusStage);
    }

    public void CompleteBonusStage()
    {
        currentStage++;
        OnStageChanged?.Invoke(currentStage);
        SetState(GameState.Playing);
    }

    public void TriggerGameOver()
    {
        SetState(GameState.GameOver);
        
        // เคลียร์จอให้โล่งตอนจบเกมแบบ Famicom!
        CleanUpSceneForGameOver();
        
        OnGameOver?.Invoke();
        StartCoroutine(GameOverSequenceRoutine());
    }
    
    private void CleanUpSceneForGameOver()
    {
        // ทำลายผู้เล่น (แม่หมู + กระเช้า)
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) Destroy(player.gameObject);
        
        // ทำลายหมาป่าทั้งหมด
        var wolves = FindObjectsByType<Wolf>(FindObjectsSortMode.None);
        foreach(var w in wolves) if (w != null) Destroy(w.gameObject);
        
        var boss = FindObjectsByType<BossWolf>(FindObjectsSortMode.None);
        foreach(var b in boss) if (b != null) Destroy(b.gameObject);

        // ทำลายลูกดอก หิน และเนื้อ
        var arrows = FindObjectsByType<Arrow>(FindObjectsSortMode.None);
        foreach(var a in arrows) if (a != null) Destroy(a.gameObject);
        
        var projectiles = FindObjectsByType<WolfProjectile>(FindObjectsSortMode.None);
        foreach(var p in projectiles) if (p != null) Destroy(p.gameObject);
        
        var meats = FindObjectsByType<MeatWeapon>(FindObjectsSortMode.None);
        foreach(var m in meats) if (m != null) Destroy(m.gameObject);
    }

    private IEnumerator GameOverSequenceRoutine()
    {
        yield return new WaitForSeconds(4f); 

        if (ScoreManager.Instance != null && ScoreManager.Instance.IsTopScore(ScoreManager.Instance.Score))
        {
            SetState(GameState.NameEntry);
        }
        else
        {
            // โหลดกลับไปฉาก MainMenu เลย
            PlayerPrefs.SetInt("ShowLeaderboardFirst", 1); 
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void FinishNameEntry()
    {
        // โหลดกลับไปฉาก MainMenu หลังกรอกชื่อเสร็จ
        PlayerPrefs.SetInt("ShowLeaderboardFirst", 1); 
        SceneManager.LoadScene("MainMenu");
    }

    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsBonus => CurrentState == GameState.BonusStage;
}
