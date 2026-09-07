using UnityEngine;

public enum PlayerState
{
    Normal,
    HoldingMeat,
    Dead
}

/// <summary>
/// Mama Pig on the gondola — vertical movement, meat pickup, damage.
/// </summary>
[RequireComponent(typeof(ArrowShooter))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minY = -3.5f; // base lowest value
    [SerializeField] private float maxY = 3.5f;
    [SerializeField] private float topPickupThreshold = 0.05f;

    [Header("Meat Pickup")]
    [SerializeField] private Transform meatSpawnPoint;
    [SerializeField] private GameObject meatPrefab;
    [SerializeField] private MeatPickupIndicator meatIndicator;
    [SerializeField] private GameObject heldMeatVisual;

    [Header("Shoot Animation")]
    [SerializeField] private SpriteRenderer mamaPigRenderer;
    [SerializeField] private Sprite spriteIdle;   // Fallback single sprite
    [SerializeField] private Sprite[] idleSprites; // New array for animation
    [SerializeField] private float idleFrameRate = 12f;
    [SerializeField] private Sprite spriteShoot;  // MamaPig_1
    [SerializeField] private float shootSpriteTime = 0.15f;
    [SerializeField] private string shootAnimName = "Shoot";
    [SerializeField] public string deadAnimName = "Fall";
    [SerializeField] public float deathFloorY = -4.5f;
    [SerializeField] private string idleAnimName = "Idle";

    public PlayerState State { get; private set; } = PlayerState.Normal;
    public float MinY => minY;
    public float MaxY => maxY;
    public bool IsAtTop => Mathf.Abs(transform.position.y - maxY) <= topPickupThreshold;

    private ArrowShooter arrowShooter;
    private bool meatAvailable;
    private Vector3 startPosition;
    private bool bossApproaching;
    private Coroutine shootSpriteRoutine;
    private Animator animator;

    private void Awake()
    {
        startPosition = transform.position;
        arrowShooter = GetComponent<ArrowShooter>();
        if (arrowShooter == null) arrowShooter = GetComponentInChildren<ArrowShooter>();
    }

    private void Start()
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
            // เจาะจงหาในลูกที่ชื่อ MamaPig ก่อน
            Transform mamaPigObj = transform.Find("MamaPig");
            if (mamaPigObj != null)
            {
                animator = mamaPigObj.GetComponent<Animator>();
            }
        }
        
        if (animator == null)
        {
            // ถ้ายังไม่เจออีกค่อยกวาดหาทั้งหมด
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying && !GameManager.Instance.IsBonus)
            return;

        if (State == PlayerState.Dead) return;

        HandleMovement();
        HandleFireInput();
        UpdateIdleAnimation();
    }

    private void UpdateIdleAnimation()
    {
        if (shootSpriteRoutine != null || mamaPigRenderer == null || animator != null) return; // Ignore if using Animator

        if (idleSprites != null && idleSprites.Length > 0)
        {
            int index = (int)(Time.time * idleFrameRate) % idleSprites.Length;
            mamaPigRenderer.sprite = idleSprites[index];
        }
        else if (spriteIdle != null)
        {
            mamaPigRenderer.sprite = spriteIdle;
        }
    }

    private void HandleFireInput()
    {
        if (!Input.GetButtonDown("Fire1") && !Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Z)) return;

        if (State == PlayerState.HoldingMeat)
        {
            ThrowMeat();
            return;
        }

        if (State == PlayerState.Normal)
        {
            if (arrowShooter.TryShoot())
                PlayShootSprite();
        }
    }

    private void PlayShootSprite()
    {
        if (animator != null)
        {
            if (shootSpriteRoutine != null) StopCoroutine(shootSpriteRoutine);
            shootSpriteRoutine = StartCoroutine(ShootAnimRoutine());
            return;
        }

        if (mamaPigRenderer == null || spriteShoot == null) return;
        if (shootSpriteRoutine != null) StopCoroutine(shootSpriteRoutine);
        shootSpriteRoutine = StartCoroutine(ShootSpriteRoutine());
    }

    private System.Collections.IEnumerator ShootAnimRoutine()
    {
        animator.Play(shootAnimName, -1, 0f);
        yield return new WaitForSeconds(shootSpriteTime);
        animator.Play(idleAnimName);
        shootSpriteRoutine = null;
    }

    private System.Collections.IEnumerator ShootSpriteRoutine()
    {
        if (animator != null) animator.enabled = false;
        
        mamaPigRenderer.sprite = spriteShoot;
        yield return new WaitForSeconds(shootSpriteTime);
        
        if (animator != null) animator.enabled = true;
        shootSpriteRoutine = null;
    }

    private void HandleMovement()
    {
        float v = Input.GetAxisRaw("Vertical");
        Vector3 pos = transform.position;
        pos.y += v * moveSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;

        if (State == PlayerState.Normal && meatAvailable && IsAtTop)
        {
            PickUpMeat();
        }
    }

    public void SetMeatAvailable(bool available)
    {
        meatAvailable = available && !bossApproaching;
        meatIndicator?.SetAvailable(meatAvailable);
    }

    public void SetBossApproaching(bool approaching)
    {
        bossApproaching = approaching;
        if (approaching) meatAvailable = false;
        meatIndicator?.SetAvailable(meatAvailable);
    }

    public bool IsMeatAvailable => meatAvailable;

    private void PickUpMeat()
    {
        meatAvailable = false;
        meatIndicator?.SetAvailable(false);
        
        if (meatPrefab == null) return; 

        State = PlayerState.HoldingMeat;
        arrowShooter.SetCanShoot(false);
        if (heldMeatVisual != null) heldMeatVisual.SetActive(true);
        ScoreManager.Instance?.AddMeatPickupScore(transform.position);
    }

    private void ThrowMeat()
    {
        if (meatPrefab != null)
        {
            Vector3 spawnPos = meatSpawnPoint != null ? meatSpawnPoint.position : transform.position;
            Instantiate(meatPrefab, spawnPos, Quaternion.identity);
            AudioManager.Instance?.PlayMeatThrow();
        }

        ScoreManager.Instance?.ResetMeatCombo();

        State = PlayerState.Normal;
        arrowShooter.SetCanShoot(true);
        if (heldMeatVisual != null) heldMeatVisual.SetActive(false);
    }

    private bool diedFromBoulder = false;

    public void SetDiedFromBoulder(bool value)
    {
        diedFromBoulder = value;
    }

    public void TakeDamage()
    {
        if (State == PlayerState.Dead) return;

        State = PlayerState.Dead;
        
        if (animator != null)
        {
            animator.enabled = true;
            animator.Update(0f);
            animator.speed = 1f;
            animator.Play(deadAnimName, -1, 0f);
        }

        GameManager.Instance?.LoseLife(diedFromBoulder);
        StartCoroutine(DeathBounceRoutine());
    }

    private System.Collections.IEnumerator DeathBounceRoutine()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        // กระเด็นไปข้างหน้า (หันซ้าย แกน x ต้องติดลบ) และร่วงลง
        float velocityX = -2.5f; // เด้งไปทางซ้าย (ข้างหน้า)
        float velocityY = 3.0f; // เด้งลอยขึ้นนิดนึง
        float gravity = 15f;
        
        while (State == PlayerState.Dead)
        {
            t += Time.deltaTime;
            float newX = startPos.x + (velocityX * t);
            float newY = startPos.y + (velocityY * t) - (0.5f * gravity * t * t);
            
            // เช็คว่าชนพื้นหรือยัง
            if (newY <= deathFloorY)
            {
                newY = deathFloorY;
                transform.position = new Vector3(newX, newY, startPos.z);
                // (อนาคต: สั่งเล่นท่านอนตายตรงนี้ได้เลย)
                break; // หลุดลูป ไม่ต้องร่วงหรือขยับต่อแล้ว
            }

            transform.position = new Vector3(newX, newY, startPos.z);
            
            yield return null;
            if (t > 2.5f) break; // GameManager restarts wave in 2 seconds anyway
        }
    }

    public void Respawn()
    {
        State = PlayerState.Normal;
        transform.position = startPosition; // Reset position
        
        if (heldMeatVisual != null) heldMeatVisual.SetActive(false);
        
        if (animator != null)
        {
            animator.Play(idleAnimName);
        }
        
        if (arrowShooter != null)
        {
            arrowShooter.SetCanShoot(true);
        }
    }

    public void OnProjectileHit()
    {
        AudioManager.Instance?.PlayRockHit();
        TakeDamage();
    }
}









