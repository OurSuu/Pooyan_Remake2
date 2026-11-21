using UnityEngine;

// สคริปต์วาดเชือกกระเช้า และคุมอนิเมชันลูกหมูที่กำลังดึงเชือก
public class ElevatorRope : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private LineRenderer lineRenderer; // ตัววาดเส้นเชือก
    [SerializeField] private Transform pigletHand; // ตำแหน่งมือลูกหมู (จุดปลายเชือก)
    [SerializeField] private float pulleyY = 4.5f; // ความสูงของรอกสลิง
    [SerializeField] private float gondolaOffset = 0.5f; // ออฟเซ็ตจุดผูกเชือกกับกระเช้า
    [SerializeField] private float ropeWidth = 0.12f; // ความหนาของเชือก
    [SerializeField] private float scrollSpeed = 0.5f; // ความเร็วขยับ Texture เชือก
    [SerializeField] private Material customRopeMaterial; // Material ลายเชือกถัก (RopeMaterial)

    [Header("Corner Curve Settings")]
    [Tooltip("รัศมีความโค้งมนตรงมุมรอก (0 = หักมุมฉาก, 0.2 - 0.35 = โค้งมนนุ่มนวล)")]
    [SerializeField] private float cornerRadius = 0.25f;
    [Tooltip("จำนวนจุดรอยต่อความโค้ง (ยิ่งเยอะยิ่งเนียน แนะนำ 5 - 8 จุด)")]
    [Range(2, 12)]
    [SerializeField] private int cornerSegments = 6;

    [Header("Rope Motion FX")]
    [Tooltip("Particle Effect ตรงรอกด้านบน (ฝุ่น/เศษใยเชือกตอนเลื่อนขึ้นลง)")]
    [SerializeField] private ParticleSystem pulleyDustFX;

    [Header("Piglet Animators (Optional)")]
    [SerializeField] private Animator piglet1; // อนิเมเตอร์ลูกหมูตัวแรก
    [SerializeField] private Animator piglet2; // อนิเมเตอร์ลูกหมูตัวที่สอง
    [SerializeField] private string pullAnimParam = "PullSpeed"; // (ไม่ได้ใช้แล้วมั้ง เลิกใช้พารามิเตอร์นี้ไปแล้ว)

    [Header("Death Settings")]
    [SerializeField] private bool keepRopeOnDeath = true; // ให้เชือกยังคงอยู่ดึงกระเช้าไว้ตอนแม่หมูร่วงตก

    private Vector3 lastPosition;
    private Material ropeMaterial;
    private float currentTextureOffset = 0f;
    private bool isRopeSuppressed = false;
    private PlayerController cachedPlayer;
    private bool wasPlayerDead = false;

    public LineRenderer LineRenderer => lineRenderer;

    /// <summary>
    /// สั่งซ่อนหรือแสดงเชือก (เช่น ตอนเล่นคัทซีนก่อนเริ่มด่าน 1 และด่าน 2)
    /// </summary>
    public void SetRopeVisible(bool visible)
    {
        isRopeSuppressed = !visible;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
            lineRenderer.gameObject.SetActive(visible);
        }
    }

    private void Start()
    {
        cachedPlayer = GetComponent<PlayerController>();
        lastPosition = transform.position;

        if (ropeWidth <= 0.06f) ropeWidth = 0.12f; // ปรับความหนาให้เห็นลวดลายเกลียวเชือกสวยงาม

        if (lineRenderer != null)
        {
            if (customRopeMaterial == null)
            {
                customRopeMaterial = Resources.Load<Material>("Material/RopeMaterial");
            }
            if (customRopeMaterial != null)
            {
                lineRenderer.material = customRopeMaterial;
            }
            ropeMaterial = lineRenderer.material; // ดึง Material มาไว้ขยับ Texture

            // ตั้งค่า LineRenderer ให้เป็น Tile และสีขาว 100% ไม่ย้อมดำทับ Texture
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
            lineRenderer.numCornerVertices = 2; // เพิ่มความเนียนของรอยต่อมุม
            lineRenderer.numCapVertices = 2;
        }

        // โหลด Particle Effect ฝุ่นรอกหมุนอัตโนมัติหากยังไม่ได้ใส่
        if (pulleyDustFX == null)
        {
            var fxPrefab = Resources.Load<GameObject>("FX/RopePulleyFX");
            if (fxPrefab != null)
            {
                var fxObj = Instantiate(fxPrefab, new Vector3(transform.position.x, pulleyY, 0f), Quaternion.identity);
                pulleyDustFX = fxObj.GetComponent<ParticleSystem>();
            }
        }

        if (!isRopeSuppressed)
        {
            UpdateRopeVisual(); // อัปเดตเชือกตั้งแต่เฟรมแรกเลย
        }
    }

    private void LateUpdate()
    {
        if (isRopeSuppressed)
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        // เช็คสถานะแม่หมู
        if (cachedPlayer == null) cachedPlayer = GetComponent<PlayerController>();
        if (cachedPlayer != null && cachedPlayer.State == PlayerState.Dead)
        {
            AudioManager.Instance?.SetRopePullSound(false);
            if (!keepRopeOnDeath)
            {
                if (lineRenderer != null) lineRenderer.enabled = false;
                return;
            }

            // ถ้าเปิด keepRopeOnDeath ไว้ เชือกยังคงโชว์เพื่อห้อยกระเช้าไว้ และลูกหมูเปลี่ยนเป็นยืนพัก (Idle)
            if (lineRenderer != null && !lineRenderer.enabled)
            {
                lineRenderer.enabled = true;
            }
            if (!wasPlayerDead)
            {
                wasPlayerDead = true;
                isPulling = false;
                if (piglet1 != null) piglet1.Play(idleStateName);
                if (piglet2 != null) piglet2.Play(idleStateName);
            }
            UpdateRopeVisual();
            return;
        }
        wasPlayerDead = false;
        if (lineRenderer != null && !lineRenderer.enabled)
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

        // จุดปลาย: ดึงไปที่มือลูกหมู
        Vector3 point2 = pigletHand != null ? pigletHand.position : new Vector3(transform.position.x + 3f, pulleyY, 0f);

        float dx = point2.x - transform.position.x;
        float dirX = dx >= 0f ? 1f : -1f;

        // ป้องกันไม่ให้รัศมีเกินระยะระหว่างจุด
        float availableVertical = Mathf.Max(0.01f, pulleyY - point0.y);
        float availableHorizontal = Mathf.Max(0.01f, Mathf.Abs(dx));
        float maxR = Mathf.Min(availableVertical * 0.45f, availableHorizontal * 0.45f);
        float r = Mathf.Clamp(cornerRadius, 0f, Mathf.Max(0f, maxR));

        if (r <= 0.01f || cornerSegments < 2)
        {
            // แบบเดิม: หักมุมฉาก 90 องศา (3 จุด)
            if (lineRenderer.positionCount != 3) lineRenderer.positionCount = 3;
            lineRenderer.SetPosition(0, point0);
            lineRenderer.SetPosition(1, new Vector3(transform.position.x, pulleyY, 0f));
            lineRenderer.SetPosition(2, point2);
        }
        else
        {
            // แบบใหม่: โค้งมนรอบรอกสวยงาม (Quarter Circle Arc)
            int totalCount = 2 + cornerSegments;
            if (lineRenderer.positionCount != totalCount)
            {
                lineRenderer.positionCount = totalCount;
            }

            // จุดเริ่มต้น: ผูกกับกระเช้า
            lineRenderer.SetPosition(0, point0);

            // คำนวณจุดกึ่งกลางของวงกลมรอก
            Vector2 center = new Vector2(transform.position.x + dirX * r, pulleyY - r);
            float startAngle = dirX >= 0f ? Mathf.PI : 0f;
            float endAngle = Mathf.PI * 0.5f;

            for (int i = 0; i < cornerSegments; i++)
            {
                float t = (float)i / (cornerSegments - 1);
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                Vector3 arcPt = new Vector3(center.x + r * Mathf.Cos(angle), center.y + r * Mathf.Sin(angle), 0f);
                lineRenderer.SetPosition(1 + i, arcPt);
            }

            // จุดปลาย: มือลูกหมู
            lineRenderer.SetPosition(totalCount - 1, point2);
        }

        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;

        // เลื่อน Texture เชือกให้ดูเหมือนว่ากำลังถูกดึงขึ้นลงจริงๆ
        if (ropeMaterial != null)
        {
            float deltaY = transform.position.y - lastPosition.y;
            if (Mathf.Abs(deltaY) > 0.0001f)
            {
                currentTextureOffset -= deltaY * scrollSpeed;
                ropeMaterial.mainTextureOffset = new Vector2(currentTextureOffset, 0);
            }
        }
    }

    [SerializeField] private string idleStateName = "Piglet_Idle";
    [SerializeField] private string pullStateName = "Piglet_RobePull";
    private bool isPulling = false;

    // อัปเดตอนิเมชันลูกหมูเวลาดึงเชือก
    private void UpdatePigletAnimations()
    {
        float deltaY = transform.position.y - lastPosition.y;
        float speed = Time.deltaTime > 0f ? (deltaY / Time.deltaTime) : 0f;
        
        // ถ้ากระเช้าขยับอยู่ ถือว่ากำลังดึงเชือก
        bool currentlyMoving = Mathf.Abs(speed) > 0.05f;
        AudioManager.Instance?.SetRopePullSound(currentlyMoving);
        
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

        // คุม Particle ฝุ่นรอกหมุนตอนเชือกเลื่อนขึ้นลง
        if (pulleyDustFX != null)
        {
            float dustR = Mathf.Max(0.05f, cornerRadius * 0.4f);
            pulleyDustFX.transform.position = new Vector3(transform.position.x + dustR, pulleyY - dustR, 0f);
            var em = pulleyDustFX.emission;
            em.enabled = currentlyMoving;
            if (currentlyMoving)
            {
                if (!pulleyDustFX.isPlaying) pulleyDustFX.Play();
            }
            else if (pulleyDustFX.isPlaying)
            {
                pulleyDustFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private void OnDestroy()
    {
        if (pulleyDustFX != null && pulleyDustFX.gameObject != null)
        {
            Destroy(pulleyDustFX.gameObject);
        }
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
