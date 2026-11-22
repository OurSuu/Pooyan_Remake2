using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Stage 1 Intro Cutscene Speed (ปรับความเร็วคัทซีนด่าน Autumn)")]
    [Tooltip("ตัวคูณความเร็วคัทซีนด่าน 1 Autumn (1.0 = ปกติ, >1.0 = เร็วขึ้น, <1.0 = ช้าลง)")]
    public float stage1CutsceneSpeed = 1.0f;

    [Header("Stage 2 / 4 Chase Cutscene Settings (ปรับระยะห่างและความเร็วคัทซีนวิ่งไล่ ด่าน Spring / Rainy)")]
    [Tooltip("ระยะห่างเพิ่มเติมระหว่างหมาป่ากับลูกหมูตัวแรก (ยิ่งมาก หมาป่ายิ่งวิ่งนำห่างออกไปทางซ้าย)")]
    public float chaseWolfExtraGap = 1.6f;
    [Tooltip("ตัวคูณระยะห่างระหว่าง ลูกหมู 1 - ลูกหมู 2 - แม่หมู (1.0 = ค่ามาตรฐาน, >1.0 = ห่างกันมากขึ้น)")]
    public float chasePursuerSpacingMultiplier = 1.0f;
    [Tooltip("ระยะเวลาพื้นฐานของคัทซีนวิ่งไล่ด่าน 2 / 4 (วินาที ยิ่งน้อยยิ่งวิ่งเร็ว)")]
    public float chaseCutsceneDuration = 4.6f;
    [Tooltip("ตัวคูณความเร็วคัทซีนวิ่งไล่ด่าน 2 / 4 (1.0 = ปกติ, >1.0 = เร็วขึ้น)")]
    public float chaseCutsceneSpeed = 1.0f;
    [Tooltip("ความเร็วท่าทางการขยับขาของตัวละครในคัทซีนวิ่งไล่")]
    public float chaseAnimSpeed = 1.25f;

    [Header("Stage 2 / 4 Clear Reunion Cutscene (คัทซีนลงกระเช้า -> วิ่งรวมตัวเข้าโบนัส)")]
    [Tooltip("ตัวคูณความเร็วรวมทั้งคัทซีนลงกระเช้า-เข้าโบนัส (1.0 = ปกติ, >1.0 = เร็วขึ้น)")]
    public float clearCutsceneSpeed = 1.0f;
    [Tooltip("เวลาเลื่อนกระเช้าลงพื้น (วินาที)")]
    public float clearBasketLowerDuration = 2.2f;
    [Tooltip("ความเร็วเดิน/วิ่งช่วงแรกของแม่หมูหลังลงจากกระเช้า")]
    public float clearMamaSlowSpeed = 1.25f;
    [Tooltip("ความเร็ววิ่งไล่ตามของลูกหมูจากด้านหลังเพื่อเข้าขบวน")]
    public float clearPigletCatchUpSpeed = 2.05f;
    [Tooltip("ความเร็วตอนวิ่งเร็วขึ้นพร้อมกันทั้งแม่หมูและลูกหมูหลังระยะเข้ากันได้")]
    public float clearGroupFastSpeed = 5.2f;
    [Tooltip("ระยะห่างเรียงแถวระหว่าง แม่หมู -> ลูกหมู 1 -> ลูกหมู 2")]
    public float clearFormationSpacing = 1.15f;

    [Header("Main Menu Attract Cutscene (คัทซีนหน้า MainMenu: หมาป่า -> แม่หมู -> ลูกหมูปล่อยลูกโป่งเป็น Select Cursor)")]
    [Tooltip("เปิดให้เล่นคัทซีนหน้า MainMenu")]
    public bool playMainMenuLoop = false;
    [Tooltip("เล่นวนลูปซ้ำเรื่อยๆ หรือไม่ (ค่าเริ่มต้น false = เล่นรอบเดียวพอ)")]
    public bool mainMenuLoopForever = false;
    [Tooltip("ความเร็วในการวิ่งผ่านหน้าจอของคัทซีนหน้า MainMenu (หน่วย/วินาที)")]
    public float mainMenuWalkSpeed = 3.1f;
    [Tooltip("ตัวคูณความเร็วรวมของคัทซีนหน้า MainMenu (1.0 = ปกติ, >1.0 = เร็วขึ้น)")]
    public float mainMenuCutsceneSpeed = 1.0f;
    [Tooltip("ความเร็วท่าทางแอนิเมชันขยับขาของคัทซีนหน้า MainMenu")]
    public float mainMenuAnimSpeed = 1.15f;
    [Tooltip("ระยะห่างเรียงแถวระหว่าง แม่หมู -> ลูกหมู 1 -> ลูกหมู 2 หน้า MainMenu")]
    public float mainMenuFormationSpacing = 1.15f;
    [Tooltip("ระยะเวลาที่ลูกโป่งลอยจากมือลูกหมูขึ้นไปเป็น UI Select Cursor (วินาที)")]
    public float mainMenuBalloonFloatDuration = 1.65f;
    [Tooltip("ตำแหน่งออฟเซ็ตของลูกโป่งเทียบกับตัวลูกหมูขณะถืออยู่ในมือ (ปรับ X/Y ให้ตรงมือใน Inspector ได้)")]
    public Vector2 mainMenuHeldBalloonOffset = new Vector2(-0.35f, 0.26f);
    [Tooltip("ระยะห่างที่หมาป่าตัวหน้าวิ่งนำหน้าแม่หมูในหน้า MainMenu")]
    public float mainMenuWolfLeadGap = 1.85f;
    [Tooltip("ใช้โทนสีและขนาดบอสหมาป่า (ชมพู & ตัวใหญ่กว่า) หรือหมาป่าปกติ (ค่าเริ่มต้น false = หมาป่าปกติ)")]
    public bool mainMenuWolfIsBoss = false;
    [Tooltip("สีเมื่อเปิดโหมดบอสหมาป่าในคัทซีนหน้า MainMenu")]
    public Color mainMenuBossWolfColor = new Color(1f, 0.68f, 0.85f, 1f);
    [Tooltip("ตัวคูณขนาดเมื่อเปิดโหมดบอสหมาป่าหน้า MainMenu")]
    public float mainMenuBossWolfScaleMultiplier = 1.15f;
    [Tooltip("เวลาพักระหว่างรอบก่อนวิ่งเข้ามาใหม่ที่หน้า MainMenu (กรณีเปิด Loop Forever)")]
    public float mainMenuLoopDelay = 0.8f;
    [Tooltip("พรีแฟบลูกโป่งจริงของเกม (มี Shader ของลูกโป่ง) — ถ้าใส่ไว้จะใช้ Prefab นี้แทนสไปรท์ธรรมดา")]
    public GameObject mainMenuBalloonPrefab;
    [Tooltip("สไปรท์ลูกโป่งสำรอง (กรณีไม่ได้ใส่ mainMenuBalloonPrefab)")]
    public Sprite mainMenuBalloonSprite;

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
    [Tooltip("ชื่อ State ท่าหมาป่าอุ้มลูกหมูหนีใน Animator (เช่น CutScene_StealPiglet)")]
    public string wolfStealState = "CutScene_StealPiglet";
    [Tooltip("ซ่อน cutscenePiglet2 ทันทีเมื่อเล่นท่า CutScene_StealPiglet (เพราะสไปรท์อุ้มลูกหมูมีถุงลูกหมูอยู่แล้ว)")]
    public bool hidePiglet2DuringSteal = true;
    [Tooltip("ชื่อ State ท่าเดินของแม่หมูใน Animator (เช่น Cutscene_Walk หรือ Cutscene_MamaWalk)")]
    public string mamaWalkState = "Cutscene_Walk";
    [Tooltip("ชื่อ State ท่าเดินของลูกหมูใน Animator (เช่น Cutscene_Walk หรือ Cutscene_PigletWalk)")]
    public string pigletWalkState = "Cutscene_Walk";
    [Tooltip("ชื่อ State ท่ากระโดดดีใจของลูกหมูใน Animator (เช่น Cutscene_Jump หรือ Cutscene_PigletJump)")]
    public string pigletJumpState = "Cutscene_Jump";

    // (เสียงคัทซีนทั้งหมดและเสียงวิ่งบนพื้น ถูกย้ายไปรวมไว้ใน Prefab AudioManager ที่เดียวแล้ว)

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
    private SpriteRenderer cutsceneBalloon;

    private Coroutine activeCutsceneRoutine;

    private void Awake()
    {
        Instance = this;
        if (IsBonusSceneActive())
        {
            enableCutscenes = false;
        }
    }

    private void Start()
    {
        if (playMainMenuLoop && enableCutscenes && !IsBonusSceneActive())
        {
            if (activeCutsceneRoutine != null) StopCoroutine(activeCutsceneRoutine);
            activeCutsceneRoutine = StartCoroutine(MainMenuLoopCutsceneRoutine());
        }
    }

    private static bool IsBonusSceneActive()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return !string.IsNullOrEmpty(sceneName) &&
               sceneName.IndexOf("Bonus", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsActiveSceneNamed(string keyword)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return !string.IsNullOrEmpty(sceneName) &&
               sceneName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsAutumnScene() => IsActiveSceneNamed("Autumn");
    private static bool IsChaseCutsceneScene() => IsActiveSceneNamed("Spring") || IsActiveSceneNamed("Rainy");

    private void OnDestroy()
    {
        StopCutsceneBGM(immediate: true);
        if (IsPlayingCutscene)
        {
            SetGameplayPlayerVisible(true);
        }
        if (cutsceneRoot != null) Destroy(cutsceneRoot);
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// เช็คว่าด่านนี้มีคัทซีนก่อนเริ่มด่านหรือไม่ (อิงตามธีมของ Scene เพื่อรองรับทั้งด่าน 1-5 และด่านสุ่มหลังจบด่าน 5)
    /// </summary>
    public bool HasIntroCutsceneForStage(int stage)
    {
        if (!enableCutscenes || IsBonusSceneActive()) return false;
        return IsAutumnScene() || IsChaseCutsceneScene();
    }

    /// <summary>
    /// เช็คว่าด่านนี้มีคัทซีนตอนจบด่านหรือไม่ (ด่านคู่: Spring / Rainy ก่อนเข้า Bonus Stage)
    /// </summary>
    public bool HasClearCutsceneForStage(int stage)
    {
        if (!enableCutscenes || IsBonusSceneActive()) return false;
        return (stage % 2 == 0) && IsChaseCutsceneScene();
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

        if (IsAutumnScene())
            activeCutsceneRoutine = StartCoroutine(Stage1IntroRoutine(onComplete));
        else if (IsChaseCutsceneScene())
            activeCutsceneRoutine = StartCoroutine(StageChaseIntroRoutine(stage, onComplete));
        else
            onComplete?.Invoke();
    }

    /// <summary>
    /// สั่งเล่นคัทซีนตอนจบด่าน (Reunion Cutscene หลังจบด่าน 2 และด่าน 4 ก่อนเข้าโบนัส)
    /// </summary>
    public void PlayStageClearCutscene(int stage, Action onComplete)
    {
        if (!HasClearCutsceneForStage(stage))
        {
            onComplete?.Invoke();
            return;
        }

        if (activeCutsceneRoutine != null)
        {
            StopCoroutine(activeCutsceneRoutine);
            activeCutsceneRoutine = null;
        }
        activeCutsceneRoutine = StartCoroutine(StageClearReunionRoutine(stage, onComplete));
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

        SetGameplayHudVisible(visible);
    }

    private void SetGameplayHudVisible(bool visible)
    {
        var ui = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui != null)
        {
            ui.SetGameplayHudVisible(visible);
        }
    }

    // =========================================================================================
    // จัดการเสียงคัทซีนและเสียงฝีเท้าผ่าน AudioManager Prefab (รวมไว้ที่เดียว)
    // =========================================================================================
    private static void ReportCutsceneRunner(AudioManager.GroundRunnerRole role, SpriteRenderer actor, bool isRunning = true, float pitchScale = 1f, float cadenceScale = 1f)
    {
        if (!isRunning || actor == null || !actor.gameObject.activeInHierarchy || AudioManager.Instance == null) return;
        AudioManager.Instance.NotifyCutsceneRunner(role, actor.transform.position.x, pitchScale, cadenceScale);
    }

    private static void TryBeginCutsceneFadeOut(ref bool triggered, bool shouldStartFade)
    {
        if (!triggered && shouldStartFade)
        {
            triggered = true;
            AudioManager.Instance?.FadeOutCutsceneAudio();
        }
    }

    private static float GetCutsceneFadeLeadTime()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.CutsceneFadeOutDuration : 1.1f;
    }

    private static void StopCutsceneBGM(bool immediate = false)
    {
        AudioManager.Instance?.StopGroundRunAudio();
        if (immediate)
            AudioManager.Instance?.StopCutsceneAudio();
        else
            AudioManager.Instance?.FadeOutCutsceneAudio();
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
        if (cutsceneBalloon != null) cutsceneBalloon.gameObject.SetActive(false);

        // เล่นท่าแอนิเมชันสำหรับคัทซีนด่าน 1
        float speedScale = Mathf.Max(0.1f, stage1CutsceneSpeed);
        PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutsceneWolf, wolfRunState, wolfCutsceneAnimator);
        SetActorAnimSpeed(cutscenePiglet1, speedScale);
        SetActorAnimSpeed(cutscenePiglet2, speedScale);
        SetActorAnimSpeed(cutsceneWolf, speedScale);

        AudioManager.Instance?.PlayStageCutsceneBGM(stage: 1, isClear: false);

        float duration = 7.6f;
        float elapsed = 0f;
        bool mamaAnimStarted = false;
        bool stealAnimStarted = false;
        bool cutsceneFadeTriggered = false;

        while (elapsed < duration)
        {
            speedScale = Mathf.Max(0.1f, stage1CutsceneSpeed);
            elapsed += Time.deltaTime * speedScale;
            // walkBob เบาๆ นุ่มนวล ไม่ขัดกับอนิเมชันขยับขา
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.04f;

            // เมื่อหมาป่าตามทันลูกหมู 2 แล้ว (t >= 2.5s) ให้สลับเล่นท่าอุ้มลูกหมูหนี (CutScene_StealPiglet)
            if (elapsed >= 2.5f && !stealAnimStarted)
            {
                stealAnimStarted = true;
                PlayActorAnim(cutsceneWolf, wolfStealState, wolfCutsceneAnimator);
                if (hidePiglet2DuringSteal && cutscenePiglet2 != null && cutscenePiglet2.gameObject.activeSelf)
                {
                    cutscenePiglet2.gameObject.SetActive(false);
                }
            }

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

                // ลูกหมู 2 ถูกจับตัวดิ้นอยู่ด้านหลังหมาป่า (ซ่อนเมื่อใช้สไปรท์อุ้มลูกหมูในถุงแล้ว)
                if (!hidePiglet2DuringSteal)
                {
                    SetActor(cutscenePiglet2, new Vector3(1.45f, groundY + pigletYOffset + 0.18f, 0f), faceLeft: false, isWolf: false);
                }
            }
            else
            {
                // เฟส 3 (3.5s เป็นต้นไป): หมาป่าอุ้มลูกหมู 2 วิ่งหนีไปทางซ้าย (ช้าลง นุ่มนวล)
                // 1. หมาป่าวิ่งจาก 1.0f ไป -8.5f (พอถึงขอบซ้าย <= -7.5f จะหายตัวเข้าป่าไปก่อน)
                float tWolf = Mathf.Clamp01((elapsed - 3.5f) / 2.0f);
                float wolfX = Mathf.Lerp(1.0f, -8.5f, tWolf);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

                // 2. ลูกหมู 2: โดนอุ้มอยู่ด้านหลังหมาป่า (x = wolfX + 0.45f) เฉพาะกรณีไม่ได้ซ่อนไว้ในสไปรท์ถุงอุ้ม
                if (!hidePiglet2DuringSteal)
                {
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
                }

                // เฟส 4: แม่หมูวิ่งพุ่งตามออกมาจากบ้านตอน 4.6 วินาที (ไล่ตามหมาป่าไปทางซ้าย)
                // 2. แม่หมูวิ่งกวดตามไปจนพ้นขอบซ้าย <= -7.5f แล้วหายตัวเข้าป่าตามไปเลย
                if (elapsed >= 4.6f)
                {
                    if (!mamaAnimStarted)
                    {
                        mamaAnimStarted = true;
                        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);
                        SetActorAnimSpeed(cutsceneMama, speedScale);
                    }
                    float tMama = Mathf.Clamp01((elapsed - 4.6f) / 2.2f);
                    float mamaX = Mathf.Lerp(7.0f, -8.5f, tMama);
                    SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
                }
            }

            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet1, cutscenePiglet1, true, elapsed >= 2.5f ? 1.12f : 1f, elapsed >= 2.5f ? 0.78f : 1f);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet2, cutscenePiglet2, elapsed < 2.5f);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Wolf, cutsceneWolf, elapsed < 2.5f || elapsed >= 3.5f, elapsed < 2.5f ? 1.05f : 0.96f, 0.92f);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Mama, cutsceneMama, elapsed >= 4.6f, 1.06f, 0.85f);
            TryBeginCutsceneFadeOut(ref cutsceneFadeTriggered, elapsed >= duration - GetCutsceneFadeLeadTime());

            yield return null;
        }

        SetActorAnimSpeed(cutsceneWolf, 1f);
        SetActorAnimSpeed(cutscenePiglet1, 1f);
        SetActorAnimSpeed(cutscenePiglet2, 1f);
        SetActorAnimSpeed(cutsceneMama, 1f);
        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        StopCutsceneBGM();
        AudioManager.Instance?.ResumeStageBGM();

        onComplete?.Invoke();
    }

    // =========================================================================================
    // 2. คัทซีนวิ่งไล่กวดก่อนเริ่มด่าน 2 (ด่าน 1 -> 2) และด่าน 4 (ด่าน 3 -> 4):
    //    - หมาป่าวิ่งหนีตาตั้งไปทางซ้าย (หันหน้าไปทางซ้ายตามทิศวิ่ง)
    //    - แม่หมูกับลูกหมู 2 ตัว วิ่งไล่กวดหมาป่าไปทางซ้ายแบบช้าๆ คอมเมดี้
    //    - ขบวนวิ่ง: หมาป่า -> ลูกหมู 1 -> ลูกหมู 2 -> แม่หมู
    //    - แต่ละตัวเมื่อวิ่งถึงขอบซ้าย (เข้าป่า) จะหายตัวไปทีละตัวตามลำดับ 1, 2, 3, 4!
    // =========================================================================================
    private IEnumerator StageChaseIntroRoutine(int stage, Action onComplete)
    {
        IsPlayingCutscene = true;
        SetGameplayPlayerVisible(false);

        yield return RunChaseCutsceneRoutine(stage);

        SetGameplayPlayerVisible(true);
        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        StopCutsceneBGM();
        AudioManager.Instance?.ResumeStageBGM();

        onComplete?.Invoke();
    }

    private IEnumerator MainMenuLoopCutsceneRoutine()
    {
        yield return null; // รอ 1 เฟรมให้ MainMenuController จัดตำแหน่ง UI BalloonCursor ให้เรียบร้อยก่อน

        var menuCtrl = FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);

        if (menuCtrl != null && menuCtrl.IsTitleVisible)
        {
            menuCtrl.SetCutsceneBalloonWaiting(true);
        }

        do
        {
            yield return RunMainMenuBalloonMarchRoutine(menuCtrl);

            if (!mainMenuLoopForever || !playMainMenuLoop || !enableCutscenes) break;

            float pause = Mathf.Max(0f, mainMenuLoopDelay);
            if (pause > 0f) yield return new WaitForSeconds(pause);
            else yield return null;
        }
        while (mainMenuLoopForever && playMainMenuLoop && enableCutscenes);

        activeCutsceneRoutine = null;
    }

    private IEnumerator RunMainMenuBalloonMarchRoutine(MainMenuController menuCtrl)
    {
        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);

        if (cutsceneWolf != null)
        {
            cutsceneWolf.color = mainMenuWolfIsBoss ? mainMenuBossWolfColor : Color.white;
            float wolfScale = actorScale * (mainMenuWolfIsBoss ? Mathf.Max(0.5f, mainMenuBossWolfScaleMultiplier) : 1f);
            cutsceneWolf.transform.localScale = new Vector3(wolfScale, wolfScale, 1f);
        }

        bool deliverToUi = menuCtrl != null && menuCtrl.IsTitleVisible && !menuCtrl.HasCutsceneDeliveredBalloon;
        bool hasPrefabBalloon = mainMenuBalloonPrefab != null && cutsceneBalloon != null && cutsceneBalloon.sprite != null;
        Sprite balloonSprite = hasPrefabBalloon
            ? cutsceneBalloon.sprite
            : (mainMenuBalloonSprite != null
                ? mainMenuBalloonSprite
                : (menuCtrl != null ? menuCtrl.GetBalloonCursorSprite() : null));

        if (cutsceneBalloon != null)
        {
            if (!hasPrefabBalloon && balloonSprite != null) cutsceneBalloon.sprite = balloonSprite;
            cutsceneBalloon.transform.rotation = Quaternion.identity;
            cutsceneBalloon.gameObject.SetActive(deliverToUi && balloonSprite != null);
        }

        // หมาป่านำหน้าสุด -> แม่หมูวิ่งนำหน้าลูกหมู -> ลูกหมู 1 -> ลูกหมู 2 (ถือลูกโป่ง)
        PlayActorAnim(cutsceneWolf, wolfRunState, wolfCutsceneAnimator);
        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);
        PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);

        bool pausedMainMenuBgm = AudioManager.Instance != null && AudioManager.Instance.PlayMainMenuCutsceneAudio();

        float gap = Mathf.Max(0.5f, mainMenuFormationSpacing);
        float wolfLead = Mathf.Max(0.6f, mainMenuWolfLeadGap);
        float mamaX = 6.2f;
        float wolfX = mamaX - wolfLead;
        float pig1X = mamaX + gap;
        float pig2X = mamaX + gap * 2f;

        bool balloonReleased = false;
        bool balloonDocked = !deliverToUi || balloonSprite == null;
        float balloonFloatElapsed = 0f;
        Vector3 balloonStartPos = Vector3.zero;
        float releaseCarryVelX = 0f;
        float heldBalloonScale = 0.25f;
        float targetBalloonScale = 0.25f;

        if (cutsceneBalloon != null && balloonSprite != null && balloonSprite.bounds.size.y > 0.001f)
        {
            GetMainMenuCursorTarget(menuCtrl, out _, out float targetWorldHeight);
            targetBalloonScale = targetWorldHeight / balloonSprite.bounds.size.y;
            heldBalloonScale = targetBalloonScale * 0.85f;
            cutsceneBalloon.transform.localScale = new Vector3(heldBalloonScale, heldBalloonScale, 1f);
        }

        const float maxPassTime = 14.0f;
        float elapsed = 0f;
        bool cutsceneFadeTriggered = false;

        while (elapsed < maxPassTime)
        {
            float speedScale = Mathf.Max(0.1f, mainMenuCutsceneSpeed);
            float dt = Time.deltaTime * speedScale;
            elapsed += dt;

            float animSpeed = Mathf.Max(0.1f, mainMenuAnimSpeed * speedScale);
            SetActorAnimSpeed(cutsceneWolf, animSpeed * (mainMenuWolfIsBoss ? 1.15f : 1.05f));
            SetActorAnimSpeed(cutsceneMama, animSpeed);
            SetActorAnimSpeed(cutscenePiglet1, animSpeed);
            SetActorAnimSpeed(cutscenePiglet2, animSpeed);

            float moveSpeedPerSec = Mathf.Max(0.5f, mainMenuWalkSpeed) * speedScale;
            mamaX -= moveSpeedPerSec * Time.deltaTime;
            wolfX = mamaX - wolfLead;
            pig1X = mamaX + gap;
            pig2X = mamaX + gap * 2f;

            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.04f;
            SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);
            SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
            SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
            SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Wolf, cutsceneWolf);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Mama, cutsceneMama);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet1, cutscenePiglet1);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet2, cutscenePiglet2);

            // อัปเดตลูกโป่งที่ลูกหมูตัวที่ 2 ถือ -> พอวิ่งผ่านตำแหน่ง UI ให้ปล่อยลอยขึ้นอย่างสมจริงไปเป็นปุ่ม Select!
            if (deliverToUi && !balloonDocked && cutsceneBalloon != null && balloonSprite != null)
            {
                if (menuCtrl != null && !menuCtrl.IsTitleVisible)
                {
                    cutsceneBalloon.transform.rotation = Quaternion.identity;
                    cutsceneBalloon.gameObject.SetActive(false);
                    menuCtrl.CompleteCutsceneBalloonDelivery();
                    balloonDocked = true;
                }
                else
                {
                    GetMainMenuCursorTarget(menuCtrl, out Vector3 targetWorldPos, out float targetWorldHeight);
                    targetBalloonScale = targetWorldHeight / Mathf.Max(0.001f, balloonSprite.bounds.size.y);

                    if (!balloonReleased)
                    {
                        float heldBob = Mathf.Sin(elapsed * 12f) * 0.025f;
                        Vector3 handPos = new Vector3(
                            pig2X + mainMenuHeldBalloonOffset.x,
                            groundY + pigletYOffset + mainMenuHeldBalloonOffset.y + walkBob + heldBob,
                            0f
                        );
                        cutsceneBalloon.sortingOrder = 13; // ซ่อนเชือกใต้ฝ่ามือลูกหมู (order 14) ให้ดูเหมือนกำไว้ในมือ
                        cutsceneBalloon.transform.position = handPos;
                        cutsceneBalloon.transform.localScale = new Vector3(heldBalloonScale, heldBalloonScale, 1f);
                        // เอียงตัวลูกโป่งลู่ลมไปด้านหลังเล็กน้อย while bottom knot stays in Piglet's fist
                        cutsceneBalloon.transform.rotation = Quaternion.Euler(0f, 0f, -12f + Mathf.Sin(elapsed * 10f) * 3f);

                        if (handPos.x <= targetWorldPos.x + 0.25f)
                        {
                            balloonReleased = true;
                            cutsceneBalloon.sortingOrder = 16;
                            balloonStartPos = handPos;
                            balloonFloatElapsed = 0f;
                            releaseCarryVelX = -moveSpeedPerSec * 0.35f;
                            AudioManager.Instance?.PlayMainMenuBalloonReleaseSFX();
                        }
                    }
                    else
                    {
                        float floatDur = Mathf.Max(0.4f, mainMenuBalloonFloatDuration);
                        balloonFloatElapsed += Time.deltaTime;
                        float rawT = Mathf.Clamp01(balloonFloatElapsed / floatDur);

                        // แรงลอยตัว (Buoyancy): เริ่มลอยออกตัวนุ่มๆ -> ลอยขึ้นเร็วกลางทาง -> ชะลอเข้าจุดจอดพอดี
                        float liftT = Mathf.SmoothStep(0f, 1f, Mathf.Pow(rawT, 0.9f));
                        // แรงเฉื่อยแนวนอนจากตอนปล่อย + แรงต้านอากาศค่อยๆ หยุด แล้วเลี้ยวเข้าเป้า Select + โยกตามลมฮีเลียม
                        float carryDecay = 1f - Mathf.Exp(-3.5f * balloonFloatElapsed);
                        float inertiaX = releaseCarryVelX * (1f - rawT) * 0.22f * Mathf.Sin(rawT * Mathf.PI);
                        float swayAmp = 0.20f * Mathf.Sin(rawT * Mathf.PI) * (1f - rawT * 0.65f);
                        float swayX = Mathf.Sin(balloonFloatElapsed * 6.5f) * swayAmp;

                        float curX = Mathf.Lerp(balloonStartPos.x, targetWorldPos.x, Mathf.SmoothStep(0f, 1f, carryDecay * 0.55f + rawT * 0.45f))
                                     + inertiaX + swayX;
                        float curY = Mathf.Lerp(balloonStartPos.y, targetWorldPos.y, liftT);

                        // เอียงลูกโป่งส่ายเบาๆ ตามจังหวะลม (หายตรงเมื่อเข้าจุดจอด)
                        float tiltZ = -Mathf.Cos(balloonFloatElapsed * 6.5f) * 14f * (1f - rawT);
                        cutsceneBalloon.transform.position = new Vector3(curX, curY, 0f);
                        cutsceneBalloon.transform.rotation = Quaternion.Euler(0f, 0f, tiltZ);

                        // ยืดหดเบาๆ ตอนปล่อยออกจากมือ (Squash & Stretch) แล้วปรับขนาดพอดีปุ่ม Select
                        float stretch = Mathf.Sin(rawT * Mathf.PI * 2f) * 0.07f * (1f - rawT);
                        float baseScale = Mathf.Lerp(heldBalloonScale, targetBalloonScale, liftT);
                        cutsceneBalloon.transform.localScale = new Vector3(baseScale * (1f - stretch), baseScale * (1f + stretch), 1f);

                        if (rawT >= 1f)
                        {
                            cutsceneBalloon.transform.rotation = Quaternion.identity;
                            cutsceneBalloon.gameObject.SetActive(false);
                            menuCtrl?.CompleteCutsceneBalloonDelivery();
                            AudioManager.Instance?.PlayMainMenuBalloonDockSFX();
                            balloonDocked = true;
                        }
                    }
                }
            }

            TryBeginCutsceneFadeOut(ref cutsceneFadeTriggered, pig2X <= -4.2f && (!balloonReleased || balloonDocked));

            if (pig2X <= LEFT_EXIT_X && (!balloonReleased || balloonDocked))
            {
                break;
            }

            yield return null;
        }

        if (deliverToUi && !balloonDocked)
        {
            if (cutsceneBalloon != null)
            {
                cutsceneBalloon.transform.rotation = Quaternion.identity;
                cutsceneBalloon.gameObject.SetActive(false);
            }
            menuCtrl?.CompleteCutsceneBalloonDelivery();
        }

        SetActorAnimSpeed(cutsceneWolf, 1f);
        SetActorAnimSpeed(cutsceneMama, 1f);
        SetActorAnimSpeed(cutscenePiglet1, 1f);
        SetActorAnimSpeed(cutscenePiglet2, 1f);
        if (cutsceneWolf != null)
        {
            cutsceneWolf.color = Color.white;
            cutsceneWolf.transform.localScale = new Vector3(actorScale, actorScale, 1f);
        }
        StopCutsceneBGM();
        if (pausedMainMenuBgm)
        {
            AudioManager.Instance?.UnPauseOrPlayMainMenuBGM();
        }
        cutsceneRoot.SetActive(false);
    }

    private static void GetMainMenuCursorTarget(MainMenuController menuCtrl, out Vector3 worldCenter, out float worldHeight)
    {
        worldCenter = new Vector3(-1.94f, -0.93f, 0f);
        worldHeight = 0.65f;

        if (menuCtrl == null || menuCtrl.BalloonCursorRect == null) return;
        Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam == null) return;

        RectTransform rt = menuCtrl.BalloonCursorRect;
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Vector3 screenBottomLeft = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            ? canvas.worldCamera.WorldToScreenPoint(corners[0])
            : corners[0];
        Vector3 screenTopRight = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            ? canvas.worldCamera.WorldToScreenPoint(corners[2])
            : corners[2];

        Vector3 worldBL = cam.ScreenToWorldPoint(new Vector3(screenBottomLeft.x, screenBottomLeft.y, -cam.transform.position.z));
        Vector3 worldTR = cam.ScreenToWorldPoint(new Vector3(screenTopRight.x, screenTopRight.y, -cam.transform.position.z));

        worldCenter = new Vector3((worldBL.x + worldTR.x) * 0.5f, (worldBL.y + worldTR.y) * 0.5f, 0f);
        worldHeight = Mathf.Max(0.1f, Mathf.Abs(worldTR.y - worldBL.y));
    }

    private IEnumerator RunChaseCutsceneRoutine(int stage)
    {
        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);
        if (cutsceneBalloon != null) cutsceneBalloon.gameObject.SetActive(false);

        // เล่นท่าแอนิเมชันสำหรับคัทซีนวิ่งไล่กวด
        PlayActorAnim(cutsceneWolf, wolfRunState, wolfCutsceneAnimator);
        PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);
        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);

        AudioManager.Instance?.PlayStageCutsceneBGM(stage: stage, isClear: false);

        // ปรับความเร็วและระยะห่างผ่านค่า public ใน Inspector
        float duration = Mathf.Max(0.5f, chaseCutsceneDuration);
        float elapsed = 0f;
        bool cutsceneFadeTriggered = false;
        float spacingMult = Mathf.Max(0f, chasePursuerSpacingMultiplier);
        float wolfStartX = 3.2f - chaseWolfExtraGap;
        float wolfEndX = -9.0f - chaseWolfExtraGap;
        float pig1StartX = 4.8f;
        float pig2StartX = pig1StartX + 1.2f * spacingMult;
        float mamaStartX = pig1StartX + 2.7f * spacingMult;

        while (elapsed < duration)
        {
            float speedScale = Mathf.Max(0.1f, chaseCutsceneSpeed);
            float animSpeed = Mathf.Max(0.1f, chaseAnimSpeed * speedScale);
            SetActorAnimSpeed(cutsceneWolf, animSpeed);
            SetActorAnimSpeed(cutscenePiglet1, animSpeed);
            SetActorAnimSpeed(cutscenePiglet2, animSpeed);
            SetActorAnimSpeed(cutsceneMama, animSpeed);

            elapsed += Time.deltaTime * speedScale;
            float t = elapsed / duration;
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.04f;

            // 1. หมาป่า: วิ่งหนีตาตั้งไปทางซ้าย (ถึงขอบซ้ายแล้วหายตัวไปก่อนเป็นตัวแรก: 1)
            float wolfX = Mathf.Lerp(wolfStartX, wolfEndX, Mathf.Clamp01(t * 1.18f));
            SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

            // 2. ลูกหมู 1 (วิ่งนำหน้าสุด): วิ่งไล่กวดหมาป่าไปทางซ้าย (หายไปเป็นตัวที่: 2)
            float pig1X = Mathf.Lerp(pig1StartX, -9.0f, Mathf.Clamp01(t * 1.12f));
            SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            // 3. ลูกหมู 2 (วิ่งอยู่หน้าแม่หมู): วิ่งเคียงข้างไปทางซ้าย (หายไปเป็นตัวที่: 3)
            float pig2X = Mathf.Lerp(pig2StartX, -9.0f, Mathf.Clamp01(t * 1.06f));
            SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            // 4. แม่หมู: วิ่งตามหลังลูกๆ ไล่กวดหมาป่าไปทางซ้าย (หายไปเป็นตัวที่: 4)
            float mamaX = Mathf.Lerp(mamaStartX, -9.0f, Mathf.Clamp01(t * 1.00f));
            SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Wolf, cutsceneWolf);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet1, cutscenePiglet1);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet2, cutscenePiglet2);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Mama, cutsceneMama);
            TryBeginCutsceneFadeOut(ref cutsceneFadeTriggered, elapsed >= duration - GetCutsceneFadeLeadTime());

            yield return null;
        }

        SetActorAnimSpeed(cutsceneWolf, 1f);
        SetActorAnimSpeed(cutscenePiglet1, 1f);
        SetActorAnimSpeed(cutscenePiglet2, 1f);
        SetActorAnimSpeed(cutsceneMama, 1f);
        cutsceneRoot.SetActive(false);
    }

    // =========================================================================================
    // 3. คัทซีนตอนจบด่านคู่ (Stage 2 / Stage 4 Clear Reunion Cutscene):
    //    - เฟส 1: กระเช้าที่แม่หมูนั่งอยู่เลื่อนลงมาล่างสุดจนถึงพื้น พร้อมเชือกที่เชื่อมต่ออยู่ตลอด (ยังไม่มีลูกหมูมายืนรอ)
    //    - เฟส 2: แม่หมูก้าวออกจากกระเช้าเดิน/วิ่งไปทางซ้ายช้าๆ แล้วลูกหมู 2 ตัววิ่งตามมาจากด้านหลัง (ฝั่งขวา)
    //    - เฟส 3: พอระยะห่างเข้าขบวนกันได้ (แม่หมู -> ลูกหมู 1 -> ลูกหมู 2) ทุกคนจะวิ่งเร็วขึ้นพร้อมกันออกไปทางซ้ายเข้าด่านโบนัส
    // =========================================================================================
    private IEnumerator StageClearReunionRoutine(int stage, Action onComplete)
    {
        IsPlayingCutscene = true;
        AudioManager.Instance?.PlayStageCutsceneBGM(stage: stage, isClear: true);

        // 1. จัดการตัวผู้เล่น (กระเช้า + แม่หมู) และเชือกให้พร้อม
        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        bool prevPlayerEnabled = true;
        Transform inBasketMama = null;

        if (player != null)
        {
            if (!player.gameObject.activeSelf)
            {
                player.gameObject.SetActive(true);
                player.Respawn();
            }

            inBasketMama = player.transform.Find("RainyMamaPig");
            if (inBasketMama == null) inBasketMama = player.transform.Find("SpringMamaPig");
            if (inBasketMama == null) inBasketMama = player.transform.Find("WinterMamaPig");
            if (inBasketMama == null) inBasketMama = player.transform.Find("MamaPig");

            // เปิดแสดงผลสไปรต์ทุกชิ้น (เกราะกระเช้าบน, เกราะกระเช้าล่าง, แม่หมู)
            foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.name != "HeldMeatVisual")
                {
                    sr.enabled = true;
                    sr.gameObject.SetActive(true);
                }
            }

            prevPlayerEnabled = player.enabled;
            player.enabled = false;
        }

        var rope = FindFirstObjectByType<ElevatorRope>(FindObjectsInactive.Include);
        if (rope != null)
        {
            rope.enabled = true;
            rope.SetRopeVisible(true);
        }

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneWolf != null) cutsceneWolf.gameObject.SetActive(false);
        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false); // ใช้กระเช้าจริงของผู้เล่น ไม่ใช้กระเช้าปลอม
        if (cutsceneMama != null) cutsceneMama.gameObject.SetActive(false);     // แม่หมูอยู่ในกระเช้าจริงก่อน
        if (cutscenePiglet1 != null) cutscenePiglet1.gameObject.SetActive(false); // ยังไม่มีลูกหมูมายืนรอในเฟสลงกระเช้า
        if (cutscenePiglet2 != null) cutscenePiglet2.gameObject.SetActive(false);

        Vector3 startGondolaPos = player != null ? player.transform.position : new Vector3(3.81f, 0.83f, 0f);
        // คำนวณพิกัดระดับพื้นล่างสุดที่ก้นกระเช้าแตะพื้นหญ้าพอดี (-3.75 + 0.92 = -2.83)
        float basketGroundY = groundY + 0.92f;
        Vector3 groundGondolaPos = new Vector3(startGondolaPos.x, basketGroundY, startGondolaPos.z);

        // -----------------------------------------------------------------------------------------
        // เฟส 1: กระเช้าแม่หมูเลื่อนลงมาแตะพื้นอย่างนุ่มนวล — เชือกขยายตามลงมาอย่างเป็นธรรมชาติ
        // -----------------------------------------------------------------------------------------
        float elapsed = 0f;
        float lowerDuration = Mathf.Max(0.3f, clearBasketLowerDuration);
        while (elapsed < lowerDuration)
        {
            elapsed += Time.deltaTime * Mathf.Max(0.1f, clearCutsceneSpeed);
            float t = Mathf.Clamp01(elapsed / lowerDuration);
            Vector3 curGondolaPos = Vector3.Lerp(startGondolaPos, groundGondolaPos, Mathf.SmoothStep(0f, 1f, t));

            if (player != null)
            {
                player.transform.position = curGondolaPos;
            }

            yield return null;
        }

        if (player != null)
        {
            player.transform.position = groundGondolaPos;
        }

        // -----------------------------------------------------------------------------------------
        // เฟส 2 & 3: แม่หมูก้าวออกจากกระเช้าเดินไปข้างหน้า (ซ้าย) ช้าๆ
        //           -> ลูกหมู 2 ตัววิ่งตามมาจากด้านหลัง (ขวา)
        //           -> พอระยะเข้าขบวนกันได้ ทุกคนก็วิ่งเร็วขึ้นพร้อมกันออกไปทางซ้าย!
        // -----------------------------------------------------------------------------------------
        if (inBasketMama != null) inBasketMama.gameObject.SetActive(false);

        float slowSpeed = Mathf.Max(0.3f, clearMamaSlowSpeed);
        float catchUpSpeed = Mathf.Max(slowSpeed + 0.4f, clearPigletCatchUpSpeed);
        float fastSpeed = Mathf.Max(slowSpeed, clearGroupFastSpeed);
        float gap = Mathf.Max(0.5f, clearFormationSpacing);

        float mamaX = groundGondolaPos.x - 0.35f;
        float pigletSpawnX = Mathf.Max(groundGondolaPos.x + 1.8f, 5.6f);
        float pig1X = pigletSpawnX;
        float pig2X = pigletSpawnX;

        bool pig1Spawned = false;
        bool pig2Spawned = false;
        bool pig1CaughtUp = false;
        bool pig2CaughtUp = false;
        bool isSprintingTogether = false;
        bool cutsceneFadeTriggered = false;
        float sprintElapsed = 0f;

        const float pig1EntryDelay = 0.35f;
        const float pig2EntryDelay = 0.95f;
        const float maxRoutineDuration = 12.0f;

        PlayActorAnim(cutsceneMama, mamaWalkState, mamaCutsceneAnimator);
        SetActorAnimSpeed(cutsceneMama, Mathf.Max(0.1f, clearCutsceneSpeed));
        SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset, 0f), faceLeft: true, isWolf: false);

        elapsed = 0f;
        while (elapsed < maxRoutineDuration)
        {
            float clearScale = Mathf.Max(0.1f, clearCutsceneSpeed);
            float dt = Time.deltaTime * clearScale;
            elapsed += dt;

            if (!pig1Spawned && elapsed >= pig1EntryDelay)
            {
                pig1Spawned = true;
                pig1X = pigletSpawnX;
                PlayActorAnim(cutscenePiglet1, pigletWalkState, pigletCutsceneAnimator);
                SetActorAnimSpeed(cutscenePiglet1, clearScale);
            }

            if (!pig2Spawned && elapsed >= pig2EntryDelay)
            {
                pig2Spawned = true;
                pig2X = pigletSpawnX;
                PlayActorAnim(cutscenePiglet2, pigletWalkState, pigletCutsceneAnimator);
                SetActorAnimSpeed(cutscenePiglet2, clearScale);
            }

            // คำนวณความเร็วของแม่หมู (ช้าในช่วงแรก -> เร็วขึ้นพร้อมกันเมื่อระยะเข้าขบวนแล้ว)
            float currentMamaSpeed = slowSpeed;
            if (isSprintingTogether)
            {
                sprintElapsed += dt;
                float accelT = Mathf.Clamp01(sprintElapsed / 0.3f);
                currentMamaSpeed = Mathf.Lerp(slowSpeed, fastSpeed, Mathf.SmoothStep(0f, 1f, accelT));
                float animSpeed = clearScale * Mathf.Lerp(1f, 1.45f, accelT);
                SetActorAnimSpeed(cutsceneMama, animSpeed);
                SetActorAnimSpeed(cutscenePiglet1, animSpeed);
                SetActorAnimSpeed(cutscenePiglet2, animSpeed);
            }
            else
            {
                SetActorAnimSpeed(cutsceneMama, clearScale);
                if (pig1Spawned) SetActorAnimSpeed(cutscenePiglet1, clearScale);
                if (pig2Spawned) SetActorAnimSpeed(cutscenePiglet2, clearScale);
            }

            mamaX -= currentMamaSpeed * dt;
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * (isSprintingTogether ? 18f : 11f))) * 0.04f;
            SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            if (pig1Spawned)
                StepTrailingPiglet(cutscenePiglet1, ref pig1X, ref pig1CaughtUp, mamaX + gap, catchUpSpeed, dt, walkBob);
            if (pig2Spawned)
                StepTrailingPiglet(cutscenePiglet2, ref pig2X, ref pig2CaughtUp, mamaX + gap * 2f, catchUpSpeed, dt, walkBob);

            float reunionPitch = isSprintingTogether ? 1.12f : 0.96f;
            float reunionCadence = isSprintingTogether ? 0.72f : 1.05f;
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Mama, cutsceneMama, true, reunionPitch, reunionCadence);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet1, cutscenePiglet1, pig1Spawned, reunionPitch, pig1CaughtUp ? reunionCadence : 0.82f);
            ReportCutsceneRunner(AudioManager.GroundRunnerRole.Piglet2, cutscenePiglet2, pig2Spawned, reunionPitch, pig2CaughtUp ? reunionCadence : 0.82f);

            // พอทั้งคู่ไล่ทันระยะเข้ากันได้แล้ว (หรือแม่หมูเดินใกล้ขอบซ้ายมากแล้ว) -> ทุกคนวิ่งเร็วขึ้นพร้อมกัน!
            if (!isSprintingTogether && ((pig1CaughtUp && pig2CaughtUp) || mamaX <= -3.5f))
            {
                isSprintingTogether = true;
                pig1CaughtUp = true;
                pig2CaughtUp = true;
            }

            TryBeginCutsceneFadeOut(ref cutsceneFadeTriggered, isSprintingTogether && pig2Spawned && pig2X <= -3.5f);

            // รอจนตัวสุดท้ายวิ่งพ้นขอบซ้ายออกนอกจอ
            if (isSprintingTogether && pig2Spawned && pig2X <= LEFT_EXIT_X)
            {
                break;
            }

            yield return null;
        }

        // -----------------------------------------------------------------------------------------
        // จบคัทซีน: ทำความสะอาด คืนค่าตัวละคร และส่งต่อไปยังด่านโบนัส
        // -----------------------------------------------------------------------------------------
        SetActorAnimSpeed(cutsceneWolf, 1f);
        SetActorAnimSpeed(cutsceneMama, 1f);
        SetActorAnimSpeed(cutscenePiglet1, 1f);
        SetActorAnimSpeed(cutscenePiglet2, 1f);
        if (cutsceneWolf != null)
        {
            cutsceneWolf.color = Color.white;
            cutsceneWolf.transform.localScale = new Vector3(actorScale, actorScale, 1f);
        }
        cutsceneRoot.SetActive(false);
        if (inBasketMama != null)
        {
            inBasketMama.gameObject.SetActive(true);
        }
        if (player != null)
        {
            player.enabled = prevPlayerEnabled;
        }

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
            if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);
            if (mainMenuBalloonPrefab != null)
            {
                var balloonObj = Instantiate(mainMenuBalloonPrefab, cutsceneRoot.transform);
                balloonObj.name = "Cutscene_Balloon";
                foreach (var col in balloonObj.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
                foreach (var b in balloonObj.GetComponentsInChildren<Balloon>(true)) b.enabled = false;
                cutsceneBalloon = balloonObj.GetComponentInChildren<SpriteRenderer>(true);
                if (cutsceneBalloon != null) cutsceneBalloon.sortingOrder = 16;
                balloonObj.SetActive(false);
            }
            else
            {
                cutsceneBalloon = CreateActor("Cutscene_Balloon", cutsceneRoot.transform, mainMenuBalloonSprite, 16);
                if (cutsceneBalloon != null) cutsceneBalloon.gameObject.SetActive(false);
            }
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

    private void StepTrailingPiglet(SpriteRenderer piglet, ref float pigX, ref bool caughtUp, float targetX, float catchUpSpeed, float dt, float bob)
    {
        if (!caughtUp)
        {
            pigX = Mathf.MoveTowards(pigX, targetX, catchUpSpeed * dt);
            if (pigX <= targetX + 0.02f)
            {
                pigX = targetX;
                caughtUp = true;
            }
        }
        else
        {
            pigX = targetX;
        }
        SetActor(piglet, new Vector3(pigX, groundY + pigletYOffset + bob, 0f), faceLeft: true, isWolf: false);
    }

    private void SetActorAnimSpeed(SpriteRenderer actor, float speed)
    {
        if (actor == null) return;
        var anim = actor.GetComponent<Animator>();
        if (anim != null) anim.speed = speed;
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
