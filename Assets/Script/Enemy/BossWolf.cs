using System.Collections;
using UnityEngine;

/// <summary>
/// บอสหมาป่าด่านคู่ (ตอนลอยขึ้น) - ตัวสีชมพู ลูกโป่งกระพริบสีแดง โล่กันลูกธนูได้หลายนัด
/// ยิงทะลุโล่จนลูกโป่งแตก หรือปาเนื้อใส่ 1 ก้อน = ร่วงตกพื้นและผ่านด่านทันที!
/// </summary>
public class BossWolf : Wolf
{
    private int remainingShieldHP; // เลือดของโล่บอส
    private bool stageClearTriggered = false;

    [Header("Boss Visuals (NES Pink Boss Wolf)")]
    [SerializeField] private Color bossBodyTint = new Color(1f, 0.68f, 0.85f, 1f); // สีชมพูตามต้นฉบับ NES (ボスオオカミ・ピンク)

    // ตอนเกิดเรียกฟังก์ชันนี้ ตั้งค่าสเตตัสต่างๆ
    public override void Initialize(LevelConfig config, bool descending, float speed, bool shield, bool rockThrow, float dropX = 0f, float[] lanes = null, int laneIndex = 0, int? overrideBalloonHP = null, BalloonColorType? overrideColorType = null)
    {
        // บอสมีโล่เสมอ และเคลื่อนที่ไวขึ้นเล็กน้อย
        base.Initialize(config, descending, speed * 1.15f, true, rockThrow, dropX, lanes, laneIndex, 1, BalloonColorType.PinkRed);
        remainingShieldHP = (config != null && config.bossShieldHP > 0) ? config.bossShieldHP : GameConstants.BossShieldHits;
        stageClearTriggered = false;

        if (balloon != null)
        {
            balloon.Initialize(this, 1, bossFlash: true, colorType: BalloonColorType.PinkRed); // ลูกโป่งบอสเลือด 1 แต่กระพริบวิบวับ
            balloon.gameObject.SetActive(false); // ซ่อนไว้ก่อนจนกว่าจะเดินถึงจุดเป่าลูกโป่ง
        }

        if (cachedSR != null)
        {
            cachedSR.color = bossBodyTint;
        }
    }

    /// <summary>
    /// โดนลูกธนูแล้วเป็นไง คืนค่า true ถ้าลูกโป่งแตก (จบด่าน), คืนค่า false ถ้าติดโล่บอส
    /// </summary>
    public bool TryTakeArrowHit()
    {
        // โล่ยังไม่แตกก็ลดเลือดโล่ไปก่อน พร้อมเล่นท่าปัดลูกธนูและเสียงโล่
        if (remainingShieldHP > 0)
        {
            remainingShieldHP--;
            AudioManager.Instance?.PlayShieldBlock(); // เสียงติ๊งๆ โดนโล่
            PlayDeflectAnimation();                   // เล่นท่าปัดป้อง
            return false;
        }

        // โล่แตกแล้ว โดนลูกโป่งเต็มๆ
        if (balloon != null && balloon.TakeDamage())
            return true;

        return false;
    }

    // ถ้าโดนเนื้อปาใส่ (เนื้อทะลุโล่ ตายทันทีในก้อนเดียว!)
    public override void OnMeatHit()
    {
        if (state == WolfState.Dead || state == WolfState.Falling) return;

        base.OnMeatHit(); // ปล่อยลูกโป่งลอยขึ้นฟ้า + เปลี่ยนเป็นสถานะ Falling ร่วงลงพื้น
        TriggerBossDefeatedStageClear();
    }

    // เผื่อโค้ดส่วนอื่นเรียก KillByMeat()
    public override void KillByMeat()
    {
        OnMeatHit();
    }

    // ตอนลูกโป่งแตก (ยิงทะลุโล่มาได้สำเร็จ)
    public override void OnBalloonPopped()
    {
        if (state == WolfState.Dead || state == WolfState.Falling) return;

        base.OnBalloonPopped(); // เปลี่ยนเป็นสถานะ Falling ร่วงลงพื้นพร้อมเสียงหมาป่าร่วง
        TriggerBossDefeatedStageClear();
    }

    private void TriggerBossDefeatedStageClear()
    {
        if (stageClearTriggered) return;
        stageClearTriggered = true;
        StartCoroutine(DelayedStageClearRoutine());
    }

    private IEnumerator DelayedStageClearRoutine()
    {
        // รอให้บอสร่วงลงมากระแทกพื้นให้เห็นสะใจก่อนนิดนึง แล้วค่อยนับคิลตัวสุดท้ายเพื่อเคลียร์ด่าน
        yield return new WaitForSeconds(1.0f);
        GetComponent<WolfDeathNotifier>()?.ReportKillNow();
        GameManager.Instance?.TriggerStageClear();
    }

    // อันนี้ถ้าบอสรอดไปถึงหน้าผาได้ งานหยาบแน่ๆ (ตามต้นฉบับ NES จะบวกโควตาหมาป่าเพิ่มแล้วบอสมาใหม่)
    protected override void OnReachedCliff()
    {
        base.OnReachedCliff();

        // ลงโทษถ้าปล่อยบอสหลุด เพิ่มหมาป่าในโควตาให้เหนื่อยเล่น 4-5 ตัว
        WolfTracker.Instance?.AddTargetKills(GameConstants.BossEscapeExtraWolves);
        FindAnyObjectByType<WolfSpawner>()?.ResetBossSpawned();
    }
}
