using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตัวเสกหมาป่าตามคอนฟิกของด่าน - ด่านคี่เสกจากข้างบน ด่านคู่เสกจากข้างล่าง
/// </summary>
public class WolfSpawner : MonoBehaviour
{
    [Header("Prefabs (พวกตัวต้นแบบ)")]
    [SerializeField] private Wolf wolfPrefab;
    [SerializeField] private BossWolf bossWolfPrefab;

    [Header("Spawn Points (จุดเกิด)")]
    [SerializeField] private Transform topSpawnArea;
    [SerializeField] private Transform bottomSpawnArea;
    [SerializeField] private Transform[] dropPoints; // สำหรับด่าน 1 (ลอยลงมา)
    [SerializeField] private Transform[] flyPoints;  // สำหรับด่าน 2 (ลอยขึ้น)
    [SerializeField] private float spawnXMin = -2f;
    [SerializeField] private float spawnXMax = 2f;

    [Header("Meat Spawn (เสกเนื้อ)")]
    [SerializeField] private float meatSpawnDelay = 2f;

    [Header("Debug Formations (รูปแบบแถวสำหรับเทส)")]
    [Tooltip("เปิดระบบจำลองรูปแบบการจัดแถว (Wave Pattern)")]
    public bool useDebugFormations = false;
    public bool allowSingle = true;
    public bool allowDoubleSameLane = true;
    public bool allowDiagonal = true;
    public bool allowHorizontal = true;
    
    [Header("Debug Patterns (รูปแบบหมาป่าสำหรับเทส)")]
    [Tooltip("เปิดใช้งานระบบจำลองรูปแบบหมาป่า")]
    public bool useDebugPatterns = false;
    public bool allowNormalWolf = true;
    public bool allowShieldWolf = true;
    public bool allowRockWolf = true;
    public bool allowShieldAndRockWolf = true;

    [Header("Debug Gameplay")]
    public bool debugRapidFireRocks = false;

    public Wolf WolfPrefab => wolfPrefab;
    public Transform[] FlyPoints => flyPoints;
    public Transform TopSpawnArea => topSpawnArea;
    public Transform BottomSpawnArea => bottomSpawnArea;

    private int wolvesSpawned; // เกิดมากี่ตัวแล้ว
    private int wolvesToSpawn; // ต้องเกิดทั้งหมดกี่ตัว
    private bool bossSpawned; // บอสเกิดรึยัง
    private bool spawning; // กำลังเสกอยู่มั้ย
    private Coroutine spawnRoutine;
    private Coroutine meatRoutine;
    private Coroutine strayBalloonRoutine;
    private PlayerController player;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        }
    }

    // ตอนเปลี่ยนสถานะเกม ถ้าไม่ได้เล่นอยู่หรืออยู่ด่านโบนัส ก็หยุดเสกซะ
    private void OnStateChanged(GameState state)
    {
        if (state != GameState.Playing && state != GameState.BonusStage)
            StopAllSpawning();
    }

    // เริ่มการเสกหมาป่า
    public void StartSpawning()
    {
        var config = LevelManager.Instance?.GetConfigForStage(GameManager.Instance?.CurrentStage ?? 1);
        if (config == null) 
        {
            Debug.LogError("StartSpawning: config is null! งานงอก หาคอนฟิกไม่เจอ!");
            return;
        }

        Debug.Log($"StartSpawning เริ่มเสกแล้วนะ! ต้องเสกทั้งหมด: {config.wolfCount}");

        wolvesSpawned = 0;
        bossSpawned = false;
        wolvesToSpawn = config.wolfCount;
        spawning = true;

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (meatRoutine != null) StopCoroutine(meatRoutine);
        if (strayBalloonRoutine != null) StopCoroutine(strayBalloonRoutine);

        spawnRoutine = StartCoroutine(SpawnWaveRoutine(config));
        meatRoutine = StartCoroutine(MeatSpawnRoutine(config));

        // ในด่านคู่ (Stage 2, 4, ... ลอยขึ้น): มีลูกโป่งเปล่าลอยขึ้นมาจากด้านล่างเป็นระยะตามต้นฉบับ NES
        if (GameManager.Instance != null && !GameManager.Instance.IsOddStage)
        {
            strayBalloonRoutine = StartCoroutine(StrayBalloonRoutine());
        }
    }

    // หยุดเสกทั้งหมด
    public void StopAllSpawning()
    {
        Debug.Log("StopAllSpawning สั่งหยุดเสก");
        spawning = false;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (meatRoutine != null) StopCoroutine(meatRoutine);
        if (strayBalloonRoutine != null) StopCoroutine(strayBalloonRoutine);
        player?.SetMeatAvailable(false); // ปิดการใช้เนื้อด้วย
    }

    // ลูปหลักในการเสกหมาป่า
    private IEnumerator SpawnWaveRoutine(LevelConfig config)
    {
        Debug.Log("SpawnWaveRoutine เริ่มทำงาน");
        yield return new WaitForSeconds(1f); // รอแป๊บนึงก่อนดรอป

        int laneCount = (dropPoints != null && dropPoints.Length > 0) ? dropPoints.Length : ((flyPoints != null && flyPoints.Length > 0) ? flyPoints.Length : 5);
        bool hasStageBoss = GameManager.Instance != null && !GameManager.Instance.IsOddStage && config.hasBossWolf;

        while (spawning)
        {
            // ดูว่าเหลือหมาป่าต้องจัดการอีกกี่ตัว
            int remainingToKill = WolfTracker.Instance != null ? WolfTracker.Instance.RemainingKills : wolvesToSpawn - wolvesSpawned;
            if (remainingToKill <= 0) break; // ครบแล้วจ้า พอ!

            // คำนวณจำนวนหมาป่าที่เสกออกมาแล้วและยังถือโควต้าคิลอยู่ (รวมทั้งตัวที่กำลังลอย และตัวที่กำลังร่วงลงพื้นแต่ยังไม่ทันนับแต้ม)
            // เพื่อไม่ให้เสกหมาป่าธรรมดาเกินโควต้าระหว่างที่หมาป่ากำลังร่วงลงพื้น!
            int inFlightQuota = WolfTracker.Instance != null ? WolfTracker.Instance.InFlightQuotaCount : 0;
            int activeTargets = WolfTracker.Instance != null ? WolfTracker.Instance.ActiveTargetCount : 0;
            int neededToSpawn = remainingToKill - inFlightQuota;

            // ถ้ามีหมาป่าอยู่ในฉากครบตามจำนวนที่ต้องฆ่าแล้ว -> รอผลลัพธ์ ไม่เสกเกินโควต้า
            // ถ้าหมาป่าหนีรอดไปได้ inFlightQuota จะลดลง ทำให้ neededToSpawn > 0 แล้วเสกตัวใหม่มาแทนที่ทันที!
            if (neededToSpawn <= 0)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            float baseWait = Mathf.Max(2f, config.spawnInterval); // กันไว้ อย่างน้อยต้องห่างกัน 2 วิ
            float waitAfterWave = baseWait;

            // สำหรับด่านคู่ที่มีบอส (เช่น ด่าน 2 Spring):
            // ล็อกโควต้าหมาป่า "1 ตัวสุดท้าย" ไว้ให้เป็น BossWolf เสมอ!
            int spawnableNormalCount = neededToSpawn;
            if (hasStageBoss && !bossSpawned)
            {
                spawnableNormalCount = neededToSpawn - 1;

                // ถ้าเสกหมาป่าธรรมดาครบโควต้าแล้ว (เหลือโควต้าตัวสุดท้ายสำหรับบอส)
                if (spawnableNormalCount <= 0)
                {
                    // รอให้หมาป่าธรรมดาที่ลอยอยู่โดนยิงร่วง หรือหนีขึ้นหน้าผาให้ชัดเจนก่อน
                    // (ถ้ามีตัวหนีขึ้นหน้าผา ในรอบถัดไป spawnableNormalCount จะกลับมา > 0 เพื่อเสกตัวธรรมดามาซ่อมก่อน)
                    if (activeTargets > 0)
                    {
                        yield return new WaitForSeconds(0.25f);
                        continue;
                    }

                    // หมาป่าธรรมดาถูกจัดการครบหมดแล้ว เหลือตัวสุดท้ายของด่าน -> ปล่อยบอสหมาป่า!
                    int bossLane = Random.Range(0, laneCount);
                    player?.SetBossApproaching(true);
                    SpawnWolf(config, isBoss: true, bossLane);
                    wolvesSpawned++;
                    bossSpawned = true;

                    yield return new WaitForSeconds(baseWait);
                    continue;
                }
            }

            // ถ้ามีหมาป่าในฉากเกิน 10 ตัว ให้รอก่อน อย่าเพิ่งเสกเยอะไปเดี๋ยวแลคและยากไป
            int activeWolves = FindObjectsByType<Wolf>(FindObjectsInactive.Exclude).Length;
            if (activeWolves > 10) 
            {
                yield return new WaitForSeconds(config.spawnInterval);
                continue;
            }

            // สุ่มรูปแบบการดรอปของหมาป่าธรรมดา
            // 0: ตัวเดียว (50%)
            // 1: สองตัวซ้อนเลนเดียวกัน (20%)
            // 2: ทแยงมุม (20%)
            // 3: แนวขวางหน้ากระดาน (10%)
            int pattern = 0;
            
            // โค้ดส่วนสุ่มรูปแบบ (รวมระบบดีบัคด้วย)
            if (useDebugFormations)
            {
                List<int> valid = new List<int>();
                if (allowSingle) valid.Add(0);
                if (allowDoubleSameLane) valid.Add(1);
                if (allowDiagonal) valid.Add(2);
                if (allowHorizontal) valid.Add(3);
                
                if (valid.Count == 0) valid.Add(0); // กันพัง

                bool found = false;
                int attempts = 0;
                while (!found && attempts < 50)
                {
                    float r = Random.value;
                    if (r < 0.50f) pattern = 0;
                    else if (r < 0.70f) pattern = 1;
                    else if (r < 0.90f) pattern = 2;
                    else pattern = 3;
                    
                    if (valid.Contains(pattern)) found = true;
                    attempts++;
                }
                if (!found) pattern = valid[Random.Range(0, valid.Count)];
            }
            else
            {
                float r = Random.value;
                if (r < 0.50f) pattern = 0;
                else if (r < 0.70f) pattern = 1;
                else if (r < 0.90f) pattern = 2;
                else pattern = 3;
            }

            // บังคับรูปแบบตามจำนวนหมาป่าธรรมดาที่อนุญาตให้เสกได้ (spawnableNormalCount)
            if (spawnableNormalCount == 1) pattern = 0; // ต้องการอีกตัวเดียว ก็ปล่อยตัวเดียวพอดีเป๊ะ
            if (spawnableNormalCount == 2 && pattern == 3) pattern = 1; // เหลือสองตัว ไม่เอาหน้ากระดาน

            if (pattern == 0) // ลงมาตัวเดียวเหงาๆ
            {
                int lane = Random.Range(0, laneCount);
                SpawnWolf(config, isBoss: false, lane);
                wolvesSpawned++;
                waitAfterWave = baseWait;
            }
            else if (pattern == 1) // สองตัวลงเลนเดียวกัน! งานหนักเลย
            {
                int lane = Random.Range(0, laneCount);
                int count = Mathf.Min(2, spawnableNormalCount);
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                for (int i = 0; i < count; i++)
                {
                    Wolf w = SpawnWolf(config, isBoss: false, lane, sharedSpeed);
                    if (w != null) w.DisableTeaseBounces(); // บังคับให้ไม่เดินหนีหลอกล่อ
                    wolvesSpawned++;
                    
                    // ทิ้งระยะห่างให้พอดีกันหน่อย
                    if (i < count - 1) yield return new WaitForSeconds(2.0f / sharedSpeed); 
                }
                waitAfterWave = baseWait * 1.2f;
            }
            else if (pattern == 2) // ทแยงมุม ลงมาเป็นสเต็ป
            {
                int count = Random.Range(2, 4); // มา 2-3 ตัว
                count = Mathf.Min(count, spawnableNormalCount);
                
                bool leftToRight = Random.value > 0.5f; // ซ้ายไปขวา หรือ ขวาไปซ้าย
                int startLane = leftToRight ? 0 : laneCount - 1;
                int step = leftToRight ? 1 : -1;
                
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                for (int i = 0; i < count; i++)
                {
                    int lane = startLane + (i * step);
                    if (lane < 0 || lane >= laneCount) break; // เลยขอบเลนก็พอ
                    
                    Wolf w = SpawnWolf(config, isBoss: false, lane, sharedSpeed);
                    if (w != null) w.DisableTeaseBounces();
                    wolvesSpawned++;
                    
                    if (i < count - 1) yield return new WaitForSeconds(0.5f); // ทิ้งช่วงนิดนึง
                }
                waitAfterWave = baseWait * 1.5f; // พักให้ผู้เล่นหายใจหน่อย
            }
            else if (pattern == 3) // หน้ากระดานเรียงหนึ่ง น่ากลัวสุดๆ
            {
                int count = Random.Range(2, 4); // 2-3 ตัว
                count = Mathf.Min(count, spawnableNormalCount);
                
                int startL = Random.Range(0, laneCount - count + 1);
                List<int> selectedLanes = new List<int>();
                for (int i = 0; i < count; i++) selectedLanes.Add(startL + i);
                
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                
                bool descending = GameManager.Instance == null || GameManager.Instance.IsOddStage;
                Transform area = descending ? topSpawnArea : bottomSpawnArea;
                float baseStartX = area != null ? area.position.x : -10f;
                
                Transform[] currentPoints = descending ? dropPoints : flyPoints;
                float[] laneXCoords = new float[laneCount];
                if (currentPoints != null && currentPoints.Length > 0)
                {
                    for (int i = 0; i < laneCount; i++) laneXCoords[i] = currentPoints[i].position.x;
                }
                else
                {
                    float rDist = spawnXMax - spawnXMin;
                    for (int i = 0; i < laneCount; i++) laneXCoords[i] = spawnXMin + rDist * (i / (float)(laneCount - 1));
                }

                int furthestLane = selectedLanes[selectedLanes.Count - 1];
                float furthestX = laneXCoords[furthestLane];

                for (int i = 0; i < count; i++)
                {
                    int lane = selectedLanes[i];
                    float targetX = laneXCoords[lane];
                    float spawnX = targetX - furthestX + baseStartX; // คำนวณจุดเกิดให้พอดีกัน
                    
                    Wolf w = SpawnWolf(config, isBoss: false, lane, sharedSpeed, spawnX);
                    if (w != null) w.DisableTeaseBounces();
                    wolvesSpawned++;
                }
                waitAfterWave = baseWait * 1.8f; // แจกชุดใหญ่ไปแล้ว ต้องให้พักนานหน่อย
            }

            // รอชุดถัดไป แต่ถ้าผู้เล่นจัดการหมาป่าบนจอหมดเกลี้ยงแล้ว ให้ปล่อยชุดถัดไป (หรือบอส) ออกมาได้ไวขึ้น!
            float waited = 0f;
            while (waited < waitAfterWave && spawning)
            {
                yield return new WaitForSeconds(0.25f);
                waited += 0.25f;
                if (waited >= 0.75f && WolfTracker.Instance != null && WolfTracker.Instance.ActiveTargetCount == 0)
                    break;
            }
        }
        Debug.Log($"SpawnWaveRoutine จบแล้ว! spawning={spawning}, เกิดทั้งหมด={wolvesSpawned}");
    }

    // ลูปเสกเนื้อ (ของกินให้แม่หมูใช้จัดการหมาป่า)
    private IEnumerator MeatSpawnRoutine(LevelConfig config)
    {
        yield return new WaitForSeconds(meatSpawnDelay);

        int nextMeatKillTarget = 0;
        float cooldownAfterThrow = 0f;

        while (spawning)
        {
            int remainingToKill = WolfTracker.Instance != null ? WolfTracker.Instance.RemainingKills : wolvesToSpawn - wolvesSpawned;

            // บอสมาแล้ว (หรือเหลือบอสตัวสุดท้ายตัวเดียว) ไม่ให้เนื้อใหม่บนรอกตามต้นฉบับ NES!
            if (!GameManager.Instance.IsOddStage && config.hasBossWolf && (bossSpawned || remainingToKill <= 1))
            {
                player?.SetBossApproaching(true);
                yield break;
            }

            // เช็คว่าเนื้อยังไม่มีบนรอก แม่หมูไม่ได้ถือเนื้ออยู่ และไม่มีก้อนเนื้อกำลังลอยอยู่ในฉาก
            if (player != null && !player.IsMeatAvailable && player.State != PlayerState.HoldingMeat)
            {
                bool meatFlying = GameObject.FindWithTag(GameConstants.TagMeat) != null;
                if (!meatFlying)
                {
                    cooldownAfterThrow += 0.5f;
                    int currentKills = WolfTracker.Instance != null ? WolfTracker.Instance.Kills : wolvesSpawned;
                    if ((currentKills >= nextMeatKillTarget || cooldownAfterThrow >= 4.5f) && remainingToKill > 0)
                    {
                        player.SetMeatAvailable(true);
                        nextMeatKillTarget = currentKills + 4; // ฆ่า 4 ตัว หรือรอครบจังหวะ ลูกหมูจะวางเนื้อชิ้นใหม่ให้บนรอก
                        cooldownAfterThrow = 0f;
                    }
                }
            }
            else
            {
                cooldownAfterThrow = 0f;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // ฟังก์ชันสร้างหมาป่า 1 ตัว
    private Wolf SpawnWolf(LevelConfig config, bool isBoss, int laneIndex, float? forcedSpeed = null, float? startXOverride = null)
    {
        bool descending = GameManager.Instance == null || GameManager.Instance.IsOddStage; // ด่านคี่ลอยลงมา

        Transform area = descending ? topSpawnArea : bottomSpawnArea;
        float startX = startXOverride.HasValue ? startXOverride.Value : (area != null ? area.position.x : -10f);
        float baseY = area != null ? area.position.y : (descending ? 4f : -4f);

        Transform[] currentPoints = descending ? dropPoints : flyPoints;
        int laneCount = (currentPoints != null && currentPoints.Length > 0) ? currentPoints.Length : 5;
        
        float[] laneXCoords = new float[laneCount];
        if (currentPoints != null && currentPoints.Length > 0)
        {
            for (int i = 0; i < laneCount; i++) laneXCoords[i] = currentPoints[i].position.x;
        }
        else
        {
            float r = spawnXMax - spawnXMin;
            for (int i = 0; i < laneCount; i++) laneXCoords[i] = spawnXMin + r * (i / (float)(laneCount - 1));
        }

        float dropX = laneXCoords[laneIndex];
        Vector3 pos = new Vector3(startX, baseY, 0f); // จุดเกิดจริงๆ

        // สุ่มความเร็ว หรือใช้ค่าที่บังคับมา
        float speed = forcedSpeed.HasValue ? forcedSpeed.Value : Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
        
        // สุ่มของสวมใส่ โล่หรือปาหิน
        bool shield = Random.value < config.shieldRatio;
        bool rock = Random.value < config.rockThrowRatio;

        // ระบบดีบัคของสวมใส่ เอาไว้เทส
        if (useDebugPatterns)
        {
            if (!allowNormalWolf && !allowShieldWolf && !allowRockWolf && !allowShieldAndRockWolf)
            {
                shield = false; rock = false; // กันพัง
            }
            else
            {
                bool isAllowed = false;
                int maxAttempts = 100;
                while (!isAllowed && maxAttempts > 0)
                {
                    shield = Random.value < config.shieldRatio;
                    rock = Random.value < config.rockThrowRatio;
                    
                    if (!shield && !rock && allowNormalWolf) isAllowed = true;
                    if (shield && !rock && allowShieldWolf) isAllowed = true;
                    if (!shield && rock && allowRockWolf) isAllowed = true;
                    if (shield && rock && allowShieldAndRockWolf) isAllowed = true;
                    
                    maxAttempts--;
                }
                
                // ถ้าสุ่มจนท้อแล้วยังไม่ได้ ก็สุ่มจากที่อนุญาตให้ตรงๆ เลย
                if (!isAllowed)
                {
                    List<int> valid = new List<int>();
                    if (allowNormalWolf) valid.Add(0);
                    if (allowShieldWolf) valid.Add(1);
                    if (allowRockWolf) valid.Add(2);
                    if (allowShieldAndRockWolf) valid.Add(3);
                    
                    int choice = valid[Random.Range(0, valid.Count)];
                    shield = (choice == 1 || choice == 3);
                    rock = (choice == 2 || choice == 3);
                }
            }
        }

        // เสกบอส! (รองรับทั้งกรณีใส่ bossWolfPrefab ไว้ และกรณีลืมใส่ให้ใช้ wolfPrefab แปลงเป็นบอสอัตโนมัติ)
        if (isBoss)
        {
            if (bossWolfPrefab != null)
            {
                var boss = Instantiate(bossWolfPrefab, pos, Quaternion.identity);
                boss.Initialize(config, descending, speed, true, rock, dropX, laneXCoords, laneIndex);
                if (debugRapidFireRocks) boss.SetRapidFire(true);
                WolfTracker.Instance?.Register(boss);
                return boss;
            }
            else if (wolfPrefab != null)
            {
                var bossObj = Instantiate(wolfPrefab.gameObject, pos, Quaternion.identity);
                var oldWolf = bossObj.GetComponent<Wolf>();
                var boss = bossObj.AddComponent<BossWolf>();
                if (oldWolf != null)
                {
                    oldWolf.CopySettingsTo(boss);
                    DestroyImmediate(oldWolf);
                }
                boss.Initialize(config, descending, speed, true, rock, dropX, laneXCoords, laneIndex);
                if (debugRapidFireRocks) boss.SetRapidFire(true);
                WolfTracker.Instance?.Register(boss);
                return boss;
            }
        }

        // เสกหมาป่าปกติ
        if (wolfPrefab == null) return null;
        var wolf = Instantiate(wolfPrefab, pos, Quaternion.identity);
        wolf.Initialize(config, descending, speed, shield, rock, dropX, laneXCoords, laneIndex);
        if (debugRapidFireRocks) wolf.SetRapidFire(true);
        WolfTracker.Instance?.Register(wolf);
        return wolf;
    }

    // ลูปปล่อยลูกโป่งเปล่าลอยขึ้นมาจากด้านล่างในด่านคู่ (Stage 2, 4, ...) ตามต้นฉบับ NES
    private IEnumerator StrayBalloonRoutine()
    {
        yield return new WaitForSeconds(4.5f);
        while (spawning)
        {
            if (GameManager.Instance == null || !GameManager.Instance.DeathFreeze)
            {
                SpawnStandaloneBalloonAtLane(-1, Random.Range(0.95f, 1.25f));
            }
            yield return new WaitForSeconds(Random.Range(6.0f, 9.0f));
        }
    }

    /// <summary>
    /// เสกลูกโป่งเปล่า 1 ลูกลอยขึ้นมาจากด้านล่างในเลนที่กำหนด (ถ้า laneIndex < 0 จะสุ่มเลนให้)
    /// </summary>
    public Balloon SpawnStandaloneBalloonAtLane(int laneIndex = -1, float floatUpSpeed = 1.1f)
    {
        if (wolfPrefab == null) return null;

        Balloon templateBalloon = wolfPrefab.GetComponentInChildren<Balloon>(true);
        if (templateBalloon == null) return null;

        int laneCount = (flyPoints != null && flyPoints.Length > 0) ? flyPoints.Length : 5;
        if (laneIndex < 0 || laneIndex >= laneCount)
            laneIndex = Random.Range(0, laneCount);

        float spawnX = 0f;
        float spawnY = bottomSpawnArea != null ? bottomSpawnArea.position.y : -3.8f;

        if (flyPoints != null && flyPoints.Length > laneIndex && flyPoints[laneIndex] != null)
        {
            spawnX = flyPoints[laneIndex].position.x;
            spawnY = flyPoints[laneIndex].position.y;
        }
        else
        {
            float r = spawnXMax - spawnXMin;
            spawnX = spawnXMin + r * (laneIndex / (float)Mathf.Max(1, laneCount - 1));
        }

        var balloonObj = Instantiate(templateBalloon.gameObject, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
        balloonObj.name = "StrayBalloon";
        balloonObj.transform.localScale = templateBalloon.transform.localScale;
        balloonObj.SetActive(true);

        var balloon = balloonObj.GetComponent<Balloon>();
        if (balloon != null)
        {
            // สุ่มสีชมพู (1 HP) หรือสีเหลืองให้ดูมีสีสัน แต่ยิงนัดเดียวแตก (1 HP) และเด้งเนื้อได้
            balloon.InitializeStandalone(floatUpSpeed, hp: 1);
        }
        return balloon;
    }

    public void ResetBossSpawned()
    {
        bossSpawned = false;
    }

    public void OnWolfDestroyed()
    {
        // เผื่อเอาไว้ดักตอนหมาป่าตาย เพื่อเช็คจบด่าน (ตอนนี้ยังไม่ได้ใช้ ปล่อยว่างไปก่อน)
    }
}



