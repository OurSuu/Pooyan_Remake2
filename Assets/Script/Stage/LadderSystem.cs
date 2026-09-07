using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// อันนี้ระบบบันไดนะ จัดการหมาป่าที่กำลังปีนบันได ถ้ามันปีนมาถึงจุดที่กัดเราได้ เราเจ็บตัวแน่นอน!
/// </summary>
public class LadderSystem : MonoBehaviour
{
    public static LadderSystem Instance { get; private set; }

    [SerializeField] private float ladderX = 3.5f; // แกน X ของบันได
    [SerializeField] private float ladderBottomY = -3.5f; // ฐานบันได
    [SerializeField] private float stepHeight = 0.6f; // ความสูงแต่ละขั้น
    [SerializeField] private int maxSteps = 12; // มีบันไดกี่ขั้น

    [Header("Manual Steps (Optional)")]
    [SerializeField] private Transform[] stepTransforms; // ขั้นบันไดแบบลากใส่เอง (เผื่ออยากตั้งจุดแบบเป๊ะๆ)

    [Header("Fatal Settings")]
    [SerializeField] private int fatalWolfCount = 5; // ถ้าหมาปีนเกินจำนวนนี้เมื่อไหร่ ซวยแน่

    private readonly Dictionary<Wolf, int> wolfSteps = new(); // เก็บว่าหมาตัวไหนอยู่ขั้นไหน
    private readonly HashSet<int> occupiedSteps = new(); // เช็คว่าขั้นไหนโดนยึดไปแล้วบ้าง

    public float LadderX => ladderX;

    private void Awake()
    {
        Instance = this; // สร้าง Singleton โง่ๆ ใช้งานง่าย
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null; // อย่าลืมเคลียร์
    }

    public int? TryReserveStepForWolf(Wolf wolf)
    {
        // เช็คก่อนว่ามีที่ให้ยืนมั้ย
        if (wolfSteps.ContainsKey(wolf)) return wolfSteps[wolf];

        int stepCount = (stepTransforms != null && stepTransforms.Length > 0) ? stepTransforms.Length : maxSteps;

        // ไล่หาขั้นที่ว่างอยู่
        for (int i = 0; i < stepCount; i++)
        {
            if (!occupiedSteps.Contains(i))
            {
                occupiedSteps.Add(i);
                wolfSteps[wolf] = i;

                // ถ้าหมาปีนมาเยอะเกิน ล่มสลายจ้า
                if (occupiedSteps.Count >= fatalWolfCount)
                {
                    TriggerLadderOverrun();
                }

                return i;
            }
        }
        return null;
    }

    private void TriggerLadderOverrun()
    {
        // หมามาเยอะไป ปีนกัดเลย
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.State != PlayerState.Dead)
        {
            AudioManager.Instance?.PlayWolfBite(); // เสียงงับๆ
            player.TakeDamage(); // ตายจ้า
        }
    }

    public float GetStepY(Wolf wolf, int? stepIndex)
    {
        if (stepIndex.HasValue)
        {
            if (stepTransforms != null && stepIndex.Value < stepTransforms.Length)
            {
                return stepTransforms[stepIndex.Value].position.y;
            }
            return ladderBottomY + stepIndex.Value * stepHeight; // คำนวณแบบสเกลปกติ
        }
        return ladderBottomY;
    }

    public void ReleaseStepByIndex(int index, Wolf wolf)
    {
        // ปล่อยขั้นบันได เผื่อหมาตายละ
        occupiedSteps.Remove(index);
        wolfSteps.Remove(wolf);
    }

    public void ReleaseStep(Wolf wolf)
    {
        if (wolfSteps.TryGetValue(wolf, out int index))
        {
            ReleaseStepByIndex(index, wolf);
        }
    }

    public void CheckBite(Wolf wolf)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null || wolf == null) return;

        float wolfY = wolf.transform.position.y;
        float playerY = player.transform.position.y;

        // ถ้าระยะห่างหมากับหมูใกล้กันเกินไป มันจะงับเรา!
        if (Mathf.Abs(playerY - wolfY) <= 1.2f)
        {
            wolf.TriggerBite();
        }
    }

    public void OnWolfReachedStep(Wolf wolf, int stepIndex)
    {
        // ปล่อยว่างไว้ก่อน เผื่อใช้เพิ่มลูกเล่นทีหลัง
    }

    public void ResetLadder()
    {
        // ล้างข้อมูลบันไดเตรียมเล่นใหม่
        wolfSteps.Clear();
        occupiedSteps.Clear();
    }
}
