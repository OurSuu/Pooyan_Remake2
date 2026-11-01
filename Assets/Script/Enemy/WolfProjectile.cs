using UnityEngine;

public enum WolfProjectileType
{
    Rock,
    Fruit
}

/// <summary>
/// หินและผลไม้ที่หมาป่าปามา - ยิงทิ้งได้แต้ม โดนตัวก็เสียเลือดนะจ๊ะ
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WolfProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f; // ความเร็วของที่ปามา
    [SerializeField] private float destroyY = -6f; // ร่วงเกินเส้นนี้ก็ลบทิ้งเลย

    [Header("Hit & Break FX Particles")]
    [Tooltip("พรีแฟบ Particle Effect ตอนยิงหินแตก (RockBreak)")]
    [SerializeField] private GameObject rockBreakPrefab;
    [Tooltip("พรีแฟบ Particle Effect ตอนหินปาโดนกระเช้า/เกราะแม่หมู (RockHitBasket)")]
    [SerializeField] private GameObject rockHitBasketPrefab;

    private static GameObject cachedBreakPrefab;
    private static GameObject cachedHitBasketPrefab;

    private WolfProjectileType type; // ประเภทของอาวุธ
    private Vector2 velocity; // ความเร็วและทิศทาง
    private bool destroyed; // พังไปหรือยัง
    private bool isDeflected; // กระดอนหรือชนกิ่งไม้/โล่แล้วหรือยัง

    [Header("Trajectory Mode (โหมดวิถีการปา)")]
    [Tooltip("เปิดใช้ระบบวิถีปาหินโค้งแบบ NES 1985 (ถ้าปิด = ใช้โค้ดเดิมที่เล็งตามตัวผู้เล่นตรงๆ 100%)")]
    [SerializeField] private bool useArcadeTrajectory = true;
    [SerializeField] private float arcGravity = 2.6f; // แรงโน้มถ่วงวิถีโค้งแบบ NES (โค้งพุ่งแนวราบเข้าช่องหน้ากระเช้า ไม่ใช่ปาย้อยตั้งฉากลงหลังคา)

    // ติดตามจำนวนหินที่กำลังบินอยู่บนจอ (จำกัด 3 ก้อนตามตู้ Arcade)
    public static int ActiveRockCount { get; private set; } = 0;
    private bool countedAsRock = false;
    private bool hasGravity = false; // เปิดใช้แรงโน้มถ่วงสำหรับวิถีโค้ง

    private void OnDestroy()
    {
        if (countedAsRock)
        {
            ActiveRockCount = Mathf.Max(0, ActiveRockCount - 1);
            countedAsRock = false;
        }
    }

    // เล็งปาใส่ผู้เล่น
    public void LaunchAtPlayer(WolfProjectileType projectileType)
    {
        type = projectileType;
        gameObject.tag = GameConstants.TagEnemyProjectile; // แปะแท็กให้รู้ว่าเป็นของอันตราย

        if (type == WolfProjectileType.Rock && !countedAsRock)
        {
            countedAsRock = true;
            ActiveRockCount++;
        }

        var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>(); // หาตัวแม่หมู

        if (useArcadeTrajectory)
        {
            LaunchArcade(player);
        }
        else
        {
            LaunchLegacy(player); // โค้ดเดิมที่ล็อคเป้าแม่หมู (สำรองไว้ 100% สลับกลับได้)
        }
    }

    // ==========================================
    // [โค้ดเดิม 100%] เล็งพุ่งตรงเข้าหาแม่หมูเป๊ะๆ
    // ==========================================
    private void LaunchLegacy(PlayerController player)
    {
        hasGravity = false;
        if (player != null)
        {
            Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            velocity = dir * speed;
        }
        else
        {
            velocity = Vector2.right * speed;
        }
    }

    // ==============================================================================
    // [โหมด NES 1985] ปาพุ่งวิถีโค้งแนวราบ (Shallow Parabolic Arc) เข้าช่องหน้ากระเช้าแม่หมู
    // ==============================================================================
    private void LaunchArcade(PlayerController player)
    {
        hasGravity = true;

        // ใช้แรงโน้มถ่วงวิถีโค้งตื้น (ป้องกันกรณี Prefab เก่าใน Inspector เซ็ตค่า 9.8 ค้างไว้จนหินย้อยตกใส่หลังคากระเช้า)
        if (arcGravity > 3.5f) arcGravity = 2.6f;

        float targetY = player != null ? player.transform.position.y : transform.position.y;
        float targetX = player != null ? player.transform.position.x : transform.position.x + 6f;

        // ในด่านคี่ (Stage 1, 3) มีการดักหน้าเล็กน้อยตามจังหวะเลื่อนของกระเช้าแม่หมู
        bool isOdd = GameManager.Instance == null || GameManager.Instance.IsOddStage;
        if (isOdd && player != null)
        {
            float pV = Input.GetAxisRaw("Vertical");
            targetY += pV * 0.35f;
        }

        // คำนวณระยะห่างแนวนอน และจำกัดมุมเงย/ก้ม (dy) ไม่ให้หมาป่าปาหินเสยขึ้นฟ้าเหมือนจรวด
        float dx = Mathf.Max(2.5f, targetX - transform.position.x);
        float rawDy = targetY - transform.position.y;
        float clampedDy = Mathf.Clamp(rawDy, -2.2f, 1.5f);

        // ความเร็วพุ่งไปข้างหน้าแนวราบ (6.5 หน่วย/วิ เพื่อให้วิถีหินพุ่งเข้าช่องกลางกระเช้าได้พอดี)
        float vx = Mathf.Max(6.5f, speed);
        float timeToTarget = dx / vx;

        // คำนวณแรงส่งแนวตั้งต้น (Vy0) ให้โค้งตื้นแบบ NES Pooyan
        float vy0 = (clampedDy / timeToTarget) + 0.5f * arcGravity * timeToTarget;
        vy0 += Random.Range(-0.15f, 0.15f);
        vy0 = Mathf.Clamp(vy0, -2.5f, 3.2f);

        velocity = new Vector2(vx, vy0);
    }

    // ปาลงมาตรงๆ (ใช้ตอนบอสอยู่บนต้นไม้)
    public void LaunchDown(WolfProjectileType projectileType, float fallSpeed = 4f)
    {
        hasGravity = false;
        type = projectileType;
        gameObject.tag = GameConstants.TagEnemyProjectile;
        velocity = Vector2.down * fallSpeed; // พุ่งลงล่างอย่างเดียว
    }

    private void Update()
    {
        // เกมหยุดเพราะแม่หมูตายอยู่ หินก็ต้องหยุดด้วยนะ
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze) return; 

        // คำนวณแรงโน้มถ่วงดึงหินให้โค้งย้อยแบบ NES
        if (hasGravity)
        {
            velocity.y -= arcGravity * Time.deltaTime;
        }
        
        // ขยับไปตามทิศทางที่เล็งไว้
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // ถ้าร่วงเลยจอ หรือหลุดออกนอกจอไปไกลๆ ก็ทำลายทิ้งซะ
        if (transform.position.y < destroyY || transform.position.x < -12f || transform.position.x > 12f)
            Destroy(gameObject);
    }

    // เวลาชนกับอะไรซักอย่าง
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destroyed || isDeflected) return;

        // ถ้าชนโล่ (หรืออะไรที่กันได้)
        if (other.CompareTag(GameConstants.TagShield))
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.OnProjectileHit(); // มีเสียงดังป๊าบ!
            }
            SpawnHitBasketEffect(transform.position);
            DeflectDown(fromShield: true);
            return;
        }

        // ถ้าชนตัวผู้เล่นเต็มๆ
        if (other.CompareTag(GameConstants.TagPlayer))
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(); // โดนตัวเต็มๆ ตายสถานเดียว
            }
            Destroy(gameObject); // ชนปุ๊บหายปั๊บ
            return;
        }

        // ถ้าชนสิ่งกีดขวาง/พื้น/แท่นกิ่งไม้
        if (other.CompareTag("Ground") || other.CompareTag("Platform"))
        {
            DeflectDown(fromShield: false);
            return;
        }
    }

    // หินกระดอนตกลงมา (ชนโล่ หรือ ชนติดแท่นกิ่งไม้)
    private void DeflectDown(bool fromShield)
    {
        if (isDeflected) return;
        isDeflected = true;

        gameObject.tag = "Untagged"; // ปลดแท็กอันตรายออก จะได้ไม่ทำร้ายแม่หมูตอนร่วงตกพื้น
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // ปิด Collider กันการชนซ้ำ

        if (fromShield)
        {
            velocity = new Vector2(-velocity.x, -5f); // สะท้อนกลับไปทางเดิมแล้วร่วงลงพื้น
        }
        else
        {
            // ถ้าชนขอบสิ่งกีดขวาง ให้ยังคงรักษาแรงพุ่งไปข้างหน้า (ไปทางแม่หมู) แล้วแฉลบร่วงลงมา
            velocity = new Vector2(Mathf.Max(3f, Mathf.Abs(velocity.x) * 0.75f), -3.5f);
            AudioManager.Instance?.PlayRockDestroy(); // เล่นเสียงหินกระทบ
        }

        hasGravity = true;
    }

    // ตอนถูกลูกธนูของเรายิงโดน (ยิงสกัดได้!)
    public void DestroyByArrow()
    {
        if (destroyed) return;
        destroyed = true;

        // ได้แต้มและเสียงต่างกันไปตามชนิดของที่ปามา
        if (type == WolfProjectileType.Rock)
        {
            SpawnBreakEffect(transform.position);
            ScoreManager.Instance?.AddRockDestroyScore(transform.position);
            AudioManager.Instance?.PlayRockDestroy();
        }
        else
        {
            ScoreManager.Instance?.AddFruitDestroyScore(transform.position);
            AudioManager.Instance?.PlayFruitHit();
        }

        Destroy(gameObject); // ลบทิ้ง
    }

    // สร้างเอฟเฟกต์หินแตก (RockBreak)
    private void SpawnBreakEffect(Vector3 pos)
    {
        GameObject prefab = rockBreakPrefab;
        if (prefab == null)
        {
            if (cachedBreakPrefab == null)
                cachedBreakPrefab = Resources.Load<GameObject>("FX/RockBreak");
            prefab = cachedBreakPrefab;
        }

        if (prefab != null)
        {
            var fx = Instantiate(prefab, pos, Quaternion.identity);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                Destroy(fx, Mathf.Max(0.8f, main.duration));
            }
            else
            {
                Destroy(fx, 1.5f);
            }
        }
    }

    // สร้างเอฟเฟกต์ประกายไฟตอนปาหินโดนกระเช้า/เกราะ (RockHitBasket)
    private void SpawnHitBasketEffect(Vector3 pos)
    {
        GameObject prefab = rockHitBasketPrefab;
        if (prefab == null)
        {
            if (cachedHitBasketPrefab == null)
                cachedHitBasketPrefab = Resources.Load<GameObject>("FX/RockHitBasket");
            prefab = cachedHitBasketPrefab;
        }

        if (prefab != null)
        {
            var fx = Instantiate(prefab, pos, Quaternion.identity);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                Destroy(fx, Mathf.Max(0.8f, main.duration));
            }
            else
            {
                Destroy(fx, 1.5f);
            }
        }
    }
}


