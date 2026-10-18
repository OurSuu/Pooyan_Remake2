using UnityEngine;

/// <summary>
/// ผลไม้ใน Bonus Stage (รองรับทั้งแบบ Famicom ทิ้งดิ่งลงมาจากบอสบนยอดไม้ และแบบ Arcade โยนวิถีโค้งจากซ้าย)
/// มีเอฟเฟกต์เรืองแสง (Bloom Glow Aura) และละอองดาวระยิบระยับ (Sparkle Trail) ทิ้งท้ายในท้องฟ้ายามค่ำคืน!
/// ยิงโดนได้แต้มสะสม Chain Combo สูงสุดถึง Nakamoto 8,000 แต้ม ยิงครบ 20 ลูกได้ Perfect Bonus 10,000 คะแนน!
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BonusFruit : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 3.5f;
    [SerializeField] private bool useArcTrajectory = true; // โยนเป็นวิถีโค้ง หรือร่วงลงมาตรงๆ
    [SerializeField] private float gravity = 9.8f;

    [Header("Fruit Sprites")]
    [SerializeField] private Sprite strawberrySprite;
    [SerializeField] private Sprite cherrySprite;
    [SerializeField] private Sprite peachSprite;

    [Header("Glow & Particle Effects")]
    [SerializeField] private ParticleSystem glowAuraParticles;   // ออร่าเรืองแสงรอบผลไม้
    [SerializeField] private ParticleSystem sparkleTrailParticles; // ละอองดาวพุ่งตามหลัง
    [SerializeField] private GameObject popFXPrefab;              // ประกายดาวแตกกระจายตอนโดนยิง

    private Vector2 velocity;
    private BonusFruitType fruitType;
    private bool isHit = false;
    private Color currentColor;

    public BonusFruitType FruitType => fruitType;

    public void Initialize(BonusFruitType type, Vector2? initialVelocity = null, bool isArc = true)
    {
        fruitType = type;
        gameObject.tag = "Untagged"; // ลบแท็กศัตรูทิ้ง หมูชนจะได้ไม่ตาย
        useArcTrajectory = isArc;

        if (initialVelocity.HasValue)
        {
            velocity = initialVelocity.Value;
        }
        else if (useArcTrajectory)
        {
            // แบบวิถีโค้งข้ามฟาก
            velocity = new Vector2(Random.Range(4.5f, 7.0f), Random.Range(6.0f, 9.5f));
        }
        else
        {
            // แบบ Famicom: บอสทิ้งลงมาตรงๆ หรือเอียงเล็กน้อย
            velocity = new Vector2(Random.Range(-0.4f, 0.4f), -fallSpeed);
        }

        var sr = GetComponent<SpriteRenderer>();

        // กำหนดสีเรืองแสง และสไปรท์ตามชนิดผลไม้
        Sprite targetSprite = null;
        switch (type)
        {
            case BonusFruitType.Strawberry:
                targetSprite = strawberrySprite;
                currentColor = new Color(1.0f, 0.25f, 0.45f, 1f); // ชมพูแดงนีออน (Ruby Glow)
                transform.localScale = Vector3.one * 1.5f;
                break;
            case BonusFruitType.Cherry:
                targetSprite = cherrySprite;
                currentColor = new Color(1.0f, 0.12f, 0.28f, 1f); // แดงทับทิมนีออน (Crimson Glow)
                transform.localScale = Vector3.one * 1.3f;
                break;
            case BonusFruitType.Peach:
                targetSprite = peachSprite;
                currentColor = new Color(1.0f, 0.72f, 0.22f, 1f); // ส้มทองอำพันนีออน (Golden Amber Glow)
                transform.localScale = Vector3.one * 1.5f;
                break;
        }

        if (sr != null)
        {
            if (targetSprite != null)
            {
                sr.sprite = targetSprite;
                sr.color = Color.white; // คงความคมชัดและดีเทลของ Texture ผลไม้
            }
            else
            {
                sr.color = currentColor;
                transform.localScale = Vector3.one * 3.5f;
            }
            sr.sortingOrder = 20;
        }

        // ปรับแต่ง Particle System ออร่าเรืองแสงและละอองดาวให้เข้ากับสีของผลไม้
        SetupGlowParticles(currentColor);

        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = 0.35f;
        }
    }

    private void SetupGlowParticles(Color glowColor)
    {
        // 1. ตั้งค่าออร่าเรืองแสงรอบตัว (Glow Aura)
        if (glowAuraParticles != null)
        {
            var main = glowAuraParticles.main;
            Color auraCol = glowColor;
            auraCol.a = 0.85f;
            main.startColor = auraCol;
            if (!glowAuraParticles.isPlaying) glowAuraParticles.Play();
        }

        // 2. ตั้งค่าละอองดาวระยิบระยับ (Sparkle Trail)
        if (sparkleTrailParticles != null)
        {
            var main = sparkleTrailParticles.main;
            Color trailCol = glowColor;
            trailCol.a = 0.9f;
            main.startColor = trailCol;
            if (!sparkleTrailParticles.isPlaying) sparkleTrailParticles.Play();
        }
    }

    private void Update()
    {
        if (useArcTrajectory)
        {
            // วิถีโค้งตามแรงโน้มถ่วง (Parabolic Arc)
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
        }
        else
        {
            // ตกลงมาด้านล่างตามแรงโน้มถ่วง/ความเร็วตก
            transform.position += (Vector3)(velocity * Time.deltaTime);
        }

        // ตกจอ หรือเลยขอบไปแล้วให้ทำลายทิ้ง
        if (transform.position.y < -5.5f || transform.position.x > 10f || transform.position.x < -10f)
        {
            // ถ้าตกร่วงพ้นขอบล่างโดยไม่โดนยิง -> Chain หลุด!
            if (!isHit && transform.position.y < -5.5f)
            {
                FindAnyObjectByType<BonusStageManager>()?.OnBonusFruitMissed();
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit) return;

        // โดนลูกธนูยิง
        if (other.CompareTag(GameConstants.TagArrow))
        {
            TakeHit(other.gameObject);
        }
    }

    public void TakeHit(GameObject arrowObj = null)
    {
        if (isHit) return;
        isHit = true;

        // แจ้ง BonusStageManager เพื่อคำนวณคะแนน Chain Combo แบบ Famicom หรือคะแนนผลไม้ปกติ
        FindAnyObjectByType<BonusStageManager>()?.OnBonusFruitHit(fruitType, transform.position);

        AudioManager.Instance?.PlayFruitHit();

        // เสกเอฟเฟกต์แตกกระจายเป็นละอองดาว (FruitPopFX) สีเดียวกับผลไม้
        SpawnPopFX();

        // ทำลายลูกธนูที่ยิงมาด้วย
        if (arrowObj != null)
        {
            var arrow = arrowObj.GetComponent<Arrow>();
            if (arrow != null)
            {
                Destroy(arrowObj);
            }
        }

        Destroy(gameObject);
    }

    private void SpawnPopFX()
    {
        if (popFXPrefab != null)
        {
            var pop = Instantiate(popFXPrefab, transform.position, Quaternion.identity);
            var ps = pop.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = currentColor;
            }
        }
    }
}
