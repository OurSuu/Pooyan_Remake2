using UnityEngine;

/// <summary>
/// คอนฟิกความยากของแต่ละด่าน — สร้างเป็น ScriptableObject asset ไว้ใช้สะดวกๆ
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Pooyan/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Wolves")]
    public int wolfCount = 24;                     // จำนวนหมาป่าในด่านนี้ (ลดลงจาก 32 เป็น 24 ให้ด่านแรกง่ายขึ้น)
    public Vector2 wolfSpeedRange = new(1.2f, 2.5f); // สปีดการเคลื่อนที่ (ลดจาก 1.5-3.0 เป็น 1.2-2.5)
    [Range(0f, 1f)] public float shieldRatio = 0.05f;      // ลดโอกาสถือโล่
    [Range(0f, 1f)] public float rockThrowRatio = 0.05f;  // ลดโอกาสปาหิน
    public float spawnInterval = 3f;               // ระยะเวลาห่างในการเกิดตัวถัดไป (เพิ่มจาก 1 เป็น 3 วิ)

    [Header("Balloon")]
    [Min(1)] public int balloonHP = 1;             // เลือดลูกโป่ง ยิงกี่ทีแตก

    [Header("Boss")]
    public bool hasBossWolf = true;                // ด่านนี้มีบอสไหม
    public bool hasTreeTopBoss;                    // มีบอสบนต้นไม้หรือเปล่า
    [Min(1)] public int bossShieldHP = 3;          // โล่บอสอึดแค่ไหน
}
