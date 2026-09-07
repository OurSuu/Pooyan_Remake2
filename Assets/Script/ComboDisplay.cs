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

        // เสกตัวหนังสือขึ้นมา
        var popup = Instantiate(popupPrefab, canvas != null ? canvas.transform : transform);
        var text = popup.GetComponent<TextMeshProUGUI>() ?? popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = $"+{points}"; // ใส่แต้มเข้าไป

        StartCoroutine(AnimatePopup(popup, worldPosition));
    }

    private IEnumerator AnimatePopup(GameObject popup, Vector3 worldPosition)
    {
        var rect = popup.GetComponent<RectTransform>();
        var canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = popup.AddComponent<CanvasGroup>();

        // แปลงพิกัดโลกมาลงจอภาพ (ใช้กับ Canvas Overlay/Camera ได้พอดี)
        Vector3 screenPos = worldPosition;
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                screenPos = mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : worldPosition;
            }
        }

        if (rect != null)
            rect.position = screenPos;

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            // ให้มันลอยขึ้นเรื่อยๆ
            if (rect != null)
                rect.position += Vector3.up * (floatSpeed * 50f * Time.deltaTime);
            // เฟดให้ค่อยๆ หายไปอย่างเท่ๆ
            canvasGroup.alpha = 1f - (elapsed / lifetime);
            yield return null;
        }

        // จบงานก็เผาทิ้งซะ
        Destroy(popup);
    }
}
