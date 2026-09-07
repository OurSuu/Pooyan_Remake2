using UnityEngine;

/// <summary>
/// บอสหมาป่าด่าน 2 (ตอนขึ้น) - มีลูกโป่งกระพริบๆ โล่กันได้ 5 ที ฆ่าปุ๊บผ่านด่านทันทีเลยโคตรเท่
/// </summary>
public class BossWolf : Wolf
{
    private int remainingShieldHP; // เลือดของโล่บอส ทนหน่อยนะ

    // ตอนเกิดเรียกฟังก์ชันนี้แหละ ตั้งค่าสเตตัสต่างๆ
    public override void Initialize(LevelConfig config, bool descending, float speed, bool shield, bool rockThrow, float dropX = 0f, float[] lanes = null, int laneIndex = 0)
    {
        // บอสมีโล่เสมอ บังคับ true ไปเลย
        base.Initialize(config, descending, speed, true, rockThrow, dropX, lanes, laneIndex);
        remainingShieldHP = config.bossShieldHP; // ดึงเลือดโล่มาจากคอนฟิก

        if (balloon != null)
            balloon.Initialize(this, 1, bossFlash: true); // ลูกโป่งบอสเลือด 1 แต่กระพริบวิบวับ
    }

    /// <summary>
    /// โดนลูกธนูแล้วเป็นไง คืนค่า true ถ้าลูกโป่งแตก (จบด่าน)
    /// </summary>
    public bool TryTakeArrowHit()
    {
        // โล่ยังไม่แตกก็ลดเลือดโล่ไปก่อน
        if (remainingShieldHP > 0)
        {
            remainingShieldHP--;
            AudioManager.Instance?.PlayShieldBlock(); // เสียงติ๊งๆ โดนโล่
            return false;
        }

        // โล่แตกแล้ว โดนลูกโป่งเต็มๆ
        if (balloon != null && balloon.TakeDamage())
            return true;

        return false;
    }

    // ถ้าโดนเนื้อร่วงทับ
    public override void KillByMeat()
    {
        if (state == WolfState.Dead) return;
        state = WolfState.Dead;
        if (balloon != null) balloon.gameObject.SetActive(false); // ซ่อนลูกโป่งไป
        GameManager.Instance?.TriggerStageClear(); // บอสตายปุ๊บ ผ่านด่านเลยจ้า!
        Destroy(gameObject);
    }

    // ตอนลูกโป่งแตก (ยิงทะลุโล่มาได้)
    public override void OnBalloonPopped()
    {
        if (state == WolfState.Dead) return;
        state = WolfState.Dead;
        GameManager.Instance?.TriggerStageClear(); // ร่วงปุ๊บ ผ่านด่านเหมือนกัน
        Destroy(gameObject);
    }

    // อันนี้ถ้าบอสรอดไปถึงหน้าผาได้ งานหยาบแน่ๆ
    protected override void OnReachedCliff()
    {
        base.OnReachedCliff();
        
        // ลงโทษหนักๆ ถ้าปล่อยบอสหลุด เพิ่มหมาป่าในโควต้าให้เหนื่อยเล่น 4 ตัว
        WolfTracker.Instance?.AddTargetKills(4);
    }
}
