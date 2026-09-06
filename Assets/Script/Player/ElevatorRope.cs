using UnityEngine;

public class ElevatorRope : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform pigletHand; // à¸ˆà¸¸à¸”à¸¡à¸·à¸­à¸¥à¸¹à¸à¸«à¸¡à¸¹ (à¸›à¸¥à¸²à¸¢à¹€à¸Šà¸·à¸­à¸à¸”à¹‰à¸²à¸™à¸‚à¸§à¸²)
    [SerializeField] private float pulleyY = 4.5f; // à¸„à¸§à¸²à¸¡à¸ªà¸¹à¸‡à¸‚à¸­à¸‡à¸£à¸­à¸ (à¸ˆà¸¸à¸”à¸«à¸±à¸à¸¨à¸­à¸)
    [SerializeField] private float gondolaOffset = 0.5f; // à¸­à¸­à¸Ÿà¹€à¸‹à¹‡à¸•à¸ˆà¸²à¸à¹à¸¡à¹ˆà¸«à¸¡à¸¹
    [SerializeField] private float ropeWidth = 0.05f;
    [SerializeField] private float scrollSpeed = 0.5f; // à¸„à¸§à¸²à¸¡à¹€à¸£à¹‡à¸§à¸à¸²à¸£à¹€à¸¥à¸·à¹ˆà¸­à¸™à¸¥à¸²à¸¢à¹€à¸Šà¸·à¸­à¸

    [Header("Piglet Animators (Optional)")]
    [SerializeField] private Animator piglet1;
    [SerializeField] private Animator piglet2;
    [SerializeField] private string pullAnimParam = "PullSpeed";

    private Vector3 lastPosition;
    private Material ropeMaterial;
    private float currentTextureOffset = 0f;

    private void Start()
    {
        lastPosition = transform.position;
        if (lineRenderer != null)
        {
            ropeMaterial = lineRenderer.material;
        }
        UpdateRopeVisual();
    }

    private void LateUpdate()
    {
        UpdateRopeVisual();
        UpdatePigletAnimations();
        lastPosition = transform.position;
    }

    private void UpdateRopeVisual()
    {
        if (lineRenderer == null) return;

        // à¸ˆà¸¸à¸”à¸—à¸µà¹ˆ 1: à¸•à¸°à¸à¸£à¹‰à¸²à¹à¸¡à¹ˆà¸«à¸¡à¸¹
        Vector3 point0 = new Vector3(transform.position.x, transform.position.y + gondolaOffset, 0f);
        
        // à¸ˆà¸¸à¸”à¸—à¸µà¹ˆ 2: à¸£à¸­à¸à¸”à¹‰à¸²à¸™à¸šà¸™ (à¸«à¸±à¸à¸¨à¸­à¸à¸•à¸±à¸§ L)
        Vector3 point1 = new Vector3(transform.position.x, pulleyY, 0f);

        // à¸ˆà¸¸à¸”à¸—à¸µà¹ˆ 3: à¸¡à¸·à¸­à¸¥à¸¹à¸à¸«à¸¡à¸¹ (à¸–à¹‰à¸²à¹„à¸¡à¹ˆà¹„à¸”à¹‰à¸•à¸±à¹‰à¸‡à¹„à¸§à¹‰ à¹ƒà¸«à¹‰à¸«à¸±à¸à¸‚à¸§à¸²à¹„à¸›à¹€à¸‰à¸¢à¹†)
        Vector3 point2 = pigletHand != null ? pigletHand.position : new Vector3(transform.position.x + 3f, pulleyY, 0f);

        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, point0);
        lineRenderer.SetPosition(1, point1);
        lineRenderer.SetPosition(2, point2);
        
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;

        // à¸„à¸³à¸™à¸§à¸“à¸à¸²à¸£à¹€à¸¥à¸·à¹ˆà¸­à¸™à¸¥à¸²à¸¢à¹€à¸Šà¸·à¸­à¸ (Texture Scrolling)
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

        private void UpdatePigletAnimations()
    {
        float deltaY = transform.position.y - lastPosition.y;
        float speed = deltaY / Time.deltaTime;
        
        bool currentlyMoving = Mathf.Abs(speed) > 0.05f;
        
        if (currentlyMoving != isPulling)
        {
            isPulling = currentlyMoving;
            string stateToPlay = isPulling ? pullStateName : idleStateName;
            
            if (piglet1 != null) piglet1.Play(stateToPlay);
            if (piglet2 != null) piglet2.Play(stateToPlay);
        }

        // Adjust animation speed based on movement speed
        float animSpeed = currentlyMoving ? Mathf.Abs(speed) * 0.2f : 1f; // 0.2f is a multiplier to make it look natural
        if (animSpeed < 0.1f && currentlyMoving) animSpeed = 0.1f;
        
        if (piglet1 != null) piglet1.speed = animSpeed;
        if (piglet2 != null) piglet2.speed = animSpeed;

        // if (piglet1 != null) piglet1.SetFloat(pullAnimParam, speed);
        // if (piglet2 != null) piglet2.SetFloat(pullAnimParam, speed);
    }
[ContextMenu("Setup L-Shape Rope (LineRenderer)")]
    private void SetupLineRenderer()
    {
        if (lineRenderer != null) return;

        GameObject ropeObj = new GameObject("L_RopeVisual");
        ropeObj.transform.SetParent(transform.parent != null ? transform.parent : null);
        
        lineRenderer = ropeObj.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = -5;
        
        // à¸—à¸³à¹ƒà¸«à¹‰ Texture à¸¥à¸²à¸à¸¢à¸²à¸§à¸•à¸²à¸¡à¹€à¸ªà¹‰à¸™ (à¸•à¹‰à¸­à¸‡à¹ƒà¸Šà¹‰ Material à¸—à¸µà¹ˆà¸¡à¸µà¸¥à¸²à¸¢à¹€à¸Šà¸·à¸­à¸)
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
        lineRenderer.startColor = new Color(0.85f, 0.65f, 0.45f);
        lineRenderer.endColor = new Color(0.85f, 0.65f, 0.45f);
    }
}




