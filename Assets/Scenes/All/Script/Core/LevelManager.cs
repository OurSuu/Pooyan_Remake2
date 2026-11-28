using UnityEngine;

/// <summary>
/// ตัวจัดการเรื่องความยากของด่านและจังหวะการเกิดเวฟต่างๆ
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelConfig defaultConfig;
    [SerializeField] private LevelConfig[] stageOverrides; // ถ้าอยากเซ็ตเฉพาะด่าน ใส่ตรงนี้

    public LevelConfig CurrentConfig { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ผูก Event พอเปลี่ยนด่านปุ๊บ ให้โหลด Config รอเลย
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

    // ดึง Config ของด่านนั้นๆ มา (โคลนเป็นสำเนาใหม่เสมอและบังคับใช้ ApplyScaling เพื่อไม่ให้แก้ไขค่าต้นฉบับใน Asset)
    public LevelConfig GetConfigForStage(int stage)
    {
        LevelConfig baseConfig = null;
        if (stageOverrides != null && stage >= 1 && stage <= stageOverrides.Length && stageOverrides[stage - 1] != null)
            baseConfig = stageOverrides[stage - 1];
        else if (stageOverrides != null && stageOverrides.Length == 1 && stageOverrides[0] != null)
            baseConfig = stageOverrides[0];
        else
            baseConfig = defaultConfig;

        LevelConfig config = baseConfig != null
            ? Instantiate(baseConfig)
            : ScriptableObject.CreateInstance<LevelConfig>();

        ApplyScaling(config, stage, isCustomStageAsset: baseConfig != null);
        CurrentConfig = config;
        return config;
    }

    // ยิ่งเล่นลึกยิ่งยาก ระบบอัพสเกลความโหด (+4 ตัวต่อด่านตาม NES และเพิ่มความเร็ว/ความถี่ต่อเนื่องทุกด่าน)
    private static void ApplyScaling(LevelConfig config, int stage, bool isCustomStageAsset)
    {
        // จำนวนหมาป่าอิงตาม NES เป๊ะ (+4 ตัวต่อด่าน: 32, 36, 40, 44, 48, 52...) ไม่ว่าด่านสุ่มจะเปิดฉากไหนขึ้นมา
        if (stage >= 1)
        {
            config.wolfCount = Mathf.Min(99, 32 + (stage - 1) * 4);
        }

        // ถ้ามี Asset ของด่าน 1-4 อยู่แล้ว จะเริ่มเร่งสปีด/ความถี่เพิ่มเติมตั้งแต่ด่าน 5 เป็นต้นไป (ถ้าไม่มี Asset จะสเกลตั้งแต่ด่าน 2)
        int scaleSteps = isCustomStageAsset
            ? Mathf.Min(14, Mathf.Max(0, stage - 4))
            : Mathf.Min(16, Mathf.Max(0, stage - 1));

        if (scaleSteps > 0)
        {
            config.wolfSpeedRange += new Vector2(0.05f, 0.06f) * scaleSteps;
            config.shieldRatio = Mathf.Min(0.65f, config.shieldRatio + scaleSteps * 0.04f);
            config.rockThrowRatio = Mathf.Min(0.65f, config.rockThrowRatio + scaleSteps * 0.04f);
            config.spawnInterval = Mathf.Max(0.95f, config.spawnInterval - scaleSteps * 0.12f);
        }

        // บังคับกฎบอสและ HP ลูกโป่งตามลำดับด่านคี่/คู่
        if (stage >= 3 && stage % 2 == 1)
            config.hasTreeTopBoss = true; // ด่านคี่ตั้งแต่ด่าน 3 (Winter / Summer / ด่านคี่สุ่ม) มีบอสวิ่งปาผลไม้ข้างบน

        if (stage % 2 == 0)
        {
            bool isSpringScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                .IndexOf("Spring", System.StringComparison.OrdinalIgnoreCase) >= 0;
            config.hasBossWolf = !isSpringScene; // เฉพาะด่าน Spring ให้เอาบอสออกชั่วคราว ตัวสุดท้ายเป็นหมาป่าธรรมดา (ด่านคู่อื่นอย่าง Rainy ยังมีบอสปกติ)
            config.balloonHP = config.balloonHP > 0 ? Mathf.Clamp(config.balloonHP, 3, 4) : 4;
            config.bossShieldHP = config.bossShieldHP > 0 ? Mathf.Clamp(config.bossShieldHP, 1, 5) : GameConstants.BossShieldHits;
        }
    }
}

