using UnityEngine;

public enum WolfState
{
    WalkingBeforeDrop,
    BlowingBalloon,
    Floating,
    Falling,
    WalkingToLadder,
    WaitingForStep,     
    Climbing,
    WaitingOnLadder,
    Biting,
    ReachedCliff,
    Dead
}

/// <summary>
/// Normal wolf â€” floating, shield, rock throw, ladder/cliff behavior.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Wolf : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Balloon balloon;
    [SerializeField] protected GameObject balloonPrefab; // เผื่ออยากลาก Balloon Prefab มาใส่ใน Inspector
    [SerializeField] protected Transform shieldTransform;
    [SerializeField] protected GameObject rockPrefab;

    [Header("Animation / Sprites")]
    [SerializeField] protected Sprite spriteDrop;   // Static sprite for descending (until you have an Animation)
    [SerializeField] protected Sprite spriteWaitLadder; // Static sprite for waiting on ladder
    [SerializeField] protected string waitLadderAnimName = "WaitLadder";
    [SerializeField] protected Sprite spriteFly;    // Static sprite for ascending (until you have an Animation)
    [SerializeField] protected Sprite spriteFall;   // Static sprite for falling after balloon popped (optional)
    [SerializeField] protected Sprite spritePushIdle; // ภาพยืนดันหินค้าง
    [SerializeField] protected string pushAnimName = "Push"; // ชื่ออนิเมชันท่าเดินดันหิน

    protected SpriteRenderer cachedSR;
    protected Animator animator;

    [Header("Movement")]
    [SerializeField] protected float fallSpeed = 4f;
    [SerializeField] protected float walkSpeed = 1.85f;
    [SerializeField] protected float climbSpeed = 1.05f;
    [Header("Ground Run Audio")]
    [SerializeField] protected bool playGroundRunSFX = true;
    [SerializeField] protected float groundRunStepInterval = 0.22f;
    protected float targetDropX;

    [Header("Shield")]
    [SerializeField] protected float shieldToggleInterval = 1.5f;

        [Header("Rock Throw")]
    [SerializeField] protected float rockThrowInterval = 3f;

    protected WolfState state = WolfState.WalkingBeforeDrop;
    public bool IsActiveTarget => 
        state == WolfState.WalkingBeforeDrop || 
        state == WolfState.BlowingBalloon || 
        state == WolfState.Floating;
    protected float floatSpeed;
    protected float baseFloatSpeed;
    protected bool hasShield;
    protected bool canThrowRock;
    protected bool rapidFire = false;

    [Header("Heights")]
    [SerializeField] protected float walkGroundY = -3.5f;
    [SerializeField] protected float deadGroundY = -4.5f;
    [SerializeField] protected float cliffHitY = 3.5f;
    [SerializeField] protected GameObject groundDustPrefab; // พรีแฟบ Particle System ควัน/ฝุ่นตลบตอนตกถึงพื้นตาย
    [SerializeField] protected GameObject biteArrowFXPrefab; // พรีแฟบ Particle ตอนงับลูกธนู
    [SerializeField] protected GameObject biteLadderFXPrefab; // พรีแฟบ Particle ตอนงับบนบันได

    protected bool isDescendingStage;
    protected float groundY;
    protected float cliffY;
    protected float shieldTimer;
    protected float rockTimer;
    protected bool shieldUp;

    
    protected int? reservedStepIndex = null;

    public WolfState State => state;
    public bool IsAlive => state != WolfState.Dead;
    public float CurrentVerticalVelocity => state == WolfState.Floating ? (isDescendingStage ? -floatSpeed : floatSpeed) : 0f;

    // เก็บลิสต์หมาป่าที่ Active อยู่ในฉากเพื่อไม่ต้องเรียก FindObjectsByType<Wolf>() ซ้ำทุกเฟรม
    private static readonly System.Collections.Generic.List<Wolf> activeWolves = new();
    public static System.Collections.Generic.IReadOnlyList<Wolf> ActiveWolves => activeWolves;

    private static GameObject cachedGroundDustPrefab;
    private static GameObject cachedBiteArrowFXPrefab;
    private static GameObject cachedBiteLadderFXPrefab;
    private static readonly System.Collections.Generic.List<ParticleSystem> groundDustPool = new();
    private static readonly System.Collections.Generic.List<ParticleSystem> biteArrowPool = new();
    private static readonly System.Collections.Generic.List<ParticleSystem> biteLadderPool = new();
    private static int wolfFXPoolSceneHandle = -1;
    private static readonly int AnimHashRun = Animator.StringToHash("run");
    private static readonly int AnimHashClimb = Animator.StringToHash("climb");
    private static readonly int AnimHashClimbUpper = Animator.StringToHash("Climb");

    protected void PlayFloatAnimation()
    {
        if (animator == null || !animator.enabled) return;

        string targetAnim = isDescendingStage ? floatDownAnimName : floatUpAnimName;
        if (!animator.HasState(0, Animator.StringToHash(targetAnim)))
        {
            if (animator.HasState(0, Animator.StringToHash("Float"))) targetAnim = "Float";
            else if (animator.HasState(0, Animator.StringToHash("Drop"))) targetAnim = "Drop";
        }
        animator.Play(targetAnim);
    }

    protected virtual void OnEnable()
    {
        if (!activeWolves.Contains(this))
            activeWolves.Add(this);
    }

    protected virtual void OnDisable()
    {
        activeWolves.Remove(this);
    }

    protected virtual void Awake()
    {
        state = WolfState.Dead;
        
        // Find Animator on child first (this is the Gpx child, NOT the Balloon)
        foreach (Transform child in transform)
        {
            animator = child.GetComponent<Animator>();
            if (animator != null)
            {
                cachedSR = child.GetComponent<SpriteRenderer>();
                break;
            }
        }
        
        // Fallback: if no Animator found, find any child SpriteRenderer that is NOT on the Balloon
        if (cachedSR == null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Balloon>() != null) continue; // skip Balloon
                cachedSR = child.GetComponent<SpriteRenderer>();
                if (cachedSR != null) break;
            }
        }

        // ป้องกันกรณีลืมต่อ หรือเผลอลาก Prefab Asset จาก Project มาใส่แทน Child ตัวลูกในตัวหมาป่า
        if (balloon == null || !balloon.transform.IsChildOf(transform))
        {
            var childBalloon = GetComponentInChildren<Balloon>(true);
            if (childBalloon != null)
            {
                balloon = childBalloon;
            }
        }

        // ถ้ายังไม่มี balloon แต่มี balloonPrefab ลากไว้ใน Inspector ให้เสกออกมาเป็นลูก
        if (balloon == null && balloonPrefab != null)
        {
            var bObj = Instantiate(balloonPrefab, transform);
            bObj.transform.localPosition = new Vector3(-0.16f, 2.16f, 0f);
            balloon = bObj.GetComponent<Balloon>();
        }

        // ถ้าลูกโป่งตัวลูกยังไม่มี Sprite แต่มี balloonPrefab ให้ดึง Sprite จาก prefab มาใส่ให้อัตโนมัติ
        if (balloon != null && balloonPrefab != null)
        {
            var mySr = balloon.GetComponent<SpriteRenderer>();
            var prefabSr = balloonPrefab.GetComponent<SpriteRenderer>();
            if (mySr != null && mySr.sprite == null && prefabSr != null && prefabSr.sprite != null)
            {
                mySr.sprite = prefabSr.sprite;
            }
        }

        PrewarmGroundDustPool();
    }

    private void PrewarmGroundDustPool()
    {
        CartoonFXHelper.Prewarm(12);
        int currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (wolfFXPoolSceneHandle != currentScene)
        {
            wolfFXPoolSceneHandle = currentScene;
            groundDustPool.Clear();
            biteArrowPool.Clear();
            biteLadderPool.Clear();
        }

        PrewarmWolfFXPool(ref cachedGroundDustPrefab, groundDustPrefab, "FX/GroundDustImpactFX", groundDustPool, 3);
        PrewarmWolfFXPool(ref cachedBiteArrowFXPrefab, biteArrowFXPrefab, "FX/WolfBiteArrowFX", biteArrowPool, 2);
        PrewarmWolfFXPool(ref cachedBiteLadderFXPrefab, biteLadderFXPrefab, "FX/WolfBiteLadderFX", biteLadderPool, 2);
    }

    private static void PrewarmWolfFXPool(
        ref GameObject cachedPrefab,
        GameObject inspectorPrefab,
        string resourcePath,
        System.Collections.Generic.List<ParticleSystem> pool,
        int targetCount)
    {
        GameObject fxPrefab = ResolveWolfFXPrefab(ref cachedPrefab, inspectorPrefab, resourcePath);
        if (fxPrefab == null || pool.Count >= targetCount) return;

        int needed = targetCount - pool.Count;
        for (int i = 0; i < needed; i++)
        {
            var ps = CreatePooledWolfFX(fxPrefab);
            if (ps != null)
            {
                ps.gameObject.SetActive(false);
                pool.Add(ps);
            }
        }
    }

    private static GameObject ResolveWolfFXPrefab(ref GameObject cachedPrefab, GameObject inspectorPrefab, string resourcePath)
    {
        if (inspectorPrefab != null) return inspectorPrefab;
        if (cachedPrefab == null)
            cachedPrefab = Resources.Load<GameObject>(resourcePath);
        return cachedPrefab;
    }

    private static ParticleSystem CreatePooledWolfFX(GameObject fxPrefab)
    {
        var go = Instantiate(fxPrefab, new Vector3(0f, -100f, 0f), Quaternion.identity);
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Disable;
        }
        return ps;
    }

    private static void PlayPooledWolfFX(
        ref GameObject cachedPrefab,
        GameObject inspectorPrefab,
        string resourcePath,
        System.Collections.Generic.List<ParticleSystem> pool,
        Vector3 spawnPos,
        float uniformScale = 1f,
        Color? tintColor = null)
    {
        GameObject fxPrefab = ResolveWolfFXPrefab(ref cachedPrefab, inspectorPrefab, resourcePath);
        if (fxPrefab == null) return;

        ParticleSystem pooledPS = null;
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            var p = pool[i];
            if (p == null)
            {
                pool.RemoveAt(i);
                continue;
            }
            if (!p.gameObject.activeSelf)
            {
                pooledPS = p;
                break;
            }
        }

        if (pooledPS == null)
        {
            pooledPS = CreatePooledWolfFX(fxPrefab);
            if (pooledPS != null) pool.Add(pooledPS);
        }

        if (pooledPS == null) return;

        if (tintColor.HasValue)
        {
            var main = pooledPS.main;
            main.startColor = tintColor.Value;
        }
        pooledPS.transform.localScale = Vector3.one * uniformScale;
        pooledPS.transform.position = spawnPos;
        pooledPS.gameObject.SetActive(true);
        pooledPS.Clear(true);
        pooledPS.Play(true);
    }

    public void CopySettingsTo(Wolf target)
    {
        if (target == null) return;
        target.balloon = this.balloon;
        target.balloonPrefab = this.balloonPrefab;
        target.shieldTransform = this.shieldTransform;
        target.rockPrefab = this.rockPrefab;
        target.spriteDrop = this.spriteDrop;
        target.spriteWaitLadder = this.spriteWaitLadder;
        target.waitLadderAnimName = this.waitLadderAnimName;
        target.spriteFly = this.spriteFly;
        target.spriteFall = this.spriteFall;
        target.spritePushIdle = this.spritePushIdle;
        target.pushAnimName = this.pushAnimName;
        target.fallSpeed = this.fallSpeed;
        target.walkSpeed = this.walkSpeed;
        target.climbSpeed = this.climbSpeed;
        target.shieldToggleInterval = this.shieldToggleInterval;
        target.rockThrowInterval = this.rockThrowInterval;
        target.walkGroundY = this.walkGroundY;
        target.deadGroundY = this.deadGroundY;
        target.cliffHitY = this.cliffHitY;
        target.groundDustPrefab = this.groundDustPrefab;
        target.biteArrowFXPrefab = this.biteArrowFXPrefab;
        target.biteLadderFXPrefab = this.biteLadderFXPrefab;
    }

    public void DisableTeaseBounces()
    {
        teaseBounces = 0;
    }

    public void SetFormationLock(bool locked)
    {
        formationLocked = locked;
        if (locked) teaseBounces = 0;
    }

    /// <summary>
    /// คืนค่าพิกัดแกน X กึ่งกลางของลูกโป่งในเลนของหมาป่าตัวนี้ เพื่อให้ลูกธนูที่ถูกงับ/ปัดร่วงลงมาตรงกับแนวลูกโป่งของหมาป่าตัวล่างพอดี
    /// </summary>
    public float GetBalloonColumnWorldX()
    {
        if (balloon != null)
        {
            var col = balloon.GetComponent<Collider2D>();
            if (col != null && col.enabled)
                return col.bounds.center.x;
            return balloon.transform.position.x;
        }
        return transform.position.x - 0.11f;
    }

    public void SetRapidFire(bool enabled)
    {
        rapidFire = enabled;
        if (enabled) canThrowRock = true;
    }

    public virtual void Initialize(
        LevelConfig config,
        bool descending,
        float speed,
        bool shield,
        bool rockThrow,
        float dropX = 0f,
        float[] lanes = null,
        int laneIndex = 0,
        int? overrideBalloonHP = null,
        BalloonColorType? overrideColorType = null)
    {
        isDescendingStage = descending;
        
        // ONLY float speed is controlled by LevelConfig (so it can be randomized per wolf)
        // walkSpeed, climbSpeed, and fallSpeed will strictly use the values you set in the Inspector!
        baseFloatSpeed = speed;
        floatSpeed = speed;
        hasShield = shield;
        canThrowRock = rockThrow;
        groundY = walkGroundY;
        cliffY = cliffHitY;
        reservedStepIndex = null;
        
        availableLanes = lanes;
        currentLane = laneIndex;

        targetDropX = dropX;
        formationLocked = false;
        teaseBounces = Random.value < 0.25f ? Random.Range(1, 4) : 0; // 25% chance to fake out 1-3 times
        state = WolfState.WalkingBeforeDrop;
        
        gameObject.tag = GameConstants.TagEnemy;

        // คำนวณเลือดและสีของลูกโป่ง:
        // - ด่านคี่ (ลอยลง: ด่าน 1 Autumn, ด่าน 3 Winter): ลูกโป่งทุกสีโดนนัดเดียวแตก (1 HP) เสมอ!
        //   โดยด่าน 3 (Winter) จะสุ่มสีลูกโป่งระหว่าง สีแดง กับ สีฟ้า (นัดเดียวแตกทั้งคู่ตาม NES)
        // - ด่านคู่ (ลอยขึ้น: ด่าน 2 Spring, ด่าน 4 Rainy): ลูกโป่ง 3 HP (ยิงตอด 3 นัดแตก)
        int targetBalloonHP = 1;
        BalloonColorType targetColor = BalloonColorType.AutoByHP;

        if (overrideBalloonHP.HasValue)
        {
            targetBalloonHP = overrideBalloonHP.Value;
        }
        else if (descending)
        {
            targetBalloonHP = 1;
        }
        else if (config != null && config.balloonHP > 1)
        {
            targetBalloonHP = config.balloonHP;
        }
        else
        {
            targetBalloonHP = 3;
        }

        if (overrideColorType.HasValue)
        {
            targetColor = overrideColorType.Value;
        }
        else if (descending)
        {
            int currentStage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : 1;
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isStage3OrHigherOdd = currentStage >= 3 ||
                activeSceneName.IndexOf("Winter", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                activeSceneName.IndexOf("Summer", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (isStage3OrHigherOdd)
            {
                // ด่าน 3 (Winter) / ด่าน 5 (Summer) สุ่มลูกโป่งสีแดงกับสีฟ้า (ทั้งคู่ 1 HP นัดเดียวแตก)
                targetColor = Random.value < 0.5f ? BalloonColorType.LightBlue : BalloonColorType.PinkRed;
            }
            else
            {
                targetColor = BalloonColorType.PinkRed;
            }
        }
        else if (targetBalloonHP >= 3)
        {
            targetColor = BalloonColorType.LightBlue;    // ด่านคู่ 3 ทีแตก = สีฟ้า
        }
        else if (targetBalloonHP == 2)
        {
            targetColor = BalloonColorType.YellowOrange;
        }
        else
        {
            targetColor = BalloonColorType.PinkRed;
        }

        if (balloon != null)
        {
            balloon.Initialize(this, targetBalloonHP, bossFlash: false, colorType: targetColor);
            balloon.gameObject.SetActive(false);
        }

        if (shieldTransform != null)
        {
            shieldTransform.gameObject.SetActive(hasShield);
            shieldTransform.gameObject.tag = GameConstants.TagShield;
        }

        shieldTimer = shieldToggleInterval;
        rockTimer = Random.Range(0.5f, 1.5f);
        if (balloon != null) balloon.gameObject.SetActive(false);

        // Reset visuals
        if (cachedSR != null) cachedSR.flipX = false;
        if (animator != null) animator.enabled = true;
    }

    /// <summary>
    /// เรียกเมื่อลูกโป่งโดนยิงแล้วยังไม่แตก — ความเร็วจะลดลงตามลูกโป่งที่หดตัว
    /// </summary>
    public virtual void OnBalloonDamaged(int currentHP, int maxHP)
    {
        if (maxHP > 1)
        {
            int currentStage = GameManager.Instance != null ? GameManager.Instance.EffectiveDifficultyStage : 2;
            float minFactor = currentStage >= 4 ? 0.64f : 0.56f;
            float hpRatio = Mathf.Clamp01((float)currentHP / Mathf.Max(1, maxHP));
            floatSpeed = baseFloatSpeed * Mathf.Lerp(minFactor, 1f, hpRatio);
        }
    }

    protected virtual void Update()
    {
        if (state == WolfState.Dead) return;
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze) return; // หยุดการทำงานของศัตรูเมื่อแม่หมูตาย

        switch (state)
        {
            case WolfState.WalkingBeforeDrop:
                UpdateWalkingBeforeDrop();
                break;
            case WolfState.BlowingBalloon:
                break;
            case WolfState.Floating:
                UpdateFloating();
                break;
            case WolfState.Falling:
                UpdateFalling();
                break;
            case WolfState.WalkingToLadder:
                UpdateWalkToLadder();
                break;
            case WolfState.WaitingForStep:
                UpdateWaitingForStep();
                break;
            case WolfState.Climbing:
                UpdateClimbing();
                break;
            case WolfState.WaitingOnLadder:
                UpdateWaitingOnLadder();
                break;
            case WolfState.ReachedCliff:
                UpdateReachedCliff();
                break;
            case WolfState.Dead:
                break;
        }
    }

    protected bool isPushIdle = false;

    protected void UpdateReachedCliff()
    {
        var boulderSys = BoulderSystem.Instance;
        if (boulderSys == null) return;

        bool isPushingMember = boulderSys.IsWolfPushing(this);
        Vector3 targetPos = boulderSys.GetCliffTargetPosition(this);
        float distance = Mathf.Abs(transform.position.x - targetPos.x);

        // เฟส 1: หมาป่าที่เพิ่งขึ้นมาถึงหน้าผา ต้องวิ่ง (ท่า Run) ไปให้ถึงจุดต่อท้ายเพื่อน/หลังก้อนหินก่อน (หินยังไม่ขยับ)
        if (!isPushingMember)
        {
            if (distance > 0.03f)
            {
                bool movingRight = targetPos.x >= transform.position.x;
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPos.x, targetPos.y, 0f), walkSpeed * Time.deltaTime);

                if (animator != null)
                {
                    if (!animator.enabled) animator.enabled = true;
                    string runAnim = !string.IsNullOrEmpty(runAnimName) ? runAnimName : "Run";
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName(runAnim))
                    {
                        animator.Play(runAnim);
                    }
                }

                if (cachedSR != null)
                    cachedSR.flipX = !movingRight;

                isPushIdle = false;
                return;
            }

            // วิ่งมาถึงจุดต่อท้ายเรียบร้อยแล้ว! แจ้ง BoulderSystem เพื่อเริ่มออกแรงดันหินพร้อมกับเพื่อนๆ ทันที
            transform.position = new Vector3(targetPos.x, targetPos.y, 0f);
            boulderSys.NotifyWolfArrivedAtBoulder(this);
            targetPos = boulderSys.GetCliffTargetPosition(this);
            distance = Mathf.Abs(transform.position.x - targetPos.x);
        }

        // เฟส 2: อยู่ในกลุ่มดันหินแล้ว — ขณะที่หินกำลังเลื่อน ทุกตัวในแถวจะเล่นท่า Push ดันหินไปพร้อมกัน!
        if (distance > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPos.x, targetPos.y, 0f), walkSpeed * Time.deltaTime);

            if (animator != null)
            {
                if (!animator.enabled) animator.enabled = true;
                string animToPlay = pushAnimName;
                if (!animator.HasState(0, Animator.StringToHash(animToPlay)))
                {
                    if (animator.HasState(0, Animator.StringToHash("PushBoulder"))) animToPlay = "PushBoulder";
                    else if (animator.HasState(0, Animator.StringToHash("PushBoluder"))) animToPlay = "PushBoluder";
                    else if (animator.HasState(0, Animator.StringToHash("Push"))) animToPlay = "Push";
                }
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(animToPlay))
                {
                    animator.Play(animToPlay);
                }
            }

            if (cachedSR != null)
                cachedSR.flipX = false; // หันหน้าไปทางขวาเข้าหาก้อนหินเสมอตอนช่วยกันออกแรงผลัก

            isPushIdle = false;
        }
        else
        {
            // ดันหินขยับเสร็จ 1 จังหวะแล้ว ยืนยันหินค้างไว้รอเพื่อนตัวถัดไปมาช่วย
            if (!isPushIdle)
            {
                isPushIdle = true;
                if (animator != null) animator.enabled = false;

                if (cachedSR != null)
                {
                    cachedSR.flipX = false; // บังคับหันหน้าเข้าหาหินเสมอตอนยืนรอคิว
                    if (spritePushIdle != null)
                        cachedSR.sprite = spritePushIdle;
                }
            }
        }
    }

    protected int teaseBounces = 0;
    protected bool formationLocked = false;
    protected int currentLane = 0;
    protected float[] availableLanes;

    protected void StartBlowingBalloon()
    {
        state = WolfState.BlowingBalloon;
        if (animator != null && animator.enabled) animator.Play("BlowBalloon");
        BalloonColorType bColor = balloon != null ? balloon.AssignedColorType : BalloonColorType.AutoByHP;
        AudioManager.Instance?.PlayBalloonInflateAt(transform.position.x, bColor, this is BossWolf);
        StartCoroutine(BlowBalloonRoutine());
    }

    private System.Collections.IEnumerator BlowBalloonRoutine()
    {
        // รอให้เล่นท่าเป่าลูกโป่ง
        yield return new WaitForSeconds(0.5f); 
        
        // ลูกโป่งโผล่มา/เด้งขึ้นบนหัว
        if (balloon != null) balloon.gameObject.SetActive(true);
        
        // รอแป๊บนึงก่อนโดดลง
        yield return new WaitForSeconds(0.3f);
        
        if (state != WolfState.BlowingBalloon) yield break; // in case killed
        
        state = WolfState.Floating;
        SetFloatingVisual();
    }

    protected virtual void UpdateWalkingBeforeDrop()
    {
        // เล่นเสียงวิ่งเฉพาะตอนเดินบนหญ้าด้านล่าง (ด่านคู่) เท่านั้น ไม่เล่นตอนอยู่บนกิ่งไม้/ต้นไม้ด้านบน
        if (!isDescendingStage)
        {
            TriggerGroundRunSFX(1.02f);
        }
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetDropX, transform.position.y, 0f), walkSpeed * Time.deltaTime);
        if (Mathf.Abs(transform.position.x - targetDropX) < 0.05f)
        {
            bool laneClear = true;
            if (!formationLocked)
            {
                for (int i = 0; i < activeWolves.Count; i++)
                {
                    var w = activeWolves[i];
                    if (w == null || w == this) continue;
                    
                    // เช็คว่ามีหมาป่าตัวอื่นอยู่ในเลนเป้าหมายเดียวกับเราไหม หรือกำลังจะไปที่เลนเดียวกัน
                    if (Mathf.Abs(w.transform.position.x - targetDropX) < 0.2f || Mathf.Abs(w.targetDropX - targetDropX) < 0.2f)
                    {
                        // 1. ถ้าตัวอื่นกำลังเป่าลูกโป่ง หรือกำลังลอยลงมา (Floating) ในเลนนี้ในระยะประชิดเกิน 1.15 หน่วย
                        if (w.state == WolfState.BlowingBalloon || w.state == WolfState.Floating)
                        {
                            if (Mathf.Abs(w.transform.position.y - transform.position.y) < 1.15f)
                            {
                                laneClear = false;
                                break;
                            }
                        }
                        // 2. ถ้าตัวอื่นก็กำลังเดินไปที่จุดดรอปเดียวกันเป๊ะๆ และยืนทับกันอยู่
                        else if (w.state == WolfState.WalkingBeforeDrop && Mathf.Abs(w.targetDropX - targetDropX) < 0.2f)
                        {
                            if (Mathf.Abs(w.transform.position.x - transform.position.x) < 0.65f)
                            {
                                laneClear = false;
                                break;
                            }
                        }
                    }
                }
            }

            if (!formationLocked && (teaseBounces > 0 || !laneClear))
            {
                if (teaseBounces > 0) teaseBounces--;
                
                if (availableLanes != null && availableLanes.Length > 0)
                {
                    int nextLane = Random.Range(0, availableLanes.Length);
                    if (nextLane == currentLane) nextLane = (nextLane + 1) % availableLanes.Length;
                    currentLane = nextLane;
                    targetDropX = availableLanes[currentLane];
                }
                else
                {
                    // Fallback if no array is provided
                    int nextLane = Random.Range(0, 4);
                    if (nextLane == currentLane) nextLane = (nextLane + 1) % 4;
                    currentLane = nextLane;
                    targetDropX = -2f + (4f * (currentLane / 3f));
                }
                
                // Flip X to face the direction wolf is running towards
                if (cachedSR != null) cachedSR.flipX = targetDropX < transform.position.x;
            }
            else
            {
                // Start blowing balloon before floating down
                StartBlowingBalloon();
            }
        }
    }

    /// <summary>
    /// Pause Animator and show the appropriate static sprite for floating.
    /// </summary>
    [SerializeField] protected string floatDownAnimName = "Drop";
    [SerializeField] protected string floatUpAnimName = "Drop";

    protected void SetFloatingVisual()
    {
        if (animator != null)
        {
            animator.enabled = true;
            PlayFloatAnimation();
        }
        else 
        {
            Sprite target = isDescendingStage ? spriteDrop : spriteFly;
            if (target != null && cachedSR != null)
            {
                cachedSR.sprite = target;
            }
        }
        // Reset flip for floating (face left towards player)
        if (cachedSR != null) cachedSR.flipX = false;
    }

    [SerializeField] protected string fallAnimName = "Fall";

    protected void SetFallingVisual()
    {
        if (animator != null && animator.HasState(0, Animator.StringToHash(fallAnimName)))
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.Play(fallAnimName);
        }
        else if (spriteFall != null && cachedSR != null)
        {
            if (animator != null) animator.enabled = false;
            cachedSR.sprite = spriteFall;
        }
    }

    protected virtual void UpdateFloating()
    {
        float dir = isDescendingStage ? -1f : 1f;
        transform.position += Vector3.up * (dir * floatSpeed * Time.deltaTime);

        if (hasShield) UpdateShield();
        if (canThrowRock) UpdateRockThrow();

        float targetY = isDescendingStage ? groundY : cliffY;
        if (isDescendingStage && transform.position.y <= targetY)
            OnReachedGround();
        else if (!isDescendingStage && transform.position.y >= targetY)
            OnReachedCliff();
    }

    protected void UpdateShield()
    {
        shieldTimer -= Time.deltaTime;
        if (shieldTimer <= 0f)
        {
            shieldUp = !shieldUp;
            shieldTimer = shieldToggleInterval;
            if (shieldTransform != null)
                shieldTransform.gameObject.SetActive(shieldUp);
        }
    }


    protected void UpdateRockThrow()
    {
        rockTimer -= Time.deltaTime;
        if (rockTimer <= 0f && rockPrefab != null)
        {
            // ขยายช่วงความสูงที่อนุญาตให้ปาหินเล็กน้อย
            if (transform.position.y < -2.85f || transform.position.y > 3.10f)
            {
                rockTimer = 0.30f;
                return;
            }

            // เพิ่มโควต้าหินบนจอเล็กน้อย (4 ก้อน) และปาถี่ขึ้นอีกนิด
            if (!rapidFire && WolfProjectile.ActiveRockCount >= 4)
            {
                rockTimer = 0.35f; // รอแล้วค่อยเช็คใหม่
                return;
            }

            rockTimer = rapidFire ? 0.5f : Mathf.Min(rockThrowInterval, 1.85f);
            StartCoroutine(ThrowRockRoutine());
        }
    }

    private System.Collections.IEnumerator ThrowRockRoutine()
    {
        // 1. เล่นท่าปาหิน
        if (animator != null && animator.enabled) animator.Play("ThrowRock");
        
        // 2. รอจังหวะง้างมือ (สมมติว่า 0.2 วินาที)
        yield return new WaitForSeconds(0.2f); 
        
        // ถ้าถูกยิงตายหรือตกพื้นไปก่อนที่จะปา ก็ยกเลิกการปา
        if (state != WolfState.Floating) yield break;

        // เช็คโควต้าอีกครั้งก่อนปล่อยหิน (ถ้าไม่ปา ให้กลับท่าลอยปกติ ไม่ค้างท่า ThrowRock)
        if (!rapidFire && WolfProjectile.ActiveRockCount >= 3)
        {
            PlayFloatAnimation();
            yield break;
        }
        
        // 3. เสกหินแล้วปาออกไป
        if (rockPrefab != null)
        {
            var rock = Instantiate(rockPrefab, transform.position, Quaternion.identity);
            rock.GetComponent<WolfProjectile>()?.LaunchAtPlayer(WolfProjectileType.Rock);
            AudioManager.Instance?.PlayRockThrow();
        }

        // 4. รอท่าปาหินจบ (0.3 วินาที) แล้วกลับสู่ท่าลอยตัวปกติ
        yield return new WaitForSeconds(0.3f); 
        
        if (state == WolfState.Floating)
        {
            PlayFloatAnimation();
        }
    }

    [SerializeField] protected string deadAnimName = "Dead";

    protected virtual void UpdateFalling()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        if (transform.position.y <= deadGroundY)
        {
            transform.position = new Vector3(transform.position.x, deadGroundY, 0f);
            state = WolfState.Dead;

            // นับคิลทันทีที่หมาป่าตกกระแทกพื้น (ไม่ต้องรอเฟดจาง 1 วินาที)
            GetComponent<WolfDeathNotifier>()?.ReportKillNow();

            SpawnGroundDust(); // เสกฝุ่นฟุ้งกระจายตอนกระแทกพื้น
            
            if (animator != null)
            {
                animator.enabled = true;
                string animToPlay = deadAnimName;
                if (!animator.HasState(0, Animator.StringToHash(animToPlay)))
                {
                    if (animator.HasState(0, Animator.StringToHash("Death"))) animToPlay = "Death";
                    else if (animator.HasState(0, Animator.StringToHash("Dead"))) animToPlay = "Dead";
                }
                animator.Play(animToPlay);
            }
            
            StartCoroutine(FadeAndDestroyRoutine());
        }
    }

    private static Color GetStageDustColor()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.IndexOf("Winter", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return new Color(0.86f, 0.94f, 1f, 0.70f);
        if (sceneName.IndexOf("Rainy", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return new Color(0.68f, 0.84f, 0.98f, 0.70f);
        if (sceneName.IndexOf("Spring", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return new Color(0.92f, 0.86f, 0.76f, 0.70f);
        return new Color(0.88f, 0.80f, 0.68f, 0.70f);
    }

    /// <summary>
    /// เสก Particle Effect ควันฟุ้งกระจายตรงพื้นตอนหมาป่าตกกระแทกพื้นตาย
    /// </summary>
    protected void SpawnGroundDust()
    {
        Vector3 spawnPos = new Vector3(transform.position.x, deadGroundY, 0f);
        PlayPooledWolfFX(
            ref cachedGroundDustPrefab,
            groundDustPrefab,
            "FX/GroundDustImpactFX",
            groundDustPool,
            spawnPos,
            1f,
            GetStageDustColor());
    }

    protected System.Collections.IEnumerator FadeAndDestroyRoutine()
    {
        SpriteRenderer sr = cachedSR;
        if (sr != null)
        {
            float duration = 1.0f;
            float time = 0;
            Color c = sr.color;
            while (time < duration)
            {
                time += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, time / duration);
                sr.color = c;
                yield return null;
            }
        }
        DestroyWolf();
    }

    protected void MarkAsEscaped()
    {
        var notifier = GetComponent<WolfDeathNotifier>();
        if (notifier != null)
        {
            notifier.MarkEscaped();
            WolfTracker.Instance?.NotifyEscapedToGround();
        }
        WolfTracker.Instance?.UnregisterTargetWolf(this);
    }

    [SerializeField] protected string runAnimName = "Run";

    protected void SetRunningVisual()
    {
        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(runAnimName);
        }
    }

    protected virtual void OnReachedGround()
    {
        state = WolfState.WalkingToLadder;
        if (balloon != null) balloon.gameObject.SetActive(false);
        SetRunningVisual();
        MarkAsEscaped();
    }

    protected virtual void OnReachedCliff()
    {
        // ถ้าเป็นด่านโบนัสปาเนื้อ หมาป่าที่ลอยพ้นขอบบนให้หายไปเลย (ไม่ไปสะสมผลักหินยักษ์ เพราะด่านโบนัสห้ามตาย!)
        if (GameManager.Instance != null && GameManager.Instance.IsBonus)
        {
            FindAnyObjectByType<BonusStageManager>()?.OnMeatBonusWolfEscaped();
            DestroyWolf();
            return;
        }

        state = WolfState.ReachedCliff;
        isPushIdle = false;
        canThrowRock = false;
        hasShield = false;
        if (shieldTransform != null) shieldTransform.gameObject.SetActive(false);
        if (balloon != null) balloon.gameObject.SetActive(false);
        SetRunningVisual();
        MarkAsEscaped();

        BoulderSystem.Instance?.RegisterWolfOnCliff(this);
    }

    protected void TriggerGroundRunSFX(float pitchScale = 1f)
    {
        if (!playGroundRunSFX || Time.deltaTime <= 0f || AudioManager.Instance == null) return;
        AudioManager.Instance.NotifyWolfGrassRun(GetInstanceID(), transform.position.x, pitchScale, groundRunStepInterval);
    }

    protected void UpdateWalkToLadder()
    {
        if (animator != null && animator.enabled)
        {
            int hash = animator.HasState(0, AnimHashRun) ? AnimHashRun : Animator.StringToHash(runAnimName);
            animator.Play(hash);
        }
        var ladder = LadderSystem.Instance;
        if (ladder == null) { DestroyWolf(); return; }

        TriggerGroundRunSFX(0.95f);
        float targetX = ladder.LadderX;
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetX, groundY, 0f), walkSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
        {
            
            var stepIndex = ladder.TryReserveStepForWolf(this);
            if (stepIndex.HasValue)
            {
                reservedStepIndex = stepIndex.Value;
                state = WolfState.Climbing;
                AudioManager.Instance?.PlayLadderClimb();
            }
            else
            {
                
                state = WolfState.WaitingForStep;
            }
        }
    }

    
    protected void UpdateWaitingForStep()
    {
        var ladder = LadderSystem.Instance;
        if (ladder == null) { DestroyWolf(); return; }
        var stepIndex = ladder.TryReserveStepForWolf(this);
        if (stepIndex.HasValue)
        {
            reservedStepIndex = stepIndex.Value;
            state = WolfState.Climbing;
            AudioManager.Instance?.PlayLadderClimb();
        }
    }

    protected void UpdateClimbing()
    {
        if (animator != null && animator.enabled)
        {
            int hash = animator.HasState(0, AnimHashClimb) ? AnimHashClimb : AnimHashClimbUpper;
            animator.Play(hash);
        }
        var ladder = LadderSystem.Instance;
        if (ladder == null) { DestroyWolf(); return; }

        float stepY = ladder.GetStepY(this, reservedStepIndex);
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(ladder.LadderX, stepY, 0f), climbSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.y - stepY) < 0.05f)
        {
            state = WolfState.WaitingOnLadder;
            ladder.OnWolfReachedStep(this, reservedStepIndex.Value);
            
            if (cachedSR != null) cachedSR.flipX = false;
        }
    }

    [Header("Ladder Bite Settings (การงับบนบันได)")]
    [Tooltip("ระยะขยับตัวยื่นขณะงับ (ค่าเริ่มต้น 0 = งับอยู่กับที่บนบันได ไม่พุ่งเลยออกนอกบันได)")]
    [SerializeField] protected float ladderBiteLunge = 0f;
    [Tooltip("ระยะเวลาการพุ่งงับ (วินาที, ค่าเริ่มต้น 0.45s ให้ตรงกับความยาวอนิเมชัน BiteRobe 0.47s)")]
    [SerializeField] protected float biteDuration = 0.45f;
    [Tooltip("ระยะห่างแนวตั้ง (Y) ที่ถือว่าอยู่ในระยะปากงับ (ค่าเริ่มต้น 0.95 หน่วย ครอบคลุมทั้งตัวแม่หมูและกระเช้า ไร้มุมอับระหว่างขั้นบันได)")]
    [SerializeField] protected float biteVerticalReach = 0.95f;
    [Tooltip("ออฟเซ็ตตำแหน่งปากหมาป่าเทียบกับจุดกึ่งกลางตัว (ค่าเริ่มต้น +0.25 หน่วย)")]
    [SerializeField] protected float biteVerticalOffset = 0.25f;

    protected float biteTimer = 0f;
    protected float biteInterval = 2f; // Arcade accurate
    protected bool isBiting = false;
    protected float ladderOriginX = 0f;

    protected Vector3 GetMouthWorldPosition()
    {
        float faceDir = (cachedSR != null && cachedSR.flipX) ? 1f : -1f;
        float yOffset = (state == WolfState.WaitingOnLadder || state == WolfState.Biting) ? biteVerticalOffset : 0.1f;
        return transform.position + new Vector3(faceDir * 0.42f, yOffset, 0f);
    }

    protected void SpawnLadderBiteFX(bool isHit)
    {
        Vector3 fxPos = GetMouthWorldPosition();
        PlayPooledWolfFX(
            ref cachedBiteLadderFXPrefab,
            biteLadderFXPrefab,
            "FX/WolfBiteLadderFX",
            biteLadderPool,
            fxPos,
            isHit ? 1.15f : 0.95f);
    }

    /// <summary>
    /// ตรวจสอบว่าผู้เล่น (แม่หมู/กระเช้า) อยู่ในระยะที่หมาป่าบนบันไดจะงับได้หรือไม่
    /// </summary>
    public bool IsPlayerInBiteRange()
    {
        var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
        if (player == null || player.State == PlayerState.Dead || player.IsInvincible) return false;

        float wolfX = transform.position.x;
        float playerX = player.transform.position.x;
        // เช็คระยะแนวนอน (X) ว่าอยู่เลนเดียวกัน (ห่างไม่เกิน 1.8 หน่วย)
        if (Mathf.Abs(playerX - wolfX) > 1.8f) return false;

        float playerY = player.transform.position.y;
        float mouthY = transform.position.y + biteVerticalOffset;
        // เช็คระยะแนวตั้ง (Y) ให้ครอบคลุมความสูงของกระเช้าและตัวแม่หมู
        return Mathf.Abs(playerY - mouthY) <= biteVerticalReach;
    }

    protected void UpdateWaitingOnLadder()
    {
        if (!isBiting)
        {
            if (spriteWaitLadder != null && cachedSR != null)
            {
                if (animator != null) animator.enabled = false; // ต้องปิด Animator ไม่งั้นมันจะบังคับเปลี่ยนรูปกลับ
                cachedSR.sprite = spriteWaitLadder;
            }
            else if (animator != null)
            {
                animator.speed = 0f; 
            }
        }
        
        biteTimer += Time.deltaTime;
        SpriteRenderer sr = cachedSR;
        
        if (!isBiting)
        {
            float warningTime = 0.35f;

            // ถ้าแม่หมูเลื่อนกระเช้าเข้ามาในระยะงับพอดี ให้เร่งเวลาเตือนภัยแล้วเตรียมงับทันที (ตอบสนองไว ไม่ยืนนิ่งเฉย)
            if (IsPlayerInBiteRange() && (biteInterval - biteTimer) > warningTime)
            {
                biteTimer = biteInterval - warningTime;
            }
            
            if (biteTimer >= biteInterval)
            {
                // เริ่มท่างับอยู่บนบันได (ไม่ออกนอกขั้นบันได) + ปล่อย Particle งับ
                isBiting = true;
                biteTimer = 0f;
                ladderOriginX = transform.position.x;

                // Visual feedback: ตัวแดงเตือนภัยขณะงับ
                if (sr != null) sr.color = new Color(1f, 0.3f, 0.3f);

                if (animator != null)
                {
                    animator.enabled = true;
                    animator.Update(0f);
                    animator.speed = 1f;
                    animator.Play("BiteRobe", -1, 0f);
                }
                SpawnLadderBiteFX(isHit: false);
            }
            else if (biteInterval - biteTimer <= warningTime)
            {
                // Warning phase: กะพริบสีส้มเตือนผู้เล่นอย่างรวดเร็ว
                if (sr != null)
                {
                    float blink = Mathf.PingPong(Time.time * 20f, 1f);
                    sr.color = Color.Lerp(Color.white, new Color(1f, 0.7f, 0f), blink);
                }
            }
            else
            {
                // Idle phase: สีปกติ
                if (sr != null) sr.color = Color.white;
            }
        }
        else
        {
            // เช็คว่าแม่หมูอยู่ในระยะงับหรือไม่
            if (IsPlayerInBiteRange())
            {
                TriggerBite();
                return;
            }

            float baseX = ladderOriginX != 0f ? ladderOriginX : (LadderSystem.Instance != null ? LadderSystem.Instance.LadderX : transform.position.x);
            if (ladderBiteLunge > 0.001f)
            {
                float t = biteTimer / biteDuration;
                float lungeOffset = Mathf.Sin(t * Mathf.PI) * ladderBiteLunge;
                transform.position = new Vector3(baseX - lungeOffset, transform.position.y, 0f);
            }
            else
            {
                transform.position = new Vector3(baseX, transform.position.y, 0f);
            }

            if (biteTimer >= biteDuration)
            {
                // จบจังหวะงับ ยืนจุดเดิมบนบันได
                transform.position = new Vector3(baseX, transform.position.y, 0f);
                isBiting = false;
                biteTimer = 0f;
                biteInterval = UnityEngine.Random.Range(1.5f, 3f);

                if (sr != null) sr.color = Color.white;
            }
        }
    }

    public virtual void OnMeatHit()
    {
        if (state == WolfState.WalkingToLadder ||
            state == WolfState.Climbing ||
            state == WolfState.WaitingOnLadder ||
            state == WolfState.ReachedCliff ||
            state == WolfState.Dead)
        {
            return;
        }

        // Release the balloon without popping it
        if (balloon != null)
        {
            balloon.ReleaseInstant();
        }

        // Transition to falling
        state = WolfState.Falling;
        WolfTracker.Instance?.UnregisterTargetWolf(this);
        SetFallingVisual();
        AudioManager.Instance?.PlayWolfFall();

        canThrowRock = false;
        hasShield = false;
        if (shieldTransform != null)
        {
            shieldTransform.gameObject.SetActive(false);
        }
    }

    public virtual void OnBalloonPopped()
    {
        
        
        if (state == WolfState.WalkingToLadder ||
            state == WolfState.Climbing ||
            state == WolfState.WaitingOnLadder ||
            state == WolfState.ReachedCliff ||
            state == WolfState.Dead)
        {
            return;
        }

        
        state = WolfState.Falling;
        WolfTracker.Instance?.UnregisterTargetWolf(this);
        SetFallingVisual();
        AudioManager.Instance?.PlayWolfFall();

        
        
        canThrowRock = false;
        hasShield = false;
        if (shieldTransform != null)
        {
            shieldTransform.gameObject.SetActive(false);
        }
    }

    public virtual void KillByMeat()
    {
        if (state == WolfState.Dead) return;
        state = WolfState.Dead;
        WolfTracker.Instance?.UnregisterTargetWolf(this);
        if (balloon != null) balloon.gameObject.SetActive(false);
        DestroyWolf();
    }

    protected virtual void OnDestroy()
    {
        WolfTracker.Instance?.UnregisterTargetWolf(this);
    }

    protected void DestroyWolf()
    {
        WolfTracker.Instance?.UnregisterTargetWolf(this);
        if (reservedStepIndex.HasValue)
        {
            LadderSystem.Instance?.ReleaseStepByIndex(reservedStepIndex.Value, this);
            reservedStepIndex = null;
        }
        else
        {
            LadderSystem.Instance?.ReleaseStep(this);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Called by LadderSystem when the wolf triggers a bite event.
    /// </summary>
    public void PlayDeflectAnimation()
    {
        PlayDeflectAnimation(null, spawnBiteFX: false);
    }

    public void PlayDeflectAnimation(Vector3? hitWorldPos, bool spawnBiteFX = true)
    {
        if (animator != null && animator.enabled)
        {
            animator.Play("BiteArrow", -1, 0f);
            // Restore animation after a brief moment in case the Animator isn't set up to return automatically
            StartCoroutine(RestoreAnimAfterBite());
        }

        if (spawnBiteFX)
        {
            Vector3 fxPos = hitWorldPos ?? GetMouthWorldPosition();
            PlayPooledWolfFX(
                ref cachedBiteArrowFXPrefab,
                biteArrowFXPrefab,
                "FX/WolfBiteArrowFX",
                biteArrowPool,
                fxPos,
                1f);
        }
    }

    private System.Collections.IEnumerator RestoreAnimAfterBite()
    {
        yield return new WaitForSeconds(0.3f);
        if (animator != null && animator.enabled)
        {
            if (state == WolfState.Floating) PlayFloatAnimation();
            else if (state == WolfState.Falling) SetFallingVisual();
            else if (state == WolfState.WalkingToLadder || state == WolfState.WalkingBeforeDrop) animator.Play(runAnimName);
        }
    }

    public void TriggerBite()
    {
        if (state != WolfState.WaitingOnLadder) return;
        var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
        if (player != null && player.IsInvincible) return;
        state = WolfState.Biting;
        AudioManager.Instance?.PlayWolfBite();
        SpawnLadderBiteFX(isHit: true);
        StartCoroutine(BiteAndDestroyRoutine());
    }

    private System.Collections.IEnumerator BiteAndDestroyRoutine()
    {
        if (animator != null) 
        {
            animator.speed = 1f; 
            animator.Play("BiteRobe");
        }
        
        // รอให้ท่ากัดเล่นจนจบก่อนค่อยหักเลือดและทำลายตัวเอง (สมมติท่ากัดยาว 0.5 วิ)
        yield return new WaitForSeconds(0.5f);
        var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
        player?.TakeDamage();
        DestroyWolf();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state == WolfState.Falling)
        {
            if (other.CompareTag(GameConstants.TagPlayer))
            {
                other.GetComponentInParent<PlayerController>()?.TakeDamage();
            }
        }
        else if (state == WolfState.WaitingOnLadder && isBiting)
        {
            // ถ้าหมาป่ากำลังพุ่งงับแล้วตัวแม่หมูหรือเกราะกระเช้าชนกับ Collider ของหมาป่า
            if (other.CompareTag(GameConstants.TagPlayer) || other.CompareTag(GameConstants.TagShield))
            {
                TriggerBite();
            }
        }
    }
}









































