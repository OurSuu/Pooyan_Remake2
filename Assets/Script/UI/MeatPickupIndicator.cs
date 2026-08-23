using UnityEngine;

/// <summary>
/// Visual indicator when meat is available at the top of the rail.
/// </summary>
public class MeatPickupIndicator : MonoBehaviour
{
    [SerializeField] private GameObject indicatorObject;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseScale = 0.15f;

    private bool isAvailable;
    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = indicatorObject != null ? indicatorObject.transform.localScale : Vector3.one;
        SetAvailable(false);
    }

    private void Update()
    {
        if (!isAvailable || indicatorObject == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        indicatorObject.transform.localScale = baseScale * pulse;
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
        if (indicatorObject != null)
            indicatorObject.SetActive(available);
    }
}
