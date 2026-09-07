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
    [SerializeField] private float shootCooldown = 0.15f; // คูลดาวน์กันกดยิงรัวเกิน
    [SerializeField] private float shootDirection = -1f; // หันหน้าไปซ้ายเป็นหลัก

    private float shootTimer; // ตัวนับเวลาคูลดาวน์
    private bool canShoot = true; // ล็อกการยิงได้ (เช่นตอนถือเนื้ออยู่)
    private readonly List<Arrow> activeArrows = new(); // เก็บลูกธนูที่กำลังลอยอยู่ในจอ

    private void Update()
    {
        shootTimer -= Time.deltaTime; // ลดเวลาคูลดาวน์ไปเรื่อยๆ
    }

    public void SetCanShoot(bool value) => canShoot = value;

    // เรียกตอนผู้เล่นกดยิง คืนค่าเป็น true ถ้ายิงออก
    public bool TryShoot()
    {
        // ถ้ายิงไม่ได้ หรือยังติดคูลดาวน์อยู่ ก็แห้วไป
        if (!canShoot || shootTimer > 0f) return false;

        CleanupDestroyed(); // เก็บกวาดลูกธนูที่พังไปแล้วออกจากลิสต์ก่อน
        
        // กฎเหล็กของ Pooyan! มีธนูในจอได้จำกัด (น่าจะ 2 ดอก)
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

        shootTimer = shootCooldown; // เริ่มนับคูลดาวน์ใหม่
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
