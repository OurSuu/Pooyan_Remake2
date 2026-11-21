using UnityEngine;

/// <summary>
/// สคริปต์นี้เอาไว้โชว์ไฟกะพริบหรือสัญลักษณ์บอกว่า เห้ย เนื้อพร้อมให้เก็บตรงรอกข้างบนแล้วนะ!
/// </summary>
public class MeatPickupIndicator : MonoBehaviour
{
    [SerializeField] private GameObject indicatorObject; // ตัวกราฟิกที่จะโชว์
    [SerializeField] private float pulseSpeed = 3f; // ความเร็วตอนเต้นตุ้บๆ
    [SerializeField] private float pulseScale = 0.15f; // ให้มันใหญ่ขึ้นแค่ไหนตอนตุ้บๆ

    private bool isAvailable; 
    private Vector3 baseScale; 

    private void Awake()
    {
        // จำไซส์เดิมไว้ก่อน
        baseScale = indicatorObject != null ? indicatorObject.transform.localScale : Vector3.one;
        SetAvailable(false); // เริ่มมายังไม่มีเนื้อ ปิดไว้ก่อน
    }

    private void Update()
    {
        if (!isAvailable || indicatorObject == null) return;

        // ทำอนิเมชันหายใจด้วยคณิตศาสตร์ล้วนๆ ไร้ซึ่ง Animator!
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        indicatorObject.transform.localScale = baseScale * pulse;
    }

    public void SetAvailable(bool available)
    {
        // เปิดปิดสัญลักษณ์ตามสั่ง
        isAvailable = available;
        if (indicatorObject != null)
            indicatorObject.SetActive(available);
    }
}
