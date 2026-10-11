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

    private WolfProjectileType type; // ประเภทของอาวุธ
    private Vector2 velocity; // ความเร็วและทิศทาง
    private bool destroyed; // พังไปหรือยัง
    private bool isDeflected; // กระดอนหรือชนกิ่งไม้/โล่แล้วหรือยัง

    [Header("Trajectory Mode (โหมดวิถีการปา)")]
    [Tooltip("เปิดใช้ระบบวิถีปาหินโค้งย้อยแบบ NES 1985 (ถ้าปิด = ใช้โค้ดเดิมที่เล็งตามตัวผู้เล่นตรงๆ 100%)")]
    [SerializeField] private bool useArcadeTrajectory = true;
    [SerializeField] private float arcGravity = 9.8f; // แรงโน้มถ่วงดึงหินให้โค้งย้อยแบบ NES

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

        var player = FindAnyObjectByType<PlayerController>(); // หาตัวแม่หมู

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
    // [โหมด NES 1985] ปางัดขึ้นบนแล้วโค้งย้อยลงมาตามแรงโน้มถ่วง (Parabolic Arc)
    // ==============================================================================
    private void LaunchArcade(PlayerController player)
    {
        hasGravity = true;

        float targetY = player != null ? player.transform.position.y : transform.position.y;
        float targetX = player != null ? player.transform.position.x : transform.position.x + 6f;

        // ในด่านคี่ (Stage 1) มีการดักหน้าเล็กน้อยตามจังหวะเลื่อนของกระเช้าแม่หมู
        bool isOdd = GameManager.Instance == null || GameManager.Instance.IsOddStage;
        if (isOdd && player != null)
        {
            float pV = Input.GetAxisRaw("Vertical");
            targetY += pV * 0.6f;
        }

        // คำนวณระยะห่าง
        float dx = Mathf.Max(2f, targetX - transform.position.x);
        float dy = targetY - transform.position.y;

        // ความเร็วพุ่งไปข้างหน้าคงที่
        float vx = speed > 0 ? speed : 6f;
        float timeToTarget = dx / vx;

        // คำนวณแรงงัดขึ้นข้างบน (Vy0) เพื่อให้โค้งย้อยไปตกที่ระดับความสูงเป้าหมาย:
        // dy = Vy0 * t - 0.5 * g * t^2  ==>  Vy0 = (dy / t) + 0.5 * g * t
        float vy0 = (dy / timeToTarget) + 0.5f * arcGravity * timeToTarget;

        // สุ่มเพิ่มความสูง-ต่ำเล็กน้อย ให้หินแต่ละก้อนมีวิถีแตกต่างกันอย่างเป็นธรรมชาติ
        vy0 += Random.Range(-0.3f, 0.3f);

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

        // เช็คการชนกับแท่นกิ่งไม้ที่หมาป่าโดดลงมาในด่านคี่ (Odd Stage)
        CheckBranchCollision();

        // ถ้าร่วงเลยจอ หรือหลุดออกนอกจอไปไกลๆ ก็ทำลายทิ้งซะ
        if (transform.position.y < destroyY || transform.position.x < -12f || transform.position.x > 12f)
            Destroy(gameObject);
    }

    // เช็คว่าหินลอยไปชนขอบแท่น/ใต้กิ่งไม้ที่หมาป่าโดดลงมาหรือไม่
    private void CheckBranchCollision()
    {
        if (isDeflected || destroyed) return;

        bool isOdd = GameManager.Instance == null || GameManager.Instance.IsOddStage;
        if (!isOdd) return;

        // ในด่านคี่ แท่นกิ่งไม้ที่หมาป่าเดินโดดลงมาจะอยู่ที่ช่วง X <= 1.65f และระดับความสูงขอบล่างกิ่งไม้ Y >= 3.35f
        // ถ้าหินกำลังพุ่งขึ้นแล้วลอยไปชนใต้แท่น หรือลอยเกินเพดานฉาก ให้ติดแท่นแล้วเด้งร่วงลงทันที
        if ((transform.position.x <= 1.65f && transform.position.y >= 3.35f && velocity.y > 0f) || transform.position.y >= 3.8f)
        {
            DeflectDown(fromShield: false);
        }
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
            // ชนติดแท่นกิ่งไม้แล้วเด้งร่วงลงมาตรงๆ ตามต้นฉบับ Arcade!
            velocity = new Vector2(velocity.x * 0.15f, -4.5f);
            AudioManager.Instance?.PlayRockDestroy(); // เล่นเสียงหินกระทบไม้
        }

        hasGravity = true;
    }

    // ตอนถูกลูกธนูของเรายิงโดน (ยิงสกัดได้!)
    public void DestroyByArrow()
    {
        if (destroyed) return;
        destroyed = true;

        // ได้แต้มต่างกันไปตามชนิดของที่ปามา
        if (type == WolfProjectileType.Rock)
            ScoreManager.Instance?.AddRockDestroyScore(transform.position);
        else
            ScoreManager.Instance?.AddFruitDestroyScore(transform.position);

        AudioManager.Instance?.PlayRockDestroy(); // เสียงดังแกร๊ง!
        Destroy(gameObject); // ลบทิ้ง
    }
}


