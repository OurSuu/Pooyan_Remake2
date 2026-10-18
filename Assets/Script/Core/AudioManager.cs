using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ตัวจัดการเรื่องเสียงทั้งหมดในเกม Pooyan Remake ทั้งเพลงประกอบแยกตามฤดู (Seasonal BGM) และเสียงเอฟเฟกต์ (SFX) ทุกชนิด
/// เตรียมช่องใส่ AudioClip ไว้ให้ครบทุกเหตุการณ์ ลากไฟล์เสียงมาใส่ใน Inspector ได้ทันที!
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("1. เพลงประกอบฉากแยกตามฤดู (Seasonal & Scene BGM)")]
    [Tooltip("เพลงหน้าเมนูหลัก (Main Menu)")]
    [SerializeField] private AudioClip bgmMainMenu;
    [Tooltip("เพลงประจำด่าน 1 - ฤดูใบไม้ร่วง (Autumn Scene)")]
    [SerializeField] private AudioClip bgmAutumn;
    [Tooltip("เพลงประจำด่าน 2 - ฤดูใบไม้ผลิ (Spring Scene)")]
    [SerializeField] private AudioClip bgmSpring;
    [Tooltip("เพลงประจำด่าน 3 - ฤดูหนาว (Winter Scene)")]
    [SerializeField] private AudioClip bgmWinter;
    [Tooltip("เพลงประจำด่าน 4 - ฤดูร้อน (Summer Scene)")]
    [SerializeField] private AudioClip bgmSummer;
    [Tooltip("เพลงประจำฉากนี้ (ใส่ช่องนี้ได้ถ้าตั้งแยกแต่ละ Scene หรือใช้เป็นเพลงสำรอง)")]
    [SerializeField] private AudioClip bgmMain;

    [Header("2. เพลงด่านโบนัส & จิงเกิ้ลเปลี่ยนด่าน (Bonus & Jingles BGM)")]
    [Tooltip("เพลงประกอบด่านโบนัสหลัก (ใช้ได้ทั้งโบนัสเนื้อและโบนัสผลไม้)")]
    [SerializeField] private AudioClip bgmBonus;
    [Tooltip("เพลงด่านโบนัสปาเนื้อโดยเฉพาะ (ถ้าไม่ใส่ จะใช้ bgmBonus แทน)")]
    [SerializeField] private AudioClip bgmBonusMeat;
    [Tooltip("เพลงด่านโบนัสยิงผลไม้โดยเฉพาะ (ถ้าไม่ใส่ จะใช้ bgmBonus แทน)")]
    [SerializeField] private AudioClip bgmBonusFruit;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเปิดด่าน 1 (Autumn) / เพลงเปิดด่านเริ่มต้น")]
    [SerializeField] private AudioClip bgmStageIntro;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเปิดด่าน 2 (Spring) (ถ้าไม่ใส่ จะใช้ bgmStageIntro แทน)")]
    [SerializeField] private AudioClip bgmStage2Intro;
    [Tooltip("เพลงจิงเกิ้ลตอนเคลียร์ด่าน (Stage Clear)")]
    [SerializeField] private AudioClip bgmStageClear;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเคลียร์ด่าน 2 รวมญาติ (ถ้าไม่ใส่ จะใช้ bgmStageClear แทน)")]
    [SerializeField] private AudioClip bgmStage2Clear;
    [Tooltip("เพลงตอนจบเกม (Game Over)")]
    [SerializeField] private AudioClip bgmGameOver;

    [Header("3. เสียงฝั่งแม่หมู & ลูกหมู (Player & Piglets SFX)")]
    [Tooltip("เสียงแม่หมูยิงธนู")]
    [SerializeField] private AudioClip seArrowFire;
    [Tooltip("เสียงแม่หมูหยิบเนื้อจากรอกด้านบน")]
    [SerializeField] private AudioClip seMeatPickup;
    [Tooltip("เสียงแม่หมูโยนเนื้อออกไป")]
    [SerializeField] private AudioClip seMeatThrow;
    [Tooltip("เสียงเนื้อชนตัวหมาป่า")]
    [SerializeField] private AudioClip seMeatHit;
    [Tooltip("เสียงเนื้อเด้งกระดอนลูกโป่งเปล่า")]
    [SerializeField] private AudioClip seMeatBounce;
    [Tooltip("เสียงลูกหมูดึงเชือกรอกตอนขยับกระเช้าขึ้น-ลง")]
    [SerializeField] private AudioClip seRopePull;
    [Tooltip("เสียงตอนแม่หมูโดนโจมตี / ร่วงตกจากกระเช้า")]
    [SerializeField] private AudioClip sePlayerFall;
    [Tooltip("เสียงตอนได้ชีวิตเพิ่ม (1-UP)")]
    [SerializeField] private AudioClip seExtraLife;

    [Header("4. เสียงฝั่งหมาป่า & ลูกโป่ง (Wolf & Balloon SFX)")]
    [Tooltip("เสียงหมาป่าเป่าลูกโป่งก่อนโดด/ลอย")]
    [SerializeField] private AudioClip seBalloonInflate;
    [Tooltip("เสียงยิงโดนลูกโป่งแตก")]
    [SerializeField] private AudioClip seBalloonPop;
    [Tooltip("เสียงยิงโดนลูกโป่งแบบอึด (สีเหลือง/บอส) แต่ลูกโป่งยังไม่แตก")]
    [SerializeField] private AudioClip seBalloonDamage;
    [Tooltip("เสียงหมาป่าร่วงตกลงพื้น")]
    [SerializeField] private AudioClip seWolfFall;
    [Tooltip("เสียงหมาป่าเอาโล่กันธนู หรืออ้าปากงับลูกธนู")]
    [SerializeField] private AudioClip seShieldBlock;
    [Tooltip("เสียงหมาป่าปีนบันได")]
    [SerializeField] private AudioClip seLadderClimb;
    [Tooltip("เสียงหมาป่ากระโดดงับแม่หมูตรงบันได")]
    [SerializeField] private AudioClip seWolfBite;

    [Header("5. เสียงก้อนหิน, หินยักษ์บนผา & ผลไม้ (Projectiles & Hazard SFX)")]
    [Tooltip("เสียงหมาป่าปาหินออกมา")]
    [SerializeField] private AudioClip seRockThrow;
    [Tooltip("เสียงก้อนหินกระทบหลังคา/เกราะกระเช้าแม่หมู")]
    [SerializeField] private AudioClip seRockHit;
    [Tooltip("เสียงยิงธนูทำลายก้อนหินกลางอากาศ")]
    [SerializeField] private AudioClip seRockDestroy;
    [Tooltip("เสียงหมาป่าช่วยกันดันก้อนหินยักษ์ขยับบนหน้าผา (ด่านคู่)")]
    [SerializeField] private AudioClip seBoulderPush;
    [Tooltip("เสียงก้อนหินยักษ์เริ่มร่วงถล่มลงมาจากหน้าผา")]
    [SerializeField] private AudioClip seBoulderFall;
    [Tooltip("เสียงก้อนหินยักษ์ตกกระแทกกระเช้าแม่หมู")]
    [SerializeField] private AudioClip seBoulderHit;
    [Tooltip("เสียงบอสหมาป่าโยนผลไม้ลงมา")]
    [SerializeField] private AudioClip seFruitDrop;
    [Tooltip("เสียงยิงธนูโดนผลไม้แตก")]
    [SerializeField] private AudioClip seFruitHit;

    [Header("6. เสียง UI (Menu & Cutscene SFX)")]
    [Tooltip("เสียงกดปุ่มเมนู / เลือกตัวอักษร")]
    [SerializeField] private AudioClip seButtonClick;

    [Header("7. ลำโพงและการตั้งค่าระดับเสียง (Audio Sources & Volume)")]
    [SerializeField] private AudioSource bgmSource;      // ลำโพงเพลง BGM
    [SerializeField] private AudioSource sfxSource;      // ลำโพงเสียงเอฟเฟกต์ทั่วไป
    [SerializeField] private AudioSource ropeLoopSource; // ลำโพงแยกสำหรับเสียงดึงเชือกต่อเนื่อง
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;
    [Range(0f, 1f)] [SerializeField] private float ropePullVolume = 0.6f;

    private float lastRopeStepTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureAudioSources();
    }

    public void EnsureAudioSources()
    {
        var sources = GetComponents<AudioSource>();
        if (bgmSource == null)
            bgmSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        if (ropeLoopSource == null)
            ropeLoopSource = sources.Length > 2 ? sources[2] : gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        ropeLoopSource.loop = true;
        ropeLoopSource.playOnAwake = false;
        ropeLoopSource.volume = ropePullVolume;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnStageClear += HandleStageClear;
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
        else
        {
            // กรณีอยู่ในหน้า MainMenu ที่ไม่มี GameManager
            PlayMainMenuBGM();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnStageClear -= HandleStageClear;
        }
        if (Instance == this) Instance = null;
    }

    private void HandleStateChanged(GameState state)
    {
        SetRopePullSound(false);
        switch (state)
        {
            case GameState.MainMenu:
                PlayMainMenuBGM();
                break;
            case GameState.Playing:
                PlayBGM(GetCurrentSeasonBGM(), true);
                break;
            case GameState.BonusStage:
                PlayBGM(GetCurrentBonusBGM(), true);
                break;
        }
    }

    /// <summary>
    /// เลือกเพลง BGM ตามฤดูของ Scene ปัจจุบัน (Autumn / Spring / Winter / Summer)
    /// รองรับทั้งการใส่แยกช่องตามฤดู และการใส่ช่อง bgmMain ประจำแต่ละ Scene
    /// </summary>
    public AudioClip GetCurrentSeasonBGM()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.IndexOf("Autumn", StringComparison.OrdinalIgnoreCase) >= 0 && bgmAutumn != null)
            return bgmAutumn;
        if (sceneName.IndexOf("Spring", StringComparison.OrdinalIgnoreCase) >= 0 && bgmSpring != null)
            return bgmSpring;
        if (sceneName.IndexOf("Winter", StringComparison.OrdinalIgnoreCase) >= 0 && bgmWinter != null)
            return bgmWinter;
        if (sceneName.IndexOf("Summer", StringComparison.OrdinalIgnoreCase) >= 0 && bgmSummer != null)
            return bgmSummer;

        // เช็คจากเลขด่านปัจจุบันเป็นลำดับถัดมา (1=Autumn, 2=Spring, 3=Winter, 4=Summer)
        if (GameManager.Instance != null)
        {
            int seasonIndex = (GameManager.Instance.CurrentStage - 1) % 4;
            switch (seasonIndex)
            {
                case 0: if (bgmAutumn != null) return bgmAutumn; break;
                case 1: if (bgmSpring != null) return bgmSpring; break;
                case 2: if (bgmWinter != null) return bgmWinter; break;
                case 3: if (bgmSummer != null) return bgmSummer; break;
            }
        }

        // ถ้าตั้งไว้ในช่อง bgmMain ของ Scene นั้นโดยตรง ก็ใช้ bgmMain ได้เลย
        return bgmMain;
    }

    /// <summary>
    /// เลือกเพลงสำหรับด่านโบนัส (แยกโบนัสปาเนื้อ / โบนัสยิงผลไม้ หรือใช้ bgmBonus ร่วมกัน)
    /// </summary>
    public AudioClip GetCurrentBonusBGM()
    {
        int stage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : 2;
        bool isMeatBonus = ((stage / 2) % 2 == 1);

        if (isMeatBonus && bgmBonusMeat != null)
            return bgmBonusMeat;
        if (!isMeatBonus && bgmBonusFruit != null)
            return bgmBonusFruit;
        if (bgmBonus != null)
            return bgmBonus;

        return GetCurrentSeasonBGM();
    }

    private void HandleGameOver()
    {
        SetRopePullSound(false);
        PlayBGM(bgmGameOver, false);
    }

    private void HandleStageClear()
    {
        SetRopePullSound(false);
        PlayBGM(bgmStageClear, false);
    }

    // ==================== BGM Controls ====================

    public void PlayBGM(AudioClip clip, bool loop)
    {
        if (bgmSource == null) return;
        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }
        if (bgmSource.isPlaying && bgmSource.clip == clip && bgmSource.loop == loop)
            return;

        bgmSource.volume = bgmVolume;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public AudioClip StageIntroBGM => bgmStageIntro;
    public AudioClip Stage2IntroBGM => bgmStage2Intro;
    public AudioClip StageClearBGM => bgmStageClear;
    public AudioClip Stage2ClearBGM => bgmStage2Clear != null ? bgmStage2Clear : bgmStageClear;

    public AudioClip GetStageIntroBGM(int stage = 1)
    {
        if (stage == 2 && bgmStage2Intro != null)
            return bgmStage2Intro;
        return bgmStageIntro;
    }

    public void PlayMainMenuBGM() => PlayBGM(bgmMainMenu != null ? bgmMainMenu : GetCurrentSeasonBGM(), true);

    public void PlayStageIntroBGM(int stage = 1)
    {
        AudioClip clip = GetStageIntroBGM(stage);
        if (clip != null)
            PlayBGM(clip, false);
        else
            ResumeStageBGM();
    }

    public void PlayStageClearBGM()
    {
        if (bgmStageClear != null)
            PlayBGM(bgmStageClear, false);
    }

    public void ResumeStageBGM()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsBonus)
            PlayBGM(GetCurrentBonusBGM(), true);
        else
            PlayBGM(GetCurrentSeasonBGM(), true);
    }

    // ==================== SFX Controls ====================

    public void PlaySFX(AudioClip clip) => PlayOneShot(clip);

    // ฝั่งแม่หมู & ลูกหมู
    public void PlayArrowFire() => PlayOneShot(seArrowFire);
    public void PlayMeatPickup() => PlayOneShot(seMeatPickup);
    public void PlayMeatThrow() => PlayOneShot(seMeatThrow);
    public void PlayMeatHit() => PlayOneShot(seMeatHit);
    public void PlayMeatBounce() => PlayOneShot(seMeatBounce != null ? seMeatBounce : seShieldBlock);
    public void PlayPlayerFall() => PlayOneShot(sePlayerFall);
    public void PlayExtraLife() => PlayOneShot(seExtraLife != null ? seExtraLife : seMeatPickup);

    /// <summary>
    /// เปิด/ปิดเสียงดึงเชือกรอกของลูกหมูเมื่อกระเช้ากำลังขยับขึ้น-ลง
    /// </summary>
    public void SetRopePullSound(bool isPulling)
    {
        if (seRopePull == null) return;

        if (ropeLoopSource != null)
        {
            if (isPulling)
            {
                if (!ropeLoopSource.isPlaying || ropeLoopSource.clip != seRopePull)
                {
                    ropeLoopSource.clip = seRopePull;
                    ropeLoopSource.volume = ropePullVolume;
                    ropeLoopSource.loop = true;
                    ropeLoopSource.Play();
                }
            }
            else if (ropeLoopSource.isPlaying)
            {
                ropeLoopSource.Stop();
            }
        }
        else if (isPulling && Time.time - lastRopeStepTime >= 0.18f)
        {
            lastRopeStepTime = Time.time;
            PlayOneShot(seRopePull);
        }
    }

    // ฝั่งหมาป่า & ลูกโป่ง
    public void PlayBalloonInflate() => PlayOneShot(seBalloonInflate);
    public void PlayBalloonPop() => PlayOneShot(seBalloonPop);
    public void PlayBalloonDamage() => PlayOneShot(seBalloonDamage != null ? seBalloonDamage : seBalloonPop);
    public void PlayWolfFall() => PlayOneShot(seWolfFall);
    public void PlayShieldBlock() => PlayOneShot(seShieldBlock);
    public void PlayLadderClimb() => PlayOneShot(seLadderClimb);
    public void PlayWolfBite() => PlayOneShot(seWolfBite);

    // ฝั่งก้อนหิน, หินถล่ม & ผลไม้
    public void PlayRockThrow() => PlayOneShot(seRockThrow);
    public void PlayRockHit() => PlayOneShot(seRockHit);
    public void PlayRockDestroy() => PlayOneShot(seRockDestroy != null ? seRockDestroy : seBalloonPop);
    public void PlayBoulderPush() => PlayOneShot(seBoulderPush);
    public void PlayBoulderFall() => PlayOneShot(seBoulderFall);
    public void PlayBoulderHit() => PlayOneShot(seBoulderHit != null ? seBoulderHit : seBoulderFall);
    public void PlayFruitDrop() => PlayOneShot(seFruitDrop != null ? seFruitDrop : seRockThrow);
    public void PlayFruitHit() => PlayOneShot(seFruitHit != null ? seFruitHit : seRockDestroy);

    // ฝั่ง UI
    public void PlayButtonClick() => PlayOneShot(seButtonClick);

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
