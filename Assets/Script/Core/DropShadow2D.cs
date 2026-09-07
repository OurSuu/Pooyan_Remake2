using UnityEngine;

/// <summary>
/// อันนี้ตัวทำเงาให้พวกสไปรต์ (Sprite) 2D ง่ายๆ ด้วยการสร้างเกมอ็อบเจ็กต์โคลนตัวเองขึ้นมา แล้วย้อมสีดำซ้อนไว้ข้างหลัง
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DropShadow2D : MonoBehaviour
{
    [Header("Shadow Settings")]
    public Vector2 shadowOffset = new Vector2(0.15f, -0.15f); // ระยะห่างเงากับตัวจริง
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f); // สีเงา มืดๆ โปร่งใสหน่อยๆ
    public int sortingOrderOffset = -1; // เอาไปซ้อนไว้เลเยอร์ใต้ตัวจริง (ลดไป 1)

    private SpriteRenderer parentRenderer; // ตัวแม่
    private SpriteRenderer shadowRenderer; // ตัวเงา

    void Start()
    {
        parentRenderer = GetComponent<SpriteRenderer>();

        // แอบเสก GameObject ขึ้นมาตั้งชื่อว่า DropShadow
        GameObject shadowGO = new GameObject("DropShadow");
        shadowGO.transform.SetParent(transform); // จับยัดเป็นลูกของตัวนี้ซะ
        shadowGO.transform.localPosition = shadowOffset; // ขยับนิดนึงให้ดูเป็นเงา
        shadowGO.transform.localRotation = Quaternion.identity;
        shadowGO.transform.localScale = Vector3.one;

        // ก็อปค่าจากตัวแม่มาใส่ตัวเงาให้หมด
        shadowRenderer = shadowGO.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = parentRenderer.sprite;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerID = parentRenderer.sortingLayerID;
        shadowRenderer.sortingLayerName = parentRenderer.sortingLayerName;
        shadowRenderer.sortingOrder = parentRenderer.sortingOrder + sortingOrderOffset;
    }

    void LateUpdate()
    {
        if (shadowRenderer == null || parentRenderer == null) return;
        
        // ก๊อปเฟรมอนิเมชันให้ตรงกัน ตัวแม่เปลี่ยนรูปไหน เงาก็ต้องเปลี่ยนตามดิ
        if (shadowRenderer.sprite != parentRenderer.sprite)
        {
            shadowRenderer.sprite = parentRenderer.sprite;
        }
        
        // อย่าลืมเช็คเผื่อตัวแม่หันซ้ายหันขวานะ
        shadowRenderer.flipX = parentRenderer.flipX;
        shadowRenderer.flipY = parentRenderer.flipY;
    }
}
