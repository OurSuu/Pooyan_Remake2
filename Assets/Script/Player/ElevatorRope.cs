using UnityEngine;

public class ElevatorRope : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform pigletHand; 
    [SerializeField] private float pulleyY = 4.5f; 
    [SerializeField] private float gondolaOffset = 0.5f; 
    [SerializeField] private float ropeWidth = 0.05f;
    [SerializeField] private float scrollSpeed = 0.5f; 

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
        var player = GetComponent<PlayerController>();
        if (player != null && player.State == PlayerState.Dead)
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            return;
        }
        else if (lineRenderer != null && !lineRenderer.enabled)
        {
            lineRenderer.enabled = true; // เปิดกลับมาตอนเกิดใหม่
        }

        UpdateRopeVisual();
        UpdatePigletAnimations();
        lastPosition = transform.position;
    }

    private void UpdateRopeVisual()
    {
        if (lineRenderer == null) return;

        
        Vector3 point0 = new Vector3(transform.position.x, transform.position.y + gondolaOffset, 0f);
        
        
        Vector3 point1 = new Vector3(transform.position.x, pulleyY, 0f);

        
        Vector3 point2 = pigletHand != null ? pigletHand.position : new Vector3(transform.position.x + 3f, pulleyY, 0f);

        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, point0);
        lineRenderer.SetPosition(1, point1);
        lineRenderer.SetPosition(2, point2);
        
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;

        
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
        
        
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
        lineRenderer.startColor = new Color(0.85f, 0.65f, 0.45f);
        lineRenderer.endColor = new Color(0.85f, 0.65f, 0.45f);
    }
}







