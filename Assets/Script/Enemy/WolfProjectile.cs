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

    // เล็งปาใส่ผู้เล่น
    public void LaunchAtPlayer(WolfProjectileType projectileType)
    {
        type = projectileType;
        gameObject.tag = GameConstants.TagEnemyProjectile; // แปะแท็กให้รู้ว่าเป็นของอันตราย

        var player = FindAnyObjectByType<PlayerController>(); // หาตัวแม่หมู
        if (player != null)
        {
            // คำนวณทิศทางพุ่งเข้าหาแม่หมู
            Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            velocity = dir * speed;
        }
        else
        {
            // ถ้าไม่เจอตัวผู้เล่น ปาไปทางซ้ายดื้อๆ เลย
            velocity = Vector2.left * speed;
        }
    }

    // ปาลงมาตรงๆ (ใช้ตอนบอสอยู่บนต้นไม้)
    public void LaunchDown(WolfProjectileType projectileType, float fallSpeed = 4f)
    {
        type = projectileType;
        gameObject.tag = GameConstants.TagEnemyProjectile;
        velocity = Vector2.down * fallSpeed; // พุ่งลงล่างอย่างเดียว
    }

    private void Update()
    {
        // เกมหยุดเพราะแม่หมูตายอยู่ หินก็ต้องหยุดด้วยนะ
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze) return; 
        
        // ขยับไปตามทิศทางที่เล็งไว้
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // ถ้าร่วงเลยจอ หรือหลุดออกนอกจอไปไกลๆ ก็ทำลายทิ้งซะ
        if (transform.position.y < destroyY || transform.position.x < -12f || transform.position.x > 12f)
            Destroy(gameObject);
    }

    // เวลาชนกับอะไรซักอย่าง
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destroyed) return;

        // ถ้าชนโล่ (หรืออะไรที่กันได้)
        if (other.CompareTag(GameConstants.TagShield))
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.OnProjectileHit(); // แจ้งผู้เล่นว่าโดนปาของใส่
                Destroy(gameObject);
                return;
            }
        }

        // ถ้าชนตัวผู้เล่นเต็มๆ
        if (other.CompareTag(GameConstants.TagPlayer))
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.OnProjectileHit(); // แจ้งว่าโดน
            }
            Destroy(gameObject); // ชนปุ๊บหายปั๊บ
            return;
        }
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
