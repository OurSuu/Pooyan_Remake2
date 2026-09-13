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

    private int wolvesSpawned; // เกิดมากี่ตัวแล้ว
    private int wolvesToSpawn; // ต้องเกิดทั้งหมดกี่ตัว
    private bool bossSpawned; // บอสเกิดรึยัง
    private bool spawning; // กำลังเสกอยู่มั้ย
    private Coroutine spawnRoutine;
    private Coroutine meatRoutine;
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

        spawnRoutine = StartCoroutine(SpawnWaveRoutine(config));
        meatRoutine = StartCoroutine(MeatSpawnRoutine(config));
    }

    // หยุดเสกทั้งหมด
    public void StopAllSpawning()
    {
        Debug.Log("StopAllSpawning สั่งหยุดเสก");
        spawning = false;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (meatRoutine != null) StopCoroutine(meatRoutine);
        player?.SetMeatAvailable(false); // ปิดการใช้เนื้อด้วย
    }

    // ลูปหลักในการเสกหมาป่า
    private IEnumerator SpawnWaveRoutine(LevelConfig config)
    {
        Debug.Log("SpawnWaveRoutine เริ่มทำงาน");
        yield return new WaitForSeconds(1f); // รอแป๊บนึงก่อนดรอป

        int laneCount = (dropPoints != null && dropPoints.Length > 0) ? dropPoints.Length : 5;

        while (spawning)
        {
            // ดูว่าเหลือหมาป่าต้องจัดการอีกกี่ตัว
            int remainingToKill = WolfTracker.Instance != null ? WolfTracker.Instance.RemainingKills : wolvesToSpawn - wolvesSpawned;
            if (remainingToKill <= 0) break; // ครบแล้วจ้า พอ!

            // ถ้ามีหมาป่าในฉากเกิน 10 ตัว ให้รอก่อน อย่าเพิ่งเสกเยอะไปเดี๋ยวแลคและยากไป
            int activeWolves = FindObjectsByType<Wolf>(FindObjectsInactive.Exclude).Length;
            if (activeWolves > 10) 
            {
                yield return new WaitForSeconds(config.spawnInterval);
                continue;
            }

            float baseWait = Mathf.Max(2f, config.spawnInterval); // กันไว้ อย่างน้อยต้องห่างกัน 2 วิ
            float waitAfterWave = baseWait;

            // สุ่มรูปแบบการดรอปของหมาป่า
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

            // บังคับรูปแบบตามจำนวนที่เหลือ
            if (remainingToKill == 1) pattern = 0; // เหลือตัวเดียวก็ลงตัวเดียวสิ
            if (remainingToKill == 2 && pattern == 3) pattern = 1; // เหลือสองตัว ไม่เอาหน้ากระดาน

            if (pattern == 0) // ลงมาตัวเดียวเหงาๆ
            {
                int lane = Random.Range(0, laneCount);
                // เช็คว่าเป็นบอสได้ไหม
                bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill <= 2;
                if (isBoss) player?.SetBossApproaching(true); // เตือนผู้เล่นว่าบอสมาแล้ว!
                
                SpawnWolf(config, isBoss, lane);
                wolvesSpawned++;
                if (isBoss) bossSpawned = true;
                
                waitAfterWave = baseWait;
            }
            else if (pattern == 1) // สองตัวลงเลนเดียวกัน! งานหนักเลย
            {
                int lane = Random.Range(0, laneCount);
                int count = Mathf.Min(2, remainingToKill);
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                for (int i = 0; i < count; i++)
                {
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    
                    Wolf w = SpawnWolf(config, isBoss, lane, sharedSpeed);
                    if (w != null) w.DisableTeaseBounces(); // บังคับให้ไม่เดินหนีหลอกล่อ
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                    
                    // ทิ้งระยะห่างให้พอดีกันหน่อย
                    if (i < count - 1) yield return new WaitForSeconds(2.0f / sharedSpeed); 
                }
                waitAfterWave = baseWait * 1.2f;
            }
            else if (pattern == 2) // ทแยงมุม ลงมาเป็นสเต็ป
            {
                int count = Random.Range(2, 4); // มา 2-3 ตัว
                count = Mathf.Min(count, remainingToKill);
                
                bool leftToRight = Random.value > 0.5f; // ซ้ายไปขวา หรือ ขวาไปซ้าย
                int startLane = leftToRight ? 0 : laneCount - 1;
                int step = leftToRight ? 1 : -1;
                
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                for (int i = 0; i < count; i++)
                {
                    int lane = startLane + (i * step);
                    if (lane < 0 || lane >= laneCount) break; // เลยขอบเลนก็พอ
                    
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    
                    Wolf w = SpawnWolf(config, isBoss, lane, sharedSpeed);
                    if (w != null) w.DisableTeaseBounces();
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                    
                    if (i < count - 1) yield return new WaitForSeconds(0.5f); // ทิ้งช่วงนิดนึง
                }
                waitAfterWave = baseWait * 1.5f; // พักให้ผู้เล่นหายใจหน่อย
            }
            else if (pattern == 3) // หน้ากระดานเรียงหนึ่ง น่ากลัวสุดๆ
            {
                int count = Random.Range(2, 4); // 2-3 ตัว
                count = Mathf.Min(count, remainingToKill);
                
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
                    
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    
                    Wolf w = SpawnWolf(config, isBoss, lane, sharedSpeed, spawnX);
                    if (w != null) w.DisableTeaseBounces();
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                }
                waitAfterWave = baseWait * 1.8f; // แจกชุดใหญ่ไปแล้ว ต้องให้พักนานหน่อย
            }

            yield return new WaitForSeconds(waitAfterWave); // รอชุดถัดไป
        }
        Debug.Log($"SpawnWaveRoutine จบแล้ว! spawning={spawning}, เกิดทั้งหมด={wolvesSpawned}");
    }

    // ลูปเสกเนื้อ (ของกินให้แม่หมูใช้จัดการหมาป่า)
    private IEnumerator MeatSpawnRoutine(LevelConfig config)
    {
        yield return new WaitForSeconds(meatSpawnDelay);

        // ด่านคลาสสิกอาเขต: เนื้อจะมาเมื่อเหลือหมาป่าตามหลักทวีคูณของ 8
        // สมมติมี 32 ตัว -> เนื้อมาตอนเหลือ 24, 16, 8 ตัว
        int nextMeatKillTarget = 0;

        while (spawning)
        {
            int remainingToKill = WolfTracker.Instance != null ? WolfTracker.Instance.RemainingKills : wolvesToSpawn - wolvesSpawned;

            // บอสมาแล้ว ไม่ให้เนื้อแล้วนะ!
            if (!GameManager.Instance.IsOddStage && config.hasBossWolf && remainingToKill <= 2)
            {
                player?.SetBossApproaching(true);
                yield break;
            }

            // เช็คว่าเนื้อยังไม่มี และแม่หมูไม่ได้ถือเนื้ออยู่
            if (player != null && !player.IsMeatAvailable && player.State != PlayerState.HoldingMeat)
            {
                int currentKills = WolfTracker.Instance != null ? WolfTracker.Instance.Kills : wolvesSpawned;
                if (currentKills >= nextMeatKillTarget && remainingToKill > 0)
                {
                    player.SetMeatAvailable(true);
                    nextMeatKillTarget = currentKills + 8; // รอฆ่าอีก 8 ตัวถึงได้อันใหม่
                }
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

        // เสกบอส!
        if (isBoss && bossWolfPrefab != null)
        {
            var boss = Instantiate(bossWolfPrefab, pos, Quaternion.identity);
            boss.Initialize(config, descending, speed, true, rock, dropX, laneXCoords, laneIndex);
            WolfTracker.Instance?.Register(boss);
            return boss;
        }

        // เสกหมาป่าปกติ
        if (wolfPrefab == null) return null;
        var wolf = Instantiate(wolfPrefab, pos, Quaternion.identity);
        wolf.Initialize(config, descending, speed, shield, rock, dropX, laneXCoords, laneIndex);
        WolfTracker.Instance?.Register(wolf);
        return wolf;
    }

    public void OnWolfDestroyed()
    {
        // เผื่อเอาไว้ดักตอนหมาป่าตาย เพื่อเช็คจบด่าน (ตอนนี้ยังไม่ได้ใช้ ปล่อยว่างไปก่อน)
    }
}

