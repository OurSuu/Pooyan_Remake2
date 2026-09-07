using UnityEngine;

// สคริปต์วาดเชือกกระเช้า และคุมอนิเมชันลูกหมูที่กำลังดึงเชือก
public class ElevatorRope : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private LineRenderer lineRenderer; // ตัววาดเส้นเชือก
    [SerializeField] private Transform pigletHand; // ตำแหน่งมือลูกหมู (จุดปลายเชือก)
    [SerializeField] private float pulleyY = 4.5f; // ความสูงของรอกสลิง
    [SerializeField] private float gondolaOffset = 0.5f; // ออฟเซ็ตจุดผูกเชือกกับกระเช้า
    [SerializeField] private float ropeWidth = 0.05f; // ความหนาของเชือก
    [SerializeField] private float scrollSpeed = 0.5f; // ความเร็วขยับ Texture เชือก

    [Header("Piglet Animators (Optional)")]
    [SerializeField] private Animator piglet1; // อนิเมเตอร์ลูกหมูตัวแรก
    [SerializeField] private Animator piglet2; // อนิเมเตอร์ลูกหมูตัวที่สอง
    [SerializeField] private string pullAnimParam = "PullSpeed"; // (ไม่ได้ใช้แล้วมั้ง เลิกใช้พารามิเตอร์นี้ไปแล้ว)

    private Vector3 lastPosition;
    private Material ropeMaterial;
    private float currentTextureOffset = 0f;

    private void Start()
    {
        lastPosition = transform.position;
        if (lineRenderer != null)
        {
            ropeMaterial = lineRenderer.material; // ดึง Material มาไว้ขยับ Texture
        }
        UpdateRopeVisual(); // อัปเดตเชือกตั้งแต่เฟรมแรกเลย
    }

    private void LateUpdate()
    {
        // เช็คว่าถ้าแม่หมูตาย ก็ซ่อนเชือกไปซะ
        var player = GetComponent<PlayerController>();
        if (player != null && player.State == PlayerState.Dead)
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            return;
        }
        else if (lineRenderer != null && !lineRenderer.enabled)
        {
            lineRenderer.enabled = true; // แม่หมูเกิดใหม่แล้ว เปิดเชือกกลับมา
        }

        UpdateRopeVisual();
        UpdatePigletAnimations();
        lastPosition = transform.position; // จำตำแหน่งไว้เทียบในเฟรมถัดไป
    }

    private void UpdateRopeVisual()
    {
        if (lineRenderer == null) return;

        // จุดที่ 0: ผูกกับกระเช้า (แม่หมู)
        Vector3 point0 = new Vector3(transform.position.x, transform.position.y + gondolaOffset, 0f);
        
        // จุดที่ 1: พาดผ่านรอกด้านบน
        Vector3 point1 = new Vector3(transform.position.x, pulleyY, 0f);

        // จุดที่ 2: ดึงไปที่มือลูกหมู
        Vector3 point2 = pigletHand != null ? pigletHand.position : new Vector3(transform.position.x + 3f, pulleyY, 0f);

        // เซ็ตให้ LineRenderer วาด 3 จุดเป็นรูปตัว L คว่ำ
        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, point0);
        lineRenderer.SetPosition(1, point1);
        lineRenderer.SetPosition(2, point2);
        
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;

        // เลื่อน Texture เชือกให้ดูเหมือนว่ากำลังถูกดึงขึ้นลงจริงๆ
        if (ropeMaterial != null)
        {
            float deltaY = transform.position.y - lastPosition.y;
            currentTextureOffset -= deltaY * scrollSpeed;
            ropeMaterial.mainTextureOffset = new Vector2(currentTextureOffset, 0);
        }
    }

    [SerializeField] private string idleStateName = "Piglet_Idle";
    [SerializeField] private string pullStateName = "Piglet_RobePull";
    private bool isPulling = false;

    // อัปเดตอนิเมชันลูกหมูเวลาดึงเชือก
    private void UpdatePigletAnimations()
    {
        float deltaY = transform.position.y - lastPosition.y;
        float speed = deltaY / Time.deltaTime;
        
        // ถ้ากระเช้าขยับอยู่ ถือว่ากำลังดึงเชือก
        bool currentlyMoving = Mathf.Abs(speed) > 0.05f;
        
        // สลับ State อนิเมชัน
        if (currentlyMoving != isPulling)
        {
            isPulling = currentlyMoving;
            string stateToPlay = isPulling ? pullStateName : idleStateName;
            
            if (piglet1 != null) piglet1.Play(stateToPlay);
            if (piglet2 != null) piglet2.Play(stateToPlay);
        }

        // ปรับความเร็วการดึงเชือกให้สัมพันธ์กับความเร็วกระเช้า (คูณ 0.2 ให้ดูธรรมชาติ ไม่ล่กไป)
        float animSpeed = currentlyMoving ? Mathf.Abs(speed) * 0.2f : 1f;
        if (animSpeed < 0.1f && currentlyMoving) animSpeed = 0.1f; // กันไม่ให้อนิเมชันหยุดนิ่ง
        
        if (piglet1 != null) piglet1.speed = animSpeed;
        if (piglet2 != null) piglet2.speed = animSpeed;
    }

    // คลิกขวาที่คอมโพเนนต์เพื่อสร้าง LineRenderer รูปตัว L อัตโนมัติได้เลย สะดวกสุดๆ
    [ContextMenu("Setup L-Shape Rope (LineRenderer)")]
    private void SetupLineRenderer()
    {
        if (lineRenderer != null) return;

        GameObject ropeObj = new GameObject("L_RopeVisual");
        ropeObj.transform.SetParent(transform.parent != null ? transform.parent : null);
        
        lineRenderer = ropeObj.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = -5; // ให้อยู่หลังสุดๆ
        
        // เซ็ต Material ให้รองรับการเลื่อน Texture
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
        lineRenderer.startColor = new Color(0.85f, 0.65f, 0.45f);
        lineRenderer.endColor = new Color(0.85f, 0.65f, 0.45f);
    }
}
