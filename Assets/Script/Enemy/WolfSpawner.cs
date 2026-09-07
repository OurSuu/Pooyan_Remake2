using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns wolves per LevelManager config â€” odd stages from top, even from bottom.
/// </summary>
public class WolfSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Wolf wolfPrefab;
    [SerializeField] private BossWolf bossWolfPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform topSpawnArea;
    [SerializeField] private Transform bottomSpawnArea;
    [SerializeField] private Transform[] dropPoints; // For Stage 1 (descending)
    [SerializeField] private Transform[] flyPoints;  // For Stage 2 (ascending)
    [SerializeField] private float spawnXMin = -2f;
    [SerializeField] private float spawnXMax = 2f;

    [Header("Meat Spawn")]
    [SerializeField] private float meatSpawnDelay = 2f;
    [SerializeField]  // Number of kills required to spawn next meat

            [Header("Debug Formations (รูปแบบแถว)")]
    [Tooltip("เปิดระบบจำลองรูปแบบการจัดแถว (Wave Pattern)")]
    public bool useDebugFormations = false;
    public bool allowSingle = true;
    public bool allowDoubleSameLane = true;
    public bool allowDiagonal = true;
    public bool allowHorizontal = true;
    [Header("Debug Patterns (รูปแบบหมาป่า)")]
    [Tooltip("เปิดใช้งานระบบจำลองรูปแบบหมาป่า")]
    public bool useDebugPatterns = false;
    public bool allowNormalWolf = true;
    public bool allowShieldWolf = true;
    public bool allowRockWolf = true;
    public bool allowShieldAndRockWolf = true;

    private int wolvesSpawned;
    private int wolvesToSpawn;
    private bool bossSpawned;
    private bool spawning;
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

    private void OnStateChanged(GameState state)
    {
        if (state != GameState.Playing && state != GameState.BonusStage)
            StopAllSpawning();
    }

    public void StartSpawning()
    {
        var config = LevelManager.Instance?.GetConfigForStage(GameManager.Instance?.CurrentStage ?? 1);
        if (config == null) 
        {
            Debug.LogError("StartSpawning: config is null!");
            return;
        }

        Debug.Log($"StartSpawning called with config.wolfCount={config.wolfCount}");

        wolvesSpawned = 0;
        bossSpawned = false;
        wolvesToSpawn = config.wolfCount;
        spawning = true;

        spawnRoutine = StartCoroutine(SpawnWaveRoutine(config));
        meatRoutine = StartCoroutine(MeatSpawnRoutine(config));
    }

    public void StopAllSpawning()
    {
        Debug.Log("StopAllSpawning called");
        spawning = false;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (meatRoutine != null) StopCoroutine(meatRoutine);
        player?.SetMeatAvailable(false);
    }

    private IEnumerator SpawnWaveRoutine(LevelConfig config)
    {
        Debug.Log("SpawnWaveRoutine started");
        yield return new WaitForSeconds(1f);

        int laneCount = (dropPoints != null && dropPoints.Length > 0) ? dropPoints.Length : 5;

        while (spawning)
        {
            int remainingToKill = WolfTracker.Instance != null ? WolfTracker.Instance.RemainingKills : wolvesToSpawn - wolvesSpawned;
            if (remainingToKill <= 0) break;

            int activeWolves = FindObjectsByType<Wolf>(FindObjectsInactive.Exclude).Length;
            if (activeWolves > 10) 
            {
                yield return new WaitForSeconds(config.spawnInterval);
                continue;
            }

            float baseWait = Mathf.Max(2f, config.spawnInterval); // Ensure at least 2s between waves
            float waitAfterWave = baseWait;

            // Pattern weights (More singles/doubles to extend stage length):
            // 0: Single (50%)
            // 1: Double Same Lane (20%)
            // 2: Diagonal (20%)
            // 3: Horizontal (10%)
                        int pattern = 0;
            
            if (useDebugFormations)
            {
                List<int> valid = new List<int>();
                if (allowSingle) valid.Add(0);
                if (allowDoubleSameLane) valid.Add(1);
                if (allowDiagonal) valid.Add(2);
                if (allowHorizontal) valid.Add(3);
                
                if (valid.Count == 0) valid.Add(0); // Fallback

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

            // Constraints
            if (remainingToKill == 1) pattern = 0;
            if (remainingToKill == 2 && pattern == 3) pattern = 1;

            if (pattern == 0) // Single Drop
            {
                int lane = Random.Range(0, laneCount);
                bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill <= 2;
                if (isBoss) player?.SetBossApproaching(true);
                SpawnWolf(config, isBoss, lane);
                wolvesSpawned++;
                if (isBoss) bossSpawned = true;
                
                waitAfterWave = baseWait;
            }
                        else if (pattern == 1) // Double Same Lane
            {
                int lane = Random.Range(0, laneCount);
                int count = Mathf.Min(2, remainingToKill);
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                for (int i = 0; i < count; i++)
                {
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    Wolf w = SpawnWolf(config, isBoss, lane, sharedSpeed);
                    if (w != null) w.DisableTeaseBounces(); // Force them not to walk away!
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                    if (i < count - 1) yield return new WaitForSeconds(2.0f / sharedSpeed); // ระยะห่างแนวตั้งที่พอดี (2.0 units)
                }
                waitAfterWave = baseWait * 1.2f;
            }
                        else if (pattern == 2) // Diagonal
            {
                int count = Random.Range(2, 4); // 2 to 3 wolves max
                count = Mathf.Min(count, remainingToKill);
                
                bool leftToRight = Random.value > 0.5f;
                int startLane = leftToRight ? 0 : laneCount - 1;
                int step = leftToRight ? 1 : -1;
                
                float sharedSpeed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                for (int i = 0; i < count; i++)
                {
                    int lane = startLane + (i * step);
                    if (lane < 0 || lane >= laneCount) break;
                    
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    Wolf w = SpawnWolf(config, isBoss, lane, sharedSpeed);
                    if (w != null) w.DisableTeaseBounces();
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                    if (i < count - 1) yield return new WaitForSeconds(0.5f);
                }
                waitAfterWave = baseWait * 1.5f;
            }
                                    else if (pattern == 3) // Horizontal Line
            {
                int count = Random.Range(2, 4); // 2 to 3 wolves max
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
                    float spawnX = targetX - furthestX + baseStartX;
                    
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    Wolf w = SpawnWolf(config, isBoss, lane, sharedSpeed, spawnX);
                    if (w != null) w.DisableTeaseBounces();
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                }
                waitAfterWave = baseWait * 1.8f; // Extra rest
            }

            yield return new WaitForSeconds(waitAfterWave);
        }
        Debug.Log($"SpawnWaveRoutine ended. spawning={spawning}, spawned={wolvesSpawned}");
    }

    private IEnumerator MeatSpawnRoutine(LevelConfig config)
    {
        yield return new WaitForSeconds(meatSpawnDelay);

        // Track the last "milestone" where meat was given
        // Arcade rule: meat spawns when remaining wolves crosses a multiple of 8
        // e.g. 32 wolves â†’ meat at 24, 16, 8 remaining
        int lastMeatMilestone = WolfTracker.Instance != null ? WolfTracker.Instance.TargetKills : wolvesToSpawn;

        while (spawning)
        {
            int remainingToKill = WolfTracker.Instance != null ? WolfTracker.Instance.RemainingKills : wolvesToSpawn - wolvesSpawned;

            if (!GameManager.Instance.IsOddStage && config.hasBossWolf && remainingToKill <= 2)
            {
                player?.SetBossApproaching(true);
                yield break;
            }

            if (player != null && !player.IsMeatAvailable && player.State != PlayerState.HoldingMeat)
            {
                // Check if we crossed a multiple-of-8 milestone
                int currentMilestone = (remainingToKill / 8) * 8;
                if (currentMilestone < lastMeatMilestone && remainingToKill > 0)
                {
                    player.SetMeatAvailable(true);
                    lastMeatMilestone = currentMilestone;
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private Wolf SpawnWolf(LevelConfig config, bool isBoss, int laneIndex, float? forcedSpeed = null, float? startXOverride = null)
    {
        bool descending = GameManager.Instance == null || GameManager.Instance.IsOddStage;

        Transform area = descending ? topSpawnArea : bottomSpawnArea;
        float startX = startXOverride.HasValue ? startXOverride.Value : (area != null ? area.position.x : -10f);
        float baseY = area != null ? area.position.y : (descending ? 4f : -4f);

        Transform[] currentPoints = descending ? dropPoints : flyPoints;
        int laneCount = (currentPoints != null && currentPoints.Length > 0) ? currentPoints.Length : 5;
        
        // Ensure array of floats to pass
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

        
        Vector3 pos = new Vector3(startX, baseY, 0f);

        float speed = forcedSpeed.HasValue ? forcedSpeed.Value : Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
                bool shield = Random.value < config.shieldRatio;
        bool rock = Random.value < config.rockThrowRatio;

        if (useDebugPatterns)
        {
            if (!allowNormalWolf && !allowShieldWolf && !allowRockWolf && !allowShieldAndRockWolf)
            {
                shield = false; rock = false; // Fallback
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
                
                // If failed due to 0% ratios, force one of the allowed patterns randomly
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

        if (isBoss && bossWolfPrefab != null)
        {
            var boss = Instantiate(bossWolfPrefab, pos, Quaternion.identity);
            boss.Initialize(config, descending, speed, true, rock, dropX, laneXCoords, laneIndex);
            WolfTracker.Instance?.Register(boss);
            return boss;
        }

        if (wolfPrefab == null) return null;
        var wolf = Instantiate(wolfPrefab, pos, Quaternion.identity);
        wolf.Initialize(config, descending, speed, shield, rock, dropX, laneXCoords, laneIndex);
        WolfTracker.Instance?.Register(wolf);
        return wolf;
    }

    public void OnWolfDestroyed()
    {
        // Hook for stage clear when all wolves dead â€” extend with active wolf counter if needed
    }
}











