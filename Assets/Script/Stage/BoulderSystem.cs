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

    private readonly List<Wolf> wolvesOnCliff = new(); // ลิสต์รายชื่อหมาที่รอดขึ้นผาได้
    private int virtualWolvesOnCliff = 0; 
    private bool boulderFalling; // เช็คว่าหินกำลังร่วงอยู่รึเปล่า

    public int WolvesOnCliff => wolvesOnCliff.Count; 
    public int WolfThreshold => GameConstants.BoulderWolfThreshold; // จำนวนตัวสูงสุดก่อนหินถล่ม

    public event System.Action<int, int> OnCliffCountChanged; // ส่งอีเวนต์บอกคนอื่นเรื่องจำนวนหมา

    private Vector3 initialBoulderPos; // จุดเริ่มของหิน
    private Vector3 boulderOffset; 
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
            }
            else
            {
                boulderOffset = new Vector3(1.14f, 0.36f, 0f);
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // ดักรอตอนเปลี่ยนด่าน
            GameManager.Instance.OnStageChanged += HandleStageChanged;
            HandleStageChanged(GameManager.Instance.CurrentStage);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStageChanged -= HandleStageChanged;
        if (Instance == this) Instance = null;
    }

    private void HandleStageChanged(int stage)
    {
        // เปิดใช้ก้อนหินเฉพาะในด่านหน้าผา
        if (boulder != null)
        {
            bool isStage2 = GameManager.Instance != null && !GameManager.Instance.IsOddStage;
            boulder.gameObject.SetActive(isStage2);
        }
    }

    private void Update()
    {
        if (boulder == null || boulderFalling || !boulder.gameObject.activeInHierarchy) return;

        int maxSlots = cliffWolfSlots != null ? cliffWolfSlots.Length : 5;
        int totalWolves = wolvesOnCliff.Count + virtualWolvesOnCliff;
        
        // ยิ่งหมาเยอะ หินยิ่งโดนดันเข้าใกล้เรา
        int frontSlotIndex = maxSlots - Mathf.Max(1, totalWolves);
        frontSlotIndex = Mathf.Clamp(frontSlotIndex, 0, maxSlots - 1);
        
        Vector3 targetBoulderPos = GetCliffSlotPosition(frontSlotIndex) + boulderOffset;
        
        // ขยับหินมาเรื่อยๆ
        boulder.position = Vector3.MoveTowards(boulder.position, targetBoulderPos, moveSpeed * Time.deltaTime);
    }

    public void RegisterWolfOnCliff(Wolf wolf)
    {
        if (boulderFalling) return; // ถ้าหินตกไปแล้ว ไม่ต้องนับเพิ่มละ

        if (!wolvesOnCliff.Contains(wolf))
        {
            wolvesOnCliff.Add(wolf);
            int total = wolvesOnCliff.Count + virtualWolvesOnCliff;
            OnCliffCountChanged?.Invoke(total, WolfThreshold);

            // ซวยแล้ว หมาครบ หินถล่ม!!
            if (total >= WolfThreshold)
            {
                TriggerBoulder();
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
        // หาว่าหมาต้องไปยืนตรงไหน
        int maxSlots = cliffWolfSlots != null ? cliffWolfSlots.Length : 5;
        int totalWolves = wolvesOnCliff.Count + virtualWolvesOnCliff;
        
        int wolfIndexInList = wolvesOnCliff.IndexOf(wolf);
        if (wolfIndexInList == -1) wolfIndexInList = wolvesOnCliff.Count;
        
        int overallIndex = virtualWolvesOnCliff + wolfIndexInList;
        
        int targetSlotIndex = (maxSlots - totalWolves) + overallIndex;
        targetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, maxSlots - 1);
        
        return GetCliffSlotPosition(targetSlotIndex);
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
        
        var player = FindAnyObjectByType<PlayerController>();
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
        virtualWolvesOnCliff = keepCount;
        boulderFalling = false;
        
        if (boulder != null)
        {
            bool isStage2 = GameManager.Instance != null && !GameManager.Instance.IsOddStage;
            boulder.gameObject.SetActive(isStage2);
            
            // จับหินกลับเข้าที่
            int maxSlots = cliffWolfSlots != null ? cliffWolfSlots.Length : 5;
            int frontSlotIndex = maxSlots - Mathf.Max(1, keepCount);
            frontSlotIndex = Mathf.Clamp(frontSlotIndex, 0, maxSlots - 1);
            boulder.position = GetCliffSlotPosition(frontSlotIndex) + boulderOffset;
        }
        
        OnCliffCountChanged?.Invoke(virtualWolvesOnCliff, WolfThreshold);
    }
}
