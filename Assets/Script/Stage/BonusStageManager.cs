using System.Collections;
using UnityEngine;

/// <summary>
/// Alternates Fruit / Meat bonus stages after even-numbered stages.
/// </summary>
public class BonusStageManager : MonoBehaviour
{
    [Header("Fruit Bonus")]
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField] private float fruitSpawnInterval = 0.8f;
    [SerializeField] private float fruitSpawnXMin = -4f;
    [SerializeField] private float fruitSpawnXMax = 4f;
    [SerializeField] private float fruitSpawnY = 5f;

    [Header("Meat Bonus")]
    [SerializeField] private Wolf wolfPrefab;
    [SerializeField] private Transform[] meatBonusSpawnPoints;

    [Header("Timing")]
    [SerializeField] private float bonusDuration = 30f;

    private BonusType currentBonusType;
    private Coroutine bonusRoutine;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
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
        int stage = GameManager.Instance?.CurrentStage ?? 1;
        currentBonusType = (stage / 2) % 2 == 0 ? BonusType.Fruit : BonusType.Meat;

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
    }

    private const int FruitBonusCount = 20; // Arcade: exactly 20 fruits
    private const int PerfectBonusScore = 10000;
    private int fruitsSpawned;
    private int fruitsHit;

    public void OnBonusFruitHit()
    {
        fruitsHit++;
    }

    private IEnumerator BonusRoutine()
    {
        var player = FindAnyObjectByType<PlayerController>();

        if (currentBonusType == BonusType.Fruit)
        {
            // --- Fruit Bonus: spawn exactly 20 fruits ---
            fruitsSpawned = 0;
            fruitsHit = 0;

            while (fruitsSpawned < FruitBonusCount)
            {
                SpawnFruit();
                fruitsSpawned++;
                yield return new WaitForSeconds(fruitSpawnInterval);
            }

            // Wait for remaining fruits to fall off screen
            yield return new WaitForSeconds(3f);

            // Perfect Bonus check
            if (fruitsHit >= FruitBonusCount)
            {
                ScoreManager.Instance?.AddScore(PerfectBonusScore);
                Debug.Log("PERFECT BONUS! +10,000 pts!");
            }
        }
        else
        {
            // --- Meat Bonus: instant unlimited meat refills ---
            if (player != null) player.SetMeatAvailable(true);

            float elapsed = 0f;
            while (elapsed < bonusDuration)
            {
                SpawnMeatBonusWolf();

                // Instant refill: check every frame-ish instead of every 2 seconds
                if (player != null && player.State == PlayerState.Normal && !player.IsMeatAvailable)
                {
                    player.SetMeatAvailable(true);
                }

                yield return new WaitForSeconds(1.5f);
                elapsed += 1.5f;

                // Also refill right after throw
                if (player != null && player.State == PlayerState.Normal && !player.IsMeatAvailable)
                {
                    player.SetMeatAvailable(true);
                }
            }
        }

        GameManager.Instance?.CompleteBonusStage();
    }

    private void SpawnFruit()
    {
        if (fruitPrefab == null) return;

        float x = Random.Range(fruitSpawnXMin, fruitSpawnXMax);
        var fruit = Instantiate(fruitPrefab, new Vector3(x, fruitSpawnY, 0f), Quaternion.identity);
        var bonusFruit = fruit.GetComponent<BonusFruit>();
        if (bonusFruit == null)
            bonusFruit = fruit.AddComponent<BonusFruit>();

        bonusFruit.Initialize(RandomFruitType());
    }

    private BonusFruitType RandomFruitType()
    {
        float r = Random.value;
        if (r < 0.5f) return BonusFruitType.Strawberry;
        if (r < 0.8f) return BonusFruitType.Cherry;
        return BonusFruitType.Peach;
    }

    private void SpawnMeatBonusWolf()
    {
        if (wolfPrefab == null || meatBonusSpawnPoints == null || meatBonusSpawnPoints.Length == 0) return;

        var point = meatBonusSpawnPoints[Random.Range(0, meatBonusSpawnPoints.Length)];
        var config = LevelManager.Instance?.CurrentConfig ?? ScriptableObject.CreateInstance<LevelConfig>();
        var wolf = Instantiate(wolfPrefab, point.position, Quaternion.identity);

        wolf.Initialize(config, descending: false, speed: 2.5f, shield: false, rockThrow: false);
    }
}

/// <summary>
/// Falling bonus fruit — destroyed by arrow for points.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BonusFruit : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 3f;

    private BonusFruitType fruitType;

    public void Initialize(BonusFruitType type)
    {
        fruitType = type;
        gameObject.tag = "Untagged"; // Removed EnemyProjectile tag to prevent player death
    }

    private void Update()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        if (transform.position.y < -6f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(GameConstants.TagArrow)) return;

        ScoreManager.Instance?.AddBonusFruitScore(fruitType, transform.position);
        AudioManager.Instance?.PlayRockDestroy();

        // Notify BonusStageManager for Perfect Bonus tracking
        FindAnyObjectByType<BonusStageManager>()?.OnBonusFruitHit();

        Destroy(gameObject);
    }
}
