using UnityEngine;

/// <summary>
/// Coordinates stage start/end â€” resets stage systems, activates tree-top boss.
/// </summary>
public class StageFlowController : MonoBehaviour
{
    [SerializeField] private WolfSpawner wolfSpawner;
    [SerializeField] private TreeTopBossWolf treeTopBoss;
    [SerializeField] private LadderSystem ladderSystem;
    [SerializeField] private BoulderSystem boulderSystem;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            
            // If the game already started before this Start() ran, catch up!
            if (GameManager.Instance.IsPlaying)
            {
                OnStageStart(GameManager.Instance.CurrentStage);
            }
        }

        if (WolfTracker.Instance != null)
            WolfTracker.Instance.OnAllWolvesDefeated += HandleAllWolvesDefeated;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        if (WolfTracker.Instance != null)
            WolfTracker.Instance.OnAllWolvesDefeated -= HandleAllWolvesDefeated;
    }

    private void HandleAllWolvesDefeated()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            GameManager.Instance.TriggerStageClear();
    }

    private void OnStateChanged(GameState state)
    {
        
        if (state == GameState.Playing)
        {
            OnStageStart(GameManager.Instance.CurrentStage);
        }
    }

    public void OnStageStart(int stage)
    {
        WolfTracker.Instance?.SuppressNotifications(true);
        ClearAllEntities();
        ladderSystem?.ResetLadder();
        boulderSystem?.ResetCliff();

        var config = LevelManager.Instance?.GetConfigForStage(stage);

        if (config != null)
        {
            WolfTracker.Instance?.BeginStage(config.wolfCount);
        }
        else
        {
            WolfTracker.Instance?.ResetCount();
        }

        if (treeTopBoss != null)
            treeTopBoss.Activate(config != null && config.hasTreeTopBoss);

        wolfSpawner?.StartSpawning();
    }

    public void RestartCurrentWave(bool diedFromBoulder = false)
    {
        ClearAllEntities();
        ladderSystem?.ResetLadder();
        
        if (diedFromBoulder && boulderSystem != null)
        {
            boulderSystem.ResetCliff(4); // Keep 4 wolves
        }
        else
        {
            boulderSystem?.ResetCliff();
        }

        var config = LevelManager.Instance?.GetConfigForStage(GameManager.Instance.CurrentStage);
        if (treeTopBoss != null)
            treeTopBoss.Activate(config != null && config.hasTreeTopBoss);

        wolfSpawner?.StartSpawning();
    }

    public void ClearAllEntities()
    {
        foreach (var tag in new[] { GameConstants.TagEnemy, GameConstants.TagEnemyProjectile, GameConstants.TagArrow, GameConstants.TagMeat })
        {
            var objs = GameObject.FindGameObjectsWithTag(tag);
            foreach (var obj in objs)
                Destroy(obj);
        }
    }
}

