using System;
using UnityEngine;

/// <summary>
/// ระบบนับจำนวนหมาป่าที่ถูกฆ่าจริงๆ การที่มันลงพื้นหรือไปถึงหน้าผาไม่นับเป็นคิลนะ
/// (ตามหลัก Arcade: ถ้ารอด ด่านคี่มันจะมาเติมใหม่ ถ้าด่านคู่มันไปดันหิน)
/// </summary>
public class WolfTracker : MonoBehaviour
{
    public static WolfTracker Instance { get; private set; } // Singleton เรียกใช้ง่ายๆ

    private int targetKills; // ต้องฆ่ากี่ตัวถึงจะผ่านด่าน
    private int kills; // ฆ่าไปได้กี่ตัวแล้ว
    private bool suppressNotifications; // ปิดการแจ้งเตือนชั่วคราว
    private bool stageCleared; // ด่านจบยัง?

    public int Kills => kills;
    public int TargetKills => targetKills;
    public int RemainingKills => Mathf.Max(0, targetKills - kills); // เหลืออีกกี่ตัว (ต่ำสุด 0)

    public event Action OnAllWolvesDefeated; // อีเวนต์ตอนฆ่าครบ
    public event Action OnWolfEscapedToGround; // อีเวนต์ตอนหมาป่าหนีรอด
    public event Action<int> OnKillCountChanged; // อีเวนต์อัปเดต UI

    private void Awake()
    {
        Instance = this; // ผูก Singleton ตอนเริ่ม
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null; // คืนค่าตอนโดนทำลาย กันบัค
    }

    // เรียกตอนเริ่มด่านใหม่ ตั้งค่าจำนวนเป้าหมาย
    public void BeginStage(int wolfKillQuota)
    {
        stageCleared = false;
        targetKills = Mathf.Max(1, wolfKillQuota);
        kills = 0;
        suppressNotifications = false;
        OnKillCountChanged?.Invoke(RemainingKills); // อัปเดต UI ให้เห็นว่าเป้าหมายมีกี่ตัว
    }

    // เอาไว้ระงับการแจ้งเตือน (เช่น ตอนแม่หมูตาย ไม่ต้องนับแต้มชั่วคราว)
    public void SuppressNotifications(bool suppress)
    {
        suppressNotifications = suppress;
    }

    // แปะระบบติดตามเวลาเสกหมาป่าแต่ละตัว
    public void Register(Wolf wolf)
    {
        if (wolf == null) return;

        var notifier = wolf.GetComponent<WolfDeathNotifier>();
        if (notifier == null)
            notifier = wolf.gameObject.AddComponent<WolfDeathNotifier>();
        notifier.Initialize(this); // ผูกมันเข้ากับ Tracker ตัวนี้แหละ
    }

    // หมาป่าโดนสอย! เข้ามาเรียกนี่เลย
    public void NotifyKilled()
    {
        // ถ้าปิดแจ้งเตือน หรือจบด่านไปแล้ว ก็ข้ามไป
        if (suppressNotifications || stageCleared) return;
        
        // ถ้าเกมค้างเพราะแม่หมูตายอยู่ ก็ไม่นับ
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze) return;

        kills++;
        OnKillCountChanged?.Invoke(RemainingKills); // แจ้ง UI
        
        // เช็คว่าครบโควต้าหรือยัง
        if (kills >= targetKills)
        {
            stageCleared = true;
            OnAllWolvesDefeated?.Invoke(); // เย้! ยิงครบแล้ว ผ่านด่าน
        }
    }

    // หมาป่ารอดถึงพื้นหรือหน้าผา แจ้งเตือนเพื่อนๆ หน่อย
    public void NotifyEscapedToGround()
    {
        if (suppressNotifications) return;
        OnWolfEscapedToGround?.Invoke();
    }

    // รีเซ็ตจำนวนคิล (เผื่อตายเริ่มใหม่)
    public void ResetCount() => BeginStage(Mathf.Max(1, targetKills));

    // โดนลงโทษ! หมาป่าเพิ่มโควต้า ต้องยิงเหนื่อยขึ้น
    public void AddTargetKills(int amount)
    {
        targetKills += amount;
        OnKillCountChanged?.Invoke(RemainingKills); // แจ้ง UI ให้สยองเล่น
    }
}

/// <summary>
/// ตัวแจ้งเตือนตอนหมาป่าตายจริงๆ เท่านั้น
/// </summary>
public class WolfDeathNotifier : MonoBehaviour
{
    private WolfTracker tracker;
    public bool countsAsKill = true; // บ่งบอกว่าตายแบบนับแต้มไหม
    private bool reported; // แจ้งไปแล้วหรือยัง

    public void Initialize(WolfTracker wolfTracker) => tracker = wolfTracker;

    // ถ้ามันหนีรอดไปได้ ก็ไม่นับเป็นคิลแล้วนะเฟ้ย
    public void MarkEscaped()
    {
        countsAsKill = false;
    }

    // พอ GameObject นี้ถูกทำลาย (ตาย หรือ ถูกลบ)
    private void OnDestroy()
    {
        // ถ้าเคยแจ้งไปแล้ว หรือไม่ต้องนับแต้ม ก็ข้ามไป
        if (reported || !countsAsKill) return;
        reported = true;
        tracker?.NotifyKilled(); // ไปบอก Tracker ว่าตัวนี้ตายจริง!
    }
}

