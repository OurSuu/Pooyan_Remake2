using UnityEngine;

public enum WolfState
{
    WalkingBeforeDrop,
    Floating,
    Falling,
    WalkingToLadder,
    WaitingForStep,     // เพิ่มสถานะใหม่
    Climbing,
    WaitingOnLadder,
    Biting,
    ReachedCliff,
    Dead
}

/// <summary>
/// Normal wolf — floating, shield, rock throw, ladder/cliff behavior.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Wolf : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Balloon balloon;
    [SerializeField] protected Transform shieldTransform;
    [SerializeField] protected GameObject rockPrefab;

    [Header("Animation / Sprites")]
    [SerializeField] protected Sprite spriteDrop;   // Static sprite for descending (until you have an Animation)
    [SerializeField] protected Sprite spriteFly;    // Static sprite for ascending (until you have an Animation)
    [SerializeField] protected Sprite spriteFall;   // Static sprite for falling after balloon popped (optional)

    protected SpriteRenderer cachedSR;
    protected Animator animator;

    [Header("Movement")]
    [SerializeField] protected float fallSpeed = 4f;
    [SerializeField] protected float walkSpeed = 2f;
    [SerializeField] protected float climbSpeed = 1.5f;
    protected float targetDropX;

    [Header("Shield")]
    [SerializeField] protected float shieldToggleInterval = 1.5f;

    [Header("Rock Throw")]
    [SerializeField] protected float rockThrowInterval = 3f;

    protected WolfState state = WolfState.WalkingBeforeDrop;
    protected float floatSpeed;
    protected bool hasShield;
    protected bool canThrowRock;

    [Header("Heights")]
    [SerializeField] protected float walkGroundY = -3.5f;
    [SerializeField] protected float deadGroundY = -4.5f;
    [SerializeField] protected float cliffHitY = 3.5f;

    protected bool isDescendingStage;
    protected float groundY;
    protected float cliffY;
    protected float shieldTimer;
    protected float rockTimer;
    protected bool shieldUp;

    // จดจำว่าตัวเองจอง step หรือยัง (จำ index หรือ ref ที่ ReserveStep คืนมา)
    protected int? reservedStepIndex = null;

    public WolfState State => state;
    public bool IsAlive => state != WolfState.Dead;

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

        // Auto-assign balloon if the Inspector reference is broken or missing
        if (balloon == null)
        {
            balloon = GetComponentInChildren<Balloon>(true);
        }
    }

    public virtual void Initialize(LevelConfig config, bool descending, float speed, bool shield, bool rockThrow, float dropX = 0f, float[] lanes = null, int laneIndex = 0)
    {
        isDescendingStage = descending;
        
        // ONLY float speed is controlled by LevelConfig (so it can be randomized per wolf)
        // walkSpeed, climbSpeed, and fallSpeed will strictly use the values you set in the Inspector!
        floatSpeed = speed;
        hasShield = shield;
        canThrowRock = rockThrow;
        groundY = walkGroundY;
        cliffY = cliffHitY;
        reservedStepIndex = null;
        
        availableLanes = lanes;
        currentLane = laneIndex;

        targetDropX = dropX;
        teaseBounces = Random.value < 0.25f ? Random.Range(1, 4) : 0; // 25% chance to fake out 1-3 times
        state = WolfState.WalkingBeforeDrop;
        
        gameObject.tag = GameConstants.TagEnemy;

        if (balloon != null)
            balloon.Initialize(this, config.balloonHP);

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

    protected virtual void Update()
    {
        if (state == WolfState.Dead) return;

        switch (state)
        {
            case WolfState.WalkingBeforeDrop:
                UpdateWalkingBeforeDrop();
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

    protected void UpdateReachedCliff()
    {
        var boulderSys = BoulderSystem.Instance;
        if (boulderSys == null) return;
        
        Vector3 targetPos = boulderSys.GetCliffTargetPosition(this);
        
        // Move horizontally towards the target slot
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPos.x, transform.position.y, 0f), walkSpeed * Time.deltaTime);
        
        // Sprite flip to face moving direction
        if (cachedSR != null && Mathf.Abs(transform.position.x - targetPos.x) > 0.05f)
        {
            cachedSR.flipX = targetPos.x < transform.position.x;
        }
    }

    protected int teaseBounces = 0;
    protected int currentLane = 0;
    protected float[] availableLanes;

    protected virtual void UpdateWalkingBeforeDrop()
    {
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetDropX, transform.position.y, 0f), walkSpeed * Time.deltaTime);
        if (Mathf.Abs(transform.position.x - targetDropX) < 0.05f)
        {
            bool laneClear = true;
            // Check if there is another wolf currently dropping right below us in the same lane
            var wolves = FindObjectsByType<Wolf>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var w in wolves)
            {
                if (w == this) continue;
                if ((w.state == WolfState.Falling || w.state == WolfState.WalkingBeforeDrop) && Mathf.Abs(w.transform.position.x - targetDropX) < 0.1f)
                {
                    if (Mathf.Abs(w.transform.position.y - transform.position.y) < 2.0f)
                    {
                        laneClear = false;
                        break;
                    }
                }
            }

            if (teaseBounces > 0 || !laneClear)
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
                // Transition to Floating: pause Animator and show static sprite
                state = WolfState.Floating;
                if (balloon != null) balloon.gameObject.SetActive(true);
                SetFloatingVisual();
            }
        }
    }

    /// <summary>
    /// Pause Animator and show the appropriate static sprite for floating.
    /// </summary>
    protected void SetFloatingVisual()
    {
        if (animator != null) animator.enabled = false;
        Sprite target = isDescendingStage ? spriteDrop : spriteFly;
        if (target != null && cachedSR != null)
        {
            cachedSR.sprite = target;
        }
        // Reset flip for floating (face left towards player)
        if (cachedSR != null) cachedSR.flipX = false;
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
            rockTimer = rockThrowInterval;
            var rock = Instantiate(rockPrefab, transform.position, Quaternion.identity);
            rock.GetComponent<WolfProjectile>()?.LaunchAtPlayer(WolfProjectileType.Rock);
        }
    }

    protected void SetFallingVisual()
    {
        if (animator != null) animator.enabled = false;
        if (spriteFall != null && cachedSR != null)
        {
            cachedSR.sprite = spriteFall;
        }
    }

    protected virtual void UpdateFalling()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        if (transform.position.y <= deadGroundY)
        {
            transform.position = new Vector3(transform.position.x, deadGroundY, 0f);
            state = WolfState.Dead;
            StartCoroutine(FadeAndDestroyRoutine());
        }
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
    }

    protected virtual void OnReachedGround()
    {
        state = WolfState.WalkingToLadder;
        if (balloon != null) balloon.gameObject.SetActive(false);

        MarkAsEscaped();
    }

    protected virtual void OnReachedCliff()
    {
        state = WolfState.ReachedCliff;
        if (balloon != null) balloon.gameObject.SetActive(false);

        MarkAsEscaped();

        BoulderSystem.Instance?.RegisterWolfOnCliff(this);
    }

    protected void UpdateWalkToLadder()
    {
        var ladder = LadderSystem.Instance;
        if (ladder == null) { DestroyWolf(); return; }

        float targetX = ladder.LadderX;
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetX, groundY, 0f), walkSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
        {
            // ถึงบันไดแล้ว — ขอจองขั้น
            var stepIndex = ladder.TryReserveStepForWolf(this);
            if (stepIndex.HasValue)
            {
                reservedStepIndex = stepIndex.Value;
                state = WolfState.Climbing;
                AudioManager.Instance?.PlayLadderClimb();
            }
            else
            {
                // ยังไม่มีขั้นว่าง ให้รอ
                state = WolfState.WaitingForStep;
            }
        }
    }

    // Update ขณะรอคิวขึ้นบันได ถ้ายังไม่มีขั้นว่าง จะวนเช็คซ้ำ
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

    protected float biteTimer = 0f;
    protected float biteInterval = 2f; // Arcade accurate
    protected float biteDuration = 0.15f; // Very quick snap (0.15s instead of 0.4s)
    protected bool isBiting = false;

    protected void UpdateWaitingOnLadder()
    {
        biteTimer += Time.deltaTime;
        SpriteRenderer sr = cachedSR;
        
        if (!isBiting)
        {
            float warningTime = 0.3f; // Shortened warning to just 0.3s
            
            if (biteTimer >= biteInterval)
            {
                // Start bite lunge
                isBiting = true;
                biteTimer = 0f;
                
                // Visual feedback: Solid red during bite
                if (sr != null) sr.color = new Color(1f, 0.3f, 0.3f);
            }
            else if (biteInterval - biteTimer <= warningTime)
            {
                // Warning phase: Blink orange/yellow rapidly
                if (sr != null) 
                {
                    float blink = Mathf.PingPong(Time.time * 20f, 1f); // Blink faster
                    sr.color = Color.Lerp(Color.white, new Color(1f, 0.7f, 0f), blink);
                }
            }
            else
            {
                // Idle phase: Normal color
                if (sr != null) sr.color = Color.white;
            }
        }
        else
        {
            // During the bite window, check if player is aligned
            LadderSystem.Instance?.CheckBite(this);
            
            // Lunge forward visually (PingPong movement)
            float t = biteTimer / biteDuration;
            float lungeOffset = Mathf.Sin(t * Mathf.PI) * 0.5f; // Lunge left by 0.5 units towards player
            if (LadderSystem.Instance != null)
            {
                transform.position = new Vector3(LadderSystem.Instance.LadderX - lungeOffset, transform.position.y, 0f);
            }
            
            if (biteTimer >= biteDuration)
            {
                // Bite window closed, reset position
                if (LadderSystem.Instance != null)
                {
                    transform.position = new Vector3(LadderSystem.Instance.LadderX, transform.position.y, 0f);
                }
                isBiting = false;
                biteTimer = 0f;
                biteInterval = UnityEngine.Random.Range(1.5f, 3f); // Arcade accurate lunge interval
                
                // Remove visual feedback
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
        // 1. เปลี่ยนวิธีเช็ค: ถ้ายืนอยู่บนพื้น เกาะบันได ถึงหน้าผา หรือตายไปแล้ว ถึงจะทำลายลูกโป่งไม่ได้ (return ทิ้ง)
        // ถ้าไม่อยู่ใน State เหล่านี้ แปลว่าอยู่กลางอากาศ = ร่วงได้หมด!
        if (state == WolfState.WalkingToLadder ||
            state == WolfState.Climbing ||
            state == WolfState.WaitingOnLadder ||
            state == WolfState.ReachedCliff ||
            state == WolfState.Dead)
        {
            return;
        }

        // 2. เปลี่ยน State เป็นตกพื้น และเล่นเสียง
        state = WolfState.Falling;
        SetFallingVisual();
        AudioManager.Instance?.PlayWolfFall();

        // 3. สิ่งสำคัญที่ต้องเพิ่ม: ปิดระบบปาหินและกางโล่ทิ้งทันทีที่ลูกโป่งแตก! 
        // ป้องกันบั๊ก "หมาป่าร่วงอยู่แต่ดันปาหินสวนกลับมาได้"
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
        if (balloon != null) balloon.gameObject.SetActive(false);
        DestroyWolf();
    }

    protected void DestroyWolf()
    {
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
    public void TriggerBite()
    {
        if (state != WolfState.WaitingOnLadder) return;
        state = WolfState.Biting;
        AudioManager.Instance?.PlayWolfBite();
        FindAnyObjectByType<PlayerController>()?.TakeDamage();
        DestroyWolf();
    }
}


