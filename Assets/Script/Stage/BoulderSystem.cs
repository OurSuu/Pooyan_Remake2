using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// เฮ้ยเพื่อน ไฟล์นี้สำหรับระบบหินกลิ้ง ในด่านที่หมาป่าไต่ขึ้นมา ถ้ามันขึ้นมาถึงหน้าผาครบ 7 ตัวเมื่อไหร่ หินจะกลิ้งทับเราตาย!
/// </summary>
public class BoulderSystem : MonoBehaviour
{
    public static BoulderSystem Instance { get; private set; }

    [SerializeField] private Transform boulder; // ตัวหิน
    [SerializeField] private float cliffY = 3.5f; // ความสูงหน้าผา
    [SerializeField] private Transform[] cliffWolfSlots; // จุดที่ให้หมาป่ายืนเรียงคิว

    private readonly List<Wolf> wolvesOnCliff = new(); // ลิสต์รายชื่อหมาที่วิ่งมาถึงจุดและกำลังช่วยกันดันหินจริงๆ
    private readonly List<Wolf> approachingWolves = new(); // ลิสต์หมาที่เพิ่งขึ้นหน้าผามาและกำลังวิ่งไปต่อแถวหลังเพื่อน
    private int virtualWolvesOnCliff = 0; 
    private bool boulderFalling; // เช็คว่าหินกำลังร่วงอยู่รึเปล่า
    private bool pendingBoulderDrop; // รอให้หมาป่าตัวสุดท้ายดันหินไปถึงขอบผาก่อนค่อยร่วง

    public int WolvesOnCliff => wolvesOnCliff.Count; 
    public int WolfThreshold => GameConstants.BoulderWolfThreshold; // จำนวนตัวสูงสุดก่อนหินถล่ม

    public event System.Action<int, int> OnCliffCountChanged; // ส่งอีเวนต์บอกคนอื่นเรื่องจำนวนหมา

    private Vector3 initialBoulderPos; // จุดเริ่มของหิน
    private Vector3 boulderOffset; 
    private float slotSpacing = 0.8f; // ระยะห่างระหว่างจุดยืนของหมาป่าแต่ละตัว
    private const float FirstWolfPushDistance = 0.35f; // หมาป่าตัวแรกมาถึงจะขยับหินได้นิดเดียว เพราะหินหนักมาก
    private float moveSpeed = 2f; 

    private void Awake()
    {
        Instance = this; 
        if (boulder != null)
        {
            initialBoulderPos = boulder.position;
            if (cliffWolfSlots != null && cliffWolfSlots.Length > 0)
            {
                // หาระยะห่างของหินไว้ จะได้ดันหินเนียนๆ
                boulderOffset = initialBoulderPos - cliffWolfSlots[cliffWolfSlots.Length - 1].position;
                if (cliffWolfSlots.Length >= 2 && cliffWolfSlots[0] != null && cliffWolfSlots[1] != null)
                {
                    float diff = Mathf.Abs(cliffWolfSlots[0].position.x - cliffWolfSlots[1].position.x);
                    if (diff > 0.1f) slotSpacing = diff;
                }
            }
            else
            {
                boulderOffset = new Vector3(1.14f, 0.36f, 0f);
            }

            // เริ่มต้นให้หินวางถอยไปทางซ้ายเล็กน้อย พอหมาป่าตัวแรกวิ่งมาถึงจะได้ออกแรงดันขยับไปได้นิดนึงสมจริง
            boulder.position = GetTargetBoulderPosition(0);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // ดักรอตอนเปลี่ยนด่านและเปลี่ยนสถานะ
            GameManager.Instance.OnStageChanged += HandleStageChanged;
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            UpdateBoulderVisibility();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageChanged -= HandleStageChanged;
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }
        if (Instance == this) Instance = null;
    }

    private void HandleStageChanged(int stage) => UpdateBoulderVisibility();
    private void HandleGameStateChanged(GameState state) => UpdateBoulderVisibility();

    private void UpdateBoulderVisibility()
    {
        // เปิดใช้ก้อนหินเฉพาะในด่านหน้าผา (และต้องไม่ใช่ช่วงด่านโบนัส)
        if (boulder != null)
        {
            bool isStage2 = GameManager.Instance != null && !GameManager.Instance.IsOddStage && !GameManager.Instance.IsBonus;
            boulder.gameObject.SetActive(isStage2);
        }
    }

    private Vector3 GetTargetBoulderPosition(int totalPushingWolves)
    {
        int maxSlots = (cliffWolfSlots != null && cliffWolfSlots.Length > 0) ? cliffWolfSlots.Length : 5;
        if (totalPushingWolves <= 0)
        {
            // ตอนยังไม่มีหมาป่ามาถึงหิน หินจะวางอยู่ก่อนจุดดันของตัวที่ 1 เล็กน้อย (FirstWolfPushDistance)
            return GetCliffSlotPosition(maxSlots - 1) + boulderOffset - new Vector3(FirstWolfPushDistance, 0f, 0f);
        }

        int frontSlotIndex = maxSlots - Mathf.Max(1, totalPushingWolves);
        frontSlotIndex = Mathf.Clamp(frontSlotIndex, 0, maxSlots - 1);
        return GetCliffSlotPosition(frontSlotIndex) + boulderOffset;
    }

    private void Update()
    {
        if (boulder == null || boulderFalling || !boulder.gameObject.activeInHierarchy) return;

        int totalPushing = wolvesOnCliff.Count + virtualWolvesOnCliff;
        Vector3 targetBoulderPos = GetTargetBoulderPosition(totalPushing);

        // ขยับหินเฉพาะเมื่อมีหมาป่าวิ่งมาถึงจุดและช่วยกันออกแรงดันแล้วเท่านั้น!
        boulder.position = Vector3.MoveTowards(boulder.position, targetBoulderPos, moveSpeed * Time.deltaTime);

        // ถ้าหมาป่ามาช่วยดันครบจำนวนแล้ว รอให้ดันหินเลื่อนไปถึงขอบผาเสร็จก่อนแล้วค่อยร่วงถล่มลงมา
        if (pendingBoulderDrop && Mathf.Abs(boulder.position.x - targetBoulderPos.x) <= 0.03f)
        {
            pendingBoulderDrop = false;
            TriggerBoulder();
        }
    }

    // เรียกตอนหมาป่าเพิ่งลอยพ้นขอบหน้าผาขึ้นมา (ให้วิ่งไปต่อคิวก่อน ยังไม่นับว่าดันหินทันที)
    public void RegisterWolfOnCliff(Wolf wolf)
    {
        if (boulderFalling || wolf == null) return;

        if (!wolvesOnCliff.Contains(wolf) && !approachingWolves.Contains(wolf))
        {
            approachingWolves.Add(wolf);
        }
    }

    // เช็คว่าหมาป่าตัวนี้วิ่งมาถึงก้อนหิน/ท้ายแถวเพื่อนและเริ่มช่วยดันหินแล้วหรือยัง
    public bool IsWolfPushing(Wolf wolf)
    {
        return wolf != null && wolvesOnCliff.Contains(wolf);
    }

    // เรียกเมื่อหมาป่าวิ่งมาถึงตำแหน่งต่อแถวหลังเพื่อน/หลังก้อนหินเรียบร้อยแล้ว -> เริ่มออกแรงดันหินพร้อมกัน!
    public void NotifyWolfArrivedAtBoulder(Wolf wolf)
    {
        if (boulderFalling || wolf == null) return;

        approachingWolves.Remove(wolf);
        if (!wolvesOnCliff.Contains(wolf))
        {
            wolvesOnCliff.Add(wolf);
            int total = wolvesOnCliff.Count + virtualWolvesOnCliff;
            OnCliffCountChanged?.Invoke(total, WolfThreshold);

            AudioManager.Instance?.PlayBoulderPush(); // เสียงหมาป่าช่วยกันออกแรงดันหินขยับ!

            // ถ้าหมาป่ามาช่วยครบจำนวนแล้ว ให้ดันหินไปจนสุดขอบผาแล้วถล่มลงมา!
            if (total >= WolfThreshold)
            {
                pendingBoulderDrop = true;
            }
        }
    }

    private void TriggerBoulder()
    {
        boulderFalling = true; // ล็อคไว้ว่าหินกำลังหล่น
        AudioManager.Instance?.PlayBoulderFall(); // เสียงหินถล่มต้องมา
        StartCoroutine(DropBoulderRoutine());
    }

    public Vector3 GetCliffTargetPosition(Wolf wolf)
    {
        int maxSlots = (cliffWolfSlots != null && cliffWolfSlots.Length > 0) ? cliffWolfSlots.Length : 5;
        int totalPushing = wolvesOnCliff.Count + virtualWolvesOnCliff;

        // กรณีหมาป่าตัวนี้อยู่ในกลุ่มที่กำลังช่วยกันดันหินแล้ว
        int wolfIndexInPushing = wolvesOnCliff.IndexOf(wolf);
        if (wolfIndexInPushing >= 0)
        {
            int overallIndex = virtualWolvesOnCliff + wolfIndexInPushing;
            int targetSlotIndex = (maxSlots - Mathf.Max(1, totalPushing)) + overallIndex;
            targetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, maxSlots - 1);
            return GetCliffSlotPosition(targetSlotIndex);
        }

        // ล้างตัวที่อาจถูกทำลายไประหว่างทางออกจากคิววิ่ง
        for (int i = approachingWolves.Count - 1; i >= 0; i--)
        {
            if (approachingWolves[i] == null) approachingWolves.RemoveAt(i);
        }

        // กรณีหมาป่าตัวนี้เพิ่งขึ้นมาถึงหน้าผาและกำลังวิ่งไปต่อท้ายแถวเพื่อน (ยังไม่ดันหินจนกว่าจะวิ่งไปถึง)
        int appIdx = approachingWolves.IndexOf(wolf);
        if (appIdx < 0) appIdx = 0;

        Vector3 backSlotPos = GetCliffSlotPosition(maxSlots - 1);
        if (totalPushing <= 0 && appIdx == 0)
        {
            // หมาป่าตัวแรกสุด: วิ่งไปแตะหลังก้อนหินที่จุดเริ่มต้นก่อน แล้วค่อยออกแรงดันขยับนิดนึง
            return backSlotPos - new Vector3(FirstWolfPushDistance, 0f, 0f);
        }

        // หมาป่าตัวที่ 2 เป็นต้นไป: วิ่งไปให้ถึงจุดต่อท้ายเพื่อนตัวสุดท้ายที่ยืนรออยู่ก่อน พอถึงปุ๊บค่อยช่วยกันดันหิน!
        return backSlotPos - new Vector3(slotSpacing * (appIdx + 1), 0f, 0f);
    }

    private Vector3 GetCliffSlotPosition(int index)
    {
        if (cliffWolfSlots != null && index < cliffWolfSlots.Length && cliffWolfSlots[index] != null)
            return cliffWolfSlots[index].position;

        return new Vector3(-3f + index * 0.5f, cliffY, 0f);
    }

    private IEnumerator DropBoulderRoutine()
    {
        if (boulder == null) yield break;

        boulder.gameObject.SetActive(true);
        Vector3 startPos = boulder.position;
        
        var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
        Vector3 targetPos = startPos + Vector3.down * 10f;
        
        if (player != null)
        {
            // ให้มันตกทะลุตัวหมูไปหน่อย
            targetPos = player.transform.position + Vector3.down * 2f; 
        }

        float elapsed = 0f;
        float duration = 1.5f;
        
        // จุดสูงสุดตอนหินเด้งนิดนึงก่อนตก
        Vector3 midPoint = (startPos + targetPos) / 2f;
        midPoint.y = startPos.y + 1.5f;

        // อนิเมชันร่วงแบบโค้งๆ
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 m1 = Vector3.Lerp(startPos, midPoint, t);
            Vector3 m2 = Vector3.Lerp(midPoint, targetPos, t);
            boulder.position = Vector3.Lerp(m1, m2, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        AudioManager.Instance?.PlayBoulderHit(); // เสียงหินยักษ์ตกกระแทกกระเช้าแม่หมู!

        if (player != null)
        {
            player.SetDiedFromBoulder(true); // เซ็ตสถานะให้ตายเพราะหิน
            player.TakeDamage(); // เรียบร้อย ตายชัวร์
        }
        else
        {
            ResetCliff();
        }
    }

    public void ResetCliff(int keepCount = 0)
    {
        // โละหมา เคลียร์หิน
        wolvesOnCliff.Clear();
        approachingWolves.Clear();
        virtualWolvesOnCliff = Mathf.Max(0, keepCount);
        boulderFalling = false;
        pendingBoulderDrop = false;
        
        if (boulder != null)
        {
            bool isStage2 = GameManager.Instance != null && !GameManager.Instance.IsOddStage && !GameManager.Instance.IsBonus;
            boulder.gameObject.SetActive(isStage2);
            boulder.position = GetTargetBoulderPosition(virtualWolvesOnCliff);
        }
        
        OnCliffCountChanged?.Invoke(virtualWolvesOnCliff, WolfThreshold);
    }
}
