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

    [Header("Editor Debug / Testing")]
    [Tooltip("ถ้าติ๊กถูก: เวลาเล่นใน Unity Editor จะบังคับใช้เลขด่าน currentStage ที่กรอกใน Inspector เสมอ")]
    [SerializeField] private bool useInspectorStageInEditor = true;
    [Tooltip("ถ้าติ๊กถูก หรือชื่อ Scene มีคำว่า Bonus: จะเริ่มเกมในโหมด Bonus Stage ทันที")]
    [SerializeField] private bool startAsBonusStage = false;

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

    public bool IsBonusSceneActive =>
        startAsBonusStage || SceneManager.GetActiveScene().name.IndexOf("Bonus", StringComparison.OrdinalIgnoreCase) >= 0;

    public void StartGame()
    {
        if (GameSession.IsActive)
        {
            currentStage = GameSession.CurrentStage;
            lives = GameSession.Lives;
        }
        else
        {
            // กรณีเริ่มเกมครั้งแรก หรือกด Play เทสจาก Scene นั้นๆ โดยตรงใน Editor
            string activeScene = SceneManager.GetActiveScene().name;
            if (currentStage <= 1)
            {
                if (activeScene.IndexOf("Spring", StringComparison.OrdinalIgnoreCase) >= 0)
                    currentStage = 2;
                else if (activeScene.IndexOf("Winter", StringComparison.OrdinalIgnoreCase) >= 0)
                    currentStage = 3;
                else if (activeScene.IndexOf("Summer", StringComparison.OrdinalIgnoreCase) >= 0)
                    currentStage = 4;
            }

            GameSession.IsActive = true;
            GameSession.CurrentStage = currentStage;
            GameSession.Lives = lives;
            GameSession.Score = 0;
        }

        DeathFreeze = false;
        ScoreManager.Instance?.ResetScore();

        bool isBonusScene = IsBonusSceneActive;

        // เตรียมตัวผู้เล่นและเคลียร์ฉากไว้ให้เรียบร้อยก่อนเริ่มคัทซีน
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetDiedFromBoulder(false);
            player.Respawn();
        }

        FindAnyObjectByType<WolfSpawner>()?.StopAllSpawning();
        FindAnyObjectByType<StageFlowController>()?.PrepareStageBeforeCutscene(currentStage);

        OnLivesChanged?.Invoke(lives);
        OnStageChanged?.Invoke(currentStage);

        // ถ้ามีคัทซีนก่อนเริ่มด่าน (เฉพาะด่าน 1 และ 2) ให้เข้าสู่สถานะ Cutscene และรอจนกว่าคัทซีนจะเล่นจบก่อนเริ่มเกมจริง!
        var cutscene = FindAnyObjectByType<StageCutsceneController>();
        if (cutscene != null && cutscene.HasIntroCutsceneForStage(currentStage) && !isBonusScene)
        {
            SetState(GameState.Cutscene);
            cutscene.PlayStageIntroCutscene(currentStage, () => BeginStageGameplay(isBonusScene));
            return;
        }

        BeginStageGameplay(isBonusScene);
    }

    private void BeginStageGameplay(bool isBonusScene)
    {
        if (isBonusScene)
        {
            EnterBonusStage();
            FindAnyObjectByType<WolfSpawner>()?.StopAllSpawning();
            FindAnyObjectByType<StageFlowController>()?.ClearAllEntities();
            return;
        }

        // เปลี่ยนสถานะเป็น Playing (StageFlowController จะรับอีเวนต์ OnStateChanged แล้วสั่ง OnStageStart ปล่อยหมาป่าทันที)
        SetState(GameState.Playing);
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
        GameSession.Lives = lives; // บันทึกไว้เผื่อเปลี่ยน Scene
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
        GameSession.Lives = lives;
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
            // ถ้ามี Scene Bonus แยกต่างหาก (เช่น Spring_Bonus) ให้โหลด Scene นั้น
            string bonusScene = GetBonusSceneNameForStage(currentStage);
            if (Application.CanStreamedLevelBeLoaded(bonusScene))
            {
                GameSession.CurrentStage = currentStage;
                GameSession.Lives = lives;
                if (ScoreManager.Instance != null) GameSession.Score = ScoreManager.Instance.Score;
                SceneManager.LoadScene(bonusScene);
            }
            else
            {
                EnterBonusStage();
            }
        }
        else
        {
            currentStage++;
            GameSession.CurrentStage = currentStage;
            GameSession.Lives = lives;
            if (ScoreManager.Instance != null) GameSession.Score = ScoreManager.Instance.Score;
            
            // เปลี่ยน Scene เป็นด่านใหม่ตามฤดู
            LoadStageScene(currentStage);
        }
    }

    public void EnterBonusStage()
    {
        SetState(GameState.BonusStage);
    }

    public void CompleteBonusStage()
    {
        currentStage++;
        GameSession.CurrentStage = currentStage;
        GameSession.Lives = lives;
        if (ScoreManager.Instance != null) GameSession.Score = ScoreManager.Instance.Score;
        
        LoadStageScene(currentStage);
    }

    private void LoadStageScene(int stage)
    {
        string targetScene = GetSceneNameForStage(stage);
        // เช็คว่ามีฉากนี้ใน Build Settings มั้ย ถ้ายังไม่มี (เช่น Summer ด่าน 4 ยังไม่เสร็จ) ให้ข้ามไปฉากที่มีและตรงประเภทด่านคี่/คู่
        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            if (stage % 2 == 0)
            {
                // ถ้าเป็นด่านคู่ (เช่น ด่าน 4 Summer ยังไม่มี) ให้ใช้ฉากด่านคู่ที่มีอยู่ (Spring)
                if (Application.CanStreamedLevelBeLoaded("Spring")) targetScene = "Spring";
                else if (Application.CanStreamedLevelBeLoaded("Winter")) targetScene = "Winter";
            }
            else
            {
                // ถ้าเป็นด่านคี่ ให้ใช้ฉากด่านคี่ที่มีอยู่ (Winter หรือ Autumn)
                if (Application.CanStreamedLevelBeLoaded("Winter")) targetScene = "Winter";
                else if (Application.CanStreamedLevelBeLoaded("Autumn")) targetScene = "Autumn";
            }
        }
        SceneManager.LoadScene(targetScene);
    }

    private string GetBonusSceneNameForStage(int stage)
    {
        string candidate = GetSceneNameForStage(stage) + "_Bonus";
        if (Application.CanStreamedLevelBeLoaded(candidate))
            return candidate;
        if (Application.CanStreamedLevelBeLoaded("Spring_Bonus"))
            return "Spring_Bonus";
        return candidate;
    }

    private string GetSceneNameForStage(int stage)
    {
        // 1=Autumn (ด่านคี่), 2=Spring (ด่านคู่), 3=Winter (ด่านคี่), 4=Summer (ด่านคู่) (แล้ววนลูป)
        int index = (stage - 1) % 4;
        switch (index)
        {
            case 0: return "Autumn";
            case 1: return "Spring";
            case 2: return "Winter";
            case 3: return "Summer";
            default: return "Autumn";
        }
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

#if UNITY_EDITOR
    [ContextMenu("Test/Jump To Current Inspector Stage")]
    public void TestJumpToCurrentInspectorStage()
    {
        GameSession.IsActive = true;
        GameSession.CurrentStage = currentStage;
        OnStageChanged?.Invoke(currentStage);
        FindAnyObjectByType<StageFlowController>()?.OnStageStart(currentStage);
    }

    [ContextMenu("Test/Trigger Stage Clear (Next Stage or Bonus)")]
    public void TestTriggerStageClear()
    {
        TriggerStageClear();
    }

    [ContextMenu("Test/Enter Bonus Stage Directly")]
    public void TestEnterBonusStage()
    {
        EnterBonusStage();
    }
#endif
}
