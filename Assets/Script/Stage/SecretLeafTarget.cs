using UnityEngine;

/// <summary>
/// Easter Egg ชิ้นที่ 1 จาก Pooyan (NES 1985 - Hudson Soft): "ใบไม้ลับ (Hidden Leaf)"
/// เงื่อนไขการเกิด: ขยับกระเช้าแม่หมูขึ้น-ลงอย่างต่อเนื่องโดยไม่หยุดนิ่งครบ 4 วินาที (~240 เฟรม)
/// รางวัลเมื่อยิงธนูโดน: รับคะแนนพิเศษ 4,000 แต้ม (GameConstants.ScoreSecretLeaf) พร้อมตัวเลข +4000 เด้งลอยขึ้น!
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SecretLeafTarget : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 1.15f;
    [SerializeField] private float swayAmplitude = 0.75f;
    [SerializeField] private float swayFrequency = 2.8f;

    private float startX;
    private float elapsed;
    private bool isHit;
    private SpriteRenderer spriteRenderer;

    private static Sprite cachedFallbackLeafSprite;

    public void Initialize(Sprite customLeafSprite = null)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (customLeafSprite != null)
        {
            spriteRenderer.sprite = customLeafSprite;
            spriteRenderer.color = Color.white;
            transform.localScale = Vector3.one * 0.85f;
        }
        else
        {
            spriteRenderer.sprite = GetOrCreateFallbackLeafSprite();
            spriteRenderer.color = new Color(0.35f, 0.95f, 0.4f, 1f);
            transform.localScale = Vector3.one * 1.1f;
        }
        spriteRenderer.sortingOrder = 22;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        startX = transform.position.x;
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        startX = transform.position.x;
    }

    private void Update()
    {
        if (isHit) return;
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze) return;

        elapsed += Time.deltaTime;

        // ใบไม้ปลิวส่ายซ้าย-ขวาเหมือนใบไม้ร่วงในต้นฉบับ NES
        float offsetX = Mathf.Sin(elapsed * swayFrequency) * swayAmplitude;
        float newY = transform.position.y - (fallSpeed * Time.deltaTime);
        transform.position = new Vector3(startX + offsetX, newY, 0f);

        // เอียงใบไม้ตามจังหวะลม
        float tiltAngle = Mathf.Cos(elapsed * swayFrequency) * 28f;
        transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);

        // ถ้าร่วงพ้นขอบล่างจอให้ทำลายทิ้ง
        if (transform.position.y < -5.5f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit) return;

        if (other.CompareTag(GameConstants.TagArrow))
        {
            TakeHit(other.gameObject);
        }
    }

    public void TakeHit(GameObject arrowObj = null)
    {
        if (isHit) return;
        isHit = true;

        // มอบคะแนนพิเศษ 4,000 แต้ม + เด้งตัวเลข +4000 บนจอ!
        ScoreManager.Instance?.AddBonusChainScore(GameConstants.ScoreSecretLeaf, transform.position);
        AudioManager.Instance?.PlayFruitHit();

        // เสกเอฟเฟกต์แตกกระจายสีเขียวมรกตทอง
        var fxPrefab = Resources.Load<GameObject>("FX/BalloonPopFX");
        if (fxPrefab != null)
        {
            var fxObj = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            var popFX = fxObj.GetComponent<BalloonPopEffect>();
            if (popFX != null)
            {
                popFX.InitAndPlay(new Color(0.4f, 1f, 0.45f, 1f));
            }
        }

        if (arrowObj != null)
        {
            var arrow = arrowObj.GetComponent<Arrow>();
            if (arrow != null)
            {
                arrow.DestroySelf();
            }
        }

        Destroy(gameObject);
    }

    private static Sprite GetOrCreateFallbackLeafSprite()
    {
        if (cachedFallbackLeafSprite != null) return cachedFallbackLeafSprite;

        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size * 0.5f) / (size * 0.4f);
                float ny = (y - size * 0.5f) / (size * 0.4f);
                // วาดทรงใบไม้แบบ Pixel Art
                float dist = (nx * nx * 1.4f) + (ny * ny * 0.8f);
                if (dist <= 0.85f)
                {
                    bool isVein = Mathf.Abs(nx) < 0.08f || Mathf.Abs(ny - Mathf.Abs(nx) * 0.6f) < 0.07f;
                    tex.SetPixel(x, y, isVein ? new Color(0.85f, 1f, 0.5f, 1f) : Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        cachedFallbackLeafSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return cachedFallbackLeafSprite;
    }
}
