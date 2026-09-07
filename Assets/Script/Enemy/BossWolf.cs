using UnityEngine;

/// <summary>
/// Ascending stage boss â€” flashing balloon, 5-hit shield, instant stage clear on kill.
/// </summary>
public class BossWolf : Wolf
{
    private int remainingShieldHP;

    public override void Initialize(LevelConfig config, bool descending, float speed, bool shield, bool rockThrow, float dropX = 0f, float[] lanes = null, int laneIndex = 0)
    {
        base.Initialize(config, descending, speed, true, rockThrow, dropX, lanes, laneIndex);
        remainingShieldHP = config.bossShieldHP;

        if (balloon != null)
            balloon.Initialize(this, 1, bossFlash: true);
    }

    /// <summary>
    /// Returns true if the balloon was popped (stage ends).
    /// </summary>
    public bool TryTakeArrowHit()
    {
        if (remainingShieldHP > 0)
        {
            remainingShieldHP--;
            AudioManager.Instance?.PlayShieldBlock();
            return false;
        }

        if (balloon != null && balloon.TakeDamage())
            return true;

        return false;
    }

    public override void KillByMeat()
    {
        if (state == WolfState.Dead) return;
        state = WolfState.Dead;
        if (balloon != null) balloon.gameObject.SetActive(false);
        GameManager.Instance?.TriggerStageClear();
        Destroy(gameObject);
    }

    public override void OnBalloonPopped()
    {
        if (state == WolfState.Dead) return;
        state = WolfState.Dead;
        GameManager.Instance?.TriggerStageClear();
        Destroy(gameObject);
    }

    protected override void OnReachedCliff()
    {
        base.OnReachedCliff();
        
        WolfTracker.Instance?.AddTargetKills(4);
    }
}


