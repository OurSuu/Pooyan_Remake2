using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบ Cheat Mode ลับ สำหรับใช้กดผ่านด่านทันทีเวลานำเสนองานอาจารย์:
/// 1. มีปุ่มลอยเล็กๆ โปร่งแสง (Discreet Button) อยู่มุมขวาล่างของจอ ไม่สะดุดตา
/// 2. มีปุ่มลัดคีย์บอร์ด (Hotkey): กด F8 หรือ ~ (Tilde) เพื่อผ่านด่านได้ทันทีโดยไม่ต้องคลิกเมาส์
/// 3. เมื่อกดจบด่าน:
///    - ด่าน 1 (Autumn): เคลียร์ด่านทันที และเปลี่ยนฉากเข้าสู่ด่าน 2
///    - ด่าน 2 (Spring): เคลียร์ด่าน และเล่นคัทซีนจบด่านรวมญาติ (Reunion Cutscene) ให้ครบถ้วนก่อนเข้าสู่ด่านโบนัส!
///    - ด่าน Bonus: จบด่านโบนัสทันทีและไปด่านถัดไป
/// 4. ปิดการแสดงผลอัตโนมัติในหน้า Menu หรือระหว่างเล่นคัทซีน
/// 5. ออกแบบเป็น Standalone ทำงานได้เองอัตโนมัติ ไม่กระทบโค้ดระบบอื่น
/// </summary>
public class CheatModeController : MonoBehaviour
{
    public static CheatModeController Instance { get; private set; }

    [Header("Cheat Mode Settings (ตั้งค่าระบบสูตรโกง)")]
    [Tooltip("เปิด/ปิดระบบ Cheat Mode")]
    public bool enableCheat = true;

    [Tooltip("ปุ่มลัดคีย์บอร์ดหลักสำหรับผ่านด่านทันที")]
    public KeyCode cheatKey = KeyCode.F8;

    [Tooltip("ปุ่มลัดคีย์บอร์ดสำรองสำหรับผ่านด่านทันที (ปุ่ม ~ ข้างเลข 1)")]
    public KeyCode alternateKey = KeyCode.BackQuote;

    [Tooltip("ปุ่มลัดสำหรับซ่อน/แสดงปุ่มบนหน้าจอ")]
    public KeyCode toggleButtonKey = KeyCode.F9;

    [Header("On-Screen Button Visual (ปุ่มเล็กแอบไว้มุมจอ)")]
    [Tooltip("แสดงปุ่มเล็กๆ บนหน้าจอหรือไม่")]
    public bool showOnScreenButton = true;

    [Tooltip("ความโปร่งแสงของปุ่มปกติ (0.18 = จางมาก เนียนไปกับฉาก ไม่สะดุดตา)")]
    [Range(0.05f, 1f)]
    public float buttonOpacity = 0.18f;

    [Tooltip("ข้อความสั้นๆ บนปุ่ม")]
    public string buttonText = "»";

    [Tooltip("ขนาดของปุ่ม (กว้าง x สูง)")]
    public Vector2 buttonSize = new Vector2(30f, 18f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            var go = new GameObject("[CheatModeController]");
            go.AddComponent<CheatModeController>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!enableCheat) return;

        // กดปุ่มลัดคีย์บอร์ด F8 หรือ ~ เพื่อเคลียร์ด่านทันที
        if (Input.GetKeyDown(cheatKey) || Input.GetKeyDown(alternateKey))
        {
            TriggerInstantClear();
        }

        // กด F9 เพื่อเปิด/ปิดการมองเห็นปุ่มบนหน้าจอ
        if (Input.GetKeyDown(toggleButtonKey))
        {
            showOnScreenButton = !showOnScreenButton;
        }
    }

    private void OnGUI()
    {
        if (!enableCheat || !showOnScreenButton) return;
        if (GameManager.Instance == null) return;

        // แสดงเฉพาะช่วงที่กำลังเล่นเกมอยู่เท่านั้น (ไม่แสดงในหน้าเมนู หรือตอนกำลังเล่นคัทซีนอยู่)
        bool isGameplayActive = GameManager.Instance.IsPlaying || 
                               GameManager.Instance.CurrentState == GameState.BonusStage || 
                               GameManager.Instance.IsBonusSceneActive;

        if (!isGameplayActive) return;

        // วางปุ่มแอบไว้มุมขวาล่างของจอ
        Rect buttonRect = new Rect(
            Screen.width - buttonSize.x - 6f,
            Screen.height - buttonSize.y - 6f,
            buttonSize.x,
            buttonSize.y
        );

        bool isHovered = buttonRect.Contains(Event.current.mousePosition);

        Color prevColor = GUI.color;
        // เมื่อเอาเมาส์ไปชี้ ปุ่มจะชัดขึ้น (0.85) เพื่อให้เล็งกดง่าย แต่เวลาปกติจะจางมาก (0.18) ไม่ให้อาจารย์สังเกตเห็น
        GUI.color = new Color(1f, 1f, 1f, isHovered ? 0.85f : buttonOpacity);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 0)
        };

        if (GUI.Button(buttonRect, buttonText, btnStyle))
        {
            TriggerInstantClear();
        }

        GUI.color = prevColor;
    }

    /// <summary>
    /// สั่งจบด่านและผ่านด่านนั้นทันที
    /// </summary>
    public static void TriggerInstantClear()
    {
        if (GameManager.Instance == null) return;

        // ถ้าเกมจบแล้วหรือกำลังเคลียร์ด่านอยู่แล้ว ไม่ต้องสั่งซ้ำ
        if (GameManager.Instance.CurrentState == GameState.StageClear ||
            GameManager.Instance.CurrentState == GameState.GameOver)
        {
            return;
        }

        // กรณีอยู่ใน Bonus Stage ให้สั่งจบโบนัสและไปด่านถัดไปทันที
        if (GameManager.Instance.CurrentState == GameState.BonusStage || GameManager.Instance.IsBonusSceneActive)
        {
            GameManager.Instance.CompleteBonusStage();
            return;
        }

        // 1. หยุดการเกิดของหมาป่าทั้งหมด
        var spawner = FindFirstObjectByType<WolfSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            spawner.StopAllSpawning();
        }

        // 2. เคลียร์ตัวหมาป่า, ลูกกระสุนหิน, ลูกธนู และเนื้อทั้งหมดในฉาก
        var flow = FindFirstObjectByType<StageFlowController>(FindObjectsInactive.Include);
        if (flow != null)
        {
            flow.ClearAllEntities();
        }

        // 3. ปิดบอสบนต้นไม้
        var boss = FindFirstObjectByType<TreeTopBossWolf>(FindObjectsInactive.Include);
        if (boss != null)
        {
            boss.Activate(false);
        }

        // 4. รีเซ็ตบันไดและหน้าผา
        if (LadderSystem.Instance != null)
        {
            LadderSystem.Instance.ResetLadder();
        }

        if (BoulderSystem.Instance != null)
        {
            BoulderSystem.Instance.ResetCliff(0);
        }

        // 5. สั่ง GameManager ให้เคลียร์ด่าน
        //    - ถ้าเป็นด่าน 2 ระบบ UIManager.ShowStageClear จะสั่งเล่นคัทซีนรวมญาติก่อนเข้าด่านโบนัสให้อัตโนมัติ!
        GameManager.Instance.TriggerStageClear();
    }
}
