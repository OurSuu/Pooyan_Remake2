using UnityEngine;

// สถานะของแม่หมู ว่าทำอะไรอยู่
public enum PlayerState
{
    Normal,         // ปกติ ขยับได้ ยิงธนูได้
    HoldingMeat,    // ถือเนื้ออยู่ ยิงธนูไม่ได้ แต่ปาเนื้อได้
    Dead            // ร่วงตายไปแล้ว
}

/// <summary>
/// สคริปต์ควบคุมแม่หมูบนกระเช้า — คุมการเลื่อนขึ้นลง, การหยิบเนื้อ, และการโดนดาเมจ
/// </summary>
[RequireComponent(typeof(ArrowShooter))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.2f; // ความเร็วขยับกระเช้า (ปรับให้คล่องตัวตามต้นฉบับ NES)
    [SerializeField] private float minY = -3.5f; // ขอบล่างสุดที่ลงได้
    [SerializeField] private float maxY = 3.5f; // ขอบบนสุดที่ขึ้นได้
    [SerializeField] private float topPickupThreshold = 0.22f; // ระยะที่ยอมให้หยิบเนื้อได้ตอนขึ้นไปถึงรอกด้านบน

    [Header("Meat Pickup")]
    [SerializeField] private Transform meatSpawnPoint; // จุดที่จะปาเนื้อออกไป
    [SerializeField] private GameObject meatPrefab; // พรีแฟบก้อนเนื้อ
    [SerializeField] private MeatPickupIndicator meatIndicator; // ตัวใบ้ว่ามีเนื้อให้เก็บ
    [SerializeField] private GameObject heldMeatVisual; // กราฟิกตอนถือเนื้อโชว์ให้เห็น

    [Header("Shoot Animation")]
    [SerializeField] private SpriteRenderer mamaPigRenderer; // ตัวเรนเดอร์แม่หมู
    [SerializeField] private Sprite spriteIdle;   // รูปตอนยืนเฉยๆ (เผื่อไม่ได้ใช้อนิเมเตอร์)
    [SerializeField] private Sprite[] idleSprites; // รูปตอนยืนเฉยๆ แบบหลายเฟรม
    [SerializeField] private float idleFrameRate = 12f; // ความเร็วสลับเฟรม
    [SerializeField] private Sprite spriteShoot;  // รูปตอนยิงธนู
    [SerializeField] private float shootSpriteTime = 0.15f; // เวลากระตุกท่ายิง
    [SerializeField] private string shootAnimName = "Shoot"; // ชื่อแอนิเมชันยิง
    [SerializeField] public string deadAnimName = "Fall"; // ชื่อแอนิเมชันตกกระเช้า
    [SerializeField] public float deathFloorY = -4.5f; // จุดตกถึงพื้นตอนตาย
    [SerializeField] private string idleAnimName = "Idle"; // ชื่อแอนิเมชันยืนเฉยๆ

    public PlayerState State { get; private set; } = PlayerState.Normal;
    public float MinY => minY;
    public float MaxY => maxY;
    
    // เช็คว่าแม่หมูอยู่บนสุดหรือยัง (ใช้ระยะเผื่ออย่างน้อย 0.20 หน่วยเพื่อให้หยิบเนื้อได้ลื่นไหล)
    public bool IsAtTop => Mathf.Abs(transform.position.y - maxY) <= Mathf.Max(0.20f, topPickupThreshold);

    private ArrowShooter arrowShooter;
    private bool meatAvailable; // มีเนื้อโผล่ให้เก็บมั้ย
    private Vector3 startPosition;
    private bool bossApproaching; // ถ้าบอสมา เนื้อจะหายไป
    private Coroutine shootSpriteRoutine;
    private Animator animator;

    // จำตำแหน่งและ Layer ดั้งเดิมของแม่หมูไว้ เพื่อคืนค่าตอนเกิดใหม่
    private Vector3 initialMamaPigLocalPos;
    private int initialMamaPigSortingOrder;
    private bool hasSavedMamaPigPos = false;

    private void Awake()
    {
        startPosition = transform.position; // จำจุดเกิดไว้
        arrowShooter = GetComponent<ArrowShooter>();
        if (arrowShooter == null) arrowShooter = GetComponentInChildren<ArrowShooter>();

        if (meatSpawnPoint == null)
        {
            Transform fp = transform.Find("FirePoint");
            if (fp != null) meatSpawnPoint = fp;
        }

        CacheMamaPigReference();
        FindAnimator();
    }

    // ค้นหา Animator ของแม่หมูให้ครบทุกจุด ทั้งตัวเองและลูก
    private void FindAnimator()
    {
        if (mamaPigRenderer != null)
        {
            animator = mamaPigRenderer.GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Transform mamaPigObj = transform.Find("MamaPig");
            if (mamaPigObj == null) mamaPigObj = transform.Find("WinterMamaPig");
            if (mamaPigObj == null) mamaPigObj = transform.Find("SpringMamaPig");
            if (mamaPigObj != null)
            {
                animator = mamaPigObj.GetComponent<Animator>();
            }
        }
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    // แคชตัวแม่หมูและจำตำแหน่งเริ่มต้นไว้
    private void CacheMamaPigReference()
    {
        if (mamaPigRenderer == null)
        {
            // พยายามหาในลูกที่มีชื่อเฉพาะ
            Transform childPig = transform.Find("WinterMamaPig");
            if (childPig == null) childPig = transform.Find("SpringMamaPig");
            if (childPig == null) childPig = transform.Find("MamaPig");
            if (childPig != null) mamaPigRenderer = childPig.GetComponent<SpriteRenderer>();

            // ถ้ายังไม่เจอ ให้หา SpriteRenderer ที่ไม่ใช่เกราะหรือกระเช้า
            if (mamaPigRenderer == null)
            {
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in renderers)
                {
                    if (r.gameObject != gameObject && !r.name.Contains("Armor") && !r.name.Contains("Basket"))
                    {
                        mamaPigRenderer = r;
                        break;
                    }
                }
            }
        }

        if (mamaPigRenderer != null && !hasSavedMamaPigPos)
        {
            initialMamaPigLocalPos = mamaPigRenderer.transform.localPosition;
            initialMamaPigSortingOrder = mamaPigRenderer.sortingOrder;
            hasSavedMamaPigPos = true;
        }
    }

    private void Start()
    {
        CacheMamaPigReference();
        FindAnimator();
    }

    private void Update()
    {
        // ถ้าเกมหยุด หรืออยู่ในฉากโบนัส ไม่ต้องทำอะไร
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying && !GameManager.Instance.IsBonus)
            return;

        // ถ้าขิตไปแล้วก็ไม่ต้องบังคับละ
        if (State == PlayerState.Dead) return;

        HandleMovement();
        HandleFireInput();
        UpdateIdleAnimation(); // ปรับท่าทางยืนเฉยๆ
    }

    // จัดการเปลี่ยน Sprite แบบไม่ง้อ Animator (เอาไว้ใช้เคสที่ใส่แค่รูป)
    private void UpdateIdleAnimation()
    {
        // ถ้ายิงอยู่ หรือใช้ Animator อยู่แล้ว ข้ามไปเลยไม่ต้องทำอะไร
        if (shootSpriteRoutine != null || mamaPigRenderer == null || animator != null) return; 

        if (idleSprites != null && idleSprites.Length > 0)
        {
            // สลับเฟรมตามเวลา
            int index = (int)(Time.time * idleFrameRate) % idleSprites.Length;
            mamaPigRenderer.sprite = idleSprites[index];
        }
        else if (spriteIdle != null)
        {
            mamaPigRenderer.sprite = spriteIdle; // ยืนแข็งเป็นหิน
        }
    }

    // จับการกดปุ่มยิง
    private void HandleFireInput()
    {
        // ปุ่มยิง: คลิกซ้าย, Spacebar, หรือปุ่ม Z
        if (!Input.GetButtonDown("Fire1") && !Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Z)) return;

        if (State == PlayerState.HoldingMeat)
        {
            ThrowMeat(); // ถ้าถือเนื้ออยู่ก็ปาเนื้อไป
            return;
        }

        // ในด่านโบนัสปาเนื้อ (Meat Bonus Stage) แม่หมูจะยิงลูกธนูไม่ได้เลย ได้แค่หยิบและปาเนื้ออย่างเดียว!
        if (isMeatBonusMode)
        {
            return;
        }

        if (State == PlayerState.Normal)
        {
            if (arrowShooter.TryShoot()) // ถ้ายิงธนูออก
                PlayShootSprite();       // ก็เล่นท่ายิงด้วย
        }
    }

    // เล่นอนิเมชันยิง
    private void PlayShootSprite()
    {
        if (State == PlayerState.Dead) return; // ตายแล้ว ห้ามเริ่มท่ายิง

        if (animator != null)
        {
            // ถ้ามีคอร์รูทีนเก่าค้างอยู่ก็สั่งหยุดก่อน จะได้ไม่ตีกัน
            if (shootSpriteRoutine != null) StopCoroutine(shootSpriteRoutine);
            shootSpriteRoutine = StartCoroutine(ShootAnimRoutine());
            return;
        }

        if (mamaPigRenderer == null || spriteShoot == null) return;
        if (shootSpriteRoutine != null) StopCoroutine(shootSpriteRoutine);
        shootSpriteRoutine = StartCoroutine(ShootSpriteRoutine());
    }

    // คอร์รูทีนคุมอนิเมเตอร์ตอนยิง
    private System.Collections.IEnumerator ShootAnimRoutine()
    {
        animator.Play(shootAnimName, -1, 0f); // บังคับเล่นแอนิเมชันยิงตั้งแต่เฟรมแรก
        yield return new WaitForSeconds(shootSpriteTime);
        
        // ต้องมั่นใจว่ายังไม่ตาย ถึงจะสั่งกลับไปเล่นท่า Idle ได้!
        if (State != PlayerState.Dead && animator != null)
        {
            animator.Play(idleAnimName); // กลับมายืนปกติ
        }
        shootSpriteRoutine = null;
    }

    // คอร์รูทีนสลับรูปตอนยิง (ใช้ตอนไม่มีอนิเมเตอร์)
    private System.Collections.IEnumerator ShootSpriteRoutine()
    {
        if (animator != null) animator.enabled = false;
        
        mamaPigRenderer.sprite = spriteShoot;
        yield return new WaitForSeconds(shootSpriteTime);
        
        if (State != PlayerState.Dead)
        {
            if (animator != null) animator.enabled = true;
        }
        shootSpriteRoutine = null;
    }

    // เลื่อนกระเช้าขึ้นลง
    private void HandleMovement()
    {
        float v = Input.GetAxisRaw("Vertical"); // รับค่า ขึ้น-ลง (W/S หรือลูกศร)
        Vector3 pos = transform.position;
        pos.y += v * moveSpeed * Time.deltaTime; // คำนวณตำแหน่งใหม่
        pos.y = Mathf.Clamp(pos.y, minY, maxY); // ล็อกไม่ให้ทะลุเพดานหรือมุดดิน
        transform.position = pos;

        // ถ้าถึงยอดแล้วมีเนื้อให้เก็บ ก็หยิบเลย!
        if (State == PlayerState.Normal && meatAvailable && IsAtTop)
        {
            PickUpMeat();
        }
    }

    private bool isMeatBonusMode = false;

    /// <summary>
    /// เปิด/ปิดโหมดด่านโบนัสเนื้อ (ยิงธนูไม่ได้ ได้แค่หยิบและปาเนื้อรัวๆ ตามต้นฉบับ NES)
    /// </summary>
    public void SetMeatBonusMode(bool enabled)
    {
        isMeatBonusMode = enabled;
        if (enabled)
        {
            bossApproaching = false;
            if (arrowShooter != null) arrowShooter.SetCanShoot(false);
            SetMeatAvailable(true);
        }
        else
        {
            if (State == PlayerState.Normal && arrowShooter != null)
                arrowShooter.SetCanShoot(true);
        }
    }

    // บอกแม่หมูว่ามีเนื้อให้เก็บนะ
    public void SetMeatAvailable(bool available)
    {
        meatAvailable = available && !bossApproaching; // แต่ถ้าบอสมาก็ห้ามเก็บเด้อ
        meatIndicator?.SetAvailable(meatAvailable);
    }

    // ถ้าบอสมา ทุกอย่างจะตึงเครียด ห้ามเก็บเนื้อ
    public void SetBossApproaching(bool approaching)
    {
        bossApproaching = approaching;
        if (approaching) meatAvailable = false;
        meatIndicator?.SetAvailable(meatAvailable);
    }

    public bool IsMeatAvailable => meatAvailable;

    // หยิบเนื้อ!
    private void PickUpMeat()
    {
        meatAvailable = false;
        meatIndicator?.SetAvailable(false);
        
        if (meatPrefab == null) return; 

        State = PlayerState.HoldingMeat; // เปลี่ยนร่างเป็นโหมดแบกเนื้อ
        arrowShooter.SetCanShoot(false); // ปิดการยิงธนู
        if (heldMeatVisual != null) heldMeatVisual.SetActive(true); // โชว์กราฟิกถือเนื้อ
        ScoreManager.Instance?.AddMeatPickupScore(transform.position); // ได้คะแนนหยิบเนื้อด้วย
        AudioManager.Instance?.PlayMeatPickup(); // เสียงหยิบเนื้อจากรอกด้านบน!
    }

    // ปาเนื้อออออ!
    private void ThrowMeat()
    {
        if (meatPrefab != null)
        {
            Vector3 spawnPos = meatSpawnPoint != null ? meatSpawnPoint.position : (transform.position + new Vector3(-0.9f, 0f, 0f));
            Instantiate(meatPrefab, spawnPos, Quaternion.identity); // ปล่อยก้อนเนื้อออกไป!
            AudioManager.Instance?.PlayMeatThrow(); // เล่นเสียงฟึบ!
        }

        ScoreManager.Instance?.ResetMeatCombo(); // รีเซ็ตคอมโบนับใหม่

        State = PlayerState.Normal; // กลับมาว่างเปล่า
        if (heldMeatVisual != null) heldMeatVisual.SetActive(false); // ซ่อนกราฟิกถือเนื้อ

        if (isMeatBonusMode)
        {
            // ในโหมดโบนัสเนื้อ: ห้ามยิงธนู และเสกเนื้อก้อนใหม่รอที่ด้านบนทันทีหลังปาเสร็จ!
            arrowShooter.SetCanShoot(false);
            StartCoroutine(ReloadBonusMeatRoutine());
        }
        else
        {
            arrowShooter.SetCanShoot(true); // ยิงธนูต่อได้ในด่านปกติ
        }
    }

    private System.Collections.IEnumerator ReloadBonusMeatRoutine()
    {
        yield return new WaitForSeconds(0.25f);
        if (isMeatBonusMode && State == PlayerState.Normal)
        {
            SetMeatAvailable(true);
        }
    }

    private bool diedFromBoulder = false; // เช็คว่าตายเพราะโดนหินทับมั้ย (อาจจะมีอนิเมชันต่างกัน)

    public void SetDiedFromBoulder(bool value)
    {
        diedFromBoulder = value;
    }

    // โดนดาเมจ! ร่วงกระเช้าตาย!
    public void TakeDamage()
    {
        if (State == PlayerState.Dead) return; // ตายซ้ำไม่ได้นะ

        State = PlayerState.Dead;
        AudioManager.Instance?.SetRopePullSound(false);
        AudioManager.Instance?.PlayPlayerFall(); // เสียงแม่หมูร่วงตกกระเช้า!

        // สั่งหยุดคอร์รูทีนยิงธนูทันที! ป้องกันไม่ให้ดีเลย์ยิงเสร็จเด้งกลับมาสั่ง animator.Play("Idle") กลางอากาศ
        if (shootSpriteRoutine != null)
        {
            StopCoroutine(shootSpriteRoutine);
            shootSpriteRoutine = null;
        }
        
        CacheMamaPigReference();

        // ปิดการยิงธนูและซ่อนก้อนเนื้อ
        if (arrowShooter != null) arrowShooter.SetCanShoot(false);
        if (heldMeatVisual != null) heldMeatVisual.SetActive(false);

        // ดัน Sorting Order ของแม่หมูขึ้นมาอยู่หน้ากระเช้า เพื่อให้เวลาเด้งตกลงมา ตัวแม่หมูจะลอยออกมาข้างหน้ากระเช้าสวยๆ
        if (mamaPigRenderer != null)
        {
            mamaPigRenderer.sortingOrder = 15;
        }

        if (animator == null)
        {
            FindAnimator();
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.Play(deadAnimName, -1, 0f); // เล่นท่าตกกระเช้า (Fall) ทันที
            animator.Update(0f); // บังคับให้อนิเมเตอร์สลับเป็นสเตท Fall ทันทีในเฟรมนี้
        }

        GameManager.Instance?.LoseLife(diedFromBoulder); // บอกเกมว่าหัก 1 ชีวิต
        StartCoroutine(DeathBounceRoutine()); // เด้งร่วงลงพื้นดุ๊กดิ๊กๆ
    }

    // คอร์รูทีนเด้งตาย (ขยับเฉพาะตัวแม่หมู ตัวกระเช้ายังคงลอยค้างอยู่ที่เดิม!)
    private System.Collections.IEnumerator DeathBounceRoutine()
    {
        Transform pigTransform = mamaPigRenderer != null ? mamaPigRenderer.transform : transform;
        float t = 0;
        Vector3 startPos = pigTransform.position;
        
        // กระเด็นไปข้างหน้า (หันซ้าย แกน x ต้องติดลบ) และร่วงลง
        float velocityX = -2.5f; // เด้งไปทางซ้าย (พุ่งมาข้างหน้า)
        float velocityY = 3.0f; // เด้งลอยขึ้นนิดนึง
        float gravity = 15f; // โดนแรงโน้มถ่วงดึงลงอย่างไว
        
        while (State == PlayerState.Dead)
        {
            t += Time.deltaTime;

            // ล็อกท่าให้เล่น Fall เสมอระหว่างกำลังกระเด็นตก ไม่ให้สเตทอื่นแซงได้
            if (animator != null && animator.enabled)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(deadAnimName))
                {
                    animator.Play(deadAnimName, -1, 0f);
                }
            }

            // สมการโปรเจกไทล์แบบบ้านๆ เลย
            float newX = startPos.x + (velocityX * t);
            float newY = startPos.y + (velocityY * t) - (0.5f * gravity * t * t);
            
            // เช็คว่าชนพื้นหรือยัง
            if (newY <= deathFloorY)
            {
                newY = deathFloorY;
                pigTransform.position = new Vector3(newX, newY, startPos.z);
                // (อนาคต: สั่งเล่นท่านอนตายตรงนี้ได้เลย ถ้างบถึงทำแอนิเมชันเพิ่ม)
                break; // ชนพื้นละ หลุดลูปโลด
            }

            pigTransform.position = new Vector3(newX, newY, startPos.z);
            
            yield return null; // รอเฟรมถัดไป
            if (t > 2.5f) break; // กันเหนียว เผื่อบั๊กตกทะลุโลก GameManager รีฉากใน 2 วิอยู่ละ
        }
    }

    // เกิดใหม่!
    public void Respawn()
    {
        State = PlayerState.Normal;
        diedFromBoulder = false;
        transform.position = startPosition; // กระเช้ากลับจุดเริ่มต้น

        if (shootSpriteRoutine != null)
        {
            StopCoroutine(shootSpriteRoutine);
            shootSpriteRoutine = null;
        }
        
        // คืนตำแหน่งและ Sorting Order ของแม่หมูกลับเข้ากระเช้า
        if (mamaPigRenderer != null && hasSavedMamaPigPos)
        {
            mamaPigRenderer.transform.localPosition = initialMamaPigLocalPos;
            mamaPigRenderer.transform.localRotation = Quaternion.identity;
            mamaPigRenderer.sortingOrder = initialMamaPigSortingOrder;
        }

        if (heldMeatVisual != null) heldMeatVisual.SetActive(false); // เอาเนื้อออก
        
        if (animator != null)
        {
            animator.Play(idleAnimName); // ยืนนิ่งๆ เตรียมพร้อม
        }
        else if (mamaPigRenderer != null && spriteIdle != null)
        {
            mamaPigRenderer.sprite = spriteIdle;
        }
        
        if (arrowShooter != null)
        {
            arrowShooter.SetCanShoot(true); // ปลดล็อกปืน เอ้ย! ปลดล็อกธนู
        }
    }

    // โดนก้อนหินหรือโปรเจกไทล์ศัตรูปาใส่
    public void OnProjectileHit()
    {
        AudioManager.Instance?.PlayRockHit(); // เสียงหินกระทบกระเช้า ปั้ก!
    }
}

