using System.Collections;
using UnityEngine;

/// <summary>
/// เฮ้ยเพื่อน ไฟล์นี้คือตัวจัดการ Bonus Stage นะ มันจะสลับไปมาระหว่างด่านเก็บผลไม้กับด่านโยนเนื้อหลังจากจบด่านเลขคู่
/// </summary>
public class BonusStageManager : MonoBehaviour
{
    [Header("Fruit Bonus")]
    [SerializeField] private GameObject fruitPrefab; // พรีแฟบผลไม้ที่จะให้ตกมา
    [SerializeField] private float fruitSpawnInterval = 0.8f; // ระยะห่างการเกิดของผลไม้
    [SerializeField] private float fruitSpawnXMin = -4f; // ขอบแกน X ซ้ายสุด
    [SerializeField] private float fruitSpawnXMax = 4f; // ขอบแกน X ขวาสุด
    [SerializeField] private float fruitSpawnY = 5f; // ความสูงที่ผลไม้จะโผล่มา

    [Header("Meat Bonus")]
    [SerializeField] private Wolf wolfPrefab; // พรีแฟบหมาป่าสำหรับโบนัสเนื้อ
    [SerializeField] private Transform[] meatBonusSpawnPoints; // จุดเกิดหมาป่า

    [Header("Timing")]
    [SerializeField] private float bonusDuration = 30f; // เวลาของโบนัสสเตจ

    private BonusType currentBonusType; // เก็บว่าตอนนี้เล่นโบนัสแบบไหนอยู่
    private Coroutine bonusRoutine; // เก็บ Coroutine เผื่อต้องสั่งหยุดกลางคัน

    private void Start()
    {
        // ดักรอ GameManager เปลี่ยนสถานะเกม
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        // เลิกฟังตอนโดนทำลาย จะได้ไม่ติดบั๊ก Memory Leak
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        // ถ้าสถานะเป็นโบนัสสเตจก็เริ่มเลย ไม่งั้นก็สั่งหยุดซะ
        if (state == GameState.BonusStage)
            StartBonusStage();
        else
            StopBonusStage();
    }

    public void StartBonusStage()
    {
        int stage = GameManager.Instance?.CurrentStage ?? 1;
        // สูตรคำนวณสลับด่านโบนัสง่ายๆ ผลไม้สลับเนื้อ
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

    private const int FruitBonusCount = 20; // ของแท้ต้องมีผลไม้ 20 ลูกเป๊ะๆ
    private const int PerfectBonusScore = 10000;
    private int fruitsSpawned; 
    private int fruitsHit; 

    public void OnBonusFruitHit()
    {
        // ยิงโดนลูกนึงก็นับไปเลยเพื่อน
        fruitsHit++;
    }

    private IEnumerator BonusRoutine()
    {
        var player = FindAnyObjectByType<PlayerController>();

        if (currentBonusType == BonusType.Fruit)
        {
            // --- โบนัสผลไม้: ต้องเกิด 20 ลูกนะ ---
            fruitsSpawned = 0;
            fruitsHit = 0;

            while (fruitsSpawned < FruitBonusCount)
            {
                SpawnFruit();
                fruitsSpawned++;
                yield return new WaitForSeconds(fruitSpawnInterval);
            }

            // รอแป๊บนึงให้ผลไม้พ้นจอ
            yield return new WaitForSeconds(3f);

            // ถ้าเก็บครบก็แจกแจ็คพ็อต 1 หมื่นแต้ม
            if (fruitsHit >= FruitBonusCount)
            {
                ScoreManager.Instance?.AddScore(PerfectBonusScore);
                Debug.Log("PERFECT BONUS! +10,000 pts! โหดจัดดด");
            }
        }
        else
        {
            // --- โบนัสเนื้อ: เสกเนื้อรัวๆ โยนได้ไม่อั้น ---
            if (player != null) player.SetMeatAvailable(true);

            float elapsed = 0f;
            while (elapsed < bonusDuration)
            {
                SpawnMeatBonusWolf();

                // รีฟิลเนื้อแบบรัวๆ เลย
                if (player != null && player.State == PlayerState.Normal && !player.IsMeatAvailable)
                {
                    player.SetMeatAvailable(true);
                }

                yield return new WaitForSeconds(1.5f);
                elapsed += 1.5f;

                // รีฟิลทันทีหลังโยนเสร็จด้วย
                if (player != null && player.State == PlayerState.Normal && !player.IsMeatAvailable)
                {
                    player.SetMeatAvailable(true);
                }
            }
        }

        // จบโบนัสแล้วจ้า
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
        // สุ่มแบบให้น้ำหนักหน่อยนึง
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

        // เซ็ตให้มันเกิดมาให้โดนโยนเนื้อใส่เฉยๆ
        wolf.Initialize(config, descending: false, speed: 2.5f, shield: false, rockThrow: false);
    }
}

/// <summary>
/// ตัวผลไม้โบนัส ถ้ายิงโดนก็จะได้คะแนนไงล่ะ
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BonusFruit : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 3f; // ความเร็วตก

    private BonusFruitType fruitType;

    public void Initialize(BonusFruitType type)
    {
        fruitType = type;
        gameObject.tag = "Untagged"; // ลบแท็กศัตรูทิ้ง หมูจะได้ไม่ตายเวลาผลไม้ชน
    }

    private void Update()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        if (transform.position.y < -6f) Destroy(gameObject); // ตกจอแล้วลบทิ้ง
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(GameConstants.TagArrow)) return; // สนใจแค่ธนูพอ

        // บวกคะแนนเลยเพื่อน
        ScoreManager.Instance?.AddBonusFruitScore(fruitType, transform.position);
        AudioManager.Instance?.PlayRockDestroy();

        // บอกผู้จัดการว่าโดนยิงไปแล้ว
        FindAnyObjectByType<BonusStageManager>()?.OnBonusFruitHit();

        Destroy(gameObject); // บึ้มมม
    }
}
