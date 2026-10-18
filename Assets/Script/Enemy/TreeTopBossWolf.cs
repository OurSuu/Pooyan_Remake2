using System.Collections;
using UnityEngine;

/// <summary>
/// บอสหมาป่าสีชมพูบนยอดไม้ในด่านคี่ (ด่าน 3 Winter เป็นต้นไป ตามคู่มือ NES: ボスオオカミ・ピンク)
/// วิ่งไป-กลับอยู่ด้านบนสุดของฉากแล้วคอยปล่อยผลไม้ตกลงมาก่อกวน (ยิงผลไม้ทิ้งได้ 200 แต้ม)
/// </summary>
public class TreeTopBossWolf : MonoBehaviour
{
    [Header("Patrol Settings (การวิ่งบนยอดไม้)")]
    [SerializeField] private float walkSpeed = 2.2f;          // ความเร็วเดินไป-กลับด้านบน
    [SerializeField] private float walkMinX = -4.8f;         // ขอบเขตเดินซ้ายสุด
    [SerializeField] private float walkMaxX = 0.8f;          // ขอบเขตเดินขวาสุด
    [SerializeField] private float topY = 3.75f;             // ความสูงบนยอดไม้

    [Header("Fruit Attack (ปล่อยผลไม้ก่อกวน)")]
    [SerializeField] private float fruitThrowInterval = 3.2f; // โยนผลไม้ทุกๆ กี่วินาที
    [SerializeField] private float fruitFallSpeed = 3.0f;     // ความเร็วผลไม้ร่วงลงมา
    [SerializeField] private GameObject fruitPrefab;          // พรีแฟบผลไม้

    [Header("Visuals & Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Color bossTint = new Color(1f, 0.68f, 0.85f, 1f); // สีชมพูตามต้นฉบับ NES

    private int walkDirection = 1; // 1 เดินขวา, -1 เดินซ้าย
    private float fruitTimer;
    private bool isThrowing = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
            spriteRenderer.color = bossTint;
    }

    private void Start()
    {
        transform.position = new Vector3((walkMinX + walkMaxX) * 0.5f, topY, 0f);
        fruitTimer = fruitThrowInterval;
        PlayRunAnimation();
    }

    private void OnEnable()
    {
        fruitTimer = fruitThrowInterval;
        isThrowing = false;
        if (spriteRenderer != null)
            spriteRenderer.color = bossTint;
        PlayRunAnimation();
    }

    private void Update()
    {
        // เกมหยุดอยู่หรือแม่หมูตายอยู่ก็หยุดทำงานชั่วคราว
        if (GameManager.Instance != null && (!GameManager.Instance.IsPlaying || GameManager.Instance.DeathFreeze))
            return;

        UpdateWalk();
        UpdateFruitThrow();
    }

    // ระบบเดินไป-กลับของบอสยอดไม้ เดินชนขอบแล้วหันหลังกลับ
    private void UpdateWalk()
    {
        transform.position += Vector3.right * (walkDirection * walkSpeed * Time.deltaTime);

        if (transform.position.x >= walkMaxX)
        {
            walkDirection = -1;
            UpdateFacing();
        }
        else if (transform.position.x <= walkMinX)
        {
            walkDirection = 1;
            UpdateFacing();
        }
    }

    private void UpdateFacing()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (walkDirection < 0);
        }
    }

    private void PlayRunAnimation()
    {
        if (animator != null && animator.enabled)
        {
            animator.Play("Run", 0, 0f);
        }
        UpdateFacing();
    }

    // ปาผลไม้ลงมาก่อกวนผู้เล่น
    private void UpdateFruitThrow()
    {
        if (isThrowing) return;

        fruitTimer -= Time.deltaTime;
        if (fruitTimer > 0f || fruitPrefab == null) return;

        fruitTimer = fruitThrowInterval + Random.Range(-0.4f, 0.5f);
        StartCoroutine(ThrowFruitRoutine());
    }

    private IEnumerator ThrowFruitRoutine()
    {
        isThrowing = true;

        if (animator != null && animator.enabled)
        {
            animator.Play("ThrowRock", 0, 0f);
            yield return new WaitForSeconds(0.18f);
        }

        SpawnStageFruit();

        if (animator != null && animator.enabled)
        {
            yield return new WaitForSeconds(0.2f);
            PlayRunAnimation();
        }

        isThrowing = false;
    }

    private void SpawnStageFruit()
    {
        if (fruitPrefab == null) return;

        Vector3 spawnPos = transform.position + new Vector3(walkDirection * 0.3f, -0.25f, 0f);
        var fruit = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);

        // ถ้าใช้ Fruit.prefab ที่มี BonusFruit อยู่ ให้ยืมการตั้งค่ารูปผลไม้ (Strawberry/Cherry/Peach) ก่อนแล้วถอด BonusFruit ออก
        var bonusFruit = fruit.GetComponent<BonusFruit>();
        if (bonusFruit != null)
        {
            BonusFruitType randomType = (BonusFruitType)Random.Range(0, 3);
            bonusFruit.Initialize(randomType, Vector2.down * fruitFallSpeed, isArc: false);
            Destroy(bonusFruit);
        }

        var proj = fruit.GetComponent<WolfProjectile>();
        if (proj == null)
            proj = fruit.AddComponent<WolfProjectile>();

        proj.LaunchDown(WolfProjectileType.Fruit, fruitFallSpeed);
        AudioManager.Instance?.PlayFruitDrop();
    }

    // เปิด/ปิดบอสตัวนี้ตามด่าน (เปิดในด่านคี่ตั้งแต่ Stage 3 เป็นต้นไป)
    public void Activate(bool active)
    {
        gameObject.SetActive(active);
    }
}
