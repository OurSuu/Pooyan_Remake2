using UnityEngine;

/// <summary>
/// ตัวควบคุมฉาก Portfolio Showcase (ใช้เฉพาะใน Scene โชว์ผลงาน ไม่ได้ใช้ในเกมจริง):
/// 1. ตั้งค่าฟอนต์ภาษาไทย (OS Dynamic Font) ให้ TextMesh ทุกป้ายในฉากอ่านได้คมชัด ไม่เป็นสี่เหลี่ยม
/// 2. คุมกล้องสำหรับพรีเซนต์งานใน Play Mode (WASD / ลูกศร / ลากเมาส์เลื่อน, Scroll ซูม, กดปุ่มมุมจอสลับโซน)
/// </summary>
[ExecuteAlways]
public class PortfolioShowcaseStage : MonoBehaviour
{
    [Header("Scene Title & Layout")]
    public string showcaseTitle = "POOYAN REMAKE — SHOWCASE";
    public string showcaseSubtitle = "";

    [Header("Camera Presets (สำหรับกดสลับมุมมองตอนพรีเซนต์)")]
    public Vector3 overviewPos = new Vector3(0f, -0.4f, -10f);
    public float overviewOrthoSize = 9.2f;

    public Vector3 topRowPos = new Vector3(0f, 3.3f, -10f);
    public float topRowOrthoSize = 5.0f;

    public Vector3 bottomRowPos = new Vector3(0f, -4.3f, -10f);
    public float bottomRowOrthoSize = 4.8f;

    [Header("Camera Navigation")]
    public bool allowCameraNavigation = true;
    public bool showCameraBar = true;
    public float reserveBottomGuiHeight = 0f;
    public float panSpeed = 10f;
    public float zoomSpeed = 4f;
    public float minOrthoSize = 2.5f;
    public float maxOrthoSize = 18f;

    private Camera targetCam;
    private Vector3 targetPos;
    private float targetOrtho;
    private bool isDragging;
    private Vector3 dragOriginWorld;

    private static Font cachedThaiFont;

    public static Font GetThaiDynamicFont()
    {
        if (cachedThaiFont != null) return cachedThaiFont;

        cachedThaiFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Segoe UI", "TH Sarabun New", "Arial" },
            32);

        if (cachedThaiFont == null)
        {
            cachedThaiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (cachedThaiFont != null)
        {
            cachedThaiFont.hideFlags = HideFlags.DontSave;
        }

        return cachedThaiFont;
    }

    public static void ApplyThaiFontToAllLabels(Transform root)
    {
        Font font = GetThaiDynamicFont();
        if (font == null) return;

        TextMesh[] labels = root != null
            ? root.GetComponentsInChildren<TextMesh>(true)
            : FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var tm in labels)
        {
            if (tm == null) continue;
            tm.font = font;
            var mr = tm.GetComponent<MeshRenderer>();
            if (mr != null && font.material != null)
            {
                mr.sharedMaterial = font.material;
            }
        }
    }

    private void OnEnable()
    {
        ApplyThaiFontToAllLabels(null);
    }

    private void Start()
    {
        ApplyThaiFontToAllLabels(null);

        targetCam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (targetCam != null)
        {
            targetPos = overviewPos;
            targetOrtho = overviewOrthoSize;
            targetCam.transform.position = targetPos;
            targetCam.orthographicSize = targetOrtho;
        }
    }

    private void Update()
    {
        if (cachedThaiFont == null)
        {
            ApplyThaiFontToAllLabels(null);
        }

        if (!Application.isPlaying) return;
        if (!allowCameraNavigation) return;
        if (targetCam == null)
        {
            targetCam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (targetCam == null) return;
        }

        // คีย์ลัดสลับมุมกล้องอย่างรวดเร็ว
        if (Input.GetKeyDown(KeyCode.Alpha1)) FocusPreset(overviewPos, overviewOrthoSize);
        if (Input.GetKeyDown(KeyCode.Alpha2)) FocusPreset(topRowPos, topRowOrthoSize);
        if (Input.GetKeyDown(KeyCode.Alpha3)) FocusPreset(bottomRowPos, bottomRowOrthoSize);
        if (Input.GetKeyDown(KeyCode.Space)) TriggerAllDemos();

        // เลื่อนกล้องด้วย WASD / ลูกศร
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            float scale = Mathf.Max(0.5f, targetOrtho / 7f);
            targetPos += new Vector3(h, v, 0f) * (panSpeed * scale * Time.unscaledDeltaTime);
        }

        // ลากเมาส์ซ้ายหรือเมาส์กลางเพื่อแพนกล้อง
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
        {
            // กันชนปุ่ม OnGUI มุมบนซ้าย และแถบควบคุมด้านล่าง (ถ้ามี)
            if (Input.mousePosition.y < Screen.height - 52f && Input.mousePosition.y > reserveBottomGuiHeight)
            {
                isDragging = true;
                dragOriginWorld = targetCam.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentWorld = targetCam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = dragOriginWorld - currentWorld;
            delta.z = 0f;
            targetPos += delta;
            targetCam.transform.position += delta;
            dragOriginWorld = targetCam.ScreenToWorldPoint(Input.mousePosition);
        }

        // ซูมด้วย Scroll Wheel
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetOrtho = Mathf.Clamp(targetOrtho - scroll * zoomSpeed * 0.45f, minOrthoSize, maxOrthoSize);
        }

        targetPos.z = -10f;
        targetCam.transform.position = Vector3.Lerp(targetCam.transform.position, targetPos, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        targetCam.orthographicSize = Mathf.Lerp(targetCam.orthographicSize, targetOrtho, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
    }

    private void FocusPreset(Vector3 pos, float ortho)
    {
        targetPos = pos;
        targetOrtho = ortho;
    }

    private void TriggerAllDemos()
    {
        var demos = FindObjectsByType<ShowcaseStationDemo>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var d in demos)
        {
            if (d != null) d.TriggerNow();
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showCameraBar) return;

        GUIStyle barStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12
        };

        GUILayout.BeginArea(new Rect(12f, 10f, Mathf.Min(Screen.width - 24f, 920f), 38f));
        GUILayout.BeginHorizontal(barStyle);

        if (GUILayout.Button("[1] Overview", btnStyle, GUILayout.Width(105f), GUILayout.Height(26f)))
            FocusPreset(overviewPos, overviewOrthoSize);

        if (GUILayout.Button("[2] Top Row", btnStyle, GUILayout.Width(115f), GUILayout.Height(26f)))
            FocusPreset(topRowPos, topRowOrthoSize);

        if (GUILayout.Button("[3] Bottom Row", btnStyle, GUILayout.Width(125f), GUILayout.Height(26f)))
            FocusPreset(bottomRowPos, bottomRowOrthoSize);

        if (GUILayout.Button("[Space] Replay Demos", btnStyle, GUILayout.Width(145f), GUILayout.Height(26f)))
            TriggerAllDemos();

        GUILayout.Space(8f);
        GUILayout.Label("WASD / Drag = Pan  |  Scroll = Zoom", GUILayout.Height(26f));

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
