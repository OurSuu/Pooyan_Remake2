using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating combo score popup when meat hits wolves.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class ComboDisplay : MonoBehaviour
{
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private Canvas canvas;

    private Camera mainCamera;

    private void Awake()
    {
        // Ensure the Canvas reference is set
        if (canvas == null) canvas = GetComponent<Canvas>();

        // Enforce correct Canvas render mode
        if (canvas != null)
        {
            // Only allow Screen Space - Overlay or Screen Space - Camera
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                Debug.LogWarning(
                    $"[ComboDisplay] Canvas RenderMode must be 'Screen Space - Overlay' or 'Screen Space - Camera'." +
                    $" It is currently set to '{canvas.renderMode}'. Changing to ScreenSpaceOverlay.");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreFloating += ShowCombo;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreFloating -= ShowCombo;
    }

    private void ShowCombo(int points, Vector3 worldPosition)
    {
        if (popupPrefab == null) return;

        var popup = Instantiate(popupPrefab, canvas != null ? canvas.transform : transform);
        var text = popup.GetComponent<TextMeshProUGUI>() ?? popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = $"+{points}";

        StartCoroutine(AnimatePopup(popup, worldPosition));
    }

    private IEnumerator AnimatePopup(GameObject popup, Vector3 worldPosition)
    {
        var rect = popup.GetComponent<RectTransform>();
        var canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = popup.AddComponent<CanvasGroup>();

        // Use WorldToScreenPoint ONY when Canvas is Screen Space - Overlay or Screen Space - Camera
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
            if (rect != null)
                rect.position += Vector3.up * (floatSpeed * 50f * Time.deltaTime);
            canvasGroup.alpha = 1f - (elapsed / lifetime);
            yield return null;
        }

        Destroy(popup);
    }
}
