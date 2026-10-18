using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบคัทซีนในฉากจริง (In-Scene Ground Cutscenes) ตามต้นฉบับ Pooyan (Arcade 1982 / NES 1985):
/// 1. คัทซีนเกิดขึ้นที่พื้นล่างเสมอ (Bottom Ground: Y ≈ -3.75) ตัวแปรปรับเป็น public ให้ปรับแต่งใน Inspector ได้
/// 2. ไม่ใช้ตัวละครหลัก (Player) มาเล่น — Player และเชือกกระเช้าด้านบนจะถูกซ่อนไว้สนิท (SetActive=false)
/// 3. ขนาดตัวละครในคัทซีนจะเท่ากับขนาดตัวละครในเกมจริง (actorScale ≈ 0.26)
/// 4. ลำดับของคัทซีน:
///    - ก่อนเริ่มด่าน 1 (Autumn): ลูกหมูเดินออกมา -> หมาป่าวิ่งสวนมาจากทางซ้าย -> ลูกหมูหันกลับวิ่งหนีไม่ทันโดนหมาป่าอุ้มไป (ลูกหมูอยู่หลังหมาป่า) -> หมาป่าหันซ้ายวิ่งหนี -> แม่หมูวิ่งตามไล่กวดไปทางซ้าย (มีการสลับทิศหันของตัวละครครบทุกคน)
///    - ก่อนเริ่มด่าน 2 (Spring): หมาป่าวิ่งหนีตาตั้งไปทางซ้าย (หันหน้าไปทางซ้ายตามทิศวิ่ง) -> แม่หมูกับลูกหมูวิ่งไล่กวดหมาป่าไปทางซ้าย
///    - ก่อนเริ่มด่านโบนัส (หลังชนะบอสด่าน 2): กระเช้าแม่หมูเลื่อนลงมาถึงพื้นล่าง -> แม่หมูเดินออกมาหาลูกหมู -> ดีใจด้วยกันแป๊บนึง -> แม่หมูเดินมุ่งหน้าไปทางทิศหมาป่า (ซ้าย) ต่อ -> เข้าสู่ด่านโบนัส
///    - หลังด่านโบนัสไปด่าน 3 (Winter): ไม่มีคัทซีน เข้าสู่เกมด่าน 3 ทันที
/// 5. รองรับการใส่ Sprite Sheet ที่เพื่อนวาดลงใน Inspector ได้ทันที
/// </summary>
public class StageCutsceneController : MonoBehaviour
{
    public static StageCutsceneController Instance { get; private set; }

    [Header("Cutscene Settings (ปรับแต่งใน Inspector ได้ตามใจชอบ)")]
    [Tooltip("เปิด/ปิดคัทซีน")]
    public bool enableCutscenes = true;

    [Tooltip("ระดับความสูงของพื้นล่างที่ตัวละครวิ่ง (ปรับลดลงเพื่อให้เท้าติดพื้นพอดี เช่น -3.75 ถึง -3.85)")]
    public float groundY = -3.75f;

    [Tooltip("ขนาดสเกลของตัวละครคัทซีน (ค่ามาตรฐานเกมคือ 0.26)")]
    public float actorScale = 0.26f;

    [Tooltip("อนุญาตให้กด Space / Enter เพื่อข้ามคัทซีน")]
    public bool allowSkip = true;

    [Header("Character Ground Offsets (ออฟเซ็ตความสูงของแต่ละตัวละคร ปรับแยกได้)")]
    [Tooltip("ออฟเซ็ตความสูงแม่หมูจาก groundY")]
    public float mamaYOffset = 0.70f;
    [Tooltip("ออฟเซ็ตความสูงหมาป่าจาก groundY")]
    public float wolfYOffset = 0.66f;
    [Tooltip("ออฟเซ็ตความสูงลูกหมูจาก groundY")]
    public float pigletYOffset = 0.0f;
    [Tooltip("ออฟเซ็ตความสูงกระเช้าจาก groundY")]
    public float basketYOffset = 0.35f;

    [Header("Sprite Sheet Frames (ลากใส่เมื่อเพื่อนวาดเสร็จ)")]
    [Tooltip("เฟรมเดิน/วิ่งของแม่หมู")]
    public Sprite[] mamaWalkSprites;
    [Tooltip("เฟรมยืนเฉยๆ ของแม่หมู")]
    public Sprite[] mamaIdleSprites;
    [Tooltip("เฟรมเดิน/วิ่งของลูกหมู")]
    public Sprite[] pigletWalkSprites;
    [Tooltip("เฟรมกระโดดดีใจของลูกหมู")]
    public Sprite[] pigletJumpSprites;
    [Tooltip("เฟรมวิ่งของหมาป่า")]
    public Sprite[] wolfWalkSprites;
    [Tooltip("เฟรมหมาป่าอุ้มลูกหมูวิ่งหนี")]
    public Sprite[] wolfKidnapSprites;
    [Tooltip("ความเร็วเฟรมเรตของ Sprite Sheet (FPS)")]
    public float spriteSheetFPS = 10f;

    [Header("Fallback Sprites (สไปรต์สำรองระหว่างรอเพื่อนวาด)")]
    public Sprite fallbackMamaSprite;
    public Sprite fallbackPigletSprite;
    public Sprite fallbackWolfSprite;
    public Sprite fallbackBasketSprite;

    public bool IsPlayingCutscene { get; private set; }

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
        if (cutsceneRoot != null) Destroy(cutsceneRoot);
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// เช็คว่าด่านนี้มีคัทซีนก่อนเริ่มด่านหรือไม่ (เฉพาะด่าน 1 และด่าน 2)
    /// </summary>
    public bool HasIntroCutsceneForStage(int stage)
    {
        if (!enableCutscenes) return false;
        // ก่อนเริ่มด่าน 1 และก่อนเริ่มด่าน 2 มีคัทซีน (ด่าน 3 เป็นต้นไปไม่มีคัทซีนตามต้นฉบับ)
        return stage == 1 || stage == 2;
    }

    /// <summary>
    /// เช็คว่าด่านนี้มีคัทซีนตอนจบด่านหรือไม่ (เฉพาะด่าน 2 ที่ผ่านแล้วจะเข้าสู่ Bonus Stage)
    /// </summary>
    public bool HasClearCutsceneForStage(int stage)
    {
        if (!enableCutscenes) return false;
        // คัทซีนดีใจลงจากกระเช้าเกิดขึ้นหลังผ่านด่าน 2 เพื่อเข้าสู่ Bonus Stage
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

        // ปิด/เปิดเส้นเชือกกระเช้า เพื่อไม่ให้มีเส้นสลิงห้อยลอยค้างอยู่บนฟ้า
        var rope = FindFirstObjectByType<ElevatorRope>(FindObjectsInactive.Include);
        if (rope != null)
        {
            var lr = rope.GetComponentInChildren<LineRenderer>(true);
            if (lr != null) lr.enabled = visible;
        }
        else
        {
            var lrObj = GameObject.Find("Visual/L_RopeVisual") ?? GameObject.Find("L_RopeVisual");
            if (lrObj != null)
            {
                var lr = lrObj.GetComponent<LineRenderer>();
                if (lr != null) lr.enabled = visible;
            }
        }
    }

    // =========================================================================================
    // 1. คัทซีนก่อนเริ่มด่าน 1 (Stage 1 Intro):
    //    - ลูกหมูเดินออกมา
    //    - หมาป่าวิ่งสวนมาจากทางซ้าย
    //    - ลูกหมูตกใจหันกลับวิ่งหนี แต่หนีไม่ทัน โดนหมาป่าจับอุ้มไว้ (ลูกหมูอยู่หลังหมาป่า)
    //    - หมาป่าหันกลับไปทางซ้ายวิ่งหนีไป
    //    - แม่หมูวิ่งตามไล่กวดไปทางซ้าย
    // =========================================================================================
    private IEnumerator Stage1IntroRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;

        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        // ซ่อนกระเช้า และลูกหมูตัวที่ 2 (ด่าน 1 มีลูกหมูตัวเดียวโดนขโมย)
        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);
        if (cutscenePiglet2 != null) cutscenePiglet2.gameObject.SetActive(false);
        if (cutsceneMama != null) cutsceneMama.gameObject.SetActive(false);

        AudioManager.Instance?.PlayStageIntroBGM();

        float duration = 4.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (allowSkip && elapsed > 0.3f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                break;

            elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(elapsed * spriteSheetFPS);
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 18f)) * 0.08f;

            if (elapsed < 1.3f)
            {
                // เฟส 1 (0.0s - 1.3s): ลูกหมูเดินเล่นออกมาทางซ้าย หมาป่าวิ่งชาร์จเข้ามาจากทางซ้าย!
                float t1 = elapsed / 1.3f;

                // ลูกหมู: เดินไปทางซ้าย (หันซ้าย)
                float pigletX = Mathf.Lerp(1.8f, 0.2f, t1);
                SetActor(cutscenePiglet1, new Vector3(pigletX, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
                ApplyFrame(cutscenePiglet1, pigletWalkSprites, fallbackPigletSprite, frame);

                // หมาป่า: วิ่งชาร์จพุ่งเข้ามาจากฝั่งซ้าย (หันขวา!)
                float wolfX = Mathf.Lerp(-8.5f, -1.2f, t1);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: false, isWolf: true);
                ApplyFrame(cutsceneWolf, wolfWalkSprites, fallbackWolfSprite, frame);
            }
            else if (elapsed < 2.2f)
            {
                // เฟส 2 (1.3s - 2.2s): ลูกหมูเห็นหมาป่า ตกใจสลับทิศหันหลังวิ่งหนีกลับบ้าน (หันขวา!) แต่หมาป่าวิ่งกวดไล่ทัน
                float t2 = (elapsed - 1.3f) / 0.9f;

                // ลูกหมู: วิ่งหนีกลับไปทางขวา (หันขวา!)
                float pigletX = Mathf.Lerp(0.2f, 1.8f, t2);
                SetActor(cutscenePiglet1, new Vector3(pigletX, groundY + pigletYOffset + walkBob * 1.2f, 0f), faceLeft: false, isWolf: false);
                ApplyFrame(cutscenePiglet1, pigletWalkSprites, fallbackPigletSprite, frame);

                // หมาป่า: วิ่งไล่ตามติดๆ ไปทางขวา (หันขวา!)
                float wolfX = Mathf.Lerp(-1.2f, 1.6f, t2);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: false, isWolf: true);
                ApplyFrame(cutsceneWolf, wolfWalkSprites, fallbackWolfSprite, frame);
            }
            else
            {
                // เฟส 3 (2.2s - 4.8s): หมาป่าจับลูกหมูได้แล้ว สลับทิศหันซ้ายวิ่งหนี โดยมีลูกหมูอยู่ด้านหลังหมาป่า!
                float t3 = Mathf.Clamp01((elapsed - 2.2f) / 1.8f);

                // หมาป่า: วิ่งหนีเตลิดไปทางซ้ายสุดจอ (หันซ้าย!)
                float wolfX = Mathf.Lerp(1.6f, -9.5f, t3);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

                if (wolfKidnapSprites != null && wolfKidnapSprites.Length > 0)
                    ApplyFrame(cutsceneWolf, wolfKidnapSprites, fallbackWolfSprite, frame);
                else
                    ApplyFrame(cutsceneWolf, wolfWalkSprites, fallbackWolfSprite, frame);

                // ลูกหมู: โดนอุ้มอยู่ด้านหลังหมาป่า (ออฟเซ็ตไปทางขวาของหมาป่า +0.45f และลอยสูงขึ้นนิดนึง)
                float carriedPigletX = wolfX + 0.45f;
                float struggleBob = Mathf.Sin(elapsed * 24f) * 0.05f;
                // ลูกหมูหันขวามองกลับไปทางบ้านขอความช่วยเหลือ
                SetActor(cutscenePiglet1, new Vector3(carriedPigletX, groundY + pigletYOffset + 0.22f + struggleBob, 0f), faceLeft: false, isWolf: false);
                ApplyFrame(cutscenePiglet1, pigletWalkSprites, fallbackPigletSprite, frame);

                // แม่หมู: เริ่มวิ่งไล่ตามออกมาตอน 2.8 วินาที
                if (elapsed >= 2.8f)
                {
                    float tMama = Mathf.Clamp01((elapsed - 2.8f) / 1.7f);
                    float mamaX = Mathf.Lerp(7.2f, -6.5f, tMama);
                    SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
                    ApplyFrame(cutsceneMama, mamaWalkSprites, fallbackMamaSprite, frame);
                }
            }

            yield return null;
        }

        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        AudioManager.Instance?.ResumeStageBGM();

        onComplete?.Invoke();
    }

    // =========================================================================================
    // 2. คัทซีนก่อนเริ่มด่าน 2 (Stage 2 Intro): หมาป่าวิ่งหนีตาตั้งไปทางซ้าย -> แม่หมูกับลูกหมูวิ่งไล่กวด
    // =========================================================================================
    private IEnumerator Stage2IntroRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;

        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);
        if (cutscenePiglet2 != null) cutscenePiglet2.gameObject.SetActive(false);

        AudioManager.Instance?.PlayStageIntroBGM();

        float duration = 3.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (allowSkip && elapsed > 0.3f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            int frame = Mathf.FloorToInt(elapsed * spriteSheetFPS);
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 18f)) * 0.08f;

            // 1. หมาป่า: วิ่งหนีตาตั้งไปทางซ้ายสุดจอ (หันหน้าไปทางซ้ายตามทิศวิ่ง! isWolf: true, faceLeft: true)
            float wolfX = Mathf.Lerp(2.0f, -9.5f, Mathf.Clamp01(t * 1.1f));
            SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);
            ApplyFrame(cutsceneWolf, wolfWalkSprites, fallbackWolfSprite, frame);

            // 2. แม่หมู: วิ่งไล่กวดตามไปทางซ้าย (หันซ้าย)
            float mamaX = Mathf.Lerp(5.2f, -7.0f, Mathf.Clamp01(t * 1.1f));
            SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
            ApplyFrame(cutsceneMama, mamaWalkSprites, fallbackMamaSprite, frame);

            // 3. ลูกหมู: วิ่งเคียงข้างแม่หมูไล่กวดไปด้วยกันทางซ้าย (หันซ้าย)
            float pigletX = Mathf.Lerp(6.6f, -5.6f, Mathf.Clamp01(t * 1.1f));
            SetActor(cutscenePiglet1, new Vector3(pigletX, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
            ApplyFrame(cutscenePiglet1, pigletWalkSprites, fallbackPigletSprite, frame);

            yield return null;
        }

        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;
        AudioManager.Instance?.ResumeStageBGM();

        onComplete?.Invoke();
    }

    // =========================================================================================
    // 3. คัทซีนก่อนเข้าด่านโบนัส (Stage 2 Clear Reunion):
    //    - กระเช้าแม่หมูเลื่อนลงมาถึงพื้นล่าง
    //    - แม่หมูเดินออกจากกระเช้าไปหาลูกหมู
    //    - แม่หมูอยู่ดีใจกับลูกหมู 2 ตัวแป๊บนึง
    //    - แม่หมูเดินมุ่งหน้าไปทางทิศหมาป่า (ซ้าย) ต่อ -> เข้าสู่ด่านโบนัส
    // =========================================================================================
    private IEnumerator Stage2ClearReunionRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;

        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        Vector3 startGondolaPos = player != null ? player.transform.position : new Vector3(3.81f, 0.88f, 0f);

        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        // หมาป่าไม่ต้องออกมาในฉากนี้
        if (cutsceneWolf != null) cutsceneWolf.gameObject.SetActive(false);

        // ลูกหมู 2 ตัวยืนรออยู่ที่พื้นฝั่งซ้าย (หันหน้ามองมาทางขวาหาแม่หมู)
        Vector3 pig1Pos = new Vector3(-0.6f, groundY + pigletYOffset, 0f);
        Vector3 pig2Pos = new Vector3(-1.5f, groundY + pigletYOffset, 0f);
        SetActor(cutscenePiglet1, pig1Pos, faceLeft: false, isWolf: false);
        SetActor(cutscenePiglet2, pig2Pos, faceLeft: false, isWolf: false);
        ApplyFrame(cutscenePiglet1, pigletWalkSprites, fallbackPigletSprite, 0);
        ApplyFrame(cutscenePiglet2, pigletWalkSprites, fallbackPigletSprite, 1);

        // จุดที่กระเช้าเลื่อนลงมาถึงพื้นดิน
        Vector3 groundGondolaPos = new Vector3(startGondolaPos.x, groundY + basketYOffset, 0f);

        // เฟส 1: กระเช้าเลื่อนลงมาแตะพื้น (1.3 วินาที)
        float elapsed = 0f;
        float lowerDuration = 1.3f;
        while (elapsed < lowerDuration)
        {
            if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lowerDuration);
            Vector3 curGondolaPos = Vector3.Lerp(startGondolaPos, groundGondolaPos, Mathf.SmoothStep(0f, 1f, t));
            if (cutsceneBasket != null)
            {
                cutsceneBasket.gameObject.SetActive(true);
                cutsceneBasket.transform.position = curGondolaPos;
            }
            // แม่หมูยืนอยู่ในกระเช้า
            SetActor(cutsceneMama, new Vector3(curGondolaPos.x, curGondolaPos.y + mamaYOffset - basketYOffset, 0f), faceLeft: true, isWolf: false);
            ApplyFrame(cutsceneMama, mamaIdleSprites, fallbackMamaSprite, 0);

            yield return null;
        }

        // เฟส 2: แม่หมูเดินก้าวออกจากกระเช้าไปทางซ้ายหาลูกหมู (1.2 วินาที)
        elapsed = 0f;
        float walkDuration = 1.2f;
        Vector3 mamaExitStart = new Vector3(groundGondolaPos.x, groundY + mamaYOffset, 0f);
        Vector3 mamaTargetPos = new Vector3(0.3f, groundY + mamaYOffset, 0f);

        while (elapsed < walkDuration)
        {
            if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / walkDuration);
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 16f)) * 0.08f;
            int frame = Mathf.FloorToInt(elapsed * spriteSheetFPS);

            Vector3 curMamaPos = Vector3.Lerp(mamaExitStart, mamaTargetPos, t);
            curMamaPos.y += walkBob;
            SetActor(cutsceneMama, curMamaPos, faceLeft: true, isWolf: false);
            ApplyFrame(cutsceneMama, mamaWalkSprites, fallbackMamaSprite, frame);

            yield return null;
        }

        // เฟส 3: แม่หมูอยู่ดีใจกับลูกหมูแป๊บนึง (1.5 วินาที)
        elapsed = 0f;
        float celebrateDuration = 1.5f;
        while (elapsed < celebrateDuration)
        {
            if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) break;

            elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(elapsed * spriteSheetFPS);

            // ลูกหมูทั้งสองกระโดดสลับกันอย่างร่าเริง
            float jump1 = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.35f;
            float jump2 = Mathf.Abs(Mathf.Sin((elapsed + 0.15f) * 12f)) * 0.32f;

            SetActor(cutscenePiglet1, new Vector3(pig1Pos.x, groundY + pigletYOffset + jump1, 0f), faceLeft: false, isWolf: false);
            SetActor(cutscenePiglet2, new Vector3(pig2Pos.x, groundY + pigletYOffset + jump2, 0f), faceLeft: false, isWolf: false);

            if (pigletJumpSprites != null && pigletJumpSprites.Length > 0)
            {
                ApplyFrame(cutscenePiglet1, pigletJumpSprites, fallbackPigletSprite, frame);
                ApplyFrame(cutscenePiglet2, pigletJumpSprites, fallbackPigletSprite, frame + 1);
            }
            else
            {
                ApplyFrame(cutscenePiglet1, pigletWalkSprites, fallbackPigletSprite, frame);
                ApplyFrame(cutscenePiglet2, pigletWalkSprites, fallbackPigletSprite, frame + 1);
            }

            // แม่หมูกระโดดดีใจทักทายลูกหมู
            float mamaHop = Mathf.Abs(Mathf.Sin(elapsed * 10f)) * 0.15f;
            SetActor(cutsceneMama, new Vector3(mamaTargetPos.x, groundY + mamaYOffset + mamaHop, 0f), faceLeft: true, isWolf: false);
            ApplyFrame(cutsceneMama, mamaIdleSprites, fallbackMamaSprite, 0);

            yield return null;
        }

        // เฟส 4: แม่หมูเดินมุ่งหน้าไปทางทิศหมาป่า (ทางซ้าย) ต่อ! (1.8 วินาที)
        elapsed = 0f;
        float marchDuration = 1.8f;
        Vector3 mamaForestTarget = new Vector3(-8.5f, groundY + mamaYOffset, 0f);

        while (elapsed < marchDuration)
        {
            if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / marchDuration);
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 16f)) * 0.08f;
            int frame = Mathf.FloorToInt(elapsed * spriteSheetFPS);

            // แม่หมู: เดินมุ่งหน้าไปทางซ้ายเข้าสู่รังหมาป่า
            Vector3 curMamaPos = Vector3.Lerp(mamaTargetPos, mamaForestTarget, t);
            curMamaPos.y += walkBob;
            SetActor(cutsceneMama, curMamaPos, faceLeft: true, isWolf: false);
            ApplyFrame(cutsceneMama, mamaWalkSprites, fallbackMamaSprite, frame);

            // ลูกหมู 2 ตัวหันซ้ายมองตามส่งแม่หมูอย่างภาคภูมิใจ
            float pigBob = Mathf.Abs(Mathf.Sin(elapsed * 8f)) * 0.10f;
            SetActor(cutscenePiglet1, new Vector3(pig1Pos.x, groundY + pigletYOffset + pigBob, 0f), faceLeft: true, isWolf: false);
            SetActor(cutscenePiglet2, new Vector3(pig2Pos.x, groundY + pigletYOffset + pigBob, 0f), faceLeft: true, isWolf: false);

            yield return null;
        }

        cutsceneRoot.SetActive(false);
        SetGameplayPlayerVisible(true);

        IsPlayingCutscene = false;
        activeCutsceneRoutine = null;

        // เปลี่ยนไปยังด่านโบนัส
        onComplete?.Invoke();
    }

    // =========================================================================================
    // สร้างตัวละครคัทซีนเฉพาะ (Dedicated Cutscene Actors)
    // =========================================================================================
    private void EnsureCutsceneActors()
    {
        if (cutsceneRoot != null) return;

        ExtractFallbackVisuals();

        cutsceneRoot = new GameObject("[InSceneCutsceneActors]");
        // ตั้งไว้ที่ Root ตำแหน่ง (0,0,0) สเกล (1,1,1) ไม่ผูกกับออฟเซ็ตของ GameObject อื่น
        cutsceneRoot.transform.position = Vector3.zero;
        cutsceneRoot.transform.rotation = Quaternion.identity;
        cutsceneRoot.transform.localScale = Vector3.one;

        cutsceneMama = CreateActor("Cutscene_Mama", cutsceneRoot.transform, fallbackMamaSprite, 15);
        cutscenePiglet1 = CreateActor("Cutscene_Piglet1", cutsceneRoot.transform, fallbackPigletSprite, 14);
        cutscenePiglet2 = CreateActor("Cutscene_Piglet2", cutsceneRoot.transform, fallbackPigletSprite, 14);
        cutsceneWolf = CreateActor("Cutscene_Wolf", cutsceneRoot.transform, fallbackWolfSprite, 16);
        cutsceneBasket = CreateActor("Cutscene_Basket", cutsceneRoot.transform, fallbackBasketSprite, 13);

        cutsceneRoot.SetActive(false);
    }

    private SpriteRenderer CreateActor(string name, Transform parent, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        // สเกลขนาดตัวละครให้ตรงกับขนาดในเกมจริง (actorScale)
        go.transform.localScale = new Vector3(actorScale, actorScale, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        go.SetActive(false); // เริ่มต้นปิดไว้เสมอ เพื่อไม่ให้มีตัวละครค้าง
        return sr;
    }

    private void SetActor(SpriteRenderer actor, Vector3 worldPos, bool faceLeft, bool isWolf = false)
    {
        if (actor == null) return;
        if (!actor.gameObject.activeSelf) actor.gameObject.SetActive(true);

        actor.transform.position = worldPos;

        if (isWolf)
        {
            // สไปรต์หมาป่าเริ่มต้นหันขวา (ตามที่ใช้ใน Wolf.cs: วิ่งซ้าย flipX=true, วิ่งขวา flipX=false)
            actor.flipX = faceLeft;
        }
        else
        {
            // สไปรต์แม่หมูและลูกหมูเริ่มต้นหันซ้าย (วิ่งซ้าย flipX=false, วิ่งขวา flipX=true)
            actor.flipX = !faceLeft;
        }
    }

    private void ApplyFrame(SpriteRenderer actor, Sprite[] sheet, Sprite fallback, int frameIdx)
    {
        if (actor == null) return;
        if (sheet != null && sheet.Length > 0)
        {
            Sprite s = sheet[Mathf.Abs(frameIdx) % sheet.Length];
            if (s != null)
            {
                actor.sprite = s;
                return;
            }
        }
        if (actor.sprite == null && fallback != null)
            actor.sprite = fallback;
    }

    /// <summary>
    /// ดึงสไปรต์แอนิเมชันและรูปตัวอย่างจากในโปรเจกต์มาใส่ให้อัตโนมัติ หากยังไม่ได้ลาก Sprite Sheet ใส่ใน Inspector
    /// </summary>
    private void ExtractFallbackVisuals()
    {
        // 1. ดึงสไปรต์แม่หมู
        if (fallbackMamaSprite == null)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                foreach (var sr in pc.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.gameObject.name.Contains("Mama") && sr.sprite != null)
                    {
                        fallbackMamaSprite = sr.sprite;
                        break;
                    }
                }
            }
        }

        // 2. ดึงสไปรต์กระเช้า (ดึงส่วนล่าง ButtomArmor หรือ IMG_5798_0)
        if (fallbackBasketSprite == null)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                foreach (var sr in pc.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite != null && (sr.gameObject.name.Contains("5798") || sr.gameObject.name.Contains("Buttom") || sr.gameObject.name.Contains("Basket")))
                    {
                        fallbackBasketSprite = sr.sprite;
                        break;
                    }
                }
            }
        }

        // 3. ดึงสไปรต์หมาป่า และแอนิเมชันวิ่งของหมาป่า
        var ws = FindFirstObjectByType<WolfSpawner>(FindObjectsInactive.Include);
        if (ws != null && ws.WolfPrefab != null)
        {
            if (fallbackWolfSprite == null)
            {
                foreach (var sr in ws.WolfPrefab.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite != null && !sr.name.Contains("Balloon") && !sr.name.Contains("Shield"))
                    {
                        fallbackWolfSprite = sr.sprite;
                        break;
                    }
                }
            }

            // ดึงเฟรมวิ่งของหมาป่าจาก Animator
            if (wolfWalkSprites == null || wolfWalkSprites.Length == 0)
            {
                var anim = ws.WolfPrefab.GetComponentInChildren<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    foreach (var clip in anim.runtimeAnimatorController.animationClips)
                    {
                        if (clip.name.Equals("Run", StringComparison.OrdinalIgnoreCase))
                        {
                            var frames = new List<Sprite>();
#if UNITY_EDITOR
                            var bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
                            foreach (var b in bindings)
                            {
                                var keyframes = UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, b);
                                foreach (var k in keyframes)
                                {
                                    if (k.value is Sprite s && !frames.Contains(s))
                                        frames.Add(s);
                                }
                            }
#endif
                            if (frames.Count > 0)
                                wolfWalkSprites = frames.ToArray();
                            break;
                        }
                    }
                }
            }
        }

        // 4. ดึงสไปรต์ลูกหมู และแอนิเมชันเดิน/ยืนของลูกหมู
        if (fallbackPigletSprite == null)
        {
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (sr.name.Contains("Piglet") && sr.sprite != null)
                {
                    fallbackPigletSprite = sr.sprite;
                    break;
                }
            }
        }

        if (pigletWalkSprites == null || pigletWalkSprites.Length == 0)
        {
            foreach (var anim in FindObjectsByType<Animator>(FindObjectsInactive.Include))
            {
                if (anim.name.Contains("Piglet") && anim.runtimeAnimatorController != null)
                {
                    foreach (var clip in anim.runtimeAnimatorController.animationClips)
                    {
                        if (clip.name.Contains("Idle") || clip.name.Contains("Walk") || clip.name.Contains("Run"))
                        {
                            var frames = new List<Sprite>();
#if UNITY_EDITOR
                            var bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
                            foreach (var b in bindings)
                            {
                                var keyframes = UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, b);
                                foreach (var k in keyframes)
                                {
                                    if (k.value is Sprite s && !frames.Contains(s))
                                        frames.Add(s);
                                }
                            }
#endif
                            if (frames.Count > 0)
                            {
                                pigletWalkSprites = frames.ToArray();
                                break;
                            }
                        }
                    }
                    if (pigletWalkSprites != null && pigletWalkSprites.Length > 0) break;
                }
            }
        }
    }
}
