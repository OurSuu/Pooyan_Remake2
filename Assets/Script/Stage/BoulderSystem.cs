using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ascending stage â€” 7 wolves on cliff triggers boulder death.
/// </summary>
public class BoulderSystem : MonoBehaviour
{
    public static BoulderSystem Instance { get; private set; }

    [SerializeField] private Transform boulder;
    [SerializeField] private float cliffY = 3.5f;
    [SerializeField] private Transform[] cliffWolfSlots;

    private readonly List<Wolf> wolvesOnCliff = new();
    private int virtualWolvesOnCliff = 0;
    private bool boulderFalling;

    public int WolvesOnCliff => wolvesOnCliff.Count;
    public int WolfThreshold => GameConstants.BoulderWolfThreshold;

    public event System.Action<int, int> OnCliffCountChanged;

    private Vector3 initialBoulderPos;
    private Vector3 boulderOffset;
    private float moveSpeed = 2f;

    private void Awake()
    {
        Instance = this;
        if (boulder != null)
        {
            initialBoulderPos = boulder.position;
            if (cliffWolfSlots != null && cliffWolfSlots.Length > 0)
            {
                // Calculate offset relative to the starting slot (furthest from edge, i.e., index length-1)
                boulderOffset = initialBoulderPos - cliffWolfSlots[cliffWolfSlots.Length - 1].position;
            }
            else
            {
                boulderOffset = new Vector3(1.14f, 0.36f, 0f);
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageChanged += HandleStageChanged;
            HandleStageChanged(GameManager.Instance.CurrentStage);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStageChanged -= HandleStageChanged;
        if (Instance == this) Instance = null;
    }

    private void HandleStageChanged(int stage)
    {
        if (boulder != null)
        {
            bool isStage2 = GameManager.Instance != null && !GameManager.Instance.IsOddStage;
            boulder.gameObject.SetActive(isStage2);
        }
    }

    private void Update()
    {
        if (boulder == null || boulderFalling || !boulder.gameObject.activeInHierarchy) return;

        int maxSlots = cliffWolfSlots != null ? cliffWolfSlots.Length : 5;
        int totalWolves = wolvesOnCliff.Count + virtualWolvesOnCliff;
        
        // Front-most wolf occupies the slot (maxSlots - totalWolves)
        // If 0 wolves, boulder stays at the starting slot (maxSlots - 1)
        int frontSlotIndex = maxSlots - Mathf.Max(1, totalWolves);
        frontSlotIndex = Mathf.Clamp(frontSlotIndex, 0, maxSlots - 1);
        
        Vector3 targetBoulderPos = GetCliffSlotPosition(frontSlotIndex) + boulderOffset;
        
        // Smoothly move boulder to target position
        boulder.position = Vector3.MoveTowards(boulder.position, targetBoulderPos, moveSpeed * Time.deltaTime);
    }

    public void RegisterWolfOnCliff(Wolf wolf)
    {
        if (boulderFalling) return;

        if (!wolvesOnCliff.Contains(wolf))
        {
            wolvesOnCliff.Add(wolf);
            int total = wolvesOnCliff.Count + virtualWolvesOnCliff;
            OnCliffCountChanged?.Invoke(total, WolfThreshold);

            if (total >= WolfThreshold)
            {
                TriggerBoulder();
            }
        }
    }

    private void TriggerBoulder()
    {
        boulderFalling = true;
        AudioManager.Instance?.PlayBoulderFall();
        StartCoroutine(DropBoulderRoutine());
    }

    public Vector3 GetCliffTargetPosition(Wolf wolf)
    {
        int maxSlots = cliffWolfSlots != null ? cliffWolfSlots.Length : 5;
        int totalWolves = wolvesOnCliff.Count + virtualWolvesOnCliff;
        
        int wolfIndexInList = wolvesOnCliff.IndexOf(wolf);
        if (wolfIndexInList == -1) wolfIndexInList = wolvesOnCliff.Count; // fallback if walking up
        
        int overallIndex = virtualWolvesOnCliff + wolfIndexInList;
        
        // 0th wolf goes to (maxSlots - totalWolves) + 0
        // 1st wolf goes to (maxSlots - totalWolves) + 1
        int targetSlotIndex = (maxSlots - totalWolves) + overallIndex;
        targetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, maxSlots - 1);
        
        return GetCliffSlotPosition(targetSlotIndex);
    }

    private Vector3 GetCliffSlotPosition(int index)
    {
        if (cliffWolfSlots != null && index < cliffWolfSlots.Length && cliffWolfSlots[index] != null)
            return cliffWolfSlots[index].position;

        return new Vector3(-3f + index * 0.5f, cliffY, 0f);
    }

    private IEnumerator DropBoulderRoutine()
    {
        if (boulder == null) yield break;

        boulder.gameObject.SetActive(true);
        Vector3 startPos = boulder.position;
        
        var player = FindAnyObjectByType<PlayerController>();
        Vector3 targetPos = startPos + Vector3.down * 10f; // fallback
        
        if (player != null)
        {
            // Drop slightly below player to ensure it passes them
            targetPos = player.transform.position + Vector3.down * 2f; 
        }

        float elapsed = 0f;
        float duration = 1.5f; // speed of fall
        
        // Arc parameters
        Vector3 midPoint = (startPos + targetPos) / 2f;
        midPoint.y = startPos.y + 1.5f; // curve upwards initially like being tossed

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Quadratic bezier curve
            Vector3 m1 = Vector3.Lerp(startPos, midPoint, t);
            Vector3 m2 = Vector3.Lerp(midPoint, targetPos, t);
            boulder.position = Vector3.Lerp(m1, m2, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (player != null)
        {
            player.SetDiedFromBoulder(true); // Signal to GameManager
            player.TakeDamage();
        }
        else
        {
            ResetCliff();
        }
    }

    public void ResetCliff(int keepCount = 0)
    {
        wolvesOnCliff.Clear();
        virtualWolvesOnCliff = keepCount;
        boulderFalling = false;
        
        if (boulder != null)
        {
            bool isStage2 = GameManager.Instance != null && !GameManager.Instance.IsOddStage;
            boulder.gameObject.SetActive(isStage2);
            
            // Instantly snap boulder to position
            int maxSlots = cliffWolfSlots != null ? cliffWolfSlots.Length : 5;
            int frontSlotIndex = maxSlots - Mathf.Max(1, keepCount);
            frontSlotIndex = Mathf.Clamp(frontSlotIndex, 0, maxSlots - 1);
            boulder.position = GetCliffSlotPosition(frontSlotIndex) + boulderOffset;
        }
        
        OnCliffCountChanged?.Invoke(virtualWolvesOnCliff, WolfThreshold);
    }
}

