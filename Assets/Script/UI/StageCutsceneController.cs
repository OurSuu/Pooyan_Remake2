using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ระบบคัทซีนในฉากจริง (In-Scene Ground Cutscenes) ตามต้นฉบับ Pooyan (Arcade 1982 / NES 1985):
/// 1. คัทซีนเกิดขึ้นที่พื้นล่างเสมอ (Bottom Ground: Y ≈ -3.75) ปรับแต่งได้ใน Inspector
/// 2. ไม่ใช้ตัวละครหลัก (Player) มาเล่น — Player และเชือกกระเช้าด้านบนจะถูกซ่อนไว้สนิท (SetActive=false)
/// 3. ไม่สามารถกดข้ามคัทซีนได้ (ต้องดูจนจบ)
/// 4. ลำดับของคัทซีน:
///    - ก่อนเริ่มด่าน 1 (Autumn): ลูกหมู 2 ตัวกำลังเดินกลับบ้านไปทางขวา -> หมาป่าวิ่งชาร์จเข้ามาจากทางซ้าย -> ลูกหมูตัวแรกวิ่งเข้าบ้านทัน แต่ตัวที่ 2 โดนหมาป่าจับอุ้มไว้ (ลูกหมูอยู่หลังหมาป่า) -> หมาป่าหันกลับวิ่งหนีไปทางซ้าย -> แม่หมูวิ่งตามไล่กวดไปทางซ้าย
///    - ก่อนเริ่มด่าน 2 (Spring): หมาป่าวิ่งหนีตาตั้งไปทางซ้าย (หันหน้าไปทางซ้าย) -> แม่หมูกับลูกหมู 2 ตัววิ่งไล่กวดหมาป่าไปทางซ้ายอย่างช้าๆ ชิลๆ คอมเมดี้ (หมาป่า -> ลูกหมู 1 -> ลูกหมู 2 -> แม่หมู)
///    - ก่อนเริ่มด่านโบนัส (หลังชนะบอสด่าน 2): กระเช้าแม่หมูเลื่อนลงมาถึงพื้นล่าง -> แม่หมูเดินออกมาหาลูกหมู 2 ตัว -> ดีใจด้วยกันอย่างมีความสุข -> แม่หมูเดินมุ่งหน้าไปทางทิศหมาป่า (ซ้าย) ต่อ -> เข้าสู่ด่านโบนัส
///    - หลังด่านโบนัสไปด่าน 3 (Winter): ไม่มีคัทซีน เข้าสู่เกมด่าน 3 ทันที
/// </summary>
public class StageCutsceneController : MonoBehaviour
{
    public static StageCutsceneController Instance { get; private set; }

    [Header("Cutscene Settings (ปรับแต่งได้ตามใจชอบ)")]
    [Tooltip("เปิด/ปิดคัทซีน")]
    public bool enableCutscenes = true;

    [Tooltip("ระดับความสูงของพื้นล่างที่ตัวละครวิ่ง (ปรับเพิ่ม-ลดตามชอบ เช่น -3.75 ถึง -3.85)")]
    public float groundY = -3.75f;

    [Tooltip("ขนาดสเกลของตัวละครคัทซีน (มาตรฐานเกมคือ 0.26)")]
    public float actorScale = 0.26f;

    [Header("Character Ground Offsets (ปรับแต่งความสูงรายตัวละคร)")]
    [Tooltip("ออฟเซ็ตความสูงแม่หมูจาก groundY")]
    public float mamaYOffset = 0.70f;
    [Tooltip("ออฟเซ็ตความสูงหมาป่าจาก groundY")]
    public float wolfYOffset = 0.66f;
    [Tooltip("ออฟเซ็ตความสูงลูกหมูจาก groundY")]
    public float pigletYOffset = 0.0f;
    [Tooltip("ออฟเซ็ตความสูงกระเช้าจาก groundY")]
    public float basketYOffset = 0.35f;

    [Header("Cutscene Animator Controllers (ลาก Animator Controller ใส่ได้เมื่อทำเสร็จ หากยังไม่ใส่จะแสดงภาพตัวอย่าง)")]
    [Tooltip("Animator Controller ของหมาป่าในคัทซีน")]
    public RuntimeAnimatorController wolfCutsceneAnimator;
    [Tooltip("Animator Controller ของแม่หมูในคัทซีน")]
    public RuntimeAnimatorController mamaCutsceneAnimator;
    [Tooltip("Animator Controller ของลูกหมูในคัทซีน")]
    public RuntimeAnimatorController pigletCutsceneAnimator;

    [Header("Cutscene Animation State Names (มี Cutscene_ นำหน้าเพื่อไม่ให้ซ้ำกับท่าในเกม)")]
    [Tooltip("ชื่อ State ท่าวิ่งของหมาป่าใน Animator (เช่น Cutscene_Run หรือ Cutscene_WolfRun)")]
    public string wolfRunState = "Cutscene_Run";
    [Tooltip("ชื่อ State ท่าเดินของแม่หมูใน Animator (เช่น Cutscene_Walk หรือ Cutscene_MamaWalk)")]
    public string mamaWalkState = "Cutscene_Walk";
    [Tooltip("ชื่อ State ท่าเดินของลูกหมูใน Animator (เช่น Cutscene_Walk หรือ Cutscene_PigletWalk)")]
    public string pigletWalkState = "Cutscene_Walk";
    [Tooltip("ชื่อ State ท่ากระโดดดีใจของลูกหมูใน Animator (เช่น Cutscene_Jump หรือ Cutscene_PigletJump)")]
    public string pigletJumpState = "Cutscene_Jump";

    [Header("Cutscene Audio (ใส่ไฟล์เสียง BGM แยกตามฉากได้)")]
    [Tooltip("เพลง BGM คัทซีนก่อนเริ่มด่าน 1 (Autumn - ลูกหมูโดนลักพาตัว)")]
    public AudioClip stage1IntroBGM;
    [Tooltip("เพลง BGM คัทซีนก่อนเริ่มด่าน 2 (Spring - วิ่งไล่กวดหมาป่า)")]
    public AudioClip stage2IntroBGM;
    [Tooltip("เพลง BGM คัทซีนเคลียร์ด่าน 2 รวมญาติก่อนเข้าโบนัส (Stage Clear / Reunion)")]
    public AudioClip stage2ClearBGM;
    [Tooltip("เพลง BGM คัทซีนสำรองทั่วไป (หากช่องเฉพาะด้านบนว่างไว้ จะใช้เพลงนี้)")]
    public AudioClip defaultCutsceneBGM;
    [Tooltip("ให้เพลงคัทซีนเล่นวนลูปหรือไม่ (ถ้าไม่ติ๊กจะเล่นรอบเดียวแบบ Jingle)")]
    public bool loopCutsceneBGM = false;

    public bool IsPlayingCutscene { get; private set; }

    // สไปรต์ตัวอย่างที่ระบบดึงจากเกมมาแสดงให้อัตโนมัติ (ไม่รกใน Inspector)
    private Sprite sampleMamaSprite;
    private Sprite samplePigletSprite;
    private Sprite sampleWolfSprite;
    private Sprite sampleBasketSprite;

    // ตัวละครคัทซีนเฉพาะ (Dedicated Cutscene Actors)
    private GameObject cutsceneRoot;
    private SpriteRenderer cutsceneMama;
    private SpriteRenderer cutscenePiglet1;
    private SpriteRenderer cutscenePiglet2;
    private SpriteRenderer cutsceneWolf;
    private SpriteRenderer cutsceneBasket;

    private Coroutine activeCutsceneRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (IsPlayingCutscene)
        {
            SetGameplayPlayerVisible(true);
        }
        if (cutsceneRoot != null) Destroy(cutsceneRoot);
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// เช็คว่าด่านนี้มีคัทซีนก่อนเริ่มด่านหรือไม่ (เฉพาะด่าน 1 และด่าน 2)
    /// </summary>
    public bool HasIntroCutsceneForStage(int stage)
    {
        if (!enableCutscenes) return false;
        return stage == 1 || stage == 2;
    }

    /// <summary>
    /// เช็คว่าด่านนี้มีคัทซีนตอนจบด่านหรือไม่ (เฉพาะด่าน 2 ที่ผ่านแล้วจะเข้าสู่ Bonus Stage)
    /// </summary>
    public bool HasClearCutsceneForStage(int stage)
    {
        if (!enableCutscenes) return false;
        return stage == 2;
    }

    /// <summary>
    /// สั่งเล่นคัทซีนก่อนเริ่มด่าน
    /// </summary>
    public void PlayStageIntroCutscene(int stage, Action onComplete)
    {
        if (!HasIntroCutsceneForStage(stage))
        {
            onComplete?.Invoke();
            return;
        }

        if (activeCutsceneRoutine != null) StopCoroutine(activeCutsceneRoutine);

        if (stage == 1)
            activeCutsceneRoutine = StartCoroutine(Stage1IntroRoutine(onComplete));
        else if (stage == 2)
            activeCutsceneRoutine = StartCoroutine(Stage2IntroRoutine(onComplete));
        else
            onComplete?.Invoke();
    }

    /// <summary>
    /// สั่งเล่นคัทซีนตอนจบด่าน (Reunion Cutscene หลังจบด่าน 2 ก่อนเข้าโบนัส)
    /// </summary>
    public void PlayStageClearCutscene(int stage, Action onComplete)
    {
        if (!HasClearCutsceneForStage(stage))
        {
            onComplete?.Invoke();
            return;
        }

        if (activeCutsceneRoutine != null) StopCoroutine(activeCutsceneRoutine);
        activeCutsceneRoutine = StartCoroutine(Stage2ClearReunionRoutine(onComplete));
    }

    // =========================================================================================
    // จัดการการซ่อน/แสดงตัว Player และเชือกกระเช้าด้านบน
    // =========================================================================================
    private void SetGameplayPlayerVisible(bool visible)
    {
        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            player.gameObject.SetActive(visible);
            if (visible)
            {
                player.Respawn();
            }
        }

        // ปิด/เปิดเส้นเชือกกระเช้า (LineRenderer) ทั้งหมดในฉาก ให้ซ่อนสนิทตอนเล่นคัทซีน และเปิดกลับมาเมื่อเริ่มเล่นเกม
        var lineRenderers = FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var lr in lineRenderers)
        {
            if (lr != null)
            {
                lr.enabled = visible;
                lr.gameObject.SetActive(visible);
            }
        }

        var ropes = FindObjectsByType<ElevatorRope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rope in ropes)
        {
            if (rope != null)
            {
                rope.SetRopeVisible(visible);
            }
        }
    }

    // =========================================================================================
    // จัดการเพลง BGM ระหว่างเล่นคัทซีน
    // =========================================================================================
    private AudioSource internalAudioSource;

    private void PlayCutsceneBGM(AudioClip clip, int stage = 1, bool isClear = false)
    {
        AudioClip clipToPlay = clip != null ? clip : defaultCutsceneBGM;

        // ดักกรณีเผลอใส่คลิปเพลงเดียวกับเพลงประจำฤดู/ด่าน (เช่น เผลอใส่ Autumn_BGM แทนที่จะเป็น Cutscene_Autumn)
        bool isAccidentallySeasonalBGM = clipToPlay != null && AudioManager.Instance != null && clipToPlay == AudioManager.Instance.GetCurrentSeasonBGM();

        if ((clipToPlay == null || isAccidentallySeasonalBGM) && AudioManager.Instance != null)
        {
            AudioClip properClip = isClear ? AudioManager.Instance.Stage2ClearBGM : AudioManager.Instance.GetStageIntroBGM(stage);
            if (properClip != null && properClip != AudioManager.Instance.GetCurrentSeasonBGM())
            {
                clipToPlay = properClip;
            }
            else if (isAccidentallySeasonalBGM)
            {
                clipToPlay = null;
            }
        }

        if (clipToPlay != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(clipToPlay, loopCutsceneBGM);
            }
            else
            {
                if (internalAudioSource == null)
                {
                    internalAudioSource = gameObject.GetComponent<AudioSource>();
                    if (internalAudioSource == null) internalAudioSource = gameObject.AddComponent<AudioSource>();
                }
                internalAudioSource.clip = clipToPlay;
                internalAudioSource.loop = loopCutsceneBGM;
                internalAudioSource.volume = 0.8f;
                internalAudioSource.Play();
            }
        }
        else
        {
            // ถ้าไม่มีเพลงคัทซีนจริงๆ ให้หยุด BGM ชั่วคราวระหว่างคัทซีน (อย่าไปเปิดเพลงด่านล่วงหน้า)
            AudioManager.Instance?.StopBGM();
        }
    }

    private void StopCutsceneBGM()
    {
        if (internalAudioSource != null && internalAudioSource.isPlaying)
        {
            internalAudioSource.Stop();
        }
    }

    // =========================================================================================
    // 1. คัทซีนก่อนเริ่มด่าน 1 (Stage 1 Intro):
    //    - ลูกหมู 2 ตัวกำลังเดินกลับบ้านไปทางขวา (ไม่ใช่เดินไปหาหมาป่า)
    //    - หมาป่าวิ่งพุ่งเข้ามาข้างหลังจากทางซ้าย
    //    - ลูกหมูตัวแรกวิ่งเข้าบ้านทัน รอดปลอดภัย
    //    - ลูกหมูตัวที่ 2 โดนหมาป่าจับอุ้มไว้ (ลูกหมูอยู่หลังหมาป่า)
    //    - หมาป่าหันกลับไปทางซ้ายวิ่งหนีเข้าป่า
    //    - แม่หมูวิ่งตามไล่กวดไปทางซ้าย
    // =========================================================================================
    private IEnumerator Stage1IntroRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;
        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);
        if (cutsceneMama != null) cutsceneMama.gameObject.SetActive(false);

        // เล่นท่าแอนิเมชันสำหรับคัทซีนด่าน 1
        PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutsceneWolf, wolfRunState, wolfCutsceneAnimator);

        PlayCutsceneBGM(stage1IntroBGM, stage: 1, isClear: false);

        float duration = 7.6f;
        float elapsed = 0f;
        bool mamaAnimStarted = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // walkBob เบาๆ นุ่มนวล ไม่ขัดกับอนิเมชันขยับขา
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.04f;

            if (elapsed < 2.5f)
            {
                // เฟส 1 (0 - 2.5s): ลูกหมู 2 ตัวเดินกลับบ้านไปทางขวาอย่างช้าๆ ชิลๆ (หันขวา faceLeft=false)
                // หมาป่าวิ่งชาร์จเข้ามาข้างหลังจากฝั่งซ้าย (หันขวา faceLeft=false)
                float t1 = elapsed / 2.5f;

                // ลูกหมู 1 (ตัวนำ): เดินกลับบ้านไปทางขวา
                float pig1X = Mathf.Lerp(-1.2f, 3.8f, t1);
                SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob, 0f), faceLeft: false, isWolf: false);

                // ลูกหมู 2 (ตัวตามหลัง): เดินตามหลังมาทางขวา
                float pig2X = Mathf.Lerp(-3.2f, 1.2f, t1);
                SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + walkBob, 0f), faceLeft: false, isWolf: false);

                // หมาป่า: วิ่งชาร์จพุ่งตามเข้ามาจากทางซ้าย
                float wolfX = Mathf.Lerp(-8.5f, 0.8f, t1);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: false, isWolf: true);
            }
            else if (elapsed < 3.5f)
            {
                // เฟส 2 (2.5 - 3.5s): หมาป่าตามทันลูกหมูตัวที่ 2! ลูกหมูตัวที่ 1 วิ่งหนีเข้าบ้านทางขวาทัน
                float t2 = (elapsed - 2.5f) / 1.0f;

                // ลูกหมู 1: วิ่งหนีเข้าบ้านไปทางขวาสุดจอ (เมื่อถึงขอบขวาจะหายเข้าบ้านทันที)
                float pig1X = Mathf.Lerp(3.8f, 7.5f, t2);
                SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob * 1.2f, 0f), faceLeft: false, isWolf: false);

                // หมาป่าจับลูกหมู 2 ได้ที่ x = 1.0f แล้วสลับทิศหันกลับไปทางซ้าย (หันซ้าย faceLeft=true)
                SetActor(cutsceneWolf, new Vector3(1.0f, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

                // ลูกหมู 2 ถูกจับตัวดิ้นอยู่ด้านหลังหมาป่า (หันหน้าดิ้นไปทางขวาตรงข้ามหมาป่า)
                SetActor(cutscenePiglet2, new Vector3(1.45f, groundY + pigletYOffset + 0.18f, 0f), faceLeft: false, isWolf: false);
            }
            else
            {
                // เฟส 3 (3.5s เป็นต้นไป): หมาป่าอุ้มลูกหมู 2 วิ่งหนีไปทางซ้าย (ช้าลง นุ่มนวล)
                // 1. หมาป่าวิ่งจาก 1.0f ไป -8.5f (พอถึงขอบซ้าย <= -7.5f จะหายตัวเข้าป่าไปก่อน)
                float tWolf = Mathf.Clamp01((elapsed - 3.5f) / 2.0f);
                float wolfX = Mathf.Lerp(1.0f, -8.5f, tWolf);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

                // 2. ลูกหมู 2: โดนอุ้มอยู่ด้านหลังหมาป่า (x = wolfX + 0.45f)
                // พอพิกัดลูกหมู 2 ถึงขอบซ้าย (<= -7.5f) ก็จะหายตัวเข้าป่าตามหลังหมาป่าทันที (หายหลังหมาป่า และหายก่อนแม่หมู!)
                float pig2X = wolfX + 0.45f;
                if (pig2X <= LEFT_EXIT_X)
                {
                    if (cutscenePiglet2.gameObject.activeSelf) cutscenePiglet2.gameObject.SetActive(false);
                }
                else
                {
                    float struggleBob = Mathf.Sin(elapsed * 18f) * 0.04f;
                    SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + 0.18f + struggleBob, 0f), faceLeft: false, isWolf: false);
                }

                // เฟส 4: แม่หมูวิ่งพุ่งตามออกมาจากบ้านตอน 4.6 วินาที (ไล่ตามหมาป่าไปทางซ้าย)
                // 2. แม่หมูวิ่งกวดตามไปจนพ้นขอบซ้าย <= -7.5f แล้วหายตัวเข้าป่าตามไปเลย
                if (elapsed >= 4.6f)
                {
                    if (!mamaAnimStarted)
                    {
                        mamaAnimStarted = true;
                        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);
                    }
                    float tMama = Mathf.Clamp01((elapsed - 4.6f) / 2.2f);
                    float mamaX = Mathf.Lerp(7.0f, -8.5f, tMama);
                    SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
                }
            }

            yield return null;
        }

        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        StopCutsceneBGM();
        AudioManager.Instance?.ResumeStageBGM();

        onComplete?.Invoke();
    }

    // =========================================================================================
    // 2. คัทซีนก่อนเริ่มด่าน 2 (Stage 2 Intro):
    //    - หมาป่าวิ่งหนีตาตั้งไปทางซ้าย (หันหน้าไปทางซ้ายตามทิศวิ่ง)
    //    - แม่หมูกับลูกหมู 2 ตัว วิ่งไล่กวดหมาป่าไปทางซ้ายแบบช้าๆ คอมเมดี้
    //    - ขบวนวิ่ง: หมาป่า -> ลูกหมู 1 -> ลูกหมู 2 -> แม่หมู
    //    - แต่ละตัวเมื่อวิ่งถึงขอบซ้าย (เข้าป่า) จะหายตัวไปทีละตัวตามลำดับ 1, 2, 3, 4!
    // =========================================================================================
    private IEnumerator Stage2IntroRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;
        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);

        // เล่นท่าแอนิเมชันสำหรับคัทซีนด่าน 2
        PlayActorAnim(cutsceneWolf, wolfRunState, wolfCutsceneAnimator);
        PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);

        PlayCutsceneBGM(stage2IntroBGM, stage: 2, isClear: false);

        // ปรับความเร็วให้ช้าลงตามคำขอ (6.8 วินาที วิ่งไล่กันแบบช้าๆ ชิลๆ คอมเมดี้)
        float duration = 6.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.04f;

            // 1. หมาป่า: วิ่งหนีตาตั้งไปทางซ้าย (ถึงขอบซ้ายแล้วหายตัวไปก่อนเป็นตัวแรก: 1)
            float wolfX = Mathf.Lerp(3.2f, -9.0f, Mathf.Clamp01(t * 1.18f));
            SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

            // 2. ลูกหมู 1 (วิ่งนำหน้าสุด): วิ่งไล่กวดหมาป่าไปทางซ้าย (หายไปเป็นตัวที่: 2)
            float pig1X = Mathf.Lerp(4.8f, -9.0f, Mathf.Clamp01(t * 1.12f));
            SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            // 3. ลูกหมู 2 (วิ่งอยู่หน้าแม่หมู): วิ่งเคียงข้างไปทางซ้าย (หายไปเป็นตัวที่: 3)
            float pig2X = Mathf.Lerp(6.0f, -9.0f, Mathf.Clamp01(t * 1.06f));
            SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            // 4. แม่หมู: วิ่งตามหลังลูกๆ ไล่กวดหมาป่าไปทางซ้าย (หายไปเป็นตัวที่: 4)
            float mamaX = Mathf.Lerp(7.5f, -9.0f, Mathf.Clamp01(t * 1.00f));
            SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            yield return null;
        }

        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        StopCutsceneBGM();
        AudioManager.Instance?.ResumeStageBGM();

        onComplete?.Invoke();
    }

    // =========================================================================================
    // 3. คัทซีนก่อนเข้าด่านโบนัส (Stage 2 Clear Reunion):
    //    - กระเช้าแม่หมูเลื่อนลงมาถึงพื้นล่าง (ช้าๆ นุ่มนวล)
    //    - แม่หมูเดินออกจากกระเช้าไปหาลูกหมู 2 ตัว
    //    - แม่หมูอยู่ดีใจกับลูกหมู 2 ตัวอย่างมีความสุข
    //    - แม่หมูเดินมุ่งหน้าไปทางทิศหมาป่า (ซ้าย) ต่อ -> เข้าสู่ด่านโบนัส
    // =========================================================================================
    private IEnumerator Stage2ClearReunionRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;
        PlayCutsceneBGM(stage2ClearBGM, stage: 2, isClear: true);

        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        Vector3 startGondolaPos = player != null ? player.transform.position : new Vector3(3.81f, 0.88f, 0f);

        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneWolf != null) cutsceneWolf.gameObject.SetActive(false);

        // ลูกหมู 2 ตัวยืนรออยู่ที่พื้นฝั่งซ้าย (หันหน้ามองมาทางขวาหาแม่หมู)
        Vector3 pig1Pos = new Vector3(-0.6f, groundY + pigletYOffset, 0f);
        Vector3 pig2Pos = new Vector3(-1.5f, groundY + pigletYOffset, 0f);
        SetActor(cutscenePiglet1, pig1Pos, faceLeft: false, isWolf: false);
        SetActor(cutscenePiglet2, pig2Pos, faceLeft: false, isWolf: false);

        Vector3 groundGondolaPos = new Vector3(startGondolaPos.x, groundY + basketYOffset, 0f);

        // เฟส 1: กระเช้าเลื่อนลงมาแตะพื้นอย่างนุ่มนวล (2.0 วินาที)
        float elapsed = 0f;
        float lowerDuration = 2.0f;
        while (elapsed < lowerDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lowerDuration);
            Vector3 curGondolaPos = Vector3.Lerp(startGondolaPos, groundGondolaPos, Mathf.SmoothStep(0f, 1f, t));
            if (cutsceneBasket != null)
            {
                cutsceneBasket.gameObject.SetActive(true);
                cutsceneBasket.transform.position = curGondolaPos;
            }
            SetActor(cutsceneMama, new Vector3(curGondolaPos.x, curGondolaPos.y + mamaYOffset - basketYOffset, 0f), faceLeft: true, isWolf: false);
            yield return null;
        }

        // เฟส 2: แม่หมูเดินก้าวออกจากกระเช้าไปหาลูกหมู (1.8 วินาที)
        elapsed = 0f;
        float walkDuration = 1.8f;
        Vector3 mamaExitStart = new Vector3(groundGondolaPos.x, groundY + mamaYOffset, 0f);
        Vector3 mamaTargetPos = new Vector3(0.3f, groundY + mamaYOffset, 0f);
        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);

        while (elapsed < walkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / walkDuration);
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.08f;

            Vector3 curMamaPos = Vector3.Lerp(mamaExitStart, mamaTargetPos, t);
            curMamaPos.y += walkBob;
            SetActor(cutsceneMama, curMamaPos, faceLeft: true, isWolf: false);
            yield return null;
        }

        // เฟส 3: แม่หมูอยู่ดีใจกับลูกหมู 2 ตัว (2.4 วินาที)
        elapsed = 0f;
        float celebrateDuration = 2.4f;
        PlayActorAnim(cutscenePiglet1, pigletJumpState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletJumpState, pigletCutsceneAnimator);

        while (elapsed < celebrateDuration)
        {
            elapsed += Time.deltaTime;

            float jump1 = Mathf.Abs(Mathf.Sin(elapsed * 10f)) * 0.38f;
            float jump2 = Mathf.Abs(Mathf.Sin((elapsed + 0.18f) * 10f)) * 0.35f;

            SetActor(cutscenePiglet1, new Vector3(pig1Pos.x, groundY + pigletYOffset + jump1, 0f), faceLeft: false, isWolf: false);
            SetActor(cutscenePiglet2, new Vector3(pig2Pos.x, groundY + pigletYOffset + jump2, 0f), faceLeft: false, isWolf: false);

            float mamaHop = Mathf.Abs(Mathf.Sin(elapsed * 8f)) * 0.15f;
            SetActor(cutsceneMama, new Vector3(mamaTargetPos.x, groundY + mamaYOffset + mamaHop, 0f), faceLeft: true, isWolf: false);

            yield return null;
        }

        // เฟส 4: แม่หมูเดินมุ่งหน้าไปทางทิศหมาป่า (ทางซ้าย) ต่อ! (2.0 วินาที)
        elapsed = 0f;
        float marchDuration = 2.0f;
        Vector3 mamaForestTarget = new Vector3(-8.5f, groundY + mamaYOffset, 0f);
        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);
        PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);

        while (elapsed < marchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / marchDuration);
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 14f)) * 0.08f;

            // แม่หมู: เดินมุ่งหน้าไปทางซ้ายเข้าสู่รังหมาป่า
            Vector3 curMamaPos = Vector3.Lerp(mamaTargetPos, mamaForestTarget, t);
            curMamaPos.y += walkBob;
            SetActor(cutsceneMama, curMamaPos, faceLeft: true, isWolf: false);

            // ลูกหมู 2 ตัวหันซ้ายมองส่งแม่หมูอย่างภาคภูมิใจ
            float pigBob = Mathf.Abs(Mathf.Sin(elapsed * 8f)) * 0.08f;
            SetActor(cutscenePiglet1, new Vector3(pig1Pos.x, groundY + pigletYOffset + pigBob, 0f), faceLeft: true, isWolf: false);
            SetActor(cutscenePiglet2, new Vector3(pig2Pos.x, groundY + pigletYOffset + pigBob, 0f), faceLeft: true, isWolf: false);

            yield return null;
        }

        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        StopCutsceneBGM();

        onComplete?.Invoke();
    }

    // =========================================================================================
    // สร้างตัวละครคัทซีนเฉพาะ (Dedicated Cutscene Actors)
    // =========================================================================================
    private void EnsureCutsceneActors()
    {
        ExtractSampleVisuals();

        if (cutsceneRoot == null)
        {
            cutsceneRoot = new GameObject("[InSceneCutsceneActors]");
            cutsceneRoot.transform.position = Vector3.zero;
            cutsceneRoot.transform.rotation = Quaternion.identity;
            cutsceneRoot.transform.localScale = Vector3.one;

            cutsceneMama = CreateActor("Cutscene_Mama", cutsceneRoot.transform, sampleMamaSprite, 15, mamaCutsceneAnimator);
            cutscenePiglet1 = CreateActor("Cutscene_Piglet1", cutsceneRoot.transform, samplePigletSprite, 14, pigletCutsceneAnimator);
            cutscenePiglet2 = CreateActor("Cutscene_Piglet2", cutsceneRoot.transform, samplePigletSprite, 14, pigletCutsceneAnimator);
            cutsceneWolf = CreateActor("Cutscene_Wolf", cutsceneRoot.transform, sampleWolfSprite, 16, wolfCutsceneAnimator);
            cutsceneBasket = CreateActor("Cutscene_Basket", cutsceneRoot.transform, sampleBasketSprite, 13);
        }
        else
        {
            UpdateActorAnimator(cutsceneMama, mamaCutsceneAnimator);
            UpdateActorAnimator(cutscenePiglet1, pigletCutsceneAnimator);
            UpdateActorAnimator(cutscenePiglet2, pigletCutsceneAnimator);
            UpdateActorAnimator(cutsceneWolf, wolfCutsceneAnimator);
        }

        cutsceneRoot.SetActive(false);
    }

    private void UpdateActorAnimator(SpriteRenderer actor, RuntimeAnimatorController controller)
    {
        if (actor == null || controller == null) return;
        var anim = actor.GetComponent<Animator>();
        if (anim == null)
        {
            anim = actor.gameObject.AddComponent<Animator>();
        }
        if (anim.runtimeAnimatorController != controller)
        {
            anim.runtimeAnimatorController = controller;
        }
    }

    private SpriteRenderer CreateActor(string name, Transform parent, Sprite sprite, int sortingOrder, RuntimeAnimatorController animController = null)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(actorScale, actorScale, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;

        if (animController != null)
        {
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;
        }

        return sr;
    }

    /// <summary>
    /// สั่งเล่นท่าแอนิเมชันของตัวละครคัทซีนตามชื่อ State ใน Animator (หากใส่ Animator Controller ไว้)
    /// </summary>
    private void PlayActorAnim(SpriteRenderer actor, string stateName, RuntimeAnimatorController fallbackController = null)
    {
        if (actor == null) return;

        // สำคัญมาก: ต้องเปิดให้ GameObject Active ก่อนเสมอ! ไม่งั้น Animator จะไม่อัปเดตสถานะและ HasState จะคืนค่า false
        if (!actor.gameObject.activeSelf)
        {
            actor.gameObject.SetActive(true);
        }

        var anim = actor.GetComponent<Animator>();
        if (anim == null && fallbackController != null)
        {
            anim = actor.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = fallbackController;
        }
        else if (anim != null && fallbackController != null && anim.runtimeAnimatorController != fallbackController)
        {
            anim.runtimeAnimatorController = fallbackController;
        }

        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(stateName))
        {
            anim.enabled = true;
            if (anim.HasState(0, Animator.StringToHash(stateName)))
            {
                anim.Play(stateName, 0, 0f);
                anim.Update(0f); // บังคับให้ Animator คำนวณเฟรมแรกทันที
            }
            else
            {
                Debug.LogWarning($"[Cutscene] Animator Controller '{anim.runtimeAnimatorController.name}' does not contain state: '{stateName}'");
            }
        }
    }

    private const float LEFT_EXIT_X = -7.5f;
    private const float RIGHT_EXIT_X = 7.2f;

    private void SetActor(SpriteRenderer actor, Vector3 worldPos, bool faceLeft, bool isWolf = false)
    {
        if (actor == null) return;

        // ถ้าวิ่งเลยขอบซ้ายของจอเข้าป่า -> หายตัวไปเลยทันที
        if (faceLeft && worldPos.x <= LEFT_EXIT_X)
        {
            if (actor.gameObject.activeSelf) actor.gameObject.SetActive(false);
            return;
        }

        // ถ้าวิ่งเลยขอบขวาของจอเข้าบ้าน -> หายตัวเข้าบ้านไปเลยทันที
        if (!faceLeft && worldPos.x >= RIGHT_EXIT_X)
        {
            if (actor.gameObject.activeSelf) actor.gameObject.SetActive(false);
            return;
        }

        if (!actor.gameObject.activeSelf) actor.gameObject.SetActive(true);

        actor.transform.position = worldPos;

        // สไปรต์ต้นฉบับทุกตัว (หมาป่า, แม่หมู, ลูกหมู) วาดหันหน้าไปทางขวา
        // - วิ่งไปทางซ้าย (faceLeft = true) -> flipX = true เพื่อหันซ้าย
        // - วิ่งไปทางขวา (faceLeft = false) -> flipX = false เพื่อหันขวาตามสไปรต์ต้นฉบับ
        actor.flipX = faceLeft;
    }

    /// <summary>
    /// ดึงสไปรต์ตัวอย่างจากในโปรเจกต์มาใส่ให้อัตโนมัติในเบื้องต้น (ไม่ต้องลากใส่ใน Inspector ให้รก)
    /// </summary>
    private void ExtractSampleVisuals()
    {
        // 1. ดึงสไปรต์แม่หมู
        if (sampleMamaSprite == null)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                foreach (var sr in pc.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.gameObject.name.Contains("Mama") && sr.sprite != null)
                    {
                        sampleMamaSprite = sr.sprite;
                        break;
                    }
                }
            }
        }

        // 2. ดึงสไปรต์กระเช้า
        if (sampleBasketSprite == null)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                foreach (var sr in pc.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite != null && (sr.gameObject.name.Contains("5798") || sr.gameObject.name.Contains("Buttom") || sr.gameObject.name.Contains("Basket")))
                    {
                        sampleBasketSprite = sr.sprite;
                        break;
                    }
                }
            }
        }

        // 3. ดึงสไปรต์หมาป่า
        if (sampleWolfSprite == null)
        {
            var ws = FindFirstObjectByType<WolfSpawner>(FindObjectsInactive.Include);
            if (ws != null && ws.WolfPrefab != null)
            {
                foreach (var sr in ws.WolfPrefab.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite != null && !sr.name.Contains("Balloon") && !sr.name.Contains("Shield"))
                    {
                        sampleWolfSprite = sr.sprite;
                        break;
                    }
                }

                if (wolfCutsceneAnimator == null)
                {
                    var anim = ws.WolfPrefab.GetComponentInChildren<Animator>();
                    if (anim != null && anim.runtimeAnimatorController != null)
                    {
                        wolfCutsceneAnimator = anim.runtimeAnimatorController;
                    }
                }
            }
        }

        // 4. ดึงสไปรต์ลูกหมู
        if (samplePigletSprite == null)
        {
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (sr.name.Contains("Piglet") && sr.sprite != null)
                {
                    samplePigletSprite = sr.sprite;
                    break;
                }
            }
        }
    }
}
