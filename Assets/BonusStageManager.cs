using System.Collections;
using UnityEngine;

public enum BonusStageMode
{
    FamicomTopBoss,      // บอสหมาป่าวิ่งบนยอดไม้แล้วทิ้งผลไม้ลงมา (เวอร์ชัน Famicom/NES สุดคลาสสิก!)
    ArcadeCliffThrowers  // หมาป่ายืนตามกิ่งไม้ฝั่งซ้ายปาวิถีโค้ง (เวอร์ชันตู้ Arcade 1982)
}

/// <summary>
/// เฮ้ยเพื่อน ไฟล์นี้คือตัวจัดการ Bonus Stage นะ!
/// รองรับทั้งระบบ Famicom (บอสหมาป่าวิ่งบนยอดไม้ทิ้งผลไม้ + คอมโบคูณคะแนน Nakamoto Chain 100->8,000)
/// และระบบ Arcade (หมาป่า 3 ตัวยืนบนกิ่งไม้ซากุระปาวิถีโค้ง)
/// </summary>
public class BonusStageManager : MonoBehaviour
{
    [Header("Bonus Style Selection")]
    [SerializeField] private BonusStageMode bonusStyle = BonusStageMode.FamicomTopBoss; // ค่าเริ่มต้นคือแบบ Famicom!
    [SerializeField] private BonusBossWolf bossWolf; // บอสหมาป่าวิ่งบนยอดไม้
    [SerializeField] private BonusWolfThrower[] wolfThrowers; // หมาป่ากิ่งไม้ฝั่งซ้าย (Arcade)

    [Header("Fruit Bonus")]
    [SerializeField] private GameObject fruitPrefab; // พรีแฟบผลไม้ที่จะให้ตกมา
    [SerializeField] private float fruitSpawnInterval = 1.3f; // ระยะห่างการทิ้ง/ปาผลไม้แต่ละลูก
    [SerializeField] private float fruitSpawnXMin = -4f; // ขอบแกน X ซ้ายสุด (สำรอง)
    [SerializeField] private float fruitSpawnXMax = 4f; // ขอบแกน X ขวาสุด (สำรอง)
    [SerializeField] private float fruitSpawnY = 5f; // ความสูงที่ผลไม้จะโผล่มา (สำรอง)

    [Header("Meat Bonus")]
    [SerializeField] private Wolf wolfPrefab; // พรีแฟบหมาป่าสำหรับโบนัสเนื้อ
    [SerializeField] private Transform[] meatBonusSpawnPoints; // จุดเกิดหมาป่า

    [Header("Timing")]
    [SerializeField] private float bonusDuration = 30f; // เวลาของโบนัสสเตจ

    // ลำดับคะแนนคอมโบ Chain ของ Famicom (100 -> 200 -> 400 -> 800 -> 1,000 -> 2,000 -> 4,000 -> 8,000 Nakamoto!)
    private readonly int[] famicomChainScores = new int[] { 100, 200, 400, 800, 1000, 2000, 4000, 8000 };
    private int currentChain = 0;

    private BonusType currentBonusType; // เก็บว่าตอนนี้เล่นโบนัสแบบไหนอยู่
    private Coroutine bonusRoutine; // เก็บ Coroutine เผื่อต้องสั่งหยุดกลางคัน

    [Header("Bonus Order (NES vs Arcade)")]
    [Tooltip("เปิดไว้ = ลำดับตาม NES 1985 (หลังด่าน 2 เป็นโบนัสปาเนื้อ, หลังด่าน 4 เป็นโบนัสยิงผลไม้)")]
    [SerializeField] private bool useNESBonusOrder = true;

    public BonusStageMode BonusStyle
    {
        get => bonusStyle;
        set => bonusStyle = value;
    }

    public BonusBossWolf BossWolf
    {
        get => bossWolf;
        set => bossWolf = value;
    }

    public BonusWolfThrower[] WolfThrowers
    {
        get => wolfThrowers;
        set => wolfThrowers = value;
    }

    private void Awake()
    {
        FindEntitiesInScene();
    }

    private void FindEntitiesInScene()
    {
        if (bossWolf == null)
            bossWolf = FindAnyObjectByType<BonusBossWolf>(FindObjectsInactive.Include);

        if (wolfThrowers == null || wolfThrowers.Length == 0)
            wolfThrowers = FindObjectsByType<BonusWolfThrower>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void Start()
    {
        // ดักรอ GameManager เปลี่ยนสถานะเกม
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            if (GameManager.Instance.CurrentState == GameState.BonusStage)
            {
                StartBonusStage();
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.BonusStage)
            StartBonusStage();
        else
            StopBonusStage();
    }

    public void StartBonusStage()
    {
        int stage = GameManager.Instance?.CurrentStage ?? 2;
        bool isFirstBonusInCycle = (stage == 2 || ((stage / 2) % 2 != 0));

        // ตามต้นฉบับ NES (Famicom): โบนัสแรก (หลังด่าน 2) คือปาเนื้อ (Meat Bonus), โบนัสที่สอง (หลังด่าน 4) คือยิงผลไม้ (Fruit Bonus)
        if (useNESBonusOrder)
            currentBonusType = isFirstBonusInCycle ? BonusType.Meat : BonusType.Fruit;
        else
            currentBonusType = isFirstBonusInCycle ? BonusType.Fruit : BonusType.Meat;

        FindAnyObjectByType<StageFlowController>()?.ClearAllEntities();
        FindAnyObjectByType<WolfSpawner>()?.StopAllSpawning();

        if (bonusRoutine != null) StopCoroutine(bonusRoutine);
        bonusRoutine = StartCoroutine(BonusRoutine());
    }

    public void StopBonusStage()
    {
        if (bonusRoutine != null)
        {
            StopCoroutine(bonusRoutine);
            bonusRoutine = null;
        }

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.SetMeatBonusMode(false);

        if (bossWolf != null) bossWolf.gameObject.SetActive(false);
    }

    private const int FruitBonusCount = 20; // 20 ลูกเป๊ะๆ ตามต้นฉบับ
    private const int PerfectBonusScore = 10000;
    private int fruitsSpawned; 
    private int fruitsHit; 

    public void OnBonusFruitHit(BonusFruitType type, Vector3 hitPosition)
    {
        fruitsHit++;

        if (bonusStyle == BonusStageMode.FamicomTopBoss)
        {
            // คำนวณคะแนนตามสเต็ป Famicom Nakamoto Chain!
            int scoreIndex = Mathf.Clamp(currentChain, 0, famicomChainScores.Length - 1);
            int points = famicomChainScores[scoreIndex];

            ScoreManager.Instance?.AddBonusChainScore(points, hitPosition);

            currentChain++;
            Debug.Log($"[Famicom Bonus Chain] Hit {currentChain}! +{points} pts!");
        }
        else
        {
            // แบบ Arcade ทั่วไป
            ScoreManager.Instance?.AddBonusFruitScore(type, hitPosition);
        }
    }

    public void OnBonusFruitMissed()
    {
        // ถ้าผลไม้หล่นถึงพื้นโดยไม่โดนยิง -> รีเซ็ต Chain ทันทีตามต้นฉบับ Famicom!
        if (currentChain > 0)
        {
            Debug.Log($"[Famicom Bonus Chain] Missed! Chain reset from {currentChain} to 0.");
            currentChain = 0;
        }
    }

    private IEnumerator BonusRoutine()
    {
        var player = FindAnyObjectByType<PlayerController>();
        FindEntitiesInScene();

        if (currentBonusType == BonusType.Fruit)
        {
            fruitsSpawned = 0;
            fruitsHit = 0;
            currentChain = 0;

            // 1. จัดการการแสดงผลของตัวละครตามสไตล์ที่เลือก
            if (bonusStyle == BonusStageMode.FamicomTopBoss)
            {
                // เปิดบอสยอดไม้ ปิดหมาป่ากิ่งไม้
                if (bossWolf != null)
                {
                    bossWolf.gameObject.SetActive(true);
                    bossWolf.SetPatrolActive(true);
                }
                if (wolfThrowers != null)
                {
                    foreach (var w in wolfThrowers) if (w != null) w.gameObject.SetActive(false);
                }
            }
            else
            {
                // เปิดหมาป่ากิ่งไม้ ปิดบอสยอดไม้
                if (bossWolf != null) bossWolf.gameObject.SetActive(false);
                if (wolfThrowers != null)
                {
                    foreach (var w in wolfThrowers) if (w != null) w.gameObject.SetActive(true);
                }
            }

            // 2. ลูปปล่อยผลไม้ 20 ลูก
            while (fruitsSpawned < FruitBonusCount)
            {
                if (bonusStyle == BonusStageMode.FamicomTopBoss && bossWolf != null)
                {
                    // บอสวิ่งด้านบนแล้วหย่อนผลไม้ร่วงลงมา!
                    bossWolf.DropFruit(() =>
                    {
                        SpawnFamicomFruit(bossWolf.DropPosition);
                    });
                }
                else if (wolfThrowers != null && wolfThrowers.Length > 0)
                {
                    // หมาป่าบนกิ่งไม้สลับกันปาวิถีโค้ง
                    var thrower = wolfThrowers[fruitsSpawned % wolfThrowers.Length];
                    if (thrower != null)
                    {
                        thrower.Throw(() =>
                        {
                            SpawnArcadeFruit(thrower.ThrowPosition);
                        });
                    }
                    else
                    {
                        SpawnArcadeFruit();
                    }
                }
                else
                {
                    SpawnArcadeFruit();
                }

                fruitsSpawned++;
                yield return new WaitForSeconds(fruitSpawnInterval);
            }

            // รอแป๊บนึงให้ผลไม้ลูกสุดท้ายตกพ้นจอ
            yield return new WaitForSeconds(3f);

            // ถ้าเก็บครบ 20 ลูก ได้ Perfect Bonus 1 หมื่นแต้ม!
            if (fruitsHit >= FruitBonusCount)
            {
                ScoreManager.Instance?.AddScore(PerfectBonusScore);
                Debug.Log("PERFECT BONUS! +10,000 pts! โหดจัดดด");
            }

            // ซ่อนบอสเมื่อจบด่าน
            if (bossWolf != null) bossWolf.gameObject.SetActive(false);
        }
        else
        {
            // --- โบนัสเนื้อ (Meat Bonus Stage ตามต้นฉบับ NES หลังด่าน 2) ---
            // 1. ซ่อนบอสโยนผลไม้และหมาป่าปาผลไม้ (เพราะรอบนี้เป็นโบนัสปาเนื้อ!)
            if (bossWolf != null) bossWolf.gameObject.SetActive(false);
            if (wolfThrowers != null)
            {
                foreach (var w in wolfThrowers) if (w != null) w.gameObject.SetActive(false);
            }

            // 2. ล็อกให้แม่หมูยิงลูกธนูไม่ได้ ได้แค่หยิบและปาเนื้ออย่างเดียว!
            if (player != null) player.SetMeatBonusMode(true);

            var spawner = FindAnyObjectByType<WolfSpawner>();
            int spawnCounter = 0;
            float elapsed = 0f;
            float spawnStep = 1.6f;

            while (elapsed < bonusDuration)
            {
                SpawnMeatBonusWolf(spawner, spawnCounter);
                spawnCounter++;

                // ทุกๆ 2 ตัว ให้ปล่อยลูกโป่งเปล่าลอยขึ้นมาด้วย เพื่อให้ปาเนื้อชนเด้งทำคอมโบตามต้นฉบับ NES!
                if (spawnCounter % 2 == 0 && spawner != null)
                {
                    spawner.SpawnStandaloneBalloonAtLane(-1, Random.Range(0.95f, 1.25f));
                }

                if (player != null && player.State == PlayerState.Normal && !player.IsMeatAvailable)
                {
                    player.SetMeatAvailable(true);
                }

                yield return new WaitForSeconds(spawnStep);
                elapsed += spawnStep;
            }

            // รอให้หมาป่าชุดสุดท้ายลอยขึ้นหรือโดนเนื้อก่อนจบโบนัส
            yield return new WaitForSeconds(2.5f);
            if (player != null) player.SetMeatBonusMode(false);
        }

        // จบโบนัสแล้วจ้า
        GameManager.Instance?.CompleteBonusStage();
    }

    /// <summary>
    /// ปล่อยผลไม้แบบ Famicom (ทิ้งดิ่งลงมาจากบอสบนยอดไม้)
    /// </summary>
    private void SpawnFamicomFruit(Vector3 spawnPos)
    {
        GameObject fruit = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
        var bonusFruit = fruit.GetComponent<BonusFruit>();
        if (bonusFruit == null) bonusFruit = fruit.AddComponent<BonusFruit>();

        // ทิ้งลงมาตรงๆ หรือมีความเร็วเฉื่อยแนวนอนเล็กน้อยตามการวิ่งของบอส
        Vector2 dropVel = new Vector2(Random.Range(-0.3f, 0.3f), -3.2f);
        bonusFruit.Initialize(GetFamicomFruitType(currentChain), dropVel, isArc: false);
        AudioManager.Instance?.PlayFruitDrop();
    }

    /// <summary>
    /// ปล่อยผลไม้แบบ Arcade (ปาวิถีโค้งจากซ้ายไปขวา)
    /// </summary>
    private void SpawnArcadeFruit(Vector3? customSpawnPos = null)
    {
        Vector3 spawnPos = customSpawnPos.HasValue ? customSpawnPos.Value : new Vector3(Mathf.Min(fruitSpawnXMin, -5.5f), Random.Range(-1.5f, 2f), 0f);
        GameObject fruit = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
        var bonusFruit = fruit.GetComponent<BonusFruit>();
        if (bonusFruit == null) bonusFruit = fruit.AddComponent<BonusFruit>();

        float targetX = Random.Range(Mathf.Max(fruitSpawnXMax, 2.5f), 5.5f);
        float horizontalSpeed = Mathf.Lerp(5.0f, 7.5f, (targetX - 2.5f) / 3.0f);
        float verticalSpeed = Random.Range(6.0f, 9.5f);
        Vector2 launchVel = new Vector2(horizontalSpeed, verticalSpeed);

        bonusFruit.Initialize(RandomFruitType(), launchVel, isArc: true);
        AudioManager.Instance?.PlayFruitDrop();
    }

    private BonusFruitType GetFamicomFruitType(int chain)
    {
        // ยิ่งทำคอมโบสูง ชนิดผลไม้ก็เปลี่ยนไป
        if (chain == 0) return BonusFruitType.Strawberry;
        if (chain == 1) return BonusFruitType.Cherry;
        return BonusFruitType.Peach;
    }

    private BonusFruitType RandomFruitType()
    {
        float r = Random.value;
        if (r < 0.45f) return BonusFruitType.Strawberry;
        if (r < 0.75f) return BonusFruitType.Cherry;
        return BonusFruitType.Peach;
    }

    private void SpawnMeatBonusWolf(WolfSpawner spawner, int index)
    {
        Wolf targetPrefab = wolfPrefab != null ? wolfPrefab : (spawner != null ? spawner.WolfPrefab : null);
        if (targetPrefab == null) return;

        Transform[] lanes = (meatBonusSpawnPoints != null && meatBonusSpawnPoints.Length > 0)
            ? meatBonusSpawnPoints
            : (spawner != null ? spawner.FlyPoints : null);

        int laneCount = (lanes != null && lanes.Length > 0) ? lanes.Length : 5;
        int laneIndex = Random.Range(0, laneCount);

        float[] laneXCoords = new float[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes != null && lanes.Length > i && lanes[i] != null)
                laneXCoords[i] = lanes[i].position.x;
            else
                laneXCoords[i] = -4.8f + (1.25f * i);
        }

        float dropX = laneXCoords[laneIndex];
        float startX = (spawner != null && spawner.BottomSpawnArea != null) ? spawner.BottomSpawnArea.position.x : -7.5f;
        float startY = (spawner != null && spawner.BottomSpawnArea != null) ? spawner.BottomSpawnArea.position.y : -3.8f;

        var config = LevelManager.Instance?.CurrentConfig ?? ScriptableObject.CreateInstance<LevelConfig>();
        var wolf = Instantiate(targetPrefab, new Vector3(startX, startY, 0f), Quaternion.identity);

        // ในด่านโบนัสเนื้อ (ตามต้นฉบับ NES หลังด่าน 2): หมาป่าลอยขึ้นจากด้านล่างด้วยลูกโป่งสีฟ้าอ่อนล้วน (1 HP) ไม่ถือโล่ ไม่ปาหิน
        float bonusFloatSpeed = Random.Range(0.85f, 1.15f);
        wolf.Initialize(config, descending: false, speed: bonusFloatSpeed, shield: false, rockThrow: false, dropX: dropX, lanes: laneXCoords, laneIndex: laneIndex, overrideBalloonHP: 1, overrideColorType: BalloonColorType.LightBlue);
        wolf.DisableTeaseBounces();
    }
}
