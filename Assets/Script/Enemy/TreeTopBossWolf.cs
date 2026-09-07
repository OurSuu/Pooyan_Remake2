using UnityEngine;

/// <summary>
/// บอสหมาป่าบนยอดไม้ (ด่าน 3+) - เดินไปมาแล้วปาผลไม้ลงมาป่วน
/// </summary>
public class TreeTopBossWolf : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 2f; // ความเร็วเดิน
    [SerializeField] private float walkMinX = -3f; // ขอบเขตเดินซ้ายสุด
    [SerializeField] private float walkMaxX = 3f; // ขอบเขตเดินขวาสุด
    [SerializeField] private float fruitThrowInterval = 2.5f; // โยนผลไม้ทุกๆ กี่วิ
    [SerializeField] private GameObject fruitPrefab; // พรีแฟบผลไม้
    [SerializeField] private float topY = 4f; // ความสูงบนต้นไม้

    private int walkDirection = 1; // 1 เดินขวา, -1 เดินซ้าย
    private float fruitTimer;

    private void Start()
    {
        transform.position = new Vector3(walkMinX, topY, 0f); // เกิดมาก็ชิดซ้ายก่อนเลย
        fruitTimer = fruitThrowInterval;
    }

    private void Update()
    {
        // เกมหยุดอยู่ก็อย่าเพิ่งทำอะไร (เช่น ตายหรือพอสเกม)
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        UpdateWalk(); // เดินป้วนเปี้ยน
        UpdateFruitThrow(); // เช็คเวลาโยนผลไม้
    }

    // ระบบเดินของบอส เดินชนขอบแล้วกลับตัว
    private void UpdateWalk()
    {
        transform.position += Vector3.right * (walkDirection * walkSpeed * Time.deltaTime);

        if (transform.position.x >= walkMaxX)
            walkDirection = -1; // สุดขอบขวา หันซ้าย
        else if (transform.position.x <= walkMinX)
            walkDirection = 1; // สุดขอบซ้าย หันขวา
    }

    // ปาผลไม้ลงมาแกล้งผู้เล่น
    private void UpdateFruitThrow()
    {
        fruitTimer -= Time.deltaTime;
        if (fruitTimer > 0f || fruitPrefab == null) return;

        fruitTimer = fruitThrowInterval; // รีเซ็ตเวลาปา
        var fruit = Instantiate(fruitPrefab, transform.position, Quaternion.identity); // เสกผลไม้
        fruit.GetComponent<WolfProjectile>()?.LaunchDown(WolfProjectileType.Fruit); // ปล่อยร่วงลงมาเลย
    }

    // เปิดปิดบอสตัวนี้ 
    public void Activate(bool active)
    {
        gameObject.SetActive(active);
    }
}
