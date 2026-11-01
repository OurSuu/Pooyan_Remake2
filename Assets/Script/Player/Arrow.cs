using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = GameConstants.ArrowSpeed; // ความเร็วลูกธนู เอามาจาก GameConstants
    [SerializeField] private float destroyX = -12f; // พิกัด X ที่จะทำลายลูกธนูทิ้ง (กันขยะล้นจอ)
    [SerializeField] private int damage = 1; // ดาเมจพื้นฐาน

    private float direction; // ทิศทางที่ยิง (-1 ซ้าย, 1 ขวา)
    private Action<Arrow> onDestroy; // callback ตอนลูกธนูโดนทำลาย จะได้ไปบอก ArrowShooter
    private Action<Arrow> onReturnToPool; // callback ตอนคืนลูกธนูกลับเข้า Pool
    private bool isDeflected; // โดนปัดทิ้งไปหรือยัง จะได้ไม่เช็คซ้ำซ้อน

    private float initialSpeed;
    private Rigidbody2D cachedRb;
    private Collider2D cachedCollider;
    private SpriteRenderer[] cachedRenderers;
    private ArrowTrailEffect cachedTrailFX;

    private void Awake()
    {
        initialSpeed = speed > 0f ? speed : GameConstants.ArrowSpeed;
        cachedRb = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        cachedTrailFX = GetComponentInChildren<ArrowTrailEffect>(true);
    }

    // ฟังก์ชันเริ่มทำงาน โดนเรียกตอน spawn ลูกธนู
    public void Initialize(float moveDirection, Action<Arrow> destroyCallback, Action<Arrow> returnToPoolCallback = null)
    {
        CancelInvoke(nameof(CompleteReturnToPool));
        isDestroyed = false;
        isDeflected = false;
        speed = initialSpeed > 0f ? initialSpeed : GameConstants.ArrowSpeed;
        transform.rotation = Quaternion.identity;

        direction = Mathf.Sign(moveDirection);
        if (direction == 0f) direction = -1f; // กันเหนียวไว้นิดนึง ถ้ามา 0 ให้ยิงซ้าย
        onDestroy = destroyCallback;
        onReturnToPool = returnToPoolCallback;

        gameObject.tag = GameConstants.TagArrow; // แปะ Tag ให้มัน

        if (cachedCollider != null) cachedCollider.enabled = true;
        if (cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null) cachedRenderers[i].enabled = true;
            }
        }

        if (cachedTrailFX != null)
        {
            cachedTrailFX.ResetTrailForPool();
        }

        if (cachedRb == null) cachedRb = GetComponent<Rigidbody2D>();
        if (cachedRb != null)
        {
            // เซ็ตการชนเป็น Continuous เพราะลูกธนูอาจจะบินเร็ว ทะลุกำแพงได้ถ้าใช้แบบธรรมดา
            cachedRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            cachedRb.gravityScale = 0f;
            cachedRb.linearVelocity = new Vector2(direction * speed, 0f); // สั่งพุ่งไปข้างหน้า!
        }
    }

    private void Update()
    {
        if (isDestroyed) return;

        // เช็คว่าถ้าบินออกนอกจอ (เลย destroyX) ก็ทำลายทิ้งซะ
        if (direction < 0f && transform.position.x < destroyX)
            DestroySelf();
        else if (direction > 0f && transform.position.x > -destroyX)
            DestroySelf();

        // ร่วงหล่นลงมาล่างจอเกินไปก็เคลียร์ทิ้งเหมือนกัน
        if (transform.position.y < -6f)
            DestroySelf();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;
        // Debug.Log ไว้เช็คว่าโดนอะไรบ้าง (ปิดไปก็ได้นะถ้าล้น Console)
        // Debug.Log("Arrow hit: " + other.gameObject.name + " with tag: " + other.tag);

        // ถ้าชนผลไม้โบนัส
        var fruit = other.GetComponent<BonusFruit>();
        if (fruit != null)
        {
            fruit.TakeHit(gameObject);
            DestroySelf();
            return;
        }

        // ถ้าชนใบไม้ลับ Easter Egg (4,000 แต้ม)
        var secretLeaf = other.GetComponent<SecretLeafTarget>();
        if (secretLeaf != null)
        {
            secretLeaf.TakeHit(gameObject);
            DestroySelf();
            return;
        }

        // ถ้าชนลูกโป่ง
        if (other.CompareTag(GameConstants.TagBalloon))
        {
            if (isDeflected) return; // ถ้าลูกธนูโดนปัดไปแล้ว ไม่ให้เจาะลูกโป่งบอสซ้ำ

            // เช็คก่อนว่านี่บอสหรือเปล่า ถ้าใช่ก็ให้บอสรับดาเมจไป
            var boss = other.GetComponentInParent<BossWolf>();
            if (boss != null)
            {
                if (!boss.TryTakeArrowHit(transform.position))
                {
                    // โล่บอสยังกันไว้ได้ -> ลูกธนูถูกปัดร่วงลงพื้นเหมือนชนโล่!
                    isDeflected = true;
                    transform.rotation = Quaternion.Euler(0, 0, 90f);
                    speed = 0;
                    direction = 0;
                    var rbBoss = GetComponent<Rigidbody2D>();
                    if (rbBoss != null)
                    {
                        rbBoss.linearVelocity = new Vector2(0f, -3f);
                        rbBoss.gravityScale = 1.5f;
                    }
                    return;
                }
                DestroySelf();
                return;
            }

            // ถ้าไม่ใช่บอส ก็หาคอมโพเนนต์ลูกโป่งมาลดเลือด
            var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();
            if (balloon != null)
                balloon.TakeDamage(damage, transform.position); 
            DestroySelf();
            return;
        }

        // ถ้าชนโล่ของศัตรู
        if (other.CompareTag(GameConstants.TagShield))
        {
            if (isDeflected) return; // โดนปัดไปแล้ว ข้ามเลย
            isDeflected = true;

            transform.rotation = Quaternion.Euler(0, 0, 90f); // หมุนลูกธนูให้หัวปักลงพื้น

            AudioManager.Instance?.PlayShieldBlock(); // เล่นเสียงตีเหล็กปึ้ง!

            speed = 0;
            direction = 0;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, -3f); // ปรับให้ร่วงลงพื้นตรงๆ
                rb.gravityScale = 1.5f; // เพิ่มแรงโน้มถ่วงให้ร่วงไวขึ้นนิดนึง
            }
            return;
        }

        // ถ้าชนตัวศัตรู (อาจจะโดนปัด)
        if (other.CompareTag(GameConstants.TagEnemy))
        {
            if (isDeflected) return; 
            isDeflected = true;

            transform.rotation = Quaternion.Euler(0, 0, 90f);

            // ส่งข้อความไปบอกศัตรูให้เล่นอนิเมชันปัดลูกธนู (ใช้ GetComponent แทน SendMessage เพื่อลดภาระ CPU)
            var wolf = other.GetComponent<Wolf>() ?? other.GetComponentInParent<Wolf>();
            if (wolf != null) wolf.PlayDeflectAnimation();

            speed = 0;
            direction = 0;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // อาการเดียวกับชนโล่เลย คือร่วงลงพื้น
                rb.linearVelocity = new Vector2(0f, -3f);
                rb.gravityScale = 1.5f;
            }
            return;
        }

        // ถ้าชนกระสุนศัตรู
        if (other.CompareTag(GameConstants.TagEnemyProjectile))
        {
            // ทำลายทั้งคู่ หักล้างกันไปเลย
            var proj = other.GetComponent<WolfProjectile>();
            if (proj != null) proj.DestroyByArrow();
            DestroySelf();
        }
    }

    private bool isDestroyed = false;
    
    // ห่อฟังก์ชัน Destroy ไว้เรียกง่ายๆ จะได้เคลียร์ Event ด้วย (รองรับ Object Pooling)
    public void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        onDestroy?.Invoke(this); // บอก ArrowShooter ว่าชั้นไปแล้วนะ (ยิงนัดต่อไปได้ทันที)

        if (onReturnToPool != null)
        {
            if (cachedCollider != null) cachedCollider.enabled = false;
            if (cachedRenderers != null)
            {
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null) cachedRenderers[i].enabled = false;
                }
            }
            if (cachedRb != null)
            {
                cachedRb.linearVelocity = Vector2.zero;
                cachedRb.gravityScale = 0f;
            }

            if (cachedTrailFX != null)
            {
                cachedTrailFX.StopTrailForPool();
                Invoke(nameof(CompleteReturnToPool), cachedTrailFX.FadeDuration);
            }
            else
            {
                CompleteReturnToPool();
            }
            return;
        }

        // ปลดเอฟเฟกต์หางลมให้ค่อยๆ สลายตัวอย่างเป็นธรรมชาติ (กรณีไม่ได้ใช้ Pool)
        var trailFX = cachedTrailFX != null ? cachedTrailFX : GetComponentInChildren<ArrowTrailEffect>();
        if (trailFX != null)
        {
            trailFX.DetachAndFade();
        }

        Destroy(gameObject);
    }

    private void CompleteReturnToPool()
    {
        gameObject.SetActive(false);
        onReturnToPool?.Invoke(this);
    }

    private void OnDestroy()
    {
        // เผื่อโดนทำลายจากที่อื่น (เช่น ปิดฉาก) จะได้ล้างคิวให้สะอาด
        if (!isDestroyed)
        {
            onDestroy?.Invoke(this);
        }
    }
}
