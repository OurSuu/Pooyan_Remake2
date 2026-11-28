using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// เฮ้ยเพื่อน นี่คือบอสหมาป่าใน Bonus Stage ฉบับ Famicom (NES) ของแท้ดั้งเดิมเลย!
/// ตัวบอสจะวิ่งไป-กลับอยู่บนยอดไม้ด้านบนของจอ แล้วคอยหย่อน/ทิ้งผลไม้เรืองแสงตกลงมาให้แม่หมูเล็งยิงเก็บแต้ม
/// ยิงโดนต่อเนื่องโดยไม่ปล่อยให้ตกพื้น คะแนนจะคูณเป็นคอมโบ Chain: 100 -> 200 -> 400 -> ... -> Nakamoto 8,000 แต้ม!
/// </summary>
public class BonusBossWolf : MonoBehaviour
{
    [Header("Patrol Settings (การวิ่งบนยอดไม้)")]
    [SerializeField] private float walkSpeed = 2.8f;       // ความเร็วในการวิ่ง
    [SerializeField] private float walkMinX = -4.8f;      // ขอบเขตวิ่งซ้ายสุด (บนกิ่งไม้ซากุระ)
    [SerializeField] private float walkMaxX = 1.2f;       // ขอบเขตวิ่งขวาสุด (ก่อนถึงกระเช้าแม่หมู)
    [SerializeField] private float topY = 3.75f;          // ความสูงระดับยอดไม้

    [Header("Visuals & Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private string runAnimName = "Run";
    [SerializeField] private string throwFruitAnimName = "ThrowFruit";
    [SerializeField] private Sprite[] throwSprites;        // เฟรมท่าปา (ถ้ามี)
    [SerializeField] private Sprite idleSprite;

    private int walkDirection = 1; // 1 = วิ่งไปขวา, -1 = วิ่งไปซ้าย
    private bool isThrowing = false;
    private bool isPatrolling = true;

    public bool IsThrowing => isThrowing;
    public Vector3 DropPosition => transform.position + new Vector3(walkDirection * 0.4f, -0.2f, 0f);

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        transform.position = new Vector3((walkMinX + walkMaxX) * 0.5f, topY, 0f);
        SetRunAnimation(true);
    }

    private void Update()
    {
        if (!isPatrolling) return;

        // วิ่งไป-กลับ ซ้ายขวาบนยอดไม้
        transform.position += Vector3.right * (walkDirection * walkSpeed * Time.deltaTime);

        // ชนขอบขวา -> หันกลับไปซ้าย
        if (transform.position.x >= walkMaxX)
        {
            walkDirection = -1;
            UpdateFacing();
        }
        // ชนขอบซ้าย -> หันกลับไปขวา
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
            // ถ้าหันซ้ายให้ flipX
            spriteRenderer.flipX = (walkDirection < 0);
        }
    }

    /// <summary>
    /// สั่งให้บอสปล่อย/ทิ้งผลไม้ลงมา!
    /// </summary>
    public void DropFruit(Action onDropCallback)
    {
        StartCoroutine(DoDropFruitRoutine(onDropCallback));
    }

    private IEnumerator DoDropFruitRoutine(Action onDropCallback)
    {
        isThrowing = true;

        // ถ้ามี Animator ให้เล่นท่า ThrowFruit (ถ้าไม่มี State นี้ใน Animator ให้ปล่อยผลไม้ต่อโดยไม่ใช้ท่า ThrowRock)
        if (animator != null && animator.enabled)
        {
            int throwHash = !string.IsNullOrEmpty(throwFruitAnimName) ? Animator.StringToHash(throwFruitAnimName) : 0;
            if (!string.IsNullOrEmpty(throwFruitAnimName) && animator.HasState(0, throwHash))
            {
                animator.Play(throwHash, 0, 0f);
                yield return new WaitForSeconds(0.2f);
                onDropCallback?.Invoke();
                yield return new WaitForSeconds(0.2f);
                SetRunAnimation(true);
            }
            else
            {
                onDropCallback?.Invoke();
                yield return new WaitForSeconds(0.15f);
            }
        }
        else if (throwSprites != null && throwSprites.Length > 4 && spriteRenderer != null)
        {
            spriteRenderer.sprite = throwSprites[4];
            onDropCallback?.Invoke();
            yield return new WaitForSeconds(0.2f);
            SetRunAnimation(true);
        }
        else
        {
            // ปล่อยผลไม้ร่วงลงมาทันที
            onDropCallback?.Invoke();
            yield return new WaitForSeconds(0.15f);
        }

        isThrowing = false;
    }

    private void SetRunAnimation(bool running)
    {
        if (animator != null && animator.enabled && !string.IsNullOrEmpty(runAnimName))
        {
            int runHash = Animator.StringToHash(runAnimName);
            if (animator.HasState(0, runHash))
            {
                animator.Play(runHash, 0, 0f);
            }
        }
        UpdateFacing();
    }

    public void SetPatrolActive(bool active)
    {
        isPatrolling = active;
        gameObject.SetActive(active);
    }
}
