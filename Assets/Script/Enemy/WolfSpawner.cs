using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns wolves per LevelManager config — odd stages from top, even from bottom.
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
    [SerializeField] private int killsPerMeat = 8; // Number of kills required to spawn next meat

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

            int activeWolves = FindObjectsByType<Wolf>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
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
            float r = Random.value;
            if (r < 0.50f) pattern = 0;
            else if (r < 0.70f) pattern = 1;
            else if (r < 0.90f) pattern = 2;
            else pattern = 3;

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
                for (int i = 0; i < count; i++)
                {
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    SpawnWolf(config, isBoss, lane);
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                    if (i < count - 1) yield return new WaitForSeconds(0.6f); // A bit slower consecutive drop
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
                
                for (int i = 0; i < count; i++)
                {
                    int lane = startLane + (i * step);
                    if (lane < 0) lane += laneCount;
                    if (lane >= laneCount) lane -= laneCount;
                    
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    SpawnWolf(config, isBoss, lane);
                    wolvesSpawned++;
                    if (isBoss) bossSpawned = true;
                    if (i < count - 1) yield return new WaitForSeconds(0.5f);
                }
                waitAfterWave = baseWait * 1.5f;
            }
            else if (pattern == 3) // Horizontal Line
            {
                int count = Random.Range(2, 4); // 2 to 3 wolves max (5 is too overwhelming for early game)
                count = Mathf.Min(count, remainingToKill);
                
                List<int> availableLanes = new List<int>();
                for (int i = 0; i < laneCount; i++) availableLanes.Add(i);
                
                for (int i = 0; i < count; i++)
                {
                    int randIdx = Random.Range(0, availableLanes.Count);
                    int lane = availableLanes[randIdx];
                    availableLanes.RemoveAt(randIdx);
                    
                    bool isBoss = !GameManager.Instance.IsOddStage && config.hasBossWolf && !bossSpawned && remainingToKill - i <= 2;
                    if (isBoss) player?.SetBossApproaching(true);
                    SpawnWolf(config, isBoss, lane);
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
        // e.g. 32 wolves → meat at 24, 16, 8 remaining
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

    private void SpawnWolf(LevelConfig config, bool isBoss, int laneIndex)
    {
        bool descending = GameManager.Instance == null || GameManager.Instance.IsOddStage;

        Transform area = descending ? topSpawnArea : bottomSpawnArea;
        float startX = area != null ? area.position.x : -10f;
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

        // เกิดที่ขอบจอ (startX) เดินเข้ามา
        Vector3 pos = new Vector3(startX, baseY, 0f);

        float speed = Random.Range(config.wolfSpeedRange.x, config.wolfSpeedRange.y);
        bool shield = Random.value < config.shieldRatio;
        bool rock = Random.value < config.rockThrowRatio;

        if (isBoss && bossWolfPrefab != null)
        {
            var boss = Instantiate(bossWolfPrefab, pos, Quaternion.identity);
            boss.Initialize(config, descending, speed, true, rock, dropX, laneXCoords, laneIndex);
            WolfTracker.Instance?.Register(boss);
            return;
        }

        if (wolfPrefab == null) return;
        var wolf = Instantiate(wolfPrefab, pos, Quaternion.identity);
        wolf.Initialize(config, descending, speed, shield, rock, dropX, laneXCoords, laneIndex);
        WolfTracker.Instance?.Register(wolf);
    }

    public void OnWolfDestroyed()
    {
        // Hook for stage clear when all wolves dead — extend with active wolf counter if needed
    }
}

