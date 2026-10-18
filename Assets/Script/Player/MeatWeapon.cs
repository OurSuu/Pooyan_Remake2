using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// อาวุธชิ้นเนื้อ (Meat Weapon) — จำลองวิถีโค้งตามต้นฉบับ Pooyan (NES 1985 / Arcade 1982) เป๊ะๆ:
/// 1. พุ่งตรงไปทางซ้ายข้ามช่องว่างหน้าลิฟต์ (~0.28 วินาที / ~2.3 ยูนิต) จนถึงเลนหมาป่าเลนแรก (Lane 5)
/// 2. โค้งลงเป็นแนวทแยงมุมลาดเอียงข้ามครบทั้ง 5 เลนไปจนถึงหมาป่าตัวไกลสุด (Lane 1 ที่ X = -3.6 ถึง -4.85)
///    โดยจำกัดความเร็วร่วงสูงสุดไว้ ไม่ให้เนื้อตกพื้นก่อนถึงเลนซ้ายสุด (กวาดคอมโบ 400 -> 800 -> 1600 -> 3200...)
/// 3. หากชนลูกโป่งเปล่าที่ลอยเดี่ยวๆ (ไม่ใช่ลูกโป่งของหมาป่าที่เพิ่งโดนเนื้อชิ้นนี้ชน) จะเด้งกระดอนขึ้นด้านบนตามความลับในคู่มือ NES!
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeatWeapon : MonoBehaviour
{
    [Header("Pooyan Authentic Meat Trajectory")]
    [Tooltip("ความเร็วพุ่งไปทางซ้าย (ค่าติดลบ = พุ่งไปทางซ้าย)")]
    [SerializeField] private float speedX = -8.2f;
    [Tooltip("ระยะเวลาช่วงแรกที่เนื้อพุ่งตรงในแนวราบข้ามช่องว่างหน้าลิฟต์ไปถึงเลนแรกก่อนเริ่มโค้งลง (วินาที)")]
    [SerializeField] private float horizontalGlideTime = 0.28f;
    [Tooltip("อัตราเร่งดึงเนื้อให้โค้งลงหลังจากพ้นช่วงพุ่งตรง")]
    [SerializeField] private float dropAcceleration = 7.5f;
    [Tooltip("ความเร็วร่วงลงสูงสุด (จำกัดไว้เพื่อให้เนื้อวิ่งตัดทแยงมุมข้ามไปถึงเลนซ้ายสุดได้อย่างแม่นยำ)")]
    [SerializeField] private float maxFallSpeed = 3.2f;
    [Tooltip("ความเร็วเด้งขึ้นเมื่อเนื้อกระทบลูกโป่งเปล่า")]
    [SerializeField] private float balloonBounceSpeedY = 4.2f;
    [Tooltip("พิกัดแกน Y ที่เนื้อหลุดขอบล่างจอแล้วทำลายทิ้ง")]
    [SerializeField] private float destroyY = -6f;
    [Tooltip("พิกัดแกน X ที่เนื้อหลุดขอบซ้ายจอแล้วทำลายทิ้ง")]
    [SerializeField] private float destroyX = -11f;

    // เก็บรายการหมาป่าที่โดนตีไปแล้ว และลูกโป่งที่ไม่ต้องเด้งซ้ำ
    private readonly HashSet<Wolf> hitWolves = new();
    private readonly HashSet<Balloon> ignoredBalloons = new();

    private Rigidbody2D rb;
    private float elapsed;
    private float currentVy;

    private void Awake()
    {
        gameObject.tag = GameConstants.TagMeat;
        rb = GetComponent<Rigidbody2D>();

        // ควบคุมวิถีโค้งด้วยตัวเองเพื่อให้ได้มุมทแยงคงที่ตามต้นฉบับ Pooyan (ไม่ถูก gravityScale ของ Rigidbody2D ดึงร่วงดิ่งเหว)
        if (rb != null)
        {
            rb.gravityScale = 0f;
            if (speedX > 0f) speedX = -speedX;
            currentVy = 0f;
            rb.linearVelocity = new Vector2(speedX, currentVy);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        elapsed += Time.fixedDeltaTime;

        // ช่วงแรกพุ่งตรงในระดับเดียวกับแม่หมูข้ามไปถึงเลนหมาป่า จากนั้นค่อยโค้งลงจนถึงความเร็วทแยงมุมสูงสุด (maxFallSpeed)
        if (elapsed > horizontalGlideTime)
        {
            currentVy = Mathf.Max(-maxFallSpeed, currentVy - dropAcceleration * Time.fixedDeltaTime);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(speedX, currentVy);
        }
    }

    private void Update()
    {
        if (transform.position.y < destroyY || transform.position.x < destroyX)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.TagArrow) || other.CompareTag(GameConstants.TagMeat))
            return;

        var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();
        var wolf = other.GetComponent<Wolf>() ?? other.GetComponentInParent<Wolf>();

        // กรณีชนลูกโป่งเปล่าที่ไม่มีหมาป่าเกาะ และไม่ใช่ลูกโป่งของหมาป่าที่เพิ่งโดนเนื้อก้อนนี้สอย
        if (balloon != null && !balloon.HasOwner)
        {
            if (balloon.CanBounceMeat && !ignoredBalloons.Contains(balloon))
            {
                ignoredBalloons.Add(balloon);
                currentVy = balloonBounceSpeedY;
                if (rb != null)
                    rb.linearVelocity = new Vector2(speedX, currentVy);

                AudioManager.Instance?.PlayMeatBounce();
            }
            return;
        }

        // กรณีชนตัวหมาป่า (หรือลูกโป่งที่ยังมีหมาป่าเกาะอยู่)
        if (wolf != null && !hitWolves.Contains(wolf))
        {
            if (!wolf.IsActiveTarget)
                return;

            hitWolves.Add(wolf);

            // จดจำลูกโป่งของหมาป่าตัวนี้ไว้ทั้งหมด เพื่อไม่ให้เนื้อไปเด้งใส่ลูกโป่งที่หมาป่าตัวนี้เพิ่งปล่อยมือ!
            foreach (var b in wolf.GetComponentsInChildren<Balloon>(true))
            {
                if (b != null) ignoredBalloons.Add(b);
            }
            if (balloon != null) ignoredBalloons.Add(balloon);

            ScoreManager.Instance?.AddMeatComboScore(wolf.transform.position);
            AudioManager.Instance?.PlayMeatHit();

            // เนื้อทะลวงผ่านตัวหมาป่าไปตามวิถีทแยงมุมเดิมโดยไม่เสียความเร็ว!
            wolf.OnMeatHit();
        }
    }
}
