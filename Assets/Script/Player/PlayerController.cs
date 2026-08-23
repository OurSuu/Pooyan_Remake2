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

    public PlayerState State { get; private set; } = PlayerState.Normal;
    public float MinY => minY;
    public float MaxY => maxY;
    public bool IsAtTop => Mathf.Abs(transform.position.y - maxY) <= topPickupThreshold;

    private ArrowShooter arrowShooter;
    private bool meatAvailable;
    private bool bossApproaching;
    private Coroutine shootSpriteRoutine;
    private Animator animator;

    private void Awake()
    {
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
        if (mamaPigRenderer == null || spriteShoot == null) return;
        if (shootSpriteRoutine != null) StopCoroutine(shootSpriteRoutine);
        shootSpriteRoutine = StartCoroutine(ShootSpriteRoutine());
    }

    private System.Collections.IEnumerator ShootSpriteRoutine()
    {
        if (animator != null) animator.enabled = false; // Pause Animator so it doesn't overwrite the Shoot sprite
        
        mamaPigRenderer.sprite = spriteShoot;
        yield return new WaitForSeconds(shootSpriteTime);
        
        if (animator != null) animator.enabled = true; // Resume Animator
        shootSpriteRoutine = null;
    }

    private void HandleMovement()
    {
        float v = Input.GetAxisRaw("Vertical");
        Vector3 pos = transform.position;
        pos.y += v * moveSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;

        // --- ลอจิกใหม่: หยิบเนื้ออัตโนมัติแบบตู้ Arcade ---
        // เช็กว่าถ้าผู้เล่นอยู่สถานะปกติ มีเนื้อให้เก็บ และกระเช้าดันขึ้นไปถึงจุดสูงสุด (IsAtTop) แล้ว
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
        
        if (meatPrefab == null) return; // Prevent picking up if no prefab assigned

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
        GameManager.Instance?.LoseLife(diedFromBoulder);
    }

    public void Respawn()
    {
        State = PlayerState.Normal;
        if (heldMeatVisual != null) heldMeatVisual.SetActive(false);
        arrowShooter.SetCanShoot(true);
    }

    public void OnProjectileHit()
    {
        AudioManager.Instance?.PlayRockHit();
        TakeDamage();
    }
}
