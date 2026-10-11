using System.Collections;
using UnityEngine;

// ลูกโป่งของหมาป่า นี่แหละเป้าหมายหลักให้เรายิง!
public class Balloon : MonoBehaviour
{
    [SerializeField] private Sprite[] damageStages; // สไปรท์ตอนลูกโป่งโดนยิงไปแต่ละขั้น (ถ้ามีหลายฮิต)
    [SerializeField] private float flashInterval = 0.15f; // ความเร็วตอนกระพริบ (เวลาเป็นบอส)
    [SerializeField] private GameObject popEffectPrefab; // พรีแฟบ Particle Effect ตอนลูกโป่งแตก

    [Header("Balloon Colors (NES Pooyan Original)")]
    [Tooltip("สีชมพู 1 HP - สีพื้นฐานของด่าน 1 และด่านทั่วไป")]
    [SerializeField] private Color pinkColor = new Color(1f, 0.42f, 0.65f, 1f);
    [Tooltip("สีเหลือง 2 HP - สีลูกโป่งอึดในด่าน 2 โดนยิง 1 ทีจะกลายเป็นสีชมพู")]
    [SerializeField] private Color yellowColor = new Color(1f, 0.88f, 0.15f, 1f);
    [Tooltip("สีแดง 3+ HP - สำหรับด่านหลังๆ ที่ลูกโป่งหนาพิเศษ")]
    [SerializeField] private Color redColor = new Color(1f, 0.25f, 0.2f, 1f);

    private int maxHP;
    private int currentHP;
    private bool isBossFlash;
    private SpriteRenderer spriteRenderer;
    private Wolf owner; // เจ้าของลูกโป่งนี้ (หมาป่าตัวไหน)

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

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

        UpdateVisual(); // อัปเดตภาพและสีลูกโป่งตอนเริ่ม
    }

    // เรียกตอนลูกโป่งโดนโจมตี คืนค่าเป็น true ถ้าลูกโป่งแตก
    public bool TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return true; // ถ้าแตกไปแล้วก็ปล่อยผ่าน

        currentHP -= damage;
        UpdateVisual(); // เปลี่ยนสไปรท์และสีตามเลือดที่เหลือ (2 HP สีเหลือง -> 1 HP สีชมพู!)

        if (currentHP <= 0)
        {
            Pop(); // เลือดหมด แตกโพล๊ะ!
            return true;
        }

        // ยังไม่แตก (เช่น สีเหลืองโดนยิงเหลือสีชมพู) เล่นเสียงกระแทกเบาๆ
        AudioManager.Instance?.PlayRockDestroy();
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

    public Color GetColorForHP(int hp)
    {
        if (hp >= 3) return redColor;
        if (hp == 2) return yellowColor;
        return pinkColor;
    }

    // อัปเดตสไปรท์ ขนาด และสีตามเลือดลูกโป่ง
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // ยิ่งเลือดน้อยยิ่งแฟบลง
        float scale = 1f - (maxHP - currentHP) * 0.05f;
        transform.localScale = Vector3.one * scale;

        // อัปเดตสีตามเลือดที่เหลือ (ถ้าไม่ใช่ลูกโป่งบอสที่กระพริบวิบวับ)
        if (!isBossFlash)
        {
            spriteRenderer.color = GetColorForHP(currentHP);
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
                spriteRenderer.color = GetColorForHP(currentHP);
            yield return new WaitForSeconds(flashInterval);
        }
    }
}

