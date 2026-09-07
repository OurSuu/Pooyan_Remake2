using System.Collections;
using UnityEngine;

// ลูกโป่งของหมาป่า นี่แหละเป้าหมายหลักให้เรายิง!
public class Balloon : MonoBehaviour
{
    [SerializeField] private Sprite[] damageStages; // สไปรท์ตอนลูกโป่งโดนยิงไปแต่ละขั้น (ถ้ามีหลายฮิต)
    [SerializeField] private float flashInterval = 0.15f; // ความเร็วตอนกระพริบ (เวลาเป็นบอส)

    private int maxHP;
    private int currentHP;
    private bool isBossFlash;
    private SpriteRenderer spriteRenderer;
    private Wolf owner; // เจ้าของลูกโป่งนี้ (หมาป่าตัวไหน)

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.tag = GameConstants.TagBalloon; // แปะแท็กให้รู้ว่านี่คือลูกโป่งนะ
    }

    public void Initialize(Wolf wolf, int hp, bool bossFlash = false)
    {
        owner = wolf;
        maxHP = Mathf.Max(1, hp); // เลือดลูกโป่ง ห้ามต่ำกว่า 1
        currentHP = maxHP;
        isBossFlash = bossFlash; // ถ้าเป็นลูกโป่งของบอส จะมีการกระพริบ

        if (isBossFlash)
            StartCoroutine(BossFlashRoutine());

        UpdateVisual(); // อัปเดตภาพลูกโป่งตอนเริ่ม
    }

    // เรียกตอนลูกโป่งโดนโจมตี คืนค่าเป็น true ถ้าลูกโป่งแตก
    public bool TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return true; // ถ้าแตกไปแล้วก็ปล่อยผ่าน

        currentHP -= damage;
        UpdateVisual(); // เปลี่ยนสไปรท์ตามเลือดที่เหลือ

        if (currentHP <= 0)
        {
            Pop(); // เลือดหมด แตกโพล๊ะ!
            return true;
        }

        return false;
    }

    public void PopInstant() => Pop(); // แตกแบบทันทีทันใด สั่งเรียกตรงๆ ได้เลย

    // ปล่อยลูกโป่งลอยหนีไป (เช่น ตอนหมาป่าโดนเนื้อทับตาย หรือตายแบบไม่ได้โดนยิงลูกโป่ง)
    public void ReleaseInstant()
    {
        StopAllCoroutines();

        transform.SetParent(null); // หลุดจากตัวหมาป่า
        StartCoroutine(ReleaseAnimationRoutine());
    }

    private IEnumerator ReleaseAnimationRoutine()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // ปิด collider ไม่ให้โดนยิงซ้ำ

        float duration = 1.0f;
        float time = 0f;
        Vector3 startPos = transform.position;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // ค่อยๆ ลอยขึ้นไปข้างบน
            transform.position = startPos + new Vector3(0, t * 5f, 0); 

            // พร้อมกับค่อยๆ จางหายไป
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject); // จางสุดปุ๊บก็ทำลายทิ้ง
    }

    // ฟังก์ชันจัดการตอนลูกโป่งแตก
    private void Pop()
    {
        StopAllCoroutines(); // หยุดลูประยิบระยับของบอสด้วย
        ScoreManager.Instance?.AddBalloonPopScore(transform.position); // ได้คะแนน!
        AudioManager.Instance?.PlayBalloonPop(); // เสียงแตก
        owner?.OnBalloonPopped(); // บอกหมาป่าว่า "เห้ย ลูกโป่งแกแตกแล้ว ร่วงไปซะ!"

        StartCoroutine(PopAnimationRoutine());
    }

    // อนิเมชันตอนแตก ขยายตัวนิดนึงแล้วจางหาย
    private IEnumerator PopAnimationRoutine()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float duration = 0.15f;
        float time = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.5f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t); // ขยายป่องขึ้น

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t); // จางลง
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    // อัปเดตสไปรท์และขนาดตามเลือดลูกโป่ง
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // ยิ่งเลือดน้อยยิ่งแฟบลง
        float scale = 1f - (maxHP - currentHP) * 0.05f;
        transform.localScale = Vector3.one * scale;

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
        var normalColor = Color.white;
        var flashColor = new Color(1f, 0.2f, 0.2f); // กระพริบแดง

        while (true)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            spriteRenderer.color = normalColor;
            yield return new WaitForSeconds(flashInterval);
        }
    }
}
