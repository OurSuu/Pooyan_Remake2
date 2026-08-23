using UnityEngine;

/// <summary>
/// Per-stage difficulty config — assign via ScriptableObject assets.
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Pooyan/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Wolves")]
    public int wolfCount = 32;
    public Vector2 wolfSpeedRange = new(1.5f, 3f);
    [Range(0f, 1f)] public float shieldRatio = 0.2f;
    [Range(0f, 1f)] public float rockThrowRatio = 0.15f;
    public float spawnInterval = 1f;

    [Header("Balloon")]
    [Min(1)] public int balloonHP = 1;

    [Header("Boss")]
    public bool hasBossWolf = true;
    public bool hasTreeTopBoss;
    [Min(1)] public int bossShieldHP = GameConstants.BossShieldHits;
}

/// <summary>
/// Provides difficulty parameters per stage and wave timing.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelConfig defaultConfig;
    [SerializeField] private LevelConfig[] stageOverrides;

    public LevelConfig CurrentConfig { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStageChanged += LoadConfigForStage;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStageChanged -= LoadConfigForStage;
    }

    public void LoadConfigForStage(int stage)
    {
        CurrentConfig = GetConfigForStage(stage);
    }

    public LevelConfig GetConfigForStage(int stage)
    {
        if (stageOverrides != null && stage >= 1 && stage <= stageOverrides.Length && stageOverrides[stage - 1] != null)
            return stageOverrides[stage - 1];

        if (defaultConfig == null)
        {
            CurrentConfig = ScriptableObject.CreateInstance<LevelConfig>();
            ApplyScaling(CurrentConfig, stage);
            return CurrentConfig;
        }

        var config = Instantiate(defaultConfig);
        ApplyScaling(config, stage);
        CurrentConfig = config;
        return config;
    }

    private static void ApplyScaling(LevelConfig config, int stage)
    {
        int tier = Mathf.Max(0, stage - 1);
        config.wolfCount = Mathf.Min(99, config.wolfCount + tier * 8); // Arcade max = 99
        config.wolfSpeedRange += new Vector2(0.1f, 0.2f) * tier;
        config.shieldRatio = Mathf.Min(0.7f, config.shieldRatio + tier * 0.05f);
        config.rockThrowRatio = Mathf.Min(0.5f, config.rockThrowRatio + tier * 0.03f);
        config.balloonHP = Mathf.Min(3, 1 + (tier / 2));
        config.spawnInterval = Mathf.Max(0.5f, config.spawnInterval - tier * 0.05f);
        config.hasTreeTopBoss = stage >= 3;
        config.hasBossWolf = stage % 2 == 0;
    }
}
