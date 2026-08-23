using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton — controls game state, lives, stage progression.
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

    private void Update()
    {
        if (CurrentState == GameState.GameOver)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                StartGame();
            }
        }
    }

    public void StartGame()
    {
        currentStage = 1;
        lives = GameConstants.StartingLives;
        DeathFreeze = false;
        SetState(GameState.Playing);
        OnLivesChanged?.Invoke(lives);

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetDiedFromBoulder(false);
            player.Respawn();
        }

        OnStageChanged?.Invoke(currentStage);
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
        // เช็คก่อนว่าด่านที่เพิ่งเคลียร์ไป (ด่านปัจจุบัน) เป็นด่านเลขคู่หรือไม่
        if (currentStage % 2 == 0)
        {
            // ถ้าเคลียร์ด่าน 2 ให้เข้า Bonus เลย โดยยังไม่ต้องบวกเลขด่าน
            EnterBonusStage();
        }
        else
        {
            // ถ้าเคลียร์ด่าน 1 (เลขคี่) ให้บวกเลขเป็นด่าน 2 แล้วเริ่มเล่นต่อได้เลย
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
        // หลังจากเล่น Bonus Stage จบแล้ว ถึงจะบวกเลขด่าน (เช่น จาก 2 ไป 3)
        currentStage++;
        OnStageChanged?.Invoke(currentStage);
        SetState(GameState.Playing);
    }

    public void TriggerGameOver()
    {
        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsBonus => CurrentState == GameState.BonusStage;
}
