using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// อาวุธชิ้นเนื้อ (Meat Weapon) — วิถีโค้ง 2-Phase Parabolic Arc + Soft Homing & Chain-Steering ตามต้นฉบับ NES Pooyan:
/// 1. Phase 1 (Horizontal Glide): พุ่งออกจากมือแม่หมูในแนวราบ (vy = 0) ข้ามช่องว่างหน้าลิฟต์ไปถึงโซนเลนขวา
/// 2. Phase 2 (Parabolic Arc + Drag): แรงโน้มถ่วงดึงความเร็วแกน Y ให้โค้งลงต่อเนื่อง พร้อมชะลอความเร็วแกน X อย่างนุ่มนวล
///    ทำให้ช่วงกลางจอวิ่งแนวทแยง และช่วงท้ายฝั่งซ้ายโค้งงุ้มลงอย่างเป็นธรรมชาติ (ไม่ตัด vx = 0 แบบหักศอก 90° ตอนปาว่าว)
/// 3. Soft Homing & Chain-Steering: ช่วยดึงเส้นโค้งเข้าหาหมาป่าที่อยู่ในระยะด้านหน้า/ด้านล่าง และเมื่อชนหมาป่าตัวแรกแล้ว
///    จะปรับมุมโค้งหาหมาป่าตัวถัดไป (ทั้งแบบขั้นบันไดซ้ายล่าง และแบบซ้อนกันแนวตั้งในคอลัมน์เดียวกัน)
/// 4. Empty Balloon Ricochet: หากชนลูกโป่งเปล่า จะเด้งเชิดหัวกลับขึ้นฟ้าเป็นวิถีโค้งพาราโบลาลูกใหม่
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeatWeapon : MonoBehaviour
{
    [Header("Pooyan 2-Phase Parabolic Trajectory")]
    [Tooltip("ความเร็วพุ่งไปทางซ้ายเริ่มต้น (ค่าติดลบ = พุ่งไปทางซ้าย)")]
    [SerializeField] private float speedX = -7.5f;
    [Tooltip("ความเร็วแนวนอนขั้นต่ำสุดช่วงปลายวิถีทางซ้าย (ไม่ตัดเป็น 0 เพื่อไม่ให้หักศอกกลางอากาศ)")]
    [SerializeField] private float minHorizontalSpeed = 2.2f;
    [Tooltip("อัตราการชะลอความเร็วแนวนอนหลังจากเริ่มโค้งลง เพื่อให้งุ้มเป็นพาราโบลาช่วงท้าย")]
    [SerializeField] private float horizontalDeceleration = 4.2f;
    [Tooltip("ระยะเวลาช่วงแรกที่เนื้อพุ่งตรงในแนวราบก่อนเริ่มเปิดแรงโน้มถ่วง (วินาที)")]
    [SerializeField] private float horizontalGlideTime = 0.25f;
    [Tooltip("อัตราเร่งแรงโน้มถ่วงดึงเนื้อให้โค้งลงหลังจากพ้นช่วงพุ่งตรง")]
    [SerializeField] private float dropAcceleration = 11.0f;
    [Tooltip("ความเร็วร่วงลงสูงสุดตามวิถีพาราโบลา")]
    [SerializeField] private float maxFallSpeed = 7.8f;
    [Tooltip("ความเร็วเด้งขึ้นเมื่อเนื้อกระทบลูกโป่งเปล่า")]
    [SerializeField] private float balloonBounceSpeedY = 4.2f;

    [Header("Soft Homing & Chain Steering")]
    [Tooltip("เปิดระบบช่วยดึงวิถีโค้งเข้าหาหมาป่าที่อยู่ในระยะ")]
    [SerializeField] private bool enableSoftHoming = true;
    [Tooltip("ความแรงในการปรับความเร็วแกน Y เข้าหาหมาป่าเป้าหมายระหว่างลอย")]
    [SerializeField] private float homingVerticalAccel = 14.5f;
    [Tooltip("ความแรงในการชะลอแกน X เมื่อลอยมาถึงเหนือหัวหมาป่าในคอลัมน์เดียวกัน")]
    [SerializeField] private float homingColumnBrakeAccel = 12.0f;

    [Header("Bounds & Ground Stop")]
    [Tooltip("ระดับพื้นล่างสุด: หากเนื้อตกลงมาถึงพื้นจะหยุดทันที ไม่ลากกวาดหมาป่าที่เพิ่งเดินออกมา")]
    [SerializeField] private float groundStopY = -3.45f;
    [Tooltip("พิกัดแกน Y ที่เนื้อหลุดขอบล่างจอแล้วทำลายทิ้ง")]
    [SerializeField] private float destroyY = -6f;
    [Tooltip("พิกัดแกน X ที่เนื้อหลุดขอบซ้ายจอแล้วทำลายทิ้ง")]
    [SerializeField] private float destroyX = -11f;

    private const float WolfTargetCenterOffsetY = 0.35f;

    // เก็บรายการหมาป่าที่โดนตีไปแล้ว และลูกโป่งที่ไม่ต้องเด้งซ้ำ
    private readonly HashSet<Wolf> hitWolves = new();
    private readonly HashSet<Balloon> ignoredBalloons = new();

    private Rigidbody2D rb;
    private Collider2D cachedCollider;
    private SpriteRenderer cachedRenderer;
    private MeatAuraEffect auraEffect;
    private float elapsed;
    private float currentVx;
    private float currentVy;
    private bool hasHitGround;

    private void Awake()
    {
        gameObject.tag = GameConstants.TagMeat;
        rb = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        cachedRenderer = GetComponentInChildren<SpriteRenderer>();
        auraEffect = GetComponentInChildren<MeatAuraEffect>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            if (speedX > 0f) speedX = -speedX;
            currentVx = speedX;
            currentVy = 0f;
            rb.linearVelocity = new Vector2(currentVx, currentVy);
        }
    }

    private void FixedUpdate()
    {
        if (hasHitGround) return;

        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (transform.position.y <= groundStopY)
        {
            StopAtGround(groundStopY);
            return;
        }

        float dt = Time.fixedDeltaTime;
        elapsed += dt;

        // Phase 2: พ้นระยะพุ่งตรงช่วงแรกแล้ว เริ่มดึงเนื้อลงตามแรงโน้มถ่วงพาราโบลา และค่อยๆ ชะลอความเร็วแกน X
        if (elapsed > horizontalGlideTime)
        {
            currentVy = Mathf.Max(-maxFallSpeed, currentVy - dropAcceleration * dt);

            float minVxAbs = Mathf.Max(0.8f, minHorizontalSpeed);
            float nextVxAbs = Mathf.MoveTowards(Mathf.Abs(currentVx), minVxAbs, horizontalDeceleration * dt);
            currentVx = -nextVxAbs;
        }

        // Soft Homing: ช่วยดึงเส้นโค้งเข้าหาหมาป่าที่อยู่ในระยะด้านหน้า/ด้านล่างอย่างนุ่มนวล
        if (enableSoftHoming && elapsed >= 0.14f)
        {
            ApplySoftHoming(dt);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(currentVx, currentVy);
        }
    }

    /// <summary>
    /// สแกนหาหมาป่าที่อยู่ด้านหน้าทางซ้ายหรือด้านล่างในคอลัมน์เดียวกัน แล้วปรับความโค้งให้เลี้ยวเข้าหาอย่างสมูท
    /// </summary>
    private void ApplySoftHoming(float dt)
    {
        Vector3 meatPos = transform.position;
        Wolf bestWolf = null;
        float bestScore = float.MaxValue;

        var wolves = Wolf.ActiveWolves;
        for (int i = 0; i < wolves.Count; i++)
        {
            var w = wolves[i];
            if (w == null || w.State != WolfState.Floating || hitWolves.Contains(w) || w.transform.position.y <= groundStopY + 0.15f) continue;

            Vector3 targetPos = w.transform.position + new Vector3(0f, WolfTargetCenterOffsetY, 0f);
            float dx = targetPos.x - meatPos.x;
            float dy = targetPos.y - meatPos.y;

            // สนใจเฉพาะหมาป่าที่อยู่ข้างหน้าทางซ้าย หรืออยู่ในคอลัมน์เดียวกันด้านล่าง
            if (dx > 0.35f || dx < -4.2f) continue;
            if (dy > 0.85f || dy < -3.8f) continue;

            float score = Mathf.Abs(dx) + Mathf.Abs(dy) * 0.85f;
            if (score < bestScore)
            {
                bestScore = score;
                bestWolf = w;
            }
        }

        if (bestWolf == null) return;

        Vector3 wolfCenter = bestWolf.transform.position + new Vector3(0f, WolfTargetCenterOffsetY, 0f);
        float targetDx = wolfCenter.x - meatPos.x;
        float targetDy = wolfCenter.y - meatPos.y;

        // กรณีเนื้อลอยมาถึงแนวคอลัมน์ของหมาป่าแล้ว และหมาป่าอยู่ด้านล่าง (เช่น ปาข้ามโซนว่างมาหาหมาป่าที่ซ้อนอยู่ฝั่งซ้าย)
        if (targetDx >= -0.85f && targetDx <= 0.35f && targetDy < -0.10f)
        {
            float desiredDiveVy = -Mathf.Clamp(Mathf.Abs(targetDy) * 4.0f, 4.5f, maxFallSpeed);
            currentVy = Mathf.MoveTowards(currentVy, desiredDiveVy, homingVerticalAccel * 1.35f * dt);

            float desiredColumnVx = Mathf.Clamp(targetDx * 3.0f, -3.2f, -1.1f);
            currentVx = Mathf.MoveTowards(currentVx, desiredColumnVx, homingColumnBrakeAccel * dt);
        }
        else if (targetDx < -0.35f)
        {
            // กรณีหมาป่าอยู่เลนถัดไปทางซ้าย: คำนวณความเร็วแกน Y ที่จะโค้งไปตัดหน้าหมาป่าพอดี
            float tReach = Mathf.Abs(targetDx) / Mathf.Max(2.2f, Mathf.Abs(currentVx));
            float predictedDy = targetDy + bestWolf.CurrentVerticalVelocity * Mathf.Min(tReach, 0.45f);
            float desiredVy = Mathf.Clamp(predictedDy / Mathf.Max(0.12f, tReach), -maxFallSpeed, 1.25f);

            currentVy = Mathf.MoveTowards(currentVy, desiredVy, homingVerticalAccel * dt);
        }
    }

    /// <summary>
    /// ทันทีที่ชนหมาป่าตัวหนึ่ง ให้สแกนหาหมาป่าตัวถัดไปเพื่อหักโค้งทำคอมโบต่อเนื่อง (ขั้นบันได หรือ ซ้อนแถวแนวตั้ง)
    /// </summary>
    private void SteerTowardNextWolfAfterHit(Wolf justHitWolf)
    {
        if (!enableSoftHoming || justHitWolf == null) return;

        Vector3 refPos = transform.position;
        Wolf bestSameColumnBelow = null;
        float bestColumnDist = float.MaxValue;

        Wolf bestLeftLaneWolf = null;
        float bestLeftScore = float.MaxValue;

        var wolves = Wolf.ActiveWolves;
        for (int i = 0; i < wolves.Count; i++)
        {
            var w = wolves[i];
            if (w == null || w.State != WolfState.Floating || hitWolves.Contains(w) || w.transform.position.y <= groundStopY + 0.15f) continue;

            Vector3 wPos = w.transform.position + new Vector3(0f, WolfTargetCenterOffsetY, 0f);
            float dx = wPos.x - refPos.x;
            float dy = wPos.y - refPos.y;

            // 1) เช็กว่ามีหมาป่าซ้อนอยู่ด้านล่างในคอลัมน์เดียวกัน/ใกล้กันไหม (เช่น นาที 4:04 / 4:18)
            if (dx >= -1.10f && dx <= 0.50f && dy <= 0.25f && dy >= -4.2f)
            {
                float dist = Mathf.Abs(dx) * 1.2f + Mathf.Abs(dy);
                if (dist < bestColumnDist)
                {
                    bestColumnDist = dist;
                    bestSameColumnBelow = w;
                }
            }

            // 2) เช็กว่ามีหมาป่าอยู่เลนถัดไปทางซ้ายไหม (เช่น ขั้นบันไดนาที 4:07 / 4:13)
            if (dx < -0.35f && dx >= -4.2f && dy <= 1.1f && dy >= -3.6f)
            {
                float score = Mathf.Abs(dx) + Mathf.Abs(dy) * 0.8f;
                if (score < bestLeftScore)
                {
                    bestLeftScore = score;
                    bestLeftLaneWolf = w;
                }
            }
        }

        // ถ้ามีตัวซ้อนอยู่ด้านล่างใกล้กว่า หรือไม่มีตัวทางซ้ายแล้ว ให้โค้งงุ้มลงไปเก็บตัวล่าง
        if (bestSameColumnBelow != null && (bestLeftLaneWolf == null || bestColumnDist <= bestLeftScore * 0.95f))
        {
            Vector3 targetPos = bestSameColumnBelow.transform.position + new Vector3(0f, WolfTargetCenterOffsetY, 0f);
            float dx = targetPos.x - refPos.x;
            float dy = targetPos.y - refPos.y;

            float diveSpeedY = Mathf.Clamp(Mathf.Abs(dy) * 4.0f, 5.0f, maxFallSpeed);
            float tDrop = Mathf.Clamp(Mathf.Abs(dy) / diveSpeedY, 0.08f, 0.55f);
            float targetVx = Mathf.Clamp(dx / tDrop, -4.2f, -0.75f);

            currentVx = Mathf.Lerp(currentVx, targetVx, 0.8f);
            currentVy = Mathf.Min(currentVy, -diveSpeedY);
        }
        else if (bestLeftLaneWolf != null)
        {
            // หักมุมทแยงพุ่งไปหาหมาป่าในเลนถัดไปทางซ้าย
            Vector3 targetPos = bestLeftLaneWolf.transform.position + new Vector3(0f, WolfTargetCenterOffsetY, 0f);
            float dx = targetPos.x - refPos.x;
            float dy = targetPos.y - refPos.y;

            currentVx = Mathf.Min(currentVx, Mathf.Max(speedX, -6.2f));
            float tReach = Mathf.Abs(dx) / Mathf.Max(2.5f, Mathf.Abs(currentVx));
            float predictedDy = dy + bestLeftLaneWolf.CurrentVerticalVelocity * Mathf.Min(tReach, 0.45f);
            float targetVy = Mathf.Clamp(predictedDy / Mathf.Max(0.10f, tReach), -maxFallSpeed, 1.35f);

            currentVy = Mathf.Lerp(currentVy, targetVy, 0.78f);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(currentVx, currentVy);
        }
    }

    private void Update()
    {
        if (!hasHitGround && transform.position.y <= groundStopY)
        {
            StopAtGround(groundStopY);
            return;
        }

        if (transform.position.y < destroyY || transform.position.x < destroyX)
            Destroy(gameObject);
    }

    private void StopAtGround(float surfaceY)
    {
        if (hasHitGround) return;
        hasHitGround = true;
        currentVx = 0f;
        currentVy = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        transform.position = new Vector3(transform.position.x, surfaceY, transform.position.z);
        StartCoroutine(FadeOutOnGroundRoutine());
    }

    private System.Collections.IEnumerator FadeOutOnGroundRoutine()
    {
        float fadeTime = 0.25f;
        float t = 0f;
        Color startColor = cachedRenderer != null ? cachedRenderer.color : Color.white;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            if (cachedRenderer != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t / fadeTime);
                cachedRenderer.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitGround) return;

        if (other.CompareTag("Ground") || other.gameObject.name.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            StopAtGround(Mathf.Max(transform.position.y, groundStopY));
            return;
        }

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
                currentVx = Mathf.Min(currentVx, speedX * 0.85f);
                if (rb != null)
                    rb.linearVelocity = new Vector2(currentVx, currentVy);

                auraEffect?.OnBounce();
                AudioManager.Instance?.PlayMeatBounce();
            }
            return;
        }

        // กรณีชนตัวหมาป่า (หรือลูกโป่งที่ยังมีหมาป่าเกาะอยู่) — หมาป่าที่เพิ่งเดินบนพื้นล่างจะไม่โดนกวาด
        if (wolf != null && !hitWolves.Contains(wolf))
        {
            if (!wolf.IsActiveTarget)
                return;
            if (wolf.State == WolfState.WalkingBeforeDrop && wolf.transform.position.y <= groundStopY + 0.5f)
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

            wolf.OnMeatHit();

            // ปรับวิถีโค้งเข้าหาหมาป่าตัวถัดไป (ถ้าไม่มีตัวถัดไปแล้ว จะโค้งพาราโบลาลงพื้นต่ออย่างสมูท ไม่หักศอก 90°)
            SteerTowardNextWolfAfterHit(wolf);
        }
    }
}
