using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// เฮ้ยเพื่อน สคริปต์นี้เอาไว้คุมหมาป่าตัวปาผลไม้ใน Bonus Stage (เหมือนต้นฉบับ Pooyan ในตู้ Arcade เลย)
/// ตัวมันจะยืนเท่ๆ อยู่บนกิ่งไม้ซากุระฝั่งซ้าย พองวดไหนจะปา ก็ง้างแขนเล่นอนิเมชันปา แล้วปล่อยผลไม้ออกจากมือเป๊ะๆ!
/// </summary>
public class BonusWolfThrower : MonoBehaviour
{
    [Header("Sprites & Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] throwSprites; // เฟรม 0-7 จาก Wolf_Throw.png
    [SerializeField] private Sprite idleSprite;      // ท่ายืนรอ (เฟรม 0)
    [SerializeField] private float frameDuration = 0.08f; // ความเร็วของแต่ละเฟรม

    [Header("Throw Point")]
    [SerializeField] private Vector3 throwOffset = new Vector3(0.5f, 0.2f, 0f); // จุดที่ผลไม้หลุดจากมือ

    private bool isThrowing = false;
    private Coroutine throwRoutine;

    public bool IsThrowing => isThrowing;
    public Vector3 ThrowPosition => transform.position + throwOffset;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (idleSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = idleSprite;
    }

    /// <summary>
    /// สั่งให้หมาป่าตัวนี้ง้างแขนปาผลไม้!
    /// onReleaseCallback จะถูกเรียกจังหวะที่มือสะบัดไปข้างหน้าพอดี (เฟรม 4)
    /// </summary>
    public void Throw(Action onReleaseCallback)
    {
        if (throwRoutine != null)
            StopCoroutine(throwRoutine);

        throwRoutine = StartCoroutine(DoThrowRoutine(onReleaseCallback));
    }

    private IEnumerator DoThrowRoutine(Action onReleaseCallback)
    {
        isThrowing = true;

        if (throwSprites != null && throwSprites.Length > 0 && spriteRenderer != null)
        {
            // เฟรม 0 - 3: ง้างแขนไปข้างหลัง เตรียมปา
            for (int i = 0; i < Mathf.Min(4, throwSprites.Length); i++)
            {
                spriteRenderer.sprite = throwSprites[i];
                yield return new WaitForSeconds(frameDuration);
            }

            // เฟรม 4: จังหวะสะบัดแขนปล่อยผลไม้ออกไป! ปล่อยตรงนี้คือเป๊ะ 100%
            if (throwSprites.Length > 4)
                spriteRenderer.sprite = throwSprites[4];

            onReleaseCallback?.Invoke();
            yield return new WaitForSeconds(frameDuration);

            // เฟรม 5 - จบ: ท่าทางตามหลัง (Follow-through)
            for (int i = 5; i < throwSprites.Length; i++)
            {
                spriteRenderer.sprite = throwSprites[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }
        else
        {
            // ถ้าไม่มีสไปรท์ ก็ปล่อยผลไม้ได้เลย
            onReleaseCallback?.Invoke();
            yield return new WaitForSeconds(frameDuration * 2);
        }

        // ปาเสร็จแล้ว กลับมายืนท่าปกติรอลูกต่อไป
        if (idleSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = idleSprite;

        isThrowing = false;
        throwRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ThrowPosition, 0.2f);
    }
}
