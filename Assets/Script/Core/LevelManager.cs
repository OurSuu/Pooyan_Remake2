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

        // ถ้าไม่มี Default เลย ก็สร้างใหม่แล้วคำนวณความยากเอา
        if (defaultConfig == null)
        {
            CurrentConfig = ScriptableObject.CreateInstance<LevelConfig>();
            ApplyScaling(CurrentConfig, stage);
            return CurrentConfig;
        }

        // ดึงจาก Default มาเพิ่มระดับความยากตามเลขด่าน (Scaling)
        var config = Instantiate(defaultConfig);
        ApplyScaling(config, stage);
        CurrentConfig = config;
        return config;
    }

    // ยิ่งเล่นลึกยิ่งยาก ระบบอัพสเกลความโหด
    private static void ApplyScaling(LevelConfig config, int stage)
    {
        int tier = Mathf.Max(0, stage - 1);
        config.wolfCount = Mathf.Min(99, config.wolfCount + tier * 8); // หมาป่าเยอะขึ้น (สูงสุด 99 แบบตู้)
        config.wolfSpeedRange += new Vector2(0.1f, 0.2f) * tier;       // วิ่งไวขึ้น
        config.shieldRatio = Mathf.Min(0.7f, config.shieldRatio + tier * 0.05f);     // ถือโล่กันเยอะขึ้น
        config.rockThrowRatio = Mathf.Min(0.5f, config.rockThrowRatio + tier * 0.03f); // ปาหินรัวๆ
        config.balloonHP = Mathf.Min(3, 1 + (tier / 2));               // ลูกโป่งอึดขึ้นด้วย
        config.spawnInterval = Mathf.Max(0.5f, config.spawnInterval - tier * 0.05f); // เกิดไวขึ้น
        config.hasTreeTopBoss = stage >= 3;                            // หลังด่าน 3 มีบอสบนต้นไม้แล้ว
        config.hasBossWolf = stage % 2 == 0;                           // ด่านคู่จะมีบอสโผล่มา
    }
}

