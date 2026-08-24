using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Descending stage — wolves climb ladder and bite when gondola aligns.
/// </summary>
public class LadderSystem : MonoBehaviour
{
    public static LadderSystem Instance { get; private set; }

    [SerializeField] private float ladderX = 3.5f;
    [SerializeField] private float ladderBottomY = -3.5f;
    [SerializeField] private float stepHeight = 0.6f;
    [SerializeField] private int maxSteps = 12;

    [Header("Manual Steps (Optional)")]
    [SerializeField] private Transform[] stepTransforms;

    [Header("Fatal Settings")]
    [SerializeField] private int fatalWolfCount = 5;

    private readonly Dictionary<Wolf, int> wolfSteps = new();
    private readonly HashSet<int> occupiedSteps = new();

    public float LadderX => ladderX;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int? TryReserveStepForWolf(Wolf wolf)
    {
        if (wolfSteps.ContainsKey(wolf)) return wolfSteps[wolf];

        int stepCount = (stepTransforms != null && stepTransforms.Length > 0) ? stepTransforms.Length : maxSteps;

        // Arcade rule: Fill from BOTTOM to TOP!
        for (int i = 0; i < stepCount; i++)
        {
            if (!occupiedSteps.Contains(i))
            {
                occupiedSteps.Add(i);
                wolfSteps[wolf] = i;

                if (occupiedSteps.Count >= fatalWolfCount)
                {
                    TriggerLadderOverrun();
                }

                return i;
            }
        }
        return null;
    }

    private void TriggerLadderOverrun()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.State != PlayerState.Dead)
        {
            AudioManager.Instance?.PlayWolfBite(); // Use bite sound for now until rope cut animation is added
            player.TakeDamage();
        }
    }

    public float GetStepY(Wolf wolf, int? stepIndex)
    {
        if (stepIndex.HasValue)
        {
            if (stepTransforms != null && stepIndex.Value < stepTransforms.Length)
            {
                return stepTransforms[stepIndex.Value].position.y;
            }
            return ladderBottomY + stepIndex.Value * stepHeight;
        }
        return ladderBottomY;
    }

    // อัปเดตฟังก์ชัน Release ให้รับ Index ด้วยตามที่ Wolf.cs เรียก
    public void ReleaseStepByIndex(int index, Wolf wolf)
    {
        occupiedSteps.Remove(index);
        wolfSteps.Remove(wolf);
    }

    public void ReleaseStep(Wolf wolf)
    {
        if (wolfSteps.TryGetValue(wolf, out int index))
        {
            ReleaseStepByIndex(index, wolf);
        }
    }

    public void CheckBite(Wolf wolf)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null || wolf == null) return;

        float wolfY = wolf.transform.position.y;
        float playerY = player.transform.position.y;

        // Increased hit window so it's easier to get bitten if the player is near
        if (Mathf.Abs(playerY - wolfY) <= 1.2f)
        {
            wolf.TriggerBite();
        }
    }

    public void OnWolfReachedStep(Wolf wolf, int stepIndex)
    {
        // Just reached the step. Bite check is handled continuously in Wolf.cs
        // We do not instantly kill the player here. The player only dies if they move in front of the wolf.
    }

    public void ResetLadder()
    {
        wolfSteps.Clear();
        occupiedSteps.Clear();
    }
}
