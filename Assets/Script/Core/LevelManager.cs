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

    // ดึง Config ของด่านนั้นๆ มา
    public LevelConfig GetConfigForStage(int stage)
    {
        // ถ้ามีเซ็ตไว้ล่วงหน้า (Override) ก็เอาอันนั้นมาใช้
        if (stageOverrides != null && stageOverrides.Length == 1 && stageOverrides[0] != null)
            return stageOverrides[0];

        if (stageOverrides != null && stage >= 1 && stage <= stageOverrides.Length && stageOverrides[stage - 1] != null)
            return stageOverrides[stage - 1];

        // ถ้าไม่มี Default เลย ก็สร้างใหม่แล้วคำนวณความยากตามด่าน
        if (defaultConfig == null)
        {
            CurrentConfig = ScriptableObject.CreateInstance<LevelConfig>();
            ApplyScaling(CurrentConfig, stage, isCustomStageAsset: false);
            return CurrentConfig;
        }

        // ดึงจาก Default ของซีนนั้นๆ มาใช้ โดยเคารพค่าที่ตั้งไว้ใน Asset ของด่าน 1-4
        // และค่อยเพิ่มความยากเมื่อวนลูปด่านสูงขึ้น (Stage 5+)
        var config = Instantiate(defaultConfig);
        ApplyScaling(config, stage, isCustomStageAsset: true);
        CurrentConfig = config;
        return config;
    }

    // ยิ่งเล่นลึกยิ่งยาก ระบบอัพสเกลความโหดโดยไม่ทับค่าที่ตั้งไว้ในแต่ละซีนตอนรอบแรก
    private static void ApplyScaling(LevelConfig config, int stage, bool isCustomStageAsset)
    {
        // ถ้าซีนมี Asset ของตัวเองอยู่แล้ว จะเพิ่มสเกลเฉพาะตอนวนรอบใหม่ (ด่าน 5 เป็นต้นไป)
        int tier = isCustomStageAsset
            ? Mathf.Max(0, (stage - 1) / 4)
            : Mathf.Max(0, stage - 1);

        if (tier > 0)
        {
            config.wolfCount = Mathf.Min(99, config.wolfCount + tier * 4);
            config.wolfSpeedRange += new Vector2(0.06f, 0.10f) * tier;
            config.shieldRatio = Mathf.Min(0.6f, config.shieldRatio + tier * 0.05f);
            config.rockThrowRatio = Mathf.Min(0.45f, config.rockThrowRatio + tier * 0.04f);
            config.yellowBalloonRatio = Mathf.Min(0.6f, config.yellowBalloonRatio + tier * 0.08f);
            config.spawnInterval = Mathf.Max(1.2f, config.spawnInterval - tier * 0.15f);
        }

        // บังคับกฎบอสตามลำดับด่านต้นฉบับ Pooyan
        if (stage >= 3 && stage % 2 == 1)
            config.hasTreeTopBoss = true; // ด่านคี่ตั้งแต่ด่าน 3 (Winter) มีบอสวิ่งปาผลไม้ข้างบน

        if (stage % 2 == 0)
            config.hasBossWolf = true;    // ด่านคู่ (Spring / Summer) มีบอสหมาป่าลูกโป่งกระพริบท้ายด่าน
    }
}

