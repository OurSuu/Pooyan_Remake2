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
    [Tooltip("เพลงประจำด่าน 4 - ฤดูฝน / ฤดูร้อน (Rainy / Summer Scene)")]
    [SerializeField] private AudioClip bgmSummer;
    [Tooltip("เพลงประจำฉากนี้ (ใส่ช่องนี้ได้ถ้าตั้งแยกแต่ละ Scene หรือใช้เป็นเพลงสำรอง)")]
    [SerializeField] private AudioClip bgmMain;

    [Header("2. เพลงด่านโบนัส & เสียงคัทซีนทั้งหมด (Bonus & Cutscene Audio — จัดการที่นี่ที่เดียว)")]
    [Tooltip("เพลงประกอบด่านโบนัสหลัก (ใช้ได้ทั้งโบนัสเนื้อและโบนัสผลไม้)")]
    [SerializeField] private AudioClip bgmBonus;
    [Tooltip("เพลงด่านโบนัสปาเนื้อโดยเฉพาะ (ถ้าไม่ใส่ จะใช้ bgmBonus แทน)")]
    [SerializeField] private AudioClip bgmBonusMeat;
    [Tooltip("เพลงด่านโบนัสยิงผลไม้โดยเฉพาะ (ถ้าไม่ใส่ จะใช้ bgmBonus แทน)")]
    [SerializeField] private AudioClip bgmBonusFruit;
    [Tooltip("เพลง / เสียงคัทซีนหน้า MainMenu (เล่นผ่าน cutsceneSource แยกจาก BGM หลัก)")]
    [SerializeField] private AudioClip bgmMainMenuCutscene;
    [Tooltip("เสียงตอนลูกหมูปล่อยลูกโป่งลอยขึ้นบนหน้า MainMenu")]
    [SerializeField] private AudioClip seMainMenuBalloonRelease;
    [Tooltip("เสียงตอนลูกโป่งลอยไปเป็นปุ่ม Select หน้า MainMenu")]
    [SerializeField] private AudioClip seMainMenuBalloonDock;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเปิดด่าน 1 (Autumn - ลูกหมูโดนลักพาตัว)")]
    [SerializeField] private AudioClip bgmStageIntro;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเปิดด่าน 2 (Spring - วิ่งไล่กวดหมาป่า)")]
    [SerializeField] private AudioClip bgmStage2Intro;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเปิดด่าน 4 (Rainy - ถ้าไม่ใส่ จะใช้ bgmStage2Intro แทน)")]
    [SerializeField] private AudioClip bgmStage4Intro;
    [Tooltip("เพลงจิงเกิ้ลตอนเคลียร์ด่าน (Stage Clear)")]
    [SerializeField] private AudioClip bgmStageClear;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเคลียร์ด่าน 2 รวมญาติ (ถ้าไม่ใส่ จะใช้ bgmStageClear แทน)")]
    [SerializeField] private AudioClip bgmStage2Clear;
    [Tooltip("เพลงจิงเกิ้ลตอนคัทซีนเคลียร์ด่าน 4 รวมญาติ (ถ้าไม่ใส่ จะใช้ bgmStage2Clear แทน)")]
    [SerializeField] private AudioClip bgmStage4Clear;
    [Tooltip("เพลงคัทซีนสำรองทั่วไป (หากช่องคัทซีนด้านบนเว้นว่างไว้ จะใช้เพลงนี้)")]
    [SerializeField] private AudioClip bgmDefaultCutscene;
    [Tooltip("ให้เพลงคัทซีนด่านเล่นวนลูปหรือไม่ (ถ้าไม่ติ๊กจะเล่นรอบเดียว)")]
    [SerializeField] private bool loopCutsceneBGM = false;
    [Tooltip("หยุดพักเพลง MainMenu BGM ชั่วคราวระหว่างเล่นเสียงคัทซีนหน้า MainMenu แล้วค่อยกลับมาเล่นต่อเมื่อจบคัทซีน")]
    [SerializeField] private bool pauseMainMenuBgmDuringCutscene = true;
    [Tooltip("ระยะเวลาค่อยๆ ลดเสียงคัทซีน (Fade Out) ช่วงใกล้จบคัทซีน (วินาที)")]
    [Range(0.2f, 3.0f)] [SerializeField] private float cutsceneFadeOutDuration = 1.1f;
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

    [Header("6. เสียง UI & การวิ่งบนพื้น (Menu & Ground Run SFX)")]
    [Tooltip("เสียงกดปุ่มเมนู / เลือกตัวอักษร")]
    [SerializeField] private AudioClip seButtonClick;
    [Tooltip("เสียงตอนวิ่ง / เดินบนพื้น (หมาป่า, ลูกหมู, แม่หมู — เล่นผ่าน groundRunSource แยกจาก Cutscene และ BGM)")]
    [SerializeField] private AudioClip seGroundRun;
    [Tooltip("ระยะห่างขั้นต่ำระหว่างเสียงก้าวเท้าบนพื้นแต่ละครั้ง (วินาที)")]
    [Range(0.05f, 0.6f)] [SerializeField] private float groundRunMinInterval = 0.14f;
    [Tooltip("เปิดเสียงฝีเท้าตอนวิ่งบนพื้นระหว่างเล่น Cutscene หรือไม่")]
    [SerializeField] private bool playCutsceneGroundRunSFX = true;
    [Tooltip("ระยะห่างจังหวะเสียงวิ่งบนพื้นระหว่าง Cutscene (วินาที)")]
    [Range(0.08f, 0.6f)] [SerializeField] private float cutsceneFootstepInterval = 0.22f;

    [Header("7. ลำโพงและการตั้งค่าระดับเสียง (Audio Sources & Volume)")]
    [SerializeField] private AudioSource bgmSource;       // ลำโพงเพลง BGM หลัก
    [SerializeField] private AudioSource sfxSource;       // ลำโพงเสียงเอฟเฟกต์ทั่วไป
    [SerializeField] private AudioSource ropeLoopSource;  // ลำโพงแยกสำหรับเสียงดึงเชือกต่อเนื่อง
    [SerializeField] private AudioSource cutsceneSource;  // ลำโพงแยกสำหรับเสียง Cutscene
    [SerializeField] private AudioSource groundRunSource; // ลำโพงแยกสำหรับเสียงวิ่งบนพื้น
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;
    [Range(0f, 1f)] [SerializeField] private float ropePullVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float cutsceneVolume = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float groundRunVolume = 0.85f;

    public enum GroundRunnerRole
    {
        Wolf = 0,
        Mama = 1,
        Piglet1 = 2,
        Piglet2 = 3
    }

    private struct RunnerAcousticProfile
    {
        public float basePitch;
        public float volumeWeight;
        public float cadenceScale;
        public float phaseStagger;
        public float loopOffsetNorm;
    }

    private float lastRopeStepTime;
    private const int GroundRunVoiceCount = 4;
    private AudioSource[] groundRunVoices;
    private readonly float[] voiceHeartbeat = { -999f, -999f, -999f, -999f };
    private readonly float[] voiceNextStepTime = new float[GroundRunVoiceCount];
    private readonly int[] voiceStepParity = new int[GroundRunVoiceCount];

    [Tooltip("ความแรงการแยกเสียงซ้าย-ขวา (Stereo Pan) ตามตำแหน่งตัวละครที่วิ่งบนหญ้า")]
    [Range(0f, 1f)] [SerializeField] private float grassStereoPanStrength = 0.85f;

    [Header("8. เสียงเป่าลูกโป่งมีมิติ (Dimensional Balloon Inflate SFX)")]
    [Range(0f, 1f)] [SerializeField] private float balloonInflateVolume = 0.90f;
    [Tooltip("ระยะเวลาไล่ระดับความตึง/ความถี่ของเสียงเป่าลูกโป่งให้ฟังดูพองขยายมีมิติ")]
    [Range(0.15f, 1.0f)] [SerializeField] private float balloonInflateSweepDuration = 0.48f;
    [Range(0f, 1f)] [SerializeField] private float balloonInflateStereoPan = 0.85f;

    private const int BalloonInflateVoiceCount = 4;
    private AudioSource[] balloonInflateVoices;
    private Coroutine[] balloonInflateRoutines;
    private int nextBalloonInflateVoice = 0;
    private bool audioSourcesInitialized = false;
    private Coroutine cutsceneFadeRoutine;
    private bool isCutsceneFadingOut = false;
    private Coroutine bgmFadeInRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureAudioSources();
        EnsureSynthesizedFallbacks();
        PreloadAllSFXClips();
    }

    // สร้างเสียง 8-bit สำรองอัตโนมัติสำหรับช่อง SFX ที่ยังไม่ได้ใส่ AudioClip ใน Inspector (หากใส่ไฟล์จริงไว้แล้วจะใช้ไฟล์จริงเสมอ)
    private void EnsureSynthesizedFallbacks()
    {
        if (seMeatThrow == null) seMeatThrow = RetroSFXSynth.GetMeatThrow();
        if (seMeatHit == null) seMeatHit = RetroSFXSynth.GetMeatHit();
        if (seMeatBounce == null) seMeatBounce = RetroSFXSynth.GetMeatBounce();
        if (seExtraLife == null) seExtraLife = RetroSFXSynth.GetExtraLife();
        if (seBalloonInflate == null) seBalloonInflate = RetroSFXSynth.GetBalloonInflate();
        if (seBoulderFall == null) seBoulderFall = RetroSFXSynth.GetBoulderFall();
        if (seButtonClick == null) seButtonClick = RetroSFXSynth.GetButtonClick();
        if (seGroundRun == null) seGroundRun = RetroSFXSynth.GetGroundRun();
    }

    // โหลดข้อมูลเสียง SFX ทั้งหมดเข้าหน่วยความจำล่วงหน้าตอนเริ่มด่าน ป้องกันอาการกระตุก (Disk Decode Spike) ตอนยิงธนูโดนเป้าครั้งแรก
    private void PreloadAllSFXClips()
    {
        PreloadClip(bgmMainMenuCutscene);
        PreloadClip(seMainMenuBalloonRelease);
        PreloadClip(seMainMenuBalloonDock);
        PreloadClip(seArrowFire);
        PreloadClip(seMeatPickup);
        PreloadClip(seMeatThrow);
        PreloadClip(seMeatHit);
        PreloadClip(seMeatBounce);
        PreloadClip(seRopePull);
        PreloadClip(sePlayerFall);
        PreloadClip(seExtraLife);
        PreloadClip(seBalloonInflate);
        PreloadClip(seBalloonPop);
        PreloadClip(seBalloonDamage);
        PreloadClip(seWolfFall);
        PreloadClip(seShieldBlock);
        PreloadClip(seLadderClimb);
        PreloadClip(seWolfBite);
        PreloadClip(seRockThrow);
        PreloadClip(seRockHit);
        PreloadClip(seRockDestroy);
        PreloadClip(seBoulderPush);
        PreloadClip(seBoulderFall);
        PreloadClip(seBoulderHit);
        PreloadClip(seFruitDrop);
        PreloadClip(seFruitHit);
        PreloadClip(seButtonClick);
        PreloadClip(seGroundRun);
    }

    private static void PreloadClip(AudioClip clip)
    {
        if (clip != null && !clip.preloadAudioData && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }
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

        if (cutsceneSource == null)
            cutsceneSource = sources.Length > 3 ? sources[3] : gameObject.AddComponent<AudioSource>();

        if (groundRunSource == null)
            groundRunSource = sources.Length > 4 ? sources[4] : gameObject.AddComponent<AudioSource>();

        if (!audioSourcesInitialized)
        {
            audioSourcesInitialized = true;

            bgmSource.loop = false;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.mute = false;
            bgmSource.volume = bgmVolume;

            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.mute = false;
            sfxSource.volume = sfxVolume;

            ropeLoopSource.loop = true;
            ropeLoopSource.playOnAwake = false;
            ropeLoopSource.spatialBlend = 0f;
            ropeLoopSource.mute = false;
            ropeLoopSource.volume = ropePullVolume;

            cutsceneSource.loop = false;
            cutsceneSource.playOnAwake = false;
            cutsceneSource.spatialBlend = 0f;
            cutsceneSource.mute = false;
            cutsceneSource.volume = cutsceneVolume;

            groundRunSource.loop = false;
            groundRunSource.playOnAwake = false;
            groundRunSource.spatialBlend = 0f;
            groundRunSource.mute = false;
            groundRunSource.volume = groundRunVolume;
        }

        if (groundRunVoices == null || groundRunVoices.Length != GroundRunVoiceCount)
        {
            var latestSources = GetComponents<AudioSource>();
            groundRunVoices = new AudioSource[GroundRunVoiceCount];
            groundRunVoices[0] = groundRunSource;
            for (int i = 1; i < GroundRunVoiceCount; i++)
            {
                AudioSource extra = (latestSources.Length > 4 + i)
                    ? latestSources[4 + i]
                    : gameObject.AddComponent<AudioSource>();
                extra.loop = false;
                extra.playOnAwake = false;
                extra.spatialBlend = 0f;
                extra.mute = false;
                extra.volume = groundRunVolume;
                groundRunVoices[i] = extra;
            }
        }

        if (balloonInflateVoices == null || balloonInflateVoices.Length != BalloonInflateVoiceCount)
        {
            Transform poolTransform = transform.Find("[BalloonInflateVoices]");
            GameObject poolObj = poolTransform != null ? poolTransform.gameObject : new GameObject("[BalloonInflateVoices]");
            if (poolTransform == null)
            {
                poolObj.transform.SetParent(transform, false);
            }

            balloonInflateVoices = new AudioSource[BalloonInflateVoiceCount];
            balloonInflateRoutines = new Coroutine[BalloonInflateVoiceCount];
            var existingPoolSources = poolObj.GetComponents<AudioSource>();
            for (int i = 0; i < BalloonInflateVoiceCount; i++)
            {
                AudioSource voice = i < existingPoolSources.Length
                    ? existingPoolSources[i]
                    : poolObj.AddComponent<AudioSource>();
                voice.loop = false;
                voice.playOnAwake = false;
                voice.spatialBlend = 0f;
                voice.mute = false;
                voice.volume = balloonInflateVolume;
                balloonInflateVoices[i] = voice;
            }
        }

        EnsureAudioListener();
    }

    private void EnsureAudioListener()
    {
        if (FindFirstObjectByType<AudioListener>() != null) return;
        Camera mainCam = Camera.main;
        if (mainCam != null)
            mainCam.gameObject.AddComponent<AudioListener>();
        else
            gameObject.AddComponent<AudioListener>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnStageClear += HandleStageClear;

            // ตรวจสอบสถานะเริ่มต้น: หากอยู่ใน Cutscene หรือกำลังเตรียมเริ่มคัทซีน อย่าเพิ่งเปิด MainMenu BGM หรือ Season BGM ทันที
            if (GameManager.Instance.CurrentState != GameState.Cutscene)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                bool isMainMenuScene = sceneName.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isMainMenuScene)
                {
                    PlayMainMenuBGM();
                }
                else if (GameManager.Instance.IsPlaying)
                {
                    PlayBGM(GetCurrentSeasonBGM(), true);
                }
            }
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
            case GameState.Cutscene:
                // ระหว่างเล่นคัทซีน ปล่อยให้ StageCutsceneController ควบคุมการเปิด/ปิดเพลงคัทซีนเอง
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
        if ((sceneName.IndexOf("Rainy", StringComparison.OrdinalIgnoreCase) >= 0 ||
             sceneName.IndexOf("Summer", StringComparison.OrdinalIgnoreCase) >= 0) && bgmSummer != null)
            return bgmSummer;

        // เช็คจากเลขด่านปัจจุบันเป็นลำดับถัดมา (1=Autumn, 2=Spring, 3=Winter, 4=Rainy, 5=Summer)
        if (GameManager.Instance != null)
        {
            int seasonIndex = (GameManager.Instance.CurrentStage - 1) % 5;
            switch (seasonIndex)
            {
                case 0: if (bgmAutumn != null) return bgmAutumn; break;
                case 1: if (bgmSpring != null) return bgmSpring; break;
                case 2: if (bgmWinter != null) return bgmWinter; break;
                case 3:
                case 4:
                    if (bgmSummer != null) return bgmSummer;
                    if (bgmMain != null) return bgmMain;
                    if (bgmSpring != null) return bgmSpring;
                    break;
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
        if (bgmFadeInRoutine != null)
        {
            StopCoroutine(bgmFadeInRoutine);
            bgmFadeInRoutine = null;
        }
        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }
        if (bgmSource.isPlaying && bgmSource.clip == clip && bgmSource.loop == loop)
        {
            bgmSource.volume = bgmVolume;
            return;
        }

        bgmSource.volume = bgmVolume;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmFadeInRoutine != null)
        {
            StopCoroutine(bgmFadeInRoutine);
            bgmFadeInRoutine = null;
        }
        if (bgmSource != null) bgmSource.Stop();
    }

    public AudioClip StageIntroBGM => bgmStageIntro;
    public AudioClip Stage2IntroBGM => bgmStage2Intro;
    public AudioClip StageClearBGM => bgmStageClear;
    public AudioClip Stage2ClearBGM => bgmStage2Clear != null ? bgmStage2Clear : bgmStageClear;
    public AudioClip MainMenuCutsceneBGM => bgmMainMenuCutscene;
    public float CutsceneFadeOutDuration => cutsceneFadeOutDuration;

    public void PauseBGM()
    {
        if (bgmFadeInRoutine != null)
        {
            StopCoroutine(bgmFadeInRoutine);
            bgmFadeInRoutine = null;
        }
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void UnPauseOrPlayMainMenuBGM()
    {
        if (bgmSource == null) return;
        AudioClip targetClip = bgmMainMenu != null ? bgmMainMenu : GetCurrentSeasonBGM();
        if (bgmSource.clip == targetClip && targetClip != null)
        {
            bgmSource.loop = false;
            if (!bgmSource.isPlaying)
            {
                bgmSource.volume = 0f;
                bgmSource.UnPause();
                StartBgmFadeIn(0.65f);
            }
            return;
        }
        PlayMainMenuBGM();
        if (bgmSource.isPlaying)
        {
            bgmSource.volume = 0f;
            StartBgmFadeIn(0.65f);
        }
    }

    private void StartBgmFadeIn(float duration)
    {
        if (bgmFadeInRoutine != null) StopCoroutine(bgmFadeInRoutine);
        if (gameObject.activeInHierarchy)
            bgmFadeInRoutine = StartCoroutine(FadeInBgmRoutine(duration));
        else if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    private System.Collections.IEnumerator FadeInBgmRoutine(float duration)
    {
        float dur = Mathf.Max(0.1f, duration);
        float elapsed = 0f;
        while (elapsed < dur && bgmSource != null && bgmSource.isPlaying)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t);
            yield return null;
        }
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        bgmFadeInRoutine = null;
    }

    // ==================== Cutscene Audio Controls (จัดการทั้งหมดจาก AudioManager Prefab ที่เดียว) ====================

    public void PlayStageCutsceneBGM(int stage, bool isClear)
    {
        AudioClip clipToPlay;
        if (isClear)
        {
            clipToPlay = (stage >= 4 && bgmStage4Clear != null) ? bgmStage4Clear : Stage2ClearBGM;
        }
        else
        {
            clipToPlay = (stage >= 4 && bgmStage4Intro != null) ? bgmStage4Intro : GetStageIntroBGM(stage);
        }

        if (clipToPlay == null || clipToPlay == GetCurrentSeasonBGM())
        {
            clipToPlay = bgmDefaultCutscene;
            if (clipToPlay == GetCurrentSeasonBGM()) clipToPlay = null;
        }

        StopBGM();
        if (clipToPlay != null)
        {
            PlayCutsceneAudio(clipToPlay, loopCutsceneBGM);
        }
    }

    public bool PlayMainMenuCutsceneAudio()
    {
        AudioClip menuClip = bgmMainMenuCutscene != null ? bgmMainMenuCutscene : bgmDefaultCutscene;
        if (menuClip == null) return false;

        bool pausedBgm = false;
        if (pauseMainMenuBgmDuringCutscene)
        {
            PauseBGM();
            pausedBgm = true;
        }

        PlayCutsceneAudio(menuClip, false);
        return pausedBgm;
    }

    public void PlayMainMenuBalloonReleaseSFX() => PlayCutsceneSFX(seMainMenuBalloonRelease);

    public void PlayMainMenuBalloonDockSFX() => PlayCutsceneSFX(seMainMenuBalloonDock);

    public void NotifyCutsceneRunner(GroundRunnerRole role, float worldX, float pitchScale = 1f, float cadenceScale = 1f, float volumeMultiplier = 1f)
    {
        if (!playCutsceneGroundRunSFX) return;
        float baseInterval = Mathf.Max(0.08f, cutsceneFootstepInterval * cadenceScale);
        TriggerGrassRunnerVoice((int)role, role, worldX, baseInterval, pitchScale, volumeMultiplier, 0);
    }

    public void NotifyCutsceneGroundRun(float intervalScale = 1f, float pitchScale = 1f)
    {
        NotifyCutsceneRunner(GroundRunnerRole.Mama, 0f, pitchScale, intervalScale, 1f);
    }

    public void NotifyWolfGrassRun(int wolfId, float worldX, float pitchScale = 1f, float stepIntervalOverride = -1f)
    {
        int hash = wolfId & 0x7FFFFFFF;
        int slot = hash % GroundRunVoiceCount;
        float interval = stepIntervalOverride > 0f ? stepIntervalOverride : Mathf.Max(0.08f, groundRunMinInterval);
        TriggerGrassRunnerVoice(slot, GroundRunnerRole.Wolf, worldX, interval, pitchScale, 0.9f, hash);
    }

    public void NotifyWolfGroundRun(float pitchScale = 1f, float stepIntervalOverride = -1f)
    {
        NotifyWolfGrassRun(0, 0f, pitchScale, stepIntervalOverride);
    }

    public void StopGroundRunAudio()
    {
        for (int i = 0; i < GroundRunVoiceCount; i++)
        {
            voiceHeartbeat[i] = -999f;
            voiceNextStepTime[i] = 0f;
            if (groundRunVoices != null && groundRunVoices[i] != null && groundRunVoices[i].isPlaying && groundRunVoices[i].loop)
            {
                groundRunVoices[i].Stop();
            }
        }
    }

    private void LateUpdate()
    {
        if (groundRunVoices == null) return;
        bool deathPaused = GameManager.Instance != null && GameManager.Instance.DeathFreeze;
        bool timeStopped = Time.timeScale <= 0f;

        for (int i = 0; i < groundRunVoices.Length; i++)
        {
            var src = groundRunVoices[i];
            if (src != null && src.isPlaying && src.loop)
            {
                if (deathPaused || timeStopped || Time.time - voiceHeartbeat[i] > 0.09f)
                {
                    src.Stop();
                }
            }
        }
    }

    public void PlayCutsceneAudio(AudioClip clip, bool loop = false, float volumeScale = 1f)
    {
        EnsureAudioSources();
        if (cutsceneSource == null) return;
        CancelCutsceneFadeOut();
        if (clip == null)
        {
            cutsceneSource.Stop();
            return;
        }
        cutsceneSource.clip = clip;
        cutsceneSource.loop = loop;
        cutsceneSource.volume = Mathf.Clamp01(cutsceneVolume * volumeScale);
        cutsceneSource.Play();
    }

    public void PlayCutsceneSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        if (cutsceneSource == null) return;
        if (!isCutsceneFadingOut)
            cutsceneSource.volume = cutsceneVolume;
        cutsceneSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void FadeOutCutsceneAudio(float durationOverride = -1f)
    {
        if (cutsceneSource == null || !cutsceneSource.isPlaying)
        {
            CancelCutsceneFadeOut();
            return;
        }
        if (isCutsceneFadingOut) return;

        float dur = durationOverride > 0f ? durationOverride : cutsceneFadeOutDuration;
        if (dur <= 0.05f || !gameObject.activeInHierarchy)
        {
            StopCutsceneAudio();
            return;
        }

        isCutsceneFadingOut = true;
        cutsceneFadeRoutine = StartCoroutine(FadeOutCutsceneRoutine(dur));
    }

    private System.Collections.IEnumerator FadeOutCutsceneRoutine(float duration)
    {
        float startVol = cutsceneSource != null ? cutsceneSource.volume : cutsceneVolume;
        float elapsed = 0f;

        while (elapsed < duration && cutsceneSource != null && cutsceneSource.isPlaying)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cutsceneSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        if (cutsceneSource != null)
        {
            cutsceneSource.Stop();
            cutsceneSource.volume = cutsceneVolume;
        }
        isCutsceneFadingOut = false;
        cutsceneFadeRoutine = null;
    }

    private void CancelCutsceneFadeOut()
    {
        if (cutsceneFadeRoutine != null)
        {
            StopCoroutine(cutsceneFadeRoutine);
            cutsceneFadeRoutine = null;
        }
        isCutsceneFadingOut = false;
        if (cutsceneSource != null)
        {
            cutsceneSource.volume = cutsceneVolume;
        }
    }

    public void StopCutsceneAudio()
    {
        CancelCutsceneFadeOut();
        if (cutsceneSource != null && cutsceneSource.isPlaying)
        {
            cutsceneSource.Stop();
        }
    }

    public AudioClip GetStageIntroBGM(int stage = 1)
    {
        if (stage % 2 == 0 && bgmStage2Intro != null)
            return bgmStage2Intro;
        return bgmStageIntro;
    }

    public void PlayMainMenuBGM() => PlayBGM(bgmMainMenu != null ? bgmMainMenu : GetCurrentSeasonBGM(), false);

    public void PlayStageIntroBGM(int stage = 1)
    {
        AudioClip clip = GetStageIntroBGM(stage);
        if (clip != null)
            PlayBGM(clip, false);
        else
            StopBGM();
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
    public void PlayBalloonInflate() => PlayBalloonInflateAt(0f, BalloonColorType.AutoByHP, false);

    public void PlayBalloonInflateAt(float worldX, BalloonColorType colorType = BalloonColorType.AutoByHP, bool isBoss = false)
    {
        if (seBalloonInflate == null) return;
        EnsureAudioSources();
        if (balloonInflateVoices == null || balloonInflateVoices.Length == 0)
        {
            PlayOneShot(seBalloonInflate);
            return;
        }

        int voiceIdx = nextBalloonInflateVoice;
        nextBalloonInflateVoice = (nextBalloonInflateVoice + 1) % balloonInflateVoices.Length;
        AudioSource voice = balloonInflateVoices[voiceIdx];
        if (voice == null) return;

        if (balloonInflateRoutines != null && balloonInflateRoutines[voiceIdx] != null)
        {
            StopCoroutine(balloonInflateRoutines[voiceIdx]);
            balloonInflateRoutines[voiceIdx] = null;
        }

        float colorPitch = 1f;
        switch (colorType)
        {
            case BalloonColorType.LightBlue: colorPitch = 0.96f; break;
            case BalloonColorType.YellowOrange: colorPitch = 1.04f; break;
            case BalloonColorType.PinkRed:
            case BalloonColorType.Green: colorPitch = 1.10f; break;
        }
        if (isBoss) colorPitch = 0.85f;

        float basePitch = colorPitch * UnityEngine.Random.Range(0.95f, 1.05f);
        float startPitch = Mathf.Clamp(basePitch * 0.86f, 0.55f, 1.65f);
        float endPitch = Mathf.Clamp(basePitch * 1.15f, 0.65f, 1.85f);

        float pan = Mathf.Clamp(worldX / 7.5f, -1f, 1f) * balloonInflateStereoPan;
        float distanceFade = Mathf.InverseLerp(9.2f, 5.2f, Mathf.Abs(worldX));
        float peakVol = Mathf.Clamp01(balloonInflateVolume * sfxVolume * (isBoss ? 1.12f : 1f) * Mathf.Max(0.45f, distanceFade));

        voice.Stop();
        voice.clip = seBalloonInflate;
        voice.loop = false;
        voice.panStereo = pan;
        voice.pitch = startPitch;
        voice.volume = peakVol * 0.72f;
        voice.Play();

        if (gameObject.activeInHierarchy && balloonInflateRoutines != null)
        {
            balloonInflateRoutines[voiceIdx] = StartCoroutine(SweepBalloonInflateRoutine(voiceIdx, voice, startPitch, endPitch, peakVol));
        }
    }

    private System.Collections.IEnumerator SweepBalloonInflateRoutine(int voiceIdx, AudioSource voice, float startPitch, float endPitch, float peakVol)
    {
        float dur = Mathf.Max(0.15f, balloonInflateSweepDuration);
        float elapsed = 0f;

        while (elapsed < dur && voice != null && voice.isPlaying)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
            voice.pitch = Mathf.Lerp(startPitch, endPitch, t);
            voice.volume = Mathf.Lerp(peakVol * 0.72f, peakVol, t);
            yield return null;
        }

        if (balloonInflateRoutines != null && voiceIdx >= 0 && voiceIdx < balloonInflateRoutines.Length)
        {
            balloonInflateRoutines[voiceIdx] = null;
        }
    }
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
    public void PlayFruitDrop() => PlayOneShot(seFruitDrop);
    public void PlayFruitHit() => PlayOneShot(seFruitHit != null ? seFruitHit : seRockDestroy);

    // ฝั่ง UI & การวิ่งบนพื้น
    public void PlayButtonClick() => PlayOneShot(seButtonClick);

    public void PlayGroundRunStep(float minIntervalOverride = -1f, float pitchScale = 1f)
    {
        float interval = minIntervalOverride > 0f ? minIntervalOverride : Mathf.Max(0.05f, groundRunMinInterval);
        TriggerGrassRunnerVoice(0, GroundRunnerRole.Wolf, 0f, interval, pitchScale, 1f, 0);
    }

    private static RunnerAcousticProfile GetRunnerProfile(GroundRunnerRole role)
    {
        switch (role)
        {
            case GroundRunnerRole.Mama:
                return new RunnerAcousticProfile { basePitch = 0.82f, volumeWeight = 1.00f, cadenceScale = 1.08f, phaseStagger = 0.09f, loopOffsetNorm = 0.29f };
            case GroundRunnerRole.Piglet1:
                return new RunnerAcousticProfile { basePitch = 1.18f, volumeWeight = 0.68f, cadenceScale = 0.84f, phaseStagger = 0.05f, loopOffsetNorm = 0.57f };
            case GroundRunnerRole.Piglet2:
                return new RunnerAcousticProfile { basePitch = 1.29f, volumeWeight = 0.62f, cadenceScale = 0.79f, phaseStagger = 0.14f, loopOffsetNorm = 0.81f };
            default:
                return new RunnerAcousticProfile { basePitch = 0.94f, volumeWeight = 0.90f, cadenceScale = 0.92f, phaseStagger = 0.00f, loopOffsetNorm = 0.00f };
        }
    }

    private void TriggerGrassRunnerVoice(int slot, GroundRunnerRole role, float worldX, float baseInterval, float pitchScale, float volumeMultiplier, int instanceVariant)
    {
        if (seGroundRun == null) return;
        EnsureAudioSources();
        if (groundRunVoices == null || slot < 0 || slot >= groundRunVoices.Length) return;
        AudioSource src = groundRunVoices[slot];
        if (src == null) return;

        RunnerAcousticProfile profile = GetRunnerProfile(role);
        float now = Time.time;
        bool wasPreviouslyActive = (now - voiceHeartbeat[slot]) <= 0.12f;
        voiceHeartbeat[slot] = now;

        // แยกมิติซ้าย-ขวา (Stereo Pan) และค่อยๆ จางเมื่อวิ่งเข้าใกล้/พ้นขอบจอ
        float pan = Mathf.Clamp(worldX / 7.5f, -1f, 1f) * grassStereoPanStrength;
        float edgeAttenuation = Mathf.InverseLerp(8.5f, 4.8f, Mathf.Abs(worldX));
        if (edgeAttenuation <= 0.001f) return;

        int activeCount = 0;
        for (int i = 0; i < voiceHeartbeat.Length; i++)
        {
            if (now - voiceHeartbeat[i] <= 0.12f) activeCount++;
        }
        float ensembleMix = activeCount > 1 ? Mathf.Lerp(1f, 1f / Mathf.Sqrt(activeCount), 0.6f) : 1f;
        float variantPitch = instanceVariant > 0 ? 1f + (((instanceVariant % 5) - 2) * 0.03f) : 1f;

        // 1) คลิปเสียงวิ่งยาว (> 0.45s เช่น ไฟล์ Loop วิ่งบนหญ้า): แยก voice ต่อตัวละครพร้อม offset ไม่ตรงกัน
        if (seGroundRun.length > 0.45f)
        {
            src.panStereo = pan;
            src.pitch = Mathf.Clamp(profile.basePitch * pitchScale * variantPitch, 0.55f, 1.65f);
            src.volume = Mathf.Clamp01(groundRunVolume * profile.volumeWeight * edgeAttenuation * ensembleMix * volumeMultiplier);

            if (!src.isPlaying || src.clip != seGroundRun || !src.loop)
            {
                src.clip = seGroundRun;
                src.loop = true;
                float normOffset = (profile.loopOffsetNorm + (instanceVariant % 7) * 0.13f) % 1f;
                src.time = seGroundRun.length * normOffset;
                src.Play();
            }
            return;
        }

        // 2) คลิปเสียงก้าวเท้าสั้น (<= 0.45s): เหลื่อมจังหวะเหยียบแต่ละตัว + สลับน้ำหนักซ้าย/ขวาทุกก้าว
        if (src.loop && src.isPlaying)
        {
            src.Stop();
            src.loop = false;
        }

        if (!wasPreviouslyActive || voiceNextStepTime[slot] <= 0f)
        {
            float stagger = profile.phaseStagger + (instanceVariant > 0 ? (instanceVariant % 4) * 0.035f : 0f);
            voiceNextStepTime[slot] = now + stagger;
        }

        if (now < voiceNextStepTime[slot]) return;

        float actorInterval = Mathf.Max(0.065f, baseInterval * profile.cadenceScale);
        voiceNextStepTime[slot] = now + actorInterval + UnityEngine.Random.Range(-0.014f, 0.018f);

        bool leftFoot = (voiceStepParity[slot]++ & 1) == 0;
        float stepPitchJitter = (leftFoot ? 0.96f : 1.04f) * UnityEngine.Random.Range(0.97f, 1.03f);
        float stepVolJitter = leftFoot ? 0.90f : 1.05f;

        src.volume = 1f;
        src.panStereo = pan;
        src.pitch = Mathf.Clamp(profile.basePitch * pitchScale * variantPitch * stepPitchJitter, 0.5f, 1.85f);
        float finalVol = Mathf.Clamp01(groundRunVolume * profile.volumeWeight * edgeAttenuation * ensembleMix * volumeMultiplier * stepVolJitter);
        src.PlayOneShot(seGroundRun, finalVol);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip, 1f);
    }
}
