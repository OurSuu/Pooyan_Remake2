using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตัวจัดการยิงธนูของแม่หมู ยิงได้สูงสุด 2 ดอกในจอเหมือนเกมต้นฉบับ
/// รับคำสั่งมาจาก PlayerController อีกที
/// </summary>
public class ArrowShooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject arrowPrefab; // พรีแฟบลูกธนู
    [SerializeField] private Transform shootPoint; // จุดปล่อยลูกธนู
    [SerializeField] private float rapidCooldown = GameConstants.RapidShootCooldown; // ดีเลย์ระหว่างนัดที่ 1 กับ 2 (0.12s)
    [SerializeField] private float reloadCooldown = GameConstants.ReloadShootCooldown; // คูลดาวน์หลังยิงครบ 2 นัด (0.40s)
    [SerializeField] private float shootDirection = -1f; // หันหน้าไปซ้ายเป็นหลัก

    private float shootTimer; // ตัวนับเวลาคูลดาวน์
    private int burstCount = 0; // จำนวนนัดที่ยิงในชุดปัจจุบัน (0, 1, 2)
    private float burstResetTimer = 0f; // ตัวนับเวลารีเซ็ตชุด ถ้าทิ้งช่วงนานเกินไป
    private bool canShoot = true; // ล็อกการยิงได้ (เช่นตอนถือเนื้ออยู่)
    private readonly List<Arrow> activeArrows = new(); // เก็บลูกธนูที่กำลังลอยอยู่ในจอ

    public float RapidCooldown
    {
        get => rapidCooldown;
        set => rapidCooldown = value;
    }

    public float ReloadCooldown
    {
        get => reloadCooldown;
        set => reloadCooldown = value;
    }

    private void Awake()
    {
        if (rapidCooldown < 0.05f) rapidCooldown = GameConstants.RapidShootCooldown;
        if (reloadCooldown < 0.1f) reloadCooldown = GameConstants.ReloadShootCooldown;
    }

    private void Update()
    {
        if (shootTimer > 0f) shootTimer -= Time.deltaTime; // ลดเวลาคูลดาวน์ไปเรื่อยๆ

        // ถ้ายิงนัดแรกแล้วทิ้งช่วงไม่ยิงนัดที่สอง ให้รีเซ็ตกลับไปเป็นนัดแรกของชุดใหม่
        if (burstCount > 0)
        {
            burstResetTimer -= Time.deltaTime;
            if (burstResetTimer <= 0f)
            {
                burstCount = 0;
            }
        }
    }

    public void SetCanShoot(bool value) => canShoot = value;

    // เรียกตอนผู้เล่นกดยิง คืนค่าเป็น true ถ้ายิงออก
    public bool TryShoot()
    {
        // ถ้ายิงไม่ได้ หรือยังติดคูลดาวน์อยู่ ก็แห้วไป
        if (!canShoot || shootTimer > 0f) return false;

        CleanupDestroyed(); // เก็บกวาดลูกธนูที่พังไปแล้วออกจากลิสต์ก่อน
        
        // กฎเหล็กของ Pooyan! มีธนูในจอได้จำกัด 2 ดอกเท่านั้น
        if (activeArrows.Count >= GameConstants.MaxArrowsOnScreen) return false;
        if (arrowPrefab == null || shootPoint == null) return false;

        // สร้างลูกธนูใหม่
        var go = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity);
        var arrow = go.GetComponent<Arrow>();
        if (arrow != null)
        {
            // โยน callback UnregisterArrow ไปด้วย พอมันพังมันจะได้ลบตัวเองออกจากลิสต์
            arrow.Initialize(shootDirection, UnregisterArrow);
            activeArrows.Add(arrow);
        }

        burstCount++;
        if (burstCount >= 2)
        {
            // ยิงครบ 2 นัดติดกันแล้ว -> ติดคูลดาวน์พักง้างธนูชุดใหม่ (0.40s) ป้องกันสแปมปืนกล
            shootTimer = reloadCooldown;
            burstCount = 0;
        }
        else
        {
            // เพิ่งยิงนัดแรก -> คูลดาวน์สั้นๆ (0.12s) เพื่อให้กดยิงนัดที่ 2 ตามไปติดๆ ได้
            shootTimer = rapidCooldown;
            burstResetTimer = reloadCooldown;
        }

        AudioManager.Instance?.PlayArrowFire(); // เล่นเสียงฟิ้ว!
        return true;
    }

    // callback ที่ลูกธนูจะเรียกตอนตาย
    private void UnregisterArrow(Arrow arrow)
    {
        activeArrows.Remove(arrow);
    }

    // ฟังก์ชันล้างลิสต์เผื่อลูกธนูหายไปเฉยๆ (เช่น ข้าม Scene)
    private void CleanupDestroyed()
    {
        activeArrows.RemoveAll(a => a == null);
    }
}
