using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = GameConstants.ArrowSpeed; // ความเร็วลูกธนู เอามาจาก GameConstants
    [SerializeField] private float destroyX = -12f; // พิกัด X ที่จะทำลายลูกธนูทิ้ง (กันขยะล้นจอ)
    [SerializeField] private int damage = 1; // ดาเมจพื้นฐาน

    [Header("Ground Stick & Fade Settings")]
    [Tooltip("ระดับความสูงแกน Y ของผิวดินที่ลูกธนูจะปัก (ค่าเริ่มต้น -3.5f)")]
    [SerializeField] private float stickGroundY = -3.5f;
    [Tooltip("ระยะเวลาที่ลูกธนูปักอยู่ที่พื้นก่อนจะเริ่มจาง (วินาที)")]
    [SerializeField] private float stickDuration = 1.0f;
    [Tooltip("ระยะเวลาค่อยๆ จางหายไป (วินาที)")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("มุมเอียงสุ่มเล็กน้อยตอนปักพื้น (องศา) เพื่อความสมจริง")]
    [SerializeField] private float stickAngleVariation = 8f;

    [Header("ShaderGraph VFX")]
    [SerializeField] private Material parryRingMaterial;
    [SerializeField] private Color muzzleEffectColor = new Color(0.88f, 0.78f, 0.52f, 0.72f);

    public Color MuzzleEffectColor => muzzleEffectColor;

    private float direction; // ทิศทางที่ยิง (-1 ซ้าย, 1 ขวา)
    private Action<Arrow> onDestroy; // callback ตอนลูกธนูโดนทำลาย จะได้ไปบอก ArrowShooter
    private Action<Arrow> onReturnToPool; // callback ตอนคืนลูกธนูกลับเข้า Pool
    private bool isDeflected; // โดนปัดทิ้งไปหรือยัง จะได้ไม่เช็คซ้ำซ้อน
    private Wolf deflectedByWolf; // หมาป่าตัวที่เพิ่งงับ/ปัดลูกธนูดอกนี้ (กันไม่ให้ลูกธนูดอกเดิมโดนลูกโป่งของตัวที่เพิ่งปัด)
    private bool isStuckInGround; // ปักอยู่ที่พื้นแล้วหรือยัง

    private float initialSpeed;
    private Rigidbody2D cachedRb;
    private Collider2D cachedCollider;
    private SpriteRenderer[] cachedRenderers;
    private Color[] initialRendererColors;
    private ArrowTrailEffect cachedTrailFX;
    private static GameObject cachedGroundDustPrefab;

    private void Awake()
    {
        initialSpeed = speed > 0f ? speed : GameConstants.ArrowSpeed;
        cachedRb = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (cachedRenderers != null)
        {
            initialRendererColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null) initialRendererColors[i] = cachedRenderers[i].color;
            }
        }
        cachedTrailFX = GetComponentInChildren<ArrowTrailEffect>(true);
    }

    public void ApplyStyleOverride(Material trailMat, Color trailColor, Color sparkColor, Color muzzleColor)
    {
        muzzleEffectColor = muzzleColor;

        if (cachedTrailFX == null)
            cachedTrailFX = GetComponentInChildren<ArrowTrailEffect>(true);

        var trail = cachedTrailFX != null ? cachedTrailFX.GetComponent<TrailRenderer>() : GetComponentInChildren<TrailRenderer>(true);
        if (trail != null)
        {
            if (trailMat != null)
                trail.sharedMaterial = trailMat;

            var grad = trail.colorGradient;
            var colorKeys = grad.colorKeys;
            if (colorKeys != null && colorKeys.Length > 0)
            {
                colorKeys[0].color = trailColor;
                grad.SetKeys(colorKeys, grad.alphaKeys);
                trail.colorGradient = grad;
            }
        }

        var ps = cachedTrailFX != null ? cachedTrailFX.GetComponent<ParticleSystem>() : GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = sparkColor;
        }
    }

    public void CollectWarmupMaterials(System.Collections.Generic.List<Material> results)
    {
        if (results == null) return;
        if (parryRingMaterial != null) results.Add(parryRingMaterial);

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var mats = r.sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null) results.Add(mats[j]);
            }
        }
    }

    // ฟังก์ชันเริ่มทำงาน โดนเรียกตอน spawn ลูกธนู
    public void Initialize(float moveDirection, Action<Arrow> destroyCallback, Action<Arrow> returnToPoolCallback = null)
    {
        CancelInvoke(nameof(CompleteReturnToPool));
        StopAllCoroutines();
        isDestroyed = false;
        isDeflected = false;
        deflectedByWolf = null;
        isStuckInGround = false;
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
            SetRenderersAlpha(1f);
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
            cachedRb.bodyType = RigidbodyType2D.Dynamic;
            // เซ็ตการชนเป็น Continuous เพราะลูกธนูอาจจะบินเร็ว ทะลุกำแพงได้ถ้าใช้แบบธรรมดา
            cachedRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            cachedRb.gravityScale = 0f;
            cachedRb.linearVelocity = new Vector2(direction * speed, 0f); // สั่งพุ่งไปข้างหน้า!
        }
    }

    private void Update()
    {
        if (isDestroyed) return;

        // ถ้าปักพื้นอยู่และกำลังค่อยๆ จาง ไม่ต้องเช็คขอบจอ
        if (isStuckInGround) return;

        // ถ้าลูกธนูถูกปัด/กัดแล้วดิ่งลงมาถึงระดับผิวดิน ให้ปักลงพื้นทันที
        if (isDeflected)
        {
            if (transform.position.y <= stickGroundY)
            {
                StickIntoGround(stickGroundY);
                return;
            }
        }

        // เช็คว่าถ้าบินออกนอกจอ (เลย destroyX) ก็ทำลายทิ้งซะ
        if (direction < 0f && transform.position.x < destroyX)
            DestroySelf();
        else if (direction > 0f && transform.position.x > -destroyX)
            DestroySelf();

        // ร่วงหล่นลงมาล่างจอเกินไปก็เคลียร์ทิ้งเหมือนกัน (เผื่อหลุดเช็ค)
        if (transform.position.y < -6f)
            DestroySelf();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed || isStuckInGround) return;

        // ถ้าลูกธนูดิ่งลงมาแล้วชนกับวัตถุพื้นดิน (ที่มี Collider) ให้ปักลงพื้น
        if (isDeflected)
        {
            if (other.CompareTag("Ground") || other.gameObject.name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                StickIntoGround(transform.position.y);
                return;
            }
        }

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
            // เช็คก่อนว่านี่บอสหรือเปล่า ถ้าใช่ก็ให้บอสรับดาเมจไป (ลูกธนูที่โดนปัดแล้วจะไม่เจาะลูกโป่งบอสซ้ำ)
            var boss = other.GetComponentInParent<BossWolf>();
            if (boss != null)
            {
                if (isDeflected) return;
                if (!boss.TryTakeArrowHit(transform.position))
                {
                    // โล่บอสยังกันไว้ได้ -> ลูกธนูถูกปัดร่วงลงพื้นเหมือนชนโล่!
                    DeflectDown(isShield: true, wolf: boss);
                    return;
                }
                DestroySelf();
                return;
            }

            // ถ้าไม่ใช่บอส ก็หาคอมโพเนนต์ลูกโป่งมาลดเลือด
            // หมายเหตุตามต้นฉบับ NES: ลูกธนูที่โดนหมาป่าตัวบนงับ/ปัดจนร่วงดิ่งลงมา (isDeflected == true)
            // สามารถปักเจาะลูกโป่งของหมาป่าตัวล่างที่ลอยซ้อนอยู่ข้างใต้ให้แตกได้! (ยกเว้นลูกโป่งของหมาป่าตัวที่เพิ่งปัดลูกธนูดอกนี้เอง)
            var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();
            if (balloon != null)
            {
                if (isDeflected && balloon.Owner != null && balloon.Owner == deflectedByWolf)
                    return;

                balloon.TakeDamage(damage, transform.position);
            }
            DestroySelf();
            return;
        }

        // ถ้าชนโล่ของศัตรู
        if (other.CompareTag(GameConstants.TagShield))
        {
            if (isDeflected) return; // โดนปัดไปแล้ว ข้ามเลย
            var shieldOwner = other.GetComponentInParent<Wolf>();
            DeflectDown(isShield: true, wolf: shieldOwner);
            return;
        }

        // ถ้าชนตัวศัตรู (หมาป่างับหรือปัดลูกธนู)
        if (other.CompareTag(GameConstants.TagEnemy))
        {
            if (isDeflected) return;
            var wolf = other.GetComponent<Wolf>() ?? other.GetComponentInParent<Wolf>();
            DeflectDown(isShield: false, wolf: wolf);
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

    /// <summary>
    /// สั่งให้ลูกธนูเสียหลักดิ่งลงพื้น (เมื่อโดนหมาป่างับ, ชนโล่, หรือชนโล่บอส)
    /// พร้อมจัดแนวแกน X ให้ตรงกับแนวลูกโป่งของเลนนั้น เพื่อให้ลูกธนูที่ร่วงดิ่งลงไปสามารถเจาะลูกโป่งของหมาป่าตัวล่างที่ซ้อนอยู่ในช่องเดียวกันได้ตาม NES!
    /// </summary>
    private void DeflectDown(bool isShield, Wolf wolf = null)
    {
        if (isDeflected) return;
        isDeflected = true;
        deflectedByWolf = wolf;

        // แจ้ง ArrowShooter ทันทีว่าลูกธนูนี้พ้นสภาพการยิงแล้ว ผู้เล่นจะได้ยิงนัดถัดไปได้ทันที ไม่ต้องรอลูกธนูปักพื้นเสร็จ
        if (onDestroy != null)
        {
            onDestroy.Invoke(this);
            onDestroy = null;
        }

        // ปิดเอฟเฟกต์ลมแหวก/หางลม
        if (cachedTrailFX != null)
        {
            cachedTrailFX.StopTrailForPool();
        }

        // จัดแนวแกน X ของลูกธนูตอนร่วงให้ตรงกับกึ่งกลางคอลัมน์ลูกโป่งของหมาป่าตัวที่ปัด
        // เพื่อไม่ให้ลูกธนูร่วงเฉียดขอบขวาของลูกโป่งตัวล่างที่ซ้อนอยู่ในเลนเดียวกัน
        if (wolf != null)
        {
            float columnX = wolf.GetBalloonColumnWorldX();
            transform.position = new Vector3(columnX, transform.position.y, transform.position.z);
        }

        // หมุนลูกธนูให้หัวดิ่งลงพื้น
        transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        if (isShield)
        {
            AudioManager.Instance?.PlayShieldBlock(); // เล่นเสียงตีเหล็กปึ้ง!
            CartoonFXHelper.SpawnShieldParry(transform.position, parryRingMaterial);
        }
        else if (wolf != null)
        {
            wolf.PlayDeflectAnimation(transform.position, spawnBiteFX: true); // หมาป่าเล่นอนิเมชันงับลูกธนู (BiteArrow) + Particle งับ
        }

        speed = 0f;
        direction = 0f;

        if (cachedRb != null)
        {
            cachedRb.linearVelocity = new Vector2(0f, -4f); // พุ่งดิ่งลงพื้นอย่างรวดเร็ว
            cachedRb.gravityScale = 2f;
        }
    }

    /// <summary>
    /// ปักลูกธนูลงพื้นดิน สั่นสะเทือนเล็กน้อย และเริ่ม Coroutine ค่อยๆ จางหายไป
    /// </summary>
    private void StickIntoGround(float surfaceY)
    {
        if (isStuckInGround || isDestroyed) return;
        isStuckInGround = true;

        // ปิดการคำนวณฟิสิกส์และการชน เพื่อให้ลูกธนูหยุดนิ่งสนิท
        if (cachedRb != null)
        {
            cachedRb.linearVelocity = Vector2.zero;
            cachedRb.gravityScale = 0f;
            cachedRb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        // ตรึงตำแหน่งแกน Y ให้อยู่ระดับผิวดิน
        transform.position = new Vector3(transform.position.x, surfaceY, transform.position.z);

        // ปักเอียงตามมุมสุ่มเล็กน้อยให้ดูสมจริง
        float slant = UnityEngine.Random.Range(-stickAngleVariation, stickAngleVariation);
        transform.rotation = Quaternion.Euler(0f, 0f, 90f + slant);

        // เสียงปักพื้นทึบๆ แน่นๆ
        AudioManager.Instance?.PlayRockHit();

        // พ่นฝุ่นผงเล็กๆ ตรงจุดที่ลูกธนูปักลงดิน
        SpawnGroundDust(new Vector3(transform.position.x, surfaceY, 0f));

        StartCoroutine(StickAndFadeRoutine());
    }

    private void SpawnGroundDust(Vector3 pos)
    {
        if (cachedGroundDustPrefab == null)
        {
            cachedGroundDustPrefab = Resources.Load<GameObject>("FX/GroundDustImpactFX");
        }
        if (cachedGroundDustPrefab != null)
        {
            var dust = Instantiate(cachedGroundDustPrefab, pos, Quaternion.identity);
            dust.transform.localScale = Vector3.one * 0.45f;
        }
    }

    private System.Collections.IEnumerator StickAndFadeRoutine()
    {
        // 1. อาการสั่นกระดิก (Quiver / Twang) ตอนปักพื้น 0.15 วินาที
        float quiverDuration = 0.15f;
        float quiverTimer = 0f;
        Quaternion baseRot = transform.rotation;
        while (quiverTimer < quiverDuration)
        {
            quiverTimer += Time.deltaTime;
            float progress = quiverTimer / quiverDuration;
            float quiverAngle = Mathf.Sin(progress * Mathf.PI * 6f) * 4f * (1f - progress);
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, quiverAngle);
            yield return null;
        }
        transform.rotation = baseRot;

        // 2. ปักคาพื้นไว้ให้เห็นชัดเจนตามระยะเวลา stickDuration
        if (stickDuration > 0f)
        {
            yield return new WaitForSeconds(stickDuration);
        }

        // 3. ค่อยๆ จางหายไปอย่างนุ่มนวล (Fade Out)
        float fadeTimer = 0f;
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (fadeTimer / fadeDuration));
            SetRenderersAlpha(alpha);
            yield return null;
        }
        SetRenderersAlpha(0f);

        // 4. ทำลายตัวเองหรือคืนเข้า Pool
        DestroySelf();
    }

    private void SetRenderersAlpha(float alpha)
    {
        if (cachedRenderers == null || initialRendererColors == null) return;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null && i < initialRendererColors.Length)
            {
                Color baseCol = initialRendererColors[i];
                cachedRenderers[i].color = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * alpha);
            }
        }
    }

    private bool isDestroyed = false;
    
    // ห่อฟังก์ชัน Destroy ไว้เรียกง่ายๆ จะได้เคลียร์ Event ด้วย (รองรับ Object Pooling)
    public void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        StopAllCoroutines();

        if (onDestroy != null)
        {
            onDestroy.Invoke(this); // บอก ArrowShooter ว่าชั้นไปแล้วนะ (ยิงนัดต่อไปได้ทันที)
            onDestroy = null;
        }

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
                cachedRb.bodyType = RigidbodyType2D.Dynamic;
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
