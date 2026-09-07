using UnityEngine;

/// <summary>
/// ตัวคุม Flow การเริ่ม/จบด่าน — คอยจัดการพวกรีเซ็ตระบบในฉาก แล้วก็สั่งเปิดปิดบอสบนต้นไม้
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
            
            // ถ้าเกมดันเริ่มไปก่อนหน้าที่ Start() ตัวนี้จะทำงาน ก็ให้มัน Catch up ไล่ตามซะ!
            if (GameManager.Instance.IsPlaying)
            {
                OnStageStart(GameManager.Instance.CurrentStage);
            }
        }

        // คอยฟังข่าวว่าปราบหมาป่าหมดรึยัง
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

    // กำจัดหมาป่าเกลี้ยงแล้ว สั่งเคลียร์ด่านได้เลย
    private void HandleAllWolvesDefeated()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            GameManager.Instance.TriggerStageClear();
    }

    // ถ้า State เกมเปลี่ยนมาเป็น Playing เมื่อไหร่ ก็เริ่มรันด่านใหม่
    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            OnStageStart(GameManager.Instance.CurrentStage);
        }
    }

    // เริ่มต้นด่าน เตรียมความพร้อม
    public void OnStageStart(int stage)
    {
        WolfTracker.Instance?.SuppressNotifications(true); // ปิดการแจ้งเตือนชั่วคราว
        ClearAllEntities(); // เคลียร์ขยะ (พวกกระสุน ศัตรูเก่า) ออกให้หมด
        ladderSystem?.ResetLadder(); // รีเซ็ตบันได
        boulderSystem?.ResetCliff(); // รีเซ็ตหน้าผากับหินกลิ้ง

        var config = LevelManager.Instance?.GetConfigForStage(stage);

        if (config != null)
        {
            WolfTracker.Instance?.BeginStage(config.wolfCount); // เซ็ตเป้าหมายหมาป่า
        }
        else
        {
            WolfTracker.Instance?.ResetCount();
        }

        // เรียกบอสต้นไม้ออกมาถ้าเซ็ตไว้
        if (treeTopBoss != null)
            treeTopBoss.Activate(config != null && config.hasTreeTopBoss);

        wolfSpawner?.StartSpawning(); // ปล่อยหมาป่า!
    }

    // รีสตาร์ทเวฟปัจจุบัน (มักจะใช้ตอนตายแล้วเริ่มใหม่ในด่านเดิม)
    public void RestartCurrentWave(bool diedFromBoulder = false)
    {
        ClearAllEntities(); // ล้างฉากก่อนเลย
        ladderSystem?.ResetLadder();
        
        if (diedFromBoulder && boulderSystem != null)
        {
            boulderSystem.ResetCliff(4); // ถ้าตายเพราะโดนหินทับ ให้เหลือหมาป่าที่หน้าผาไว้ 4 ตัว
        }
        else
        {
            boulderSystem?.ResetCliff();
        }

        // เปิดปิดบอสใหม่ตาม Config ของด่าน
        var config = LevelManager.Instance?.GetConfigForStage(GameManager.Instance.CurrentStage);
        if (treeTopBoss != null)
            treeTopBoss.Activate(config != null && config.hasTreeTopBoss);

        wolfSpawner?.StartSpawning(); // ลุยต่อ!
    }

    // ฟังก์ชันเคลียร์ขยะในฉาก หาตาม Tag แล้วสั่งทำลายทิ้งให้เหี้ยน
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
