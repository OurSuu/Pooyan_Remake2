using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// อันนี้ทำตัวเลขคอมโบเด้งๆ นะเพื่อน เวลาโยนเนื้อไปโดนหมาป่าหลายๆ ตัว คะแนนมันจะลอยขึ้นมาโชว์!
/// </summary>
[RequireComponent(typeof(Canvas))]
public class ComboDisplay : MonoBehaviour
{
    [SerializeField] private GameObject popupPrefab; // ตัวหนังสือที่มันจะเด้งลอยๆ
    [SerializeField] private float floatSpeed = 1.5f; // ความเร็วตอนลอยขึ้น
    [SerializeField] private float lifetime = 1f; // อยู่ได้กี่วิก่อนหาย
    [SerializeField] private Canvas canvas; // ตัว Canvas หลัก

    private Camera mainCamera;

    private void Awake()
    {
        // ลาก Canvas มาใส่ให้หน่อยถ้าลืม
        if (canvas == null) canvas = GetComponent<Canvas>();

        if (canvas != null)
        {
            // บังคับให้ใช้ Overlay หรือ Camera เท่านั้น ไม่งั้นบั๊กแสดงผล
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                Debug.LogWarning(
                    $"[ComboDisplay] เห้ย Canvas RenderMode ต้องเป็น 'Screen Space - Overlay' หรือ 'Screen Space - Camera' น้า" +
                    $" ตอนนี้มันเป็น '{canvas.renderMode}' ขอจับเปลี่ยนเป็น ScreenSpaceOverlay เลยละกัน");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // โยงกับ ScoreManager รอจับตัวเลขเด้ง
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreFloating += ShowCombo;
    }

    private void OnDestroy()
    {
        // เก็บกวาด event ด้วยนะเพื่อน
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreFloating -= ShowCombo;
    }

    private void ShowCombo(int points, Vector3 worldPosition)
    {
        if (popupPrefab == null) return;
        if (mainCamera == null) mainCamera = Camera.main;

        // เสกตัวหนังสือขึ้นมา
        var popup = Instantiate(popupPrefab, canvas != null ? canvas.transform : transform);
        var text = popup.GetComponent<TextMeshProUGUI>() ?? popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"+{points}"; // ใส่แต้มเข้าไป
            text.color = GetScoreColor(points);
        }

        StartCoroutine(AnimatePopup(popup, worldPosition, points));
    }

    private Color GetScoreColor(int points)
    {
        if (points >= 4000) return new Color(0.35f, 1f, 0.95f, 1f); // สีฟ้านีออนสว่าง (Easter Egg / Nakamoto 4000-8000+)
        if (points >= 1600) return new Color(1f, 0.45f, 0.95f, 1f); // สีชมพูม่วงนีออน (คอมโบเนื้อขั้นสูง 1600+)
        if (points >= 400) return new Color(1f, 0.65f, 0.15f, 1f);  // สีส้มทอง (คอมโบเนื้อ 400 / 800)
        return new Color(1f, 0.96f, 0.35f, 1f);                     // สีเหลืองสว่างสไตล์ Arcade (100 / 200)
    }

    private IEnumerator AnimatePopup(GameObject popup, Vector3 worldPosition, int points)
    {
        var rect = popup.GetComponent<RectTransform>();
        var canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = popup.AddComponent<CanvasGroup>();

        if (mainCamera == null) mainCamera = Camera.main;

        // แปลงพิกัดโลกมาลงจอภาพ (ใช้กับ Canvas Overlay/Camera ได้พอดี)
        Vector3 screenPos = worldPosition;
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenPos = mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : worldPosition;
            }
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                Camera cam = canvas.worldCamera != null ? canvas.worldCamera : mainCamera;
                if (cam != null)
                {
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        canvas.GetComponent<RectTransform>(),
                        cam.WorldToScreenPoint(worldPosition),
                        cam,
                        out screenPos);
                }
            }
        }

        if (rect != null)
        {
            rect.position = screenPos;
        }

        float baseScale = points >= 4000 ? 1.4f : (points >= 1600 ? 1.25f : (points >= 400 ? 1.12f : 1f));
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            // ให้มันลอยขึ้นเรื่อยๆ
            if (rect != null)
            {
                rect.position += Vector3.up * (floatSpeed * 55f * Time.deltaTime);

                // เด้งขยาย (Pop-in) ในช่วง 20% แรกของอนิเมชัน
                float popMultiplier = t < 0.2f
                    ? Mathf.Lerp(0.6f, 1.2f, t / 0.2f)
                    : Mathf.Lerp(1.2f, 1.0f, (t - 0.2f) / 0.8f);
                rect.localScale = Vector3.one * (baseScale * popMultiplier);
            }

            // เฟดให้ค่อยๆ หายไปในช่วงครึ่งหลัง
            canvasGroup.alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
            yield return null;
        }

        // จบงานก็เผาทิ้งซะ
        Destroy(popup);
    }
}
