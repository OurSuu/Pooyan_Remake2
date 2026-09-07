using System;
using UnityEngine;

/// <summary>
/// Counts wolves that were actually defeated. Landing / reaching the cliff does not count
/// as a kill (arcade: replacements spawn on odd stages; cliff wolves push the boulder).
/// </summary>
public class WolfTracker : MonoBehaviour
{
    public static WolfTracker Instance { get; private set; }

    private int targetKills;
    private int kills;
    private bool suppressNotifications;
    private bool stageCleared;

    public int Kills => kills;
    public int TargetKills => targetKills;
    public int RemainingKills => Mathf.Max(0, targetKills - kills);

    public event Action OnAllWolvesDefeated;
    public event Action OnWolfEscapedToGround;
    public event Action<int> OnKillCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BeginStage(int wolfKillQuota)
    {
        stageCleared = false;
        targetKills = Mathf.Max(1, wolfKillQuota);
        kills = 0;
        suppressNotifications = false;
        OnKillCountChanged?.Invoke(RemainingKills);
    }

    public void SuppressNotifications(bool suppress)
    {
        suppressNotifications = suppress;
    }

    public void Register(Wolf wolf)
    {
        if (wolf == null) return;

        var notifier = wolf.GetComponent<WolfDeathNotifier>();
        if (notifier == null)
            notifier = wolf.gameObject.AddComponent<WolfDeathNotifier>();
        notifier.Initialize(this);
    }

    public void NotifyKilled()
    {
        if (suppressNotifications || stageCleared) return;
        
        if (GameManager.Instance != null && GameManager.Instance.DeathFreeze) return;

        kills++;
        OnKillCountChanged?.Invoke(RemainingKills);
        
        if (kills >= targetKills)
        {
            stageCleared = true;
            OnAllWolvesDefeated?.Invoke();
        }
    }

    public void NotifyEscapedToGround()
    {
        if (suppressNotifications) return;
        OnWolfEscapedToGround?.Invoke();
    }

    public void ResetCount() => BeginStage(Mathf.Max(1, targetKills));

    public void AddTargetKills(int amount)
    {
        targetKills += amount;
        OnKillCountChanged?.Invoke(RemainingKills);
    }
}

/// <summary>
/// Notifies WolfTracker only when a wolf is actually defeated, not when it escapes.
/// </summary>
public class WolfDeathNotifier : MonoBehaviour
{
    private WolfTracker tracker;
    private bool countsAsKill = true;
    private bool reported;

    public void Initialize(WolfTracker wolfTracker) => tracker = wolfTracker;

    public void MarkEscaped()
    {
        countsAsKill = false;
    }

    private void OnDestroy()
    {
        if (reported || !countsAsKill) return;
        reported = true;
        tracker?.NotifyKilled();
    }
}

