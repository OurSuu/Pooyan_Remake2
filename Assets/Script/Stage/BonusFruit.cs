using UnityEngine;

/// <summary>
/// ผลไม้ใน Bonus Stage (ด่านต่อจาก Stage 2 ตามต้นฉบับ Pooyan)
/// หมาป่าจะโยนผลไม้ข้ามฟากมาเป็นแนวโค้ง หรือร่วงลงมาให้แม่หมูยิงเก็บแต้ม
/// ยิงโดนได้แต้ม ยิงครบ 20 ลูกได้ Perfect Bonus 10,000 คะแนน!
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BonusFruit : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 3f;
    [SerializeField] private bool useArcTrajectory = true; // โยนเป็นวิถีโค้งแบบต้นฉบับ
    [SerializeField] private float gravity = 9.8f;

    private Vector2 velocity;
    private BonusFruitType fruitType;
    private bool isHit = false;

    public BonusFruitType FruitType => fruitType;

    public void Initialize(BonusFruitType type, Vector2? initialVelocity = null)
    {
        fruitType = type;
        gameObject.tag = "Untagged"; // ลบแท็กศัตรูทิ้ง หมูชนจะได้ไม่ตาย

        if (initialVelocity.HasValue)
        {
            velocity = initialVelocity.Value;
            useArcTrajectory = true;
        }
        else if (useArcTrajectory)
        {
            // โยนจากฝั่งซ้ายพุ่งโค้งขึ้นไปทางขวาแถวกระเช้าแม่หมู
            velocity = new Vector2(Random.Range(3.5f, 6.0f), Random.Range(5.0f, 8.5f));
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            switch (type)
            {
                case BonusFruitType.Strawberry:
                    sr.color = new Color(1f, 0.25f, 0.35f);
                    transform.localScale = Vector3.one * 0.7f;
                    break;
                case BonusFruitType.Cherry:
                    sr.color = new Color(0.9f, 0.15f, 0.2f);
                    transform.localScale = Vector3.one * 0.55f;
                    break;
                case BonusFruitType.Peach:
                    sr.color = new Color(1f, 0.65f, 0.4f);
                    transform.localScale = Vector3.one * 0.85f;
                    break;
            }
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
            // ตกลงมาตรงๆ
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        }

        // ตกจอ หรือเลยขอบขวาไปแล้วให้ทำลายทิ้ง
        if (transform.position.y < -6f || transform.position.x > 10f)
        {
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

        // บวกคะแนน
        ScoreManager.Instance?.AddBonusFruitScore(fruitType, transform.position);
        AudioManager.Instance?.PlayRockDestroy();

        // แจ้ง BonusStageManager ว่ายิงโดน
        FindAnyObjectByType<BonusStageManager>()?.OnBonusFruitHit();

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
}
