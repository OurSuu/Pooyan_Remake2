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
    private bool isDeflected; // โดนปัดทิ้งไปหรือยัง จะได้ไม่เช็คซ้ำซ้อน

    // ฟังก์ชันเริ่มทำงาน โดนเรียกตอน spawn ลูกธนู
    public void Initialize(float moveDirection, Action<Arrow> destroyCallback)
    {
        direction = Mathf.Sign(moveDirection);
        if (direction == 0f) direction = -1f; // กันเหนียวไว้นิดนึง ถ้ามา 0 ให้ยิงซ้าย
        onDestroy = destroyCallback;

        gameObject.tag = GameConstants.TagArrow; // แปะ Tag ให้มัน

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // เซ็ตการชนเป็น Continuous เพราะลูกธนูอาจจะบินเร็ว ทะลุกำแพงได้ถ้าใช้แบบธรรมดา
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = new Vector2(direction * speed, 0f); // สั่งพุ่งไปข้างหน้า!
        }
    }

    private void Update()
    {
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
        // Debug.Log ไว้เช็คว่าโดนอะไรบ้าง (ปิดไปก็ได้นะถ้าล้น Console)
        Debug.Log("Arrow hit: " + other.gameObject.name + " with tag: " + other.tag);

        // ถ้าชนลูกโป่ง
        if (other.CompareTag(GameConstants.TagBalloon))
        {
            // เช็คก่อนว่านี่บอสหรือเปล่า ถ้าใช่ก็ให้บอสรับดาเมจไป
            var boss = other.GetComponentInParent<BossWolf>();
            if (boss != null)
            {
                boss.TryTakeArrowHit();
                DestroySelf();
                return;
            }

            // ถ้าไม่ใช่บอส ก็หาคอมโพเนนต์ลูกโป่งมาลดเลือด
            var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();
            if (balloon != null)
                balloon.TakeDamage(damage); 
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

            // ส่งข้อความไปบอกศัตรูให้เล่นอนิเมชันปัดลูกธนู
            other.SendMessage("PlayDeflectAnimation", SendMessageOptions.DontRequireReceiver);

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
    
    // ห่อฟังก์ชัน Destroy ไว้เรียกง่ายๆ จะได้เคลียร์ Event ด้วย
    private void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        onDestroy?.Invoke(this); // บอก ArrowShooter ว่าชั้นไปแล้วนะ
        Destroy(gameObject);
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
