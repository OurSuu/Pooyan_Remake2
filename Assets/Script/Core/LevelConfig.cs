using UnityEngine;

/// <summary>
/// คอนฟิกความยากของแต่ละด่าน — สร้างเป็น ScriptableObject asset ไว้ใช้สะดวกๆ
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Pooyan/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Wolves")]
    public int wolfCount = 20;                     // จำนวนหมาป่าในด่านนี้
    public Vector2 wolfSpeedRange = new(0.85f, 1.25f); // สปีดการลอย (ปรับให้ใกล้เคียง NES ต้นฉบับ ลอยข้ามจอ ~6-8 วินาที)
    [Range(0f, 1f)] public float shieldRatio = 0.1f;      // โอกาสถือโล่
    [Range(0f, 1f)] public float rockThrowRatio = 0.15f;  // โอกาสปาหิน
    public float spawnInterval = 2.5f;             // ระยะเวลาห่างในการเกิดตัวถัดไป

    [Header("Balloon")]
    [Min(1)] public int balloonHP = 1;                     // เลือดลูกโป่งพื้นฐาน (1 = ปกติ)
    [Range(0f, 1f)] public float yellowBalloonRatio = 0.3f; // โอกาสเกิดลูกโป่งสีเหลือง (2 HP) ในด่านที่รองรับ (ต้นฉบับ Famicom ด่าน 2 คือ 30%)

    [Header("Boss")]
    public bool hasBossWolf = true;                // ด่านนี้มีบอสไหม
    public bool hasTreeTopBoss;                    // มีบอสบนต้นไม้หรือเปล่า
    [Min(1)] public int bossShieldHP = 3;          // โล่บอสอึดแค่ไหน
}
