using System.Collections;
using UnityEngine;

/// <summary>
/// บอสหมาป่าด่านคู่ (ตอนลอยขึ้น) - ตัวสีชมพู ลูกโป่งกระพริบสีแดง โล่กันลูกธนูได้หลายนัด
/// ยิงทะลุโล่จนลูกโป่งแตก หรือปาเนื้อใส่ 1 ก้อน = ร่วงตกพื้นและผ่านด่านทันที!
/// </summary>
public class BossWolf : Wolf
{
    private int remainingShieldHP; // เลือดของโล่บอส
    private int maxShieldHP;
    private bool stageClearTriggered = false;

    [Header("Boss Visuals (NES Pink Boss Wolf)")]
    [SerializeField] private Color bossBodyTint = new Color(1f, 0.68f, 0.85f, 1f); // สีชมพูตามต้นฉบับ NES (ボスオオカミ・ピンク)
    [SerializeField] private float bossSpeedMultiplier = 1.75f; // ตัวคูณความเร็วลอยขึ้นของบอส (ลดจาก 2.05 ลงเล็กน้อยให้ยังท้าทาย)
    [SerializeField] private float minBossAscendSpeed = 1.55f; // ความเร็วขั้นต่ำตอนลอยขึ้นด่าน 2
    [SerializeField] private float stage4MinBossAscendSpeed = 1.70f; // ความเร็วขั้นต่ำตอนลอยขึ้นด่าน 4+
    [SerializeField] private float maxBossAscendSpeed = 1.95f; // เพดานความเร็วสูงสุดตอนลอยขึ้นของบอส
    [SerializeField] private int maxBossShieldHits = 5; // จำนวนครั้งสูงสุดที่โล่กันธนูได้ (รวมลูกโป่ง 1 นัด = 6 นัด)
    [SerializeField] private float bossWalkSpeedMultiplier = 1.25f;

    private float initialWalkSpeed = -1f;

    // ตอนเกิดเรียกฟังก์ชันนี้ ตั้งค่าสเตตัสต่างๆ
    public override void Initialize(LevelConfig config, bool descending, float speed, bool shield, bool rockThrow, float dropX = 0f, float[] lanes = null, int laneIndex = 0, int? overrideBalloonHP = null, BalloonColorType? overrideColorType = null)
    {
        if (initialWalkSpeed < 0f) initialWalkSpeed = walkSpeed;
        walkSpeed = initialWalkSpeed * bossWalkSpeedMultiplier;

        int currentStage = GameManager.Instance != null ? GameManager.Instance.EffectiveDifficultyStage : 2;
        float minSpeed = currentStage >= 4 ? stage4MinBossAscendSpeed : minBossAscendSpeed;
        float maxSpeed = currentStage >= 4 ? maxBossAscendSpeed : Mathf.Min(maxBossAscendSpeed, 1.85f);
        float bossAscendSpeed = Mathf.Clamp(speed * bossSpeedMultiplier, minSpeed, maxSpeed);

        // บอสมีโล่เสมอ แต่คุมสปีดและเลือดโล่ให้อยู่ในระยะที่ยิงทัน
        base.Initialize(config, descending, bossAscendSpeed, true, rockThrow, dropX, lanes, laneIndex, 1, BalloonColorType.PinkRed);
        int rawShieldHP = (config != null && config.bossShieldHP > 0) ? config.bossShieldHP : GameConstants.BossShieldHits;
        maxShieldHP = Mathf.Clamp(rawShieldHP, 1, Mathf.Max(1, maxBossShieldHits));
        remainingShieldHP = maxShieldHP;
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
    public bool TryTakeArrowHit(Vector3? arrowPos = null)
    {
        // โล่ยังไม่แตกก็ลดเลือดโล่ไปก่อน พร้อมเล่นท่าปัดลูกธนู เสียงโล่ และชะลอความเร็วลอยขึ้นเพียงเล็กน้อย
        if (remainingShieldHP > 0)
        {
            remainingShieldHP--;
            float shieldRatio = Mathf.Clamp01((float)remainingShieldHP / Mathf.Max(1, maxShieldHP));
            floatSpeed = baseFloatSpeed * Mathf.Lerp(0.88f, 1f, shieldRatio);
            AudioManager.Instance?.PlayShieldBlock(); // เสียงติ๊งๆ โดนโล่
            PlayDeflectAnimation();                   // เล่นท่าปัดป้อง
            return false;
        }

        // โล่แตกแล้ว โดนลูกโป่งเต็มๆ
        if (balloon != null && balloon.TakeDamage(1, arrowPos))
            return true;

        return false;
    }

    public override void OnBalloonDamaged(int currentHP, int maxHP)
    {
        // บอสหมาป่าไม่ลดความเร็วตอนลอยขึ้นเมื่อโดนยิง
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
