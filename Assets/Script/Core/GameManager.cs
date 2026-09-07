using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton จ้า — ตัวคุมสถานะเกม (Game State), จำนวนชีวิต, แล้วก็การเปลี่ยนด่าน
/// </summary>
public class GameManager : MonoBehaviour
{
    // เข้าถึงผ่าน GameManager.Instance ได้เลย
    public static GameManager Instance { get; private set; }

    [Header("Stage")]
    // ด่านปัจจุบันกับชีวิตที่เหลืออยู่
    [SerializeField] private int currentStage = 1;
    [SerializeField] private int lives = GameConstants.StartingLives;

    // สถานะเกมตอนนี้ เริ่มมาก็เข้า Main Menu ก่อนเลย
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int CurrentStage => currentStage;
    public int Lives => lives;
    
    // DeathFreeze เอาไว้ฟรีซเกมชั่วคราวตอนผู้เล่นตาย
    public bool DeathFreeze { get; private set; }
    
    // ด่านคี่ใช่มั้ย? (เอาไว้เช็คประเภทด่าน)
    public bool IsOddStage => currentStage % 2 == 1;
    
    // ทิศทางด่าน: ด่านคี่หมาป่าลงมา (Descending) ด่านคู่หมาป่าลอยขึ้น (Ascending)
    public StageDirection CurrentDirection =>
        IsOddStage ? StageDirection.Descending : StageDirection.Ascending;

    // Events ต่างๆ เอาไว้บอกชาวบ้านตอนมีอะไรเปลี่ยนแปลง
    public event Action<GameState> OnStateChanged;
    public event Action<int> OnStageChanged;
    public event Action<int> OnLivesChanged;
    public event Action OnStageClear;
    public event Action OnGameOver;

    private void Awake()
    {
        // ทำ Singleton pattern กันเบิ้ล
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // ถ้า Game Over อยู่ แล้วกดปุ่มพวกนี้ ก็จะเริ่มเกมใหม่
        if (CurrentState == GameState.GameOver)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                StartGame();
            }
        }
    }

    // เริ่มเกมใหม่ เซ็ตทุกอย่างกลับไปค่าเริ่มต้น
    public void StartGame()
    {
        currentStage = 1;
        lives = GameConstants.StartingLives;
        DeathFreeze = false;
        SetState(GameState.Playing);
        OnLivesChanged?.Invoke(lives);

        // หา Player แล้วสั่งเกิดใหม่ (Respawn)
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetDiedFromBoulder(false);
            player.Respawn();
        }

        OnStageChanged?.Invoke(currentStage);
    }

    // เช็คว่าระบบเกมเพลย์ควรทำงานอยู่มั้ย 
    public bool ShouldRunGameplay
    {
        get
        {
            // ถ้าแช่แข็งตอนตายอยู่ ก็ไม่ต้องรัน
            if (DeathFreeze) return false;
            // รันเฉพาะตอนเล่นปกติ หรือเข้า Bonus Stage
            return CurrentState == GameState.Playing || CurrentState == GameState.BonusStage;
        }
    }

    // เปลี่ยน State เกม แล้วยิง Event บอกคนอื่นด้วย
    public void SetState(GameState state)
    {
        if (CurrentState == state) return;
        CurrentState = state;
        OnStateChanged?.Invoke(state);
    }

    // หักชีวิตตอนตาย!
    public void LoseLife(bool diedFromBoulder = false)
    {
        if (CurrentState == GameState.BonusStage) return; // ในด่านโบนัสห้ามตาย
        if (DeathFreeze) return; // ถ้าฟรีซอยู่แปลว่าตายไปแล้ว ข้ามไป

        DeathFreeze = true;
        lives = Mathf.Max(0, lives - 1);
        OnLivesChanged?.Invoke(lives);

        // หยุดเกิดหมาป่าชั่วคราว
        var spawner = FindAnyObjectByType<WolfSpawner>();
        spawner?.StopAllSpawning();

        if (lives <= 0)
            TriggerGameOver();
        else
            StartCoroutine(RestartWaveRoutine(diedFromBoulder)); // ถ้ายังมีชีวิตเหลือ ก็รันรอเริ่มเวฟใหม่
    }

    // Coroutine รอเวลาหลังตายแล้วค่อยเริ่มใหม่
    private IEnumerator RestartWaveRoutine(bool diedFromBoulder)
    {
        yield return new WaitForSeconds(2f); // หน่วงเวลาให้ทำใจ 2 วิ
        DeathFreeze = false;

        // เรียกผู้เล่นเกิดใหม่
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetDiedFromBoulder(false);
            player.Respawn();
        }

        // รีเซ็ตเวฟ
        FindAnyObjectByType<StageFlowController>()?.RestartCurrentWave(diedFromBoulder);
    }

    // ปลดฟรีซเกมซะ
    public void ClearDeathFreeze()
    {
        DeathFreeze = false;
    }

    // เพิ่มชีวิต (1-Up!)
    public void AddLife()
    {
        lives++;
        OnLivesChanged?.Invoke(lives);
        AudioManager.Instance?.PlayExtraLife(); // เล่นเสียงแจ่มๆ
    }

    // เคลียร์ด่านแล้วโว้ย!
    public void TriggerStageClear()
    {
        if (CurrentState == GameState.StageClear || CurrentState == GameState.GameOver) return;
        SetState(GameState.StageClear);
        OnStageClear?.Invoke();
    }

    // ไปด่านถัดไป
    public void AdvanceStage()
    {
        if (currentStage % 2 == 0)
        {
            // ถ้าเพิ่งผ่านด่านคู่มา ให้เข้าด่านโบนัส
            EnterBonusStage();
        }
        else
        {
            // ถ้าด่านคี่ ก็ลุยด่านต่อไปเลย
            currentStage++;
            OnStageChanged?.Invoke(currentStage);
            SetState(GameState.Playing);
        }
    }

    public void EnterBonusStage()
    {
        SetState(GameState.BonusStage);
    }

    // จบด่านโบนัสแล้ว ไปต่อ!
    public void CompleteBonusStage()
    {
        currentStage++;
        OnStageChanged?.Invoke(currentStage);
        SetState(GameState.Playing);
    }

    // เกมโอเวอร์ จบเห่
    public void TriggerGameOver()
    {
        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    // ตัวช่วยเช็ค State ง่ายๆ
    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsBonus => CurrentState == GameState.BonusStage;
}
