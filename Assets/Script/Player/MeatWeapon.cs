using UnityEngine;

/// <summary>
/// อาวุธชิ้นเนื้อ — โยนเป็นวิถีโค้งพาราโบลา (เหมือนตู้เกม Pooyan สมัยก่อน)
/// ทะลุทะลวงได้, ข้ามโล่ได้, เก็บแต้มคอมโบกระจุย!
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeatWeapon : MonoBehaviour
{
    [Header("Parabolic Arc")]
    [SerializeField] private float lobSpeedX = -13f;        // ความเร็วพุ่งไปข้างหน้า แกน X (ติดลบคือไปซ้าย)
    [SerializeField] private float lobSpeedY = 5f;         // แรงกระโดดลอยขึ้นบน แกน Y
    [SerializeField] private float gravityScale = 1.5f;    // แรงโน้มถ่วง (ยิ่งเยอะยิ่งร่วงเร็ว)
    [SerializeField] private float destroyY = -6f;         // พิกัดร่วงหลุดจอแล้วลบทิ้ง

    // เก็บรายการหมาป่าที่โดนตีไปแล้ว จะได้ไม่เบิ้ลดามเมจตัวเดิม
    private readonly System.Collections.Generic.HashSet<Wolf> hitWolves = new();

    private void Awake()
    {
        gameObject.tag = GameConstants.TagMeat; // แปะ Tag ว่าเป็นเนื้อ
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale; // ตั้งค่าแรงโน้มถ่วงให้ร่วงสมจริง
        rb.linearVelocity = new Vector2(lobSpeedX, lobSpeedY); // ใส่แรงโยนตั้งต้นเข้าไปเลยตู้ม!
    }

    private void Update()
    {
        // ตกขอบจอเมื่อไหร่ก็ทำลายทิ้ง คืน Memory
        if (transform.position.y < destroyY)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // เนื้อไม่ชนธนูหรือเนื้อด้วยกันเอง ข้ามไปเลย
        if (other.CompareTag(GameConstants.TagArrow) || other.CompareTag(GameConstants.TagMeat))
            return;

        var wolf = other.GetComponent<Wolf>() ?? other.GetComponentInParent<Wolf>();
        var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();

        // ถ้าโดนแค่ลูกโป่งเปล่าๆ (หมาป่าตายไปแล้ว) ให้ความเร็วร่วงตกลงมานิดนึง
        if (balloon != null && wolf == null)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.5f);
            return;
        }

        // ถ้าปาโดนหมาป่า (ไม่ว่าจะโดนตัวมันหรือลูกโป่งมัน) และยังไม่เคยโดนตัวนี้
        if (wolf != null && !hitWolves.Contains(wolf))
        {
            hitWolves.Add(wolf); // จดไว้ว่าโดนตัวนี้แล้วนะ
            ScoreManager.Instance?.AddMeatComboScore(wolf.transform.position); // บวกคะแนนคอมโบโลด
            AudioManager.Instance?.PlayMeatHit(); // เล่นเสียงเนื้อฟาดหน้า

            // เนื้อโหดมาก! ไม่มีการกระเด้งหรือลดความเร็วใดๆ ร่วงตกตามแรงโน้มถ่วงทะลวงต่อไปยาวๆ
            var rbHit = GetComponent<Rigidbody2D>();

            wolf.OnMeatHit(); // สั่งให้หมาป่าตายซะ
        }
    }
}
