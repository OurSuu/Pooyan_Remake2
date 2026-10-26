using System.Collections;
using UnityEngine;

public enum BalloonColorType
{
    AutoByHP = 0,
    LightBlue = 1,    // 水色 (ฟ้าอ่อน): ด่านคี่=ลอยช้า, ด่านคู่ Stage 2 & ด่านโบนัสปาเนื้อ=สีหลักทั้งด่าน
    YellowOrange = 2, // オレンジ/黄色 (เหลืองส้ม): ด่านคี่=ความเร็วปานกลาง
    PinkRed = 3,      // 赤/ピンク (ชมพูแดง): ด่านคี่=ความเร็วสูงสุด, หรือลูกโป่งบอส/ด่านคู่ระดับสูง
    Green = 4         // 黄緑 (เขียวอ่อน): ด่านคู่ Stage 4 (ลอยเร็วมาก 1 HP) หรือลูกโป่งเปล่าจากท่อพื้น
}

// ลูกโป่งของหมาป่า นี่แหละเป้าหมายหลักให้เรายิง!
public class Balloon : MonoBehaviour
{
    [SerializeField] private Sprite[] damageStages; // สไปรท์ตอนลูกโป่งโดนยิงไปแต่ละขั้น (ถ้ามีหลายฮิต)
    [SerializeField] private float flashInterval = 0.15f; // ความเร็วตอนกระพริบ (เวลาเป็นบอส)
    [SerializeField] private GameObject popEffectPrefab; // พรีแฟบ Particle Effect ตอนลูกโป่งแตก

    [Header("Custom Balloon Sprites (Optional)")]
    [Tooltip("ใส่รูปสไปรท์ที่เพื่อนวาดแยกสีมาได้เลย (ถ้าใส่ช่องนี้ เกมจะไม่ย้อมสีทับ เพื่อให้เห็นสีจริงจากภาพวาด)")]
    [SerializeField] private Sprite pinkSprite;   // รูปสีชมพู/แดง
    [SerializeField] private Sprite yellowSprite; // รูปสีเหลือง/ส้ม
    [SerializeField] private Sprite blueSprite;   // รูปสีฟ้าอ่อน
    [SerializeField] private Sprite redSprite;    // รูปสีแดงเข้ม

    [Header("Balloon Colors (HP-based: Red=1HP, Yellow=2HP, Blue=3HP, Boss=Special Flash)")]
    [Tooltip("สีแดง 1 HP - ยิง 1 ทีแตก (ด่าน 1)")]
    [SerializeField] private Color pinkColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Tooltip("สีเหลือง/ส้ม 2 HP - ยิง 2 ทีแตก")]
    [SerializeField] private Color yellowColor = new Color(1f, 0.82f, 0.16f, 1f);
    [Tooltip("สีฟ้า 3 HP - ยิง 3 ทีแตก (ด่าน 2 และ ด่าน 3)")]
    [SerializeField] private Color blueColor = new Color(0.28f, 0.85f, 1f, 1f);
    [Tooltip("สีม่วงชมพูพิเศษสำหรับลูกโป่งบอส (กระพริบสลับขาว ต้องยิง 8 ทีแตก)")]
    [SerializeField] private Color redColor = new Color(1f, 0.30f, 0.75f, 1f);
    [Tooltip("สีเขียวอ่อน (สำหรับลูกโป่งเปล่าจากท่อพื้น)")]
    [SerializeField] private Color greenColor = new Color(0.55f, 1f, 0.28f, 1f);

    [Tooltip("ถ้าเปิดไว้ ลูกโป่งสีฟ้า 3 HP ในด่านคู่จะคงสีฟ้าไว้ตลอดตอนโดนยิง (หดขนาดลงอย่างเดียวตาม NES ต้นฉบับ)")]
    [SerializeField] private bool keepBlueColorWhenShrinking = true;

    private int maxHP;
    private int currentHP;
    private bool isBossFlash;
    private BalloonColorType assignedColorType = BalloonColorType.AutoByHP;
    private SpriteRenderer spriteRenderer;
    private Wolf owner; // เจ้าของลูกโป่งนี้ (หมาป่าตัวไหน)
    private Vector3 initialScale = Vector3.one;
    private bool hasCachedScale = false;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public BalloonColorType AssignedColorType => assignedColorType;
    public bool HasOwner => owner != null;
    public bool CanBounceMeat { get; private set; } = false;

    private void CacheComponents()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (!hasCachedScale && transform.localScale != Vector3.zero)
        {
            initialScale = transform.localScale;
            hasCachedScale = true;
        }
        gameObject.tag = GameConstants.TagBalloon; // แปะแท็กให้รู้ว่านี่คือลูกโป่งนะ
    }

    private void Awake()
    {
        CacheComponents();
    }

    public void Initialize(Wolf wolf, int hp, bool bossFlash = false, BalloonColorType colorType = BalloonColorType.AutoByHP)
    {
        CacheComponents();
        owner = wolf;
        CanBounceMeat = (wolf == null);
        maxHP = Mathf.Max(1, hp); // เลือดลูกโป่ง ห้ามต่ำกว่า 1
        currentHP = maxHP;
        isBossFlash = bossFlash; // ถ้าเป็นลูกโป่งของบอส จะมีการกระพริบ
        assignedColorType = colorType;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        StopAllCoroutines();
        if (isBossFlash && gameObject.activeInHierarchy)
            StartCoroutine(BossFlashRoutine());

        UpdateVisual(); // อัปเดตภาพและสีลูกโป่งตอนเริ่ม
    }

    /// <summary>
    /// เสกลูกโป่งเปล่าลอยขึ้นมาจากด้านล่างโดยไม่มีหมาป่าเกาะ (ใช้ในด่านคู่ และด่านโบนัสปาเนื้อ ตามต้นฉบับ NES)
    /// ยิงแตกได้ 200 แต้ม หรือปาเนื้อชนเพื่อให้เนื้อเด้งกระดอนข้ามเลนได้!
    /// </summary>
    public void InitializeStandalone(float floatUpSpeed = 1.15f, int hp = 1, BalloonColorType colorType = BalloonColorType.AutoByHP)
    {
        gameObject.SetActive(true);
        Initialize(null, hp, bossFlash: false, colorType: colorType);
        CanBounceMeat = true;
        StartCoroutine(StandaloneFloatUpRoutine(floatUpSpeed));
    }

    private IEnumerator StandaloneFloatUpRoutine(float floatUpSpeed)
    {
        float phase = Random.Range(0f, Mathf.PI * 2f);
        float baseX = transform.position.x;

        while (transform.position.y < 5.5f)
        {
            if (GameManager.Instance == null || !GameManager.Instance.DeathFreeze)
            {
                float swayX = Mathf.Sin(Time.time * 2.5f + phase) * 0.2f;
                transform.position = new Vector3(baseX + swayX, transform.position.y + floatUpSpeed * Time.deltaTime, 0f);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnEnable()
    {
        CacheComponents();
        if (maxHP > 0)
        {
            UpdateVisual();
        }
        if (isBossFlash)
        {
            StopAllCoroutines();
            StartCoroutine(BossFlashRoutine());
        }
    }

    // เรียกตอนลูกโป่งโดนโจมตี คืนค่าเป็น true ถ้าลูกโป่งแตก
    public bool TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return true; // ถ้าแตกไปแล้วก็ปล่อยผ่าน

        currentHP -= damage;
        UpdateVisual(); // อัปเดตขนาดที่หดลงและสีตามเลือดที่เหลือ

        if (currentHP <= 0)
        {
            Pop(); // เลือดหมด แตกโพล๊ะ!
            return true;
        }

        // แจ้งหมาป่าให้ลดความเร็วลอยขึ้นตามลูกโป่งที่หดเล็กลง (กลไกด่านคู่ NES)
        owner?.OnBalloonDamaged(currentHP, maxHP);

        // ยังไม่แตก เล่นเสียงยิงตอดลูกโป่ง
        AudioManager.Instance?.PlayBalloonDamage();
        return false;
    }

    public void PopInstant() => Pop(); // แตกแบบทันทีทันใด สั่งเรียกตรงๆ ได้เลย

    // ปล่อยลูกโป่งลอยหนีไป (ตอนหมาป่าโดนเนื้อแล้วปล่อยมือจากลูกโป่ง ตามต้นฉบับ NES)
    public void ReleaseInstant()
    {
        StopAllCoroutines();

        owner = null; // ไม่มีหมาป่าเกาะแล้ว กลายเป็นลูกโป่งเปล่า!
        CanBounceMeat = false;
        transform.SetParent(null); // หลุดจากตัวหมาป่า
        if (gameObject.activeInHierarchy)
            StartCoroutine(ReleaseAnimationRoutine());
        else
            Destroy(gameObject);
    }

    private IEnumerator ReleaseAnimationRoutine()
    {
        // เปิด Collider ไว้เพื่อให้ยิงลูกโป่งที่หลุดมือเก็บ 200 แต้มได้ และให้เนื้อลูกถัดไปชนเด้งได้ตามคู่มือ NES!
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        float elapsed = 0f;
        float floatUpSpeed = 2.2f;

        while (transform.position.y < 5.5f && elapsed < 4.5f)
        {
            if (GameManager.Instance == null || !GameManager.Instance.DeathFreeze)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= 0.35f) CanBounceMeat = true; // กันไม่ให้เด้งเนื้อก้อนเดียวกับที่เพิ่งชนตัวหมาป่า
                transform.position += new Vector3(0f, floatUpSpeed * Time.deltaTime, 0f);
            }
            yield return null;
        }

        Destroy(gameObject); // ลอยพ้นขอบบนจอแล้วทำลายทิ้ง
    }

    // ฟังก์ชันจัดการตอนลูกโป่งแตก
    private void Pop()
    {
        StopAllCoroutines(); // หยุดลูประยิบระยับของบอสด้วย
        ScoreManager.Instance?.AddBalloonPopScore(transform.position); // ได้คะแนน!
        AudioManager.Instance?.PlayBalloonPop(); // เสียงแตก
        owner?.OnBalloonPopped(); // บอกหมาป่าว่า "เห้ย ลูกโป่งแกแตกแล้ว ร่วงไปซะ!"

        SpawnPopEffect(); // ปล่อย Particle Effect ลูกโป่งแตก!

        StartCoroutine(PopAnimationRoutine());
    }

    private void SpawnPopEffect()
    {
        GameObject fxPrefab = popEffectPrefab;
        if (fxPrefab == null)
        {
            fxPrefab = Resources.Load<GameObject>("FX/BalloonPopFX");
        }

        if (fxPrefab != null)
        {
            var fxObj = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            var popFX = fxObj.GetComponent<BalloonPopEffect>();
            Color balloonCol = spriteRenderer != null ? spriteRenderer.color : pinkColor;
            if (popFX != null)
            {
                popFX.InitAndPlay(balloonCol);
            }
        }
    }

    // อนิเมชันตอนแตก ซ่อนสไปรท์และทำลายอ็อบเจกต์ (มี Particle Effect เล่นแทนที่แล้ว)
    private IEnumerator PopAnimationRoutine()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    public Color GetBalloonColor()
    {
        if (isBossFlash)
        {
            // สีพิเศษของลูกโป่งบอส (ม่วงชมพูสด กระพริบสลับขาว)
            return new Color(0.95f, 0.25f, 0.88f, 1f);
        }

        switch (assignedColorType)
        {
            case BalloonColorType.LightBlue:
                return blueColor;
            case BalloonColorType.YellowOrange:
                return yellowColor;
            case BalloonColorType.PinkRed:
                return new Color(1f, 0.22f, 0.22f, 1f); // สีแดงสด (1 ทีแตก)
            case BalloonColorType.Green:
                return greenColor;
            default:
                return GetColorForHP(currentHP);
        }
    }

    public Color GetColorForHP(int hp)
    {
        if (isBossFlash)
            return new Color(0.95f, 0.25f, 0.88f, 1f);

        // ลูกโป่ง 3 HP (สีฟ้า) คงสีฟ้าไว้ตลอดตอนโดนยิงตอดและหดขนาดลง
        if (keepBlueColorWhenShrinking && maxHP == 3)
            return blueColor;

        if (hp >= 3) return blueColor;                  // 3 ทีแตก = สีฟ้า
        if (hp == 2) return yellowColor;                // 2 ทีแตก = สีเหลือง/ส้ม
        return new Color(1f, 0.22f, 0.22f, 1f);         // 1 ทีแตก = สีแดง
    }

    // อัปเดตสไปรท์ ขนาด และสีตามเลือดลูกโป่ง
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // ลูกโป่งบอสจะไม่หดขนาดตอนโดนยิง ส่วนลูกโป่งด่านคู่ (3 HP) จะหดเล็กลงทีละขั้นชัดเจนตามต้นฉบับ NES
        if (isBossFlash || maxHP <= 1)
        {
            transform.localScale = initialScale;
        }
        else
        {
            int hitsTaken = Mathf.Max(0, maxHP - currentHP);
            float scaleMultiplier = Mathf.Clamp(1f - (hitsTaken * 0.22f), 0.45f, 1f);
            transform.localScale = initialScale * scaleMultiplier;
        }

        // เช็คว่ามีรูปสไปรท์แยกตามสีที่ผู้ใช้นำมาใส่เองหรือไม่
        Sprite customColorSprite = null;
        if (assignedColorType == BalloonColorType.LightBlue && blueSprite != null) customColorSprite = blueSprite;
        else if (assignedColorType == BalloonColorType.YellowOrange && yellowSprite != null) customColorSprite = yellowSprite;
        else if (assignedColorType == BalloonColorType.PinkRed && pinkSprite != null) customColorSprite = pinkSprite;
        else if (keepBlueColorWhenShrinking && maxHP == 3 && blueSprite != null) customColorSprite = blueSprite;
        else if (currentHP >= 4 && redSprite != null) customColorSprite = redSprite;
        else if (currentHP == 3 && blueSprite != null) customColorSprite = blueSprite;
        else if (currentHP == 2 && yellowSprite != null) customColorSprite = yellowSprite;
        else if (currentHP == 1 && pinkSprite != null) customColorSprite = pinkSprite;

        if (customColorSprite != null)
        {
            spriteRenderer.sprite = customColorSprite;
            if (!isBossFlash)
            {
                spriteRenderer.color = Color.white; // ไม่ย้อมสี ให้โชว์สีจริงจากภาพวาด
            }
            return;
        }

        // ย้อมสีตามชนิดลูกโป่ง NES (หรือตามเลือดถ้าเป็นโหมด AutoByHP)
        if (!isBossFlash)
        {
            spriteRenderer.color = GetBalloonColor();
        }

        if (damageStages == null || damageStages.Length == 0) return;

        // คำนวณว่าจะแสดงสไปรท์ระดับความเสียหายไหน
        int stageIndex = maxHP <= 1
            ? 0
            : Mathf.Clamp(Mathf.FloorToInt((1f - (float)currentHP / maxHP) * (damageStages.Length - 1)), 0, damageStages.Length - 1);

        spriteRenderer.sprite = damageStages[stageIndex];
    }

    // บอสสเปเชียล: ลูกโป่งกระพริบวิบวับ
    private IEnumerator BossFlashRoutine()
    {
        var flashColor = Color.white;

        while (true)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            if (spriteRenderer != null)
                spriteRenderer.color = GetBalloonColor();
            yield return new WaitForSeconds(flashInterval);
        }
    }
}

