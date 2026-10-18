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

    [Header("Animation State Names (เมื่อสร้างคลิปท่าเสร็จ สามารถระบุชื่อ State ใน Animator ได้)")]
    [Tooltip("ชื่อ State ท่าวิ่งของหมาป่าใน Animator")]
    public string wolfRunState = "Run";
    [Tooltip("ชื่อ State ท่ายืน/เดินของแม่หมูใน Animator")]
    public string mamaWalkState = "Idle";
    [Tooltip("ชื่อ State ท่าเดินของลูกหมูใน Animator")]
    public string pigletWalkState = "Piglet_Idle";
    [Tooltip("ชื่อ State ท่ากระโดดดีใจของลูกหมูใน Animator")]
    public string pigletJumpState = "Piglet_Idle";

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

        AudioManager.Instance?.PlayStageIntroBGM();

        float duration = 5.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 16f)) * 0.08f;

            if (elapsed < 1.8f)
            {
                // เฟส 1: ลูกหมู 2 ตัวเดินกลับบ้านไปทางขวา (หันขวา)
                // หมาป่าวิ่งชาร์จเข้ามาข้างหลังจากฝั่งซ้าย (หันขวา)
                float t1 = elapsed / 1.8f;

                // ลูกหมู 1 (ตัวนำ): เดินกลับบ้านไปทางขวา (หันขวา)
                float pig1X = Mathf.Lerp(-1.0f, 4.0f, t1);
                SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob, 0f), faceLeft: false, isWolf: false);

                // ลูกหมู 2 (ตัวตามหลัง): เดินตามหลังมาทางขวา (หันขวา)
                float pig2X = Mathf.Lerp(-2.8f, 1.2f, t1);
                SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + walkBob, 0f), faceLeft: false, isWolf: false);

                // หมาป่า: วิ่งชาร์จพุ่งตามเข้ามาจากทางซ้าย (หันขวา)
                float wolfX = Mathf.Lerp(-8.5f, 0.8f, t1);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: false, isWolf: true);
            }
            else if (elapsed < 2.5f)
            {
                // เฟส 2: หมาป่าตามทันลูกหมูตัวที่ 2! ลูกหมูตัวที่ 1 วิ่งหนีเข้าบ้านทางขวาทัน
                float t2 = (elapsed - 1.8f) / 0.7f;

                // ลูกหมู 1: วิ่งหนีเข้าบ้านไปทางขวาสุดจอ (รอดปลอดภัย!)
                float pig1X = Mathf.Lerp(4.0f, 7.5f, t2);
                SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob * 1.5f, 0f), faceLeft: false, isWolf: false);
                if (pig1X >= 7.2f && cutscenePiglet1.gameObject.activeSelf)
                {
                    cutscenePiglet1.gameObject.SetActive(false); // วิ่งเข้าบ้านไปแล้ว
                }

                // หมาป่าจับลูกหมู 2 ได้ที่ x = 1.2f แล้วเตรียมสลับทิศหันกลับไปทางซ้าย (หันซ้าย)
                SetActor(cutsceneWolf, new Vector3(1.2f, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

                // ลูกหมู 2 ถูกจับตัวดิ้นอยู่ด้านหลังหมาป่า
                SetActor(cutscenePiglet2, new Vector3(1.65f, groundY + pigletYOffset + 0.22f, 0f), faceLeft: false, isWolf: false);
            }
            else
            {
                // เฟส 3: หมาป่าอุ้มลูกหมู 2 วิ่งหนีเตลิดไปทางซ้ายสุดจอ! และแม่หมูวิ่งตามออกมาไล่กวด
                float t3 = Mathf.Clamp01((elapsed - 2.5f) / 1.8f);

                // หมาป่า: วิ่งหนีไปทางซ้าย (หันซ้าย)
                float wolfX = Mathf.Lerp(1.2f, -9.5f, t3);
                SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

                // ลูกหมู 2: โดนอุ้มอยู่ด้านหลังหมาป่า (x = wolfX + 0.45f)
                float struggleBob = Mathf.Sin(elapsed * 24f) * 0.05f;
                SetActor(cutscenePiglet2, new Vector3(wolfX + 0.45f, groundY + pigletYOffset + 0.22f + struggleBob, 0f), faceLeft: false, isWolf: false);

                // แม่หมู: วิ่งไล่ตามออกมาจากบ้านทางขวาตอน 2.8 วินาที
                if (elapsed >= 2.8f)
                {
                    float tMama = Mathf.Clamp01((elapsed - 2.8f) / 1.8f);
                    float mamaX = Mathf.Lerp(7.0f, -6.5f, tMama);
                    SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);
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
    // 2. คัทซีนก่อนเริ่มด่าน 2 (Stage 2 Intro):
    //    - หมาป่าวิ่งหนีตาตั้งไปทางซ้าย (หันหน้าไปทางซ้ายตามทิศวิ่ง)
    //    - แม่หมูกับลูกหมู 2 ตัว วิ่งไล่กวดหมาป่าไปทางซ้ายแบบช้าๆ คอมเมดี้
    //    - ขบวนวิ่ง: หมาป่า -> ลูกหมู 1 -> ลูกหมู 2 -> แม่หมู
    // =========================================================================================
    private IEnumerator Stage2IntroRoutine(Action onComplete)
    {
        IsPlayingCutscene = true;
        SetGameplayPlayerVisible(false);

        EnsureCutsceneActors();
        cutsceneRoot.SetActive(true);

        if (cutsceneBasket != null) cutsceneBasket.gameObject.SetActive(false);

        AudioManager.Instance?.PlayStageIntroBGM();

        // ปรับความเร็วให้ช้าลงตามคำขอ (5.6 วินาที วิ่งไล่กันแบบช้าๆ ชิลๆ คอมเมดี้)
        float duration = 5.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // ก้าวช้าลงอย่างนุ่มนวล
            float walkBob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.08f;

            // 1. หมาป่า: วิ่งหนีตาตั้งไปทางซ้าย (หันหน้าไปทางซ้ายตามทิศวิ่ง)
            float wolfX = Mathf.Lerp(2.0f, -9.5f, Mathf.Clamp01(t * 1.08f));
            SetActor(cutsceneWolf, new Vector3(wolfX, groundY + wolfYOffset + walkBob, 0f), faceLeft: true, isWolf: true);

            // 2. ลูกหมู 1 (วิ่งนำหน้าสุด): วิ่งไล่กวดหมาป่าไปทางซ้าย (หันซ้าย)
            float pig1X = Mathf.Lerp(4.5f, -7.5f, Mathf.Clamp01(t * 1.08f));
            SetActor(cutscenePiglet1, new Vector3(pig1X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            // 3. ลูกหมู 2 (วิ่งอยู่หน้าแม่หมู): วิ่งเคียงข้างไปทางซ้าย (หันซ้าย)
            float pig2X = Mathf.Lerp(5.8f, -6.5f, Mathf.Clamp01(t * 1.08f));
            SetActor(cutscenePiglet2, new Vector3(pig2X, groundY + pigletYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

            // 4. แม่หมู: วิ่งตามหลังลูกๆ ไล่กวดหมาป่าไปทางซ้าย (หันซ้าย)
            float mamaX = Mathf.Lerp(7.2f, -5.5f, Mathf.Clamp01(t * 1.08f));
            SetActor(cutsceneMama, new Vector3(mamaX, groundY + mamaYOffset + walkBob, 0f), faceLeft: true, isWolf: false);

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
    //    - กระเช้าแม่หมูเลื่อนลงมาถึงพื้นล่าง (ช้าๆ นุ่มนวล)
    //    - แม่หมูเดินออกจากกระเช้าไปหาลูกหมู 2 ตัว
    //    - แม่หมูอยู่ดีใจกับลูกหมู 2 ตัวอย่างมีความสุข
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

        onComplete?.Invoke();
    }

    // =========================================================================================
    // สร้างตัวละครคัทซีนเฉพาะ (Dedicated Cutscene Actors)
    // =========================================================================================
    private void EnsureCutsceneActors()
    {
        if (cutsceneRoot != null) return;

        ExtractSampleVisuals();

        cutsceneRoot = new GameObject("[InSceneCutsceneActors]");
        cutsceneRoot.transform.position = Vector3.zero;
        cutsceneRoot.transform.rotation = Quaternion.identity;
        cutsceneRoot.transform.localScale = Vector3.one;

        cutsceneMama = CreateActor("Cutscene_Mama", cutsceneRoot.transform, sampleMamaSprite, 15);
        cutscenePiglet1 = CreateActor("Cutscene_Piglet1", cutsceneRoot.transform, samplePigletSprite, 14);
        cutscenePiglet2 = CreateActor("Cutscene_Piglet2", cutsceneRoot.transform, samplePigletSprite, 14);
        cutsceneWolf = CreateActor("Cutscene_Wolf", cutsceneRoot.transform, sampleWolfSprite, 16);
        cutsceneBasket = CreateActor("Cutscene_Basket", cutsceneRoot.transform, sampleBasketSprite, 13);

        cutsceneRoot.SetActive(false);
    }

    private SpriteRenderer CreateActor(string name, Transform parent, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(actorScale, actorScale, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        go.SetActive(false);
        return sr;
    }

    private void SetActor(SpriteRenderer actor, Vector3 worldPos, bool faceLeft, bool isWolf = false)
    {
        if (actor == null) return;
        if (!actor.gameObject.activeSelf) actor.gameObject.SetActive(true);

        actor.transform.position = worldPos;

        if (isWolf)
        {
            // สไปรต์หมาป่าเริ่มต้นหันขวา (ตาม Wolf.cs: วิ่งซ้าย flipX=true, วิ่งขวา flipX=false)
            actor.flipX = faceLeft;
        }
        else
        {
            // สไปรต์แม่หมูและลูกหมูเริ่มต้นหันซ้าย (วิ่งซ้าย flipX=false, วิ่งขวา flipX=true)
            actor.flipX = !faceLeft;
        }
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
